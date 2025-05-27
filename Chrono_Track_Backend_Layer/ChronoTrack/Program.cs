using ChronoTrack.Endpoints;
using ChronoTrack.Services;
using ChronoTrack.Services.Interfaces;
using Scalar.AspNetCore;
using System.Text.Json.Serialization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ChronoTrack.Persistence;
using Npgsql.EntityFrameworkCore.PostgreSQL;
using ChronoTrack.Utilities;
using Microsoft.AspNetCore.Cors.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Configure DateTime handling
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.AllowTrailingCommas = true;
        options.JsonSerializerOptions.ReadCommentHandling = JsonCommentHandling.Skip;
    });

// Configure JSON serialization for minimal APIs
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
    options.SerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    options.SerializerOptions.PropertyNameCaseInsensitive = true;
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.AllowTrailingCommas = true;
    options.SerializerOptions.ReadCommentHandling = JsonCommentHandling.Skip;
});

// Add problem details
builder.Services.AddProblemDetails();

// Add OpenAPI support
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();

// Register DbContext
builder.Services.AddDbContext<ChronoTrackDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    object value = options.UseNpgsql(connectionString, opt =>
    {
        opt.EnableRetryOnFailure(3);
        opt.CommandTimeout(30);
    });
});

// Configure CORS policy for MSAL
builder.Services.AddCors(options =>
{
    options.AddPolicy("MsalPolicy", policy =>
    {
        policy.WithOrigins(
                "https://login.microsoftonline.com", 
                "https://app.chronotrack.com", // Replace with your app's domain
                "https://localhost",
                "http://localhost",
                "http://localhost:5000",
                "https://localhost:7094",
                "http://10.0.2.2", // For Android emulator
                "http://localhost:5173", // For typical Vite/Vue development
                "http://localhost:8080", // For Vue development
                "capacitor://localhost", // For Capacitor
                "ionic://localhost" // For Ionic
            )
            .SetIsOriginAllowedToAllowWildcardSubdomains()
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

// Register Services
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IDepartmentService, DepartmentService>();
builder.Services.AddScoped<IUserRoleService, UserRoleService>();
builder.Services.AddScoped<IAttendanceService, AttendanceService>();

var app = builder.Build();

// Create a scope to work with DbContext
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var dbContext = services.GetRequiredService<ChronoTrackDbContext>();
    var logger = services.GetRequiredService<ILogger<Program>>();
    
    try
    {
        // Ensure migration history exists
        await dbContext.EnsureMigrationHistoryExistsAsync();
        
        // Use the PostgreSQL fixer for containerized environment
        await PostgresDbFixer.FixPostgresDatabase(dbContext, logger);
        
        // Also try the service approach as a fallback for creating roles
        await PostgresDbFixer.CreateMissingRolesViaService(app.Services, logger);
        
        // Legacy methods kept for backward compatibility
        await EnsureDepartmentsExistAsync(dbContext);
        await EnsureUserRolesExistAsync(dbContext);
        
        logger.LogInformation("Database initialization completed successfully");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while initializing the database");
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // Enable OpenAPI documentation
    app.MapOpenApi();
    app.MapScalarApiReference();
    
    // Use development CORS policy
    app.UseCors("MsalPolicy");
}
else
{
    // Use the CORS policy in production too
    app.UseCors("MsalPolicy");
}

// Add middleware to handle DateTime conversion
app.Use(async (context, next) =>
{
    try
    {
        // Convert query string DateTime parameters to UTC
        if (context.Request.Query != null && context.Request.Query.Count > 0)
        {
            var queryDict = new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>();

            foreach (var item in context.Request.Query)
            {
                var key = item.Key;
                var value = item.Value;

                if (key != null && value.Count > 0 && !string.IsNullOrEmpty(value[0]))
                {
                    if (DateTime.TryParse(value[0], out DateTime dateTime) && dateTime.Kind != DateTimeKind.Utc)
                    {
                        queryDict[key] = new Microsoft.Extensions.Primitives.StringValues(dateTime.ToUniversalTime().ToString("o"));
                    }
                    else
                    {
                        queryDict[key] = value;
                    }
                }
                else
                {
                    queryDict[key] = value;
                }
            }

            context.Request.Query = new Microsoft.AspNetCore.Http.QueryCollection(queryDict);
        }
    }
    catch (Exception ex)
    {
        // Log the exception but don't stop the request
        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Error in DateTime conversion middleware");
    }

    await next();
});

app.UseHttpsRedirection();
app.UseAuthorization();

// Map endpoints
app.MapControllers();
app.MapUserEndpoints();
app.MapDepartmentEndpoints();
app.MapUserRoleEndpoints();
app.MapAttendanceEndpoints();
app.MapAuthEndpoints();

// Add a health check endpoint for Microsoft auth validation
app.MapGet("/api/auth/microsoft/config", () => 
{
    return Results.Ok(new 
    {
        Status = "OK",
        Message = "Microsoft authentication is properly configured",
        Timestamp = DateTime.UtcNow
    });
})
.WithTags("Authentication")
.WithName("MicrosoftAuthConfig")
.WithDescription("Verifies that Microsoft authentication is properly configured")
.WithOpenApi();

app.MapGet("/", () => "Hello World!")
   .Produces(200, typeof(string));

app.Run();

// Helper method to ensure all departments exist in database
async Task EnsureDepartmentsExistAsync(ChronoTrackDbContext context)
{
    try
    {
        // Define all required departments with IDs matching frontend
        var requiredDepartments = new List<(Guid Id, string Name)>
        {
            (Guid.Parse("3d490a70-94ce-4d15-9494-5248280c2ce3"), "IT"),
            (Guid.Parse("7c9e6679-7425-40de-944b-e07fc1f90ae7"), "HR"),
            (Guid.Parse("98a52f9d-16be-4a4f-a6c9-6e9df8e1e6eb"), "Marketing"),
            (Guid.Parse("f8c3de3d-1fea-4d7c-a8b0-29f63c4c3454"), "Business"),
            (Guid.Parse("6a534922-c788-4386-a38c-aabc856bdca7"), "Design"),
            (Guid.Parse("f4ed6c3a-c6d3-47b9-b7e5-a67893a8b3a2"), "Sales"),
            (Guid.Parse("74b2c633-f052-4e50-b00c-9a4f6a2599d6"), "Management")
        };
        
        // Check for each department and add if missing
        foreach (var dept in requiredDepartments)
        {
            var exists = await context.Departments.AnyAsync(d => d.Id == dept.Id);
            if (!exists)
            {
                context.Departments.Add(new ChronoTrack.Models.Department
                {
                    Id = dept.Id,
                    DepartmentName = dept.Name,
                    Description = $"{dept.Name} Department"
                });
                Console.WriteLine($"Added missing department: {dept.Name}");
            }
        }
        
        await context.SaveChangesAsync();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error ensuring departments: {ex.Message}");
    }
}

// Helper method to ensure all user roles exist in database
async Task EnsureUserRolesExistAsync(ChronoTrackDbContext context)
{
    try
    {
        // Define all required roles with IDs matching frontend
        var requiredRoles = new List<(Guid Id, string Name)>
        {
            // Administrative roles
            (Guid.Parse("2c5e174e-3b0e-446f-86af-483d56fd7210"), "Administrator"),
            (Guid.Parse("e34e5f87-e2c9-4a3a-b234-77d5d7066987"), "Manager"),
            (Guid.Parse("a9f1d24f-5b5a-4a87-a1ed-e6541b4a6fec"), "Employee"),
            
            // IT Department roles
            (Guid.Parse("11111111-1111-1111-2222-111111111111"), "Software Developer"),
            (Guid.Parse("66666666-6666-6666-2222-666666666666"), "DevOps Engineer"),
            (Guid.Parse("88888888-8888-8888-2222-888888888888"), "System Analyst"),
            (Guid.Parse("55555555-5555-5555-2222-555555555555"), "QA Engineer"),
            
            // Design Department roles
            (Guid.Parse("22222222-2222-2222-2222-222222222222"), "UI/UX Designer"),
            
            // Management Department roles
            (Guid.Parse("33333333-3333-3333-2222-333333333333"), "Project Manager"),
            (Guid.Parse("77777777-7777-7777-2222-777777777777"), "Product Manager"),
            
            // Business Department roles
            (Guid.Parse("44444444-4444-4444-2222-444444444444"), "Business Analyst"),
            
            // HR Department roles
            (Guid.Parse("99999999-9999-9999-2222-999999999999"), "HR Specialist"),
            
            // Marketing Department roles
            (Guid.Parse("aaaaaaaa-aaaa-aaaa-2222-aaaaaaaaaaaa"), "Marketing Specialist"),
            
            // Sales Department roles
            (Guid.Parse("bbbbbbbb-bbbb-bbbb-2222-bbbbbbbbbbbb"), "Sales Representative")
        };
        
        // Check for each role and add if missing
        foreach (var role in requiredRoles)
        {
            var exists = await context.UserRoles.AnyAsync(r => r.Id == role.Id);
            if (!exists)
            {
                context.UserRoles.Add(new ChronoTrack.Models.UserRole
                {
                    Id = role.Id,
                    UserRoleName = role.Name
                });
                Console.WriteLine($"Added missing role: {role.Name}");
            }
        }
        
        await context.SaveChangesAsync();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error ensuring user roles: {ex.Message}");
    }
}
