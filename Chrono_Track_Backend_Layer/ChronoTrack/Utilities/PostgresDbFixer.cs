using Microsoft.EntityFrameworkCore;
using ChronoTrack.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace ChronoTrack.Utilities
{
    public static class PostgresDbFixer
    {
        public static async Task FixPostgresDatabase(ChronoTrackDbContext dbContext, ILogger logger)
        {
            logger.LogInformation("Starting PostgreSQL database fixes for containerized environment");
            
            try
            {
                // Check for shadow properties and fix them
                await FixShadowProperties(dbContext, logger);
                
                // Fix default dates (-infinity values)
                await FixDefaultDates(dbContext, logger);
                
                // Ensure all required roles exist
                await DirectDatabaseFixer.FixRoles(dbContext, logger);
                
                // Ensure all required departments exist
                await DirectDatabaseFixer.FixDepartments(dbContext, logger);
                
                // Create a test user if needed
                await DirectDatabaseFixer.CreateTestUser(dbContext, logger);
                
                logger.LogInformation("PostgreSQL database fixes completed");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error fixing PostgreSQL database");
            }
        }
        
        private static async Task FixShadowProperties(ChronoTrackDbContext dbContext, ILogger logger)
        {
            try
            {
                logger.LogInformation("Checking for shadow property issues in Users table...");
                
                // Check if both DepartmentID and DepartmentId exist in the Users table
                var checkQuery = @"
                    SELECT 
                        COUNT(*) FILTER (WHERE column_name = 'DepartmentID') as dep_id_uppercase,
                        COUNT(*) FILTER (WHERE column_name = 'DepartmentId') as dep_id_mixedcase,
                        COUNT(*) FILTER (WHERE column_name = 'UserRoleID') as role_id_uppercase,
                        COUNT(*) FILTER (WHERE column_name = 'UserRoleId') as role_id_mixedcase
                    FROM information_schema.columns 
                    WHERE table_schema = 'app' 
                    AND table_name = 'Users'";
                
                using var cmd = dbContext.Database.GetDbConnection().CreateCommand();
                cmd.CommandText = checkQuery;
                
                if (dbContext.Database.GetDbConnection().State != System.Data.ConnectionState.Open)
                    await dbContext.Database.GetDbConnection().OpenAsync();
                
                var hasDuplicateColumns = false;
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        var depIdUppercase = reader.GetInt64(0) > 0;
                        var depIdMixedcase = reader.GetInt64(1) > 0;
                        var roleIdUppercase = reader.GetInt64(2) > 0;
                        var roleIdMixedcase = reader.GetInt64(3) > 0;
                        
                        logger.LogInformation($"Column check results: DepartmentID={depIdUppercase}, DepartmentId={depIdMixedcase}, UserRoleID={roleIdUppercase}, UserRoleId={roleIdMixedcase}");
                        
                        // If both versions exist, we need to fix it
                        hasDuplicateColumns = (depIdUppercase && depIdMixedcase) || (roleIdUppercase && roleIdMixedcase);
                    }
                }
                
                if (hasDuplicateColumns)
                {
                    logger.LogInformation("Found duplicate shadow columns in Users table, applying fix...");
                    
                    // Update all references to use the uppercase versions and then drop the mixedcase ones
                    await dbContext.Database.ExecuteSqlRawAsync(@"
                        -- First, make sure all data from shadow properties is copied to main properties
                        UPDATE app.""Users"" 
                        SET ""DepartmentID"" = ""DepartmentId"" 
                        WHERE ""DepartmentID"" IS NULL AND ""DepartmentId"" IS NOT NULL;
                        
                        UPDATE app.""Users"" 
                        SET ""UserRoleID"" = ""UserRoleId"" 
                        WHERE ""UserRoleID"" IS NULL AND ""UserRoleId"" IS NOT NULL;");
                    
                    // Check if we have foreign key constraints on the shadow properties
                    var constraintQuery = @"
                        SELECT constraint_name 
                        FROM information_schema.table_constraints 
                        WHERE constraint_type = 'FOREIGN KEY' 
                        AND table_schema = 'app' 
                        AND table_name = 'Users'";
                    
                    cmd.CommandText = constraintQuery;
                    var constraints = new List<string>();
                    
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            constraints.Add(reader.GetString(0));
                        }
                    }
                    
                    logger.LogInformation($"Found {constraints.Count} foreign key constraints to check");
                    
                    // Drop constraints that reference the shadow properties
                    foreach (var constraint in constraints)
                    {
                        // Check if this constraint refers to DepartmentId or UserRoleId
                        cmd.CommandText = $@"
                            SELECT column_name 
                            FROM information_schema.constraint_column_usage 
                            WHERE constraint_name = '{constraint}' 
                            AND table_schema = 'app' 
                            AND table_name = 'Users'";
                        
                        string? columnName = null;
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                columnName = reader.GetString(0);
                            }
                        }
                        
                        if (columnName == "DepartmentId" || columnName == "UserRoleId")
                        {
                            logger.LogInformation($"Dropping constraint {constraint} that references shadow property {columnName}");
                            await dbContext.Database.ExecuteSqlRawAsync($@"ALTER TABLE app.""Users"" DROP CONSTRAINT ""{constraint}""");
                        }
                    }
                    
                    // Drop shadow property columns if they exist
                    await dbContext.Database.ExecuteSqlRawAsync(@"
                        -- Drop indexes first
                        DROP INDEX IF EXISTS app.""IX_Users_DepartmentId"";
                        DROP INDEX IF EXISTS app.""IX_Users_UserRoleId"";
                        
                        -- Then drop the columns
                        ALTER TABLE app.""Users"" 
                        DROP COLUMN IF EXISTS ""DepartmentId"",
                        DROP COLUMN IF EXISTS ""UserRoleId"";");
                    
                    logger.LogInformation("Shadow properties fixed");
                }
                else
                {
                    logger.LogInformation("No shadow property issues detected");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error fixing shadow properties");
            }
        }
        
        private static async Task FixDefaultDates(ChronoTrackDbContext dbContext, ILogger logger)
        {
            try
            {
                logger.LogInformation("Checking for -infinity date values...");
                
                // Fix -infinity timestamps in all tables
                var tableNames = new[] { "Users", "UserRoles", "UserTypes", "Departments", "Attendances" };
                foreach (var tableName in tableNames)
                {
                    // Count rows with -infinity dates
                    var count = await dbContext.Database.ExecuteSqlRawAsync(
                        $"SELECT COUNT(*) FROM app.\"{tableName}\" WHERE \"Created\" = '-infinity'::timestamp OR \"LastModified\" = '-infinity'::timestamp");
                    
                    if (count > 0)
                    {
                        logger.LogInformation($"Fixing {count} rows with -infinity dates in {tableName}");
                        
                        // Update to current timestamp
                        var now = DateTime.UtcNow;
                        await dbContext.Database.ExecuteSqlRawAsync(
                            $"UPDATE app.\"{tableName}\" SET \"Created\" = {now}, \"LastModified\" = {now} WHERE \"Created\" = '-infinity'::timestamp OR \"LastModified\" = '-infinity'::timestamp");
                    }
                }
                
                logger.LogInformation("Date fixes completed");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error fixing default dates");
            }
        }

        public static async Task CreateMissingRolesViaService(IServiceProvider services, ILogger logger)
        {
            try
            {
                using var scope = services.CreateScope();
                var roleService = scope.ServiceProvider.GetRequiredService<ChronoTrack.Services.Interfaces.IUserRoleService>();
                
                // Check if the Software Developer role exists
                var roleExists = await CheckRoleExistsAsync(scope.ServiceProvider, 
                    Guid.Parse("11111111-1111-1111-2222-111111111111"));
                
                if (!roleExists)
                {
                    logger.LogInformation("Creating Software Developer role via service");
                    
                    var createRoleDto = new ChronoTrack.DTOs.CreateUserRoleDto("Software Developer");
                    
                    var result = await roleService.CreateUserRoleAsync(createRoleDto);
                    
                    if (result.Success)
                    {
                        logger.LogInformation("Successfully created Software Developer role with ID: {RoleId}", 
                            result.Data?.Id);
                    }
                    else
                    {
                        logger.LogWarning("Failed to create Software Developer role: {Message}", 
                            result.Message);
                    }
                }
                else
                {
                    logger.LogInformation("Software Developer role already exists");
                }
                
                // Add other roles if needed
                var rolesToAdd = new[]
                {
                    ("DevOps Engineer", "66666666-6666-6666-2222-666666666666"),
                    ("System Analyst", "88888888-8888-8888-2222-888888888888"),
                    ("QA Engineer", "55555555-5555-5555-2222-555555555555"),
                    ("UI/UX Designer", "22222222-2222-2222-2222-222222222222"),
                    ("Project Manager", "33333333-3333-3333-2222-333333333333"),
                    ("Product Manager", "77777777-7777-7777-2222-777777777777"),
                    ("Business Analyst", "44444444-4444-4444-2222-444444444444"),
                    ("HR Specialist", "99999999-9999-9999-2222-999999999999"),
                    ("Marketing Specialist", "aaaaaaaa-aaaa-aaaa-2222-aaaaaaaaaaaa"),
                    ("Sales Representative", "bbbbbbbb-bbbb-bbbb-2222-bbbbbbbbbbbb")
                };
                
                foreach (var (name, idString) in rolesToAdd)
                {
                    var id = Guid.Parse(idString);
                    var exists = await CheckRoleExistsAsync(scope.ServiceProvider, id);
                    
                    if (!exists)
                    {
                        logger.LogInformation("Creating {RoleName} role via service", name);
                        
                        var createRoleDto = new ChronoTrack.DTOs.CreateUserRoleDto(name);
                        
                        var result = await roleService.CreateUserRoleAsync(createRoleDto);
                        
                        if (result.Success)
                        {
                            logger.LogInformation("Successfully created {RoleName} role", name);
                        }
                        else
                        {
                            logger.LogWarning("Failed to create {RoleName} role: {Message}", 
                                name, result.Message);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error creating roles via service");
            }
        }

        private static async Task<bool> CheckRoleExistsAsync(IServiceProvider services, Guid roleId)
        {
            try
            {
                var dbContext = services.GetRequiredService<ChronoTrack.Persistence.ChronoTrackDbContext>();
                return await dbContext.UserRoles.AnyAsync(r => r.Id == roleId);
            }
            catch
            {
                // If there's an error checking, assume it doesn't exist
                return false;
            }
        }
    }
} 