using ChronoTrack.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ChronoTrack.Utilities
{
    public static class DirectDatabaseFixer
    {
        public static async Task FixRoles(ChronoTrackDbContext dbContext, ILogger logger)
        {
            try
            {
                // Check if the Software Developer role exists using direct SQL
                var roleExists = await dbContext.Database.ExecuteSqlRawAsync(
                    "SELECT COUNT(*) FROM app.\"UserRoles\" WHERE \"Id\" = '11111111-1111-1111-2222-111111111111'");
                
                if (roleExists == 0)
                {
                    // Add the Software Developer role directly using SQL
                    var now = DateTime.UtcNow;
                    var result = await dbContext.Database.ExecuteSqlRawAsync(
                        "INSERT INTO app.\"UserRoles\" (\"Id\", \"UserRoleName\", \"Created\", \"LastModified\") " +
                        "VALUES ('11111111-1111-1111-2222-111111111111', 'Software Developer', {0}, {1})",
                        now, now);
                    
                    if (result > 0)
                    {
                        logger.LogInformation("Successfully added Software Developer role using direct SQL");
                    }
                    else
                    {
                        logger.LogWarning("Failed to add Software Developer role using direct SQL");
                    }
                }
                else
                {
                    logger.LogInformation("Software Developer role already exists in the database");
                }
                
                // Add more roles if needed
                var rolesToAdd = new[]
                {
                    (Guid.Parse("66666666-6666-6666-2222-666666666666"), "DevOps Engineer"),
                    (Guid.Parse("88888888-8888-8888-2222-888888888888"), "System Analyst"),
                    (Guid.Parse("55555555-5555-5555-2222-555555555555"), "QA Engineer"),
                    (Guid.Parse("22222222-2222-2222-2222-222222222222"), "UI/UX Designer"),
                    (Guid.Parse("33333333-3333-3333-2222-333333333333"), "Project Manager"),
                    (Guid.Parse("77777777-7777-7777-2222-777777777777"), "Product Manager"),
                    (Guid.Parse("44444444-4444-4444-2222-444444444444"), "Business Analyst"),
                    (Guid.Parse("99999999-9999-9999-2222-999999999999"), "HR Specialist"),
                    (Guid.Parse("aaaaaaaa-aaaa-aaaa-2222-aaaaaaaaaaaa"), "Marketing Specialist"),
                    (Guid.Parse("bbbbbbbb-bbbb-bbbb-2222-bbbbbbbbbbbb"), "Sales Representative")
                };
                
                foreach (var (id, name) in rolesToAdd)
                {
                    var exists = await dbContext.Database.ExecuteSqlRawAsync(
                        $"SELECT COUNT(*) FROM app.\"UserRoles\" WHERE \"Id\" = '{id}'");
                    
                    if (exists == 0)
                    {
                        var now = DateTime.UtcNow;
                        var result = await dbContext.Database.ExecuteSqlRawAsync(
                            "INSERT INTO app.\"UserRoles\" (\"Id\", \"UserRoleName\", \"Created\", \"LastModified\") " +
                            "VALUES ({0}, {1}, {2}, {3})",
                            id, name, now, now);
                        
                        if (result > 0)
                        {
                            logger.LogInformation($"Added missing role: {name}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error fixing roles with direct SQL");
            }
        }

        public static async Task FixDepartments(ChronoTrackDbContext dbContext, ILogger logger)
        {
            try
            {
                // Check which departments are missing and add them
                var departmentsToAdd = new[]
                {
                    (Guid.Parse("3d490a70-94ce-4d15-9494-5248280c2ce3"), "IT"),
                    (Guid.Parse("7c9e6679-7425-40de-944b-e07fc1f90ae7"), "HR"),
                    (Guid.Parse("98a52f9d-16be-4a4f-a6c9-6e9df8e1e6eb"), "Marketing"),
                    (Guid.Parse("f8c3de3d-1fea-4d7c-a8b0-29f63c4c3454"), "Business"),
                    (Guid.Parse("6a534922-c788-4386-a38c-aabc856bdca7"), "Design"),
                    (Guid.Parse("f4ed6c3a-c6d3-47b9-b7e5-a67893a8b3a2"), "Sales"),
                    (Guid.Parse("74b2c633-f052-4e50-b00c-9a4f6a2599d6"), "Management")
                };
                
                // Check table schema to see if Description column exists
                var columnsQuery = await dbContext.Database.ExecuteSqlRawAsync(
                    "SELECT COUNT(*) FROM information_schema.columns " +
                    "WHERE table_schema = 'app' AND table_name = 'Departments' AND column_name = 'Description'");
                
                bool hasDescriptionColumn = columnsQuery > 0;
                logger.LogInformation($"Department table has Description column: {hasDescriptionColumn}");
                
                foreach (var (id, name) in departmentsToAdd)
                {
                    var exists = await dbContext.Database.ExecuteSqlRawAsync(
                        $"SELECT COUNT(*) FROM app.\"Departments\" WHERE \"Id\" = '{id}'");
                    
                    if (exists == 0)
                    {
                        var now = DateTime.UtcNow;
                        string sql;
                        
                        if (hasDescriptionColumn)
                        {
                            // Insert with Description column
                            sql = "INSERT INTO app.\"Departments\" (\"Id\", \"DepartmentName\", \"Description\", \"Created\", \"LastModified\") " +
                                  "VALUES ({0}, {1}, {2}, {3}, {4})";
                            await dbContext.Database.ExecuteSqlRawAsync(sql, id, name, "", now, now);
                        }
                        else
                        {
                            // Insert without Description column
                            sql = "INSERT INTO app.\"Departments\" (\"Id\", \"DepartmentName\", \"Created\", \"LastModified\") " +
                                  "VALUES ({0}, {1}, {2}, {3})";
                            await dbContext.Database.ExecuteSqlRawAsync(sql, id, name, now, now);
                        }
                        
                        logger.LogInformation($"Added missing department: {name}");
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error fixing departments with direct SQL");
            }
        }

        public static async Task CreateTestUser(ChronoTrackDbContext dbContext, ILogger logger)
        {
            try
            {
                var userId = "000001";
                var firstName = "Test";
                var lastName = "User";
                var emailAddress = "test@example.com";
                var password = "Pass@123";
                var address = "123 Test St";
                var phoneNumber = "+1234567890";
                var gender = 0; // Male
                
                // IT Department
                var departmentId = "3d490a70-94ce-4d15-9494-5248280c2ce3";
                
                // Software Developer role
                var roleId = "11111111-1111-1111-2222-111111111111";
                
                // Regular User type
                var userTypeId = "c7b013f0-5201-4317-abd8-c211f91b7330";
                
                // First check if user schema exists
                try
                {
                    // Check if user with this email already exists
                    var userExists = await dbContext.Database.ExecuteSqlRawAsync(
                        "SELECT COUNT(*) FROM app.\"Users\" WHERE \"EmailAddress\" = {0}", emailAddress);
                    
                    if (userExists == 0)
                    {
                        logger.LogInformation("Creating test user directly via SQL");
                        
                        var id = Guid.NewGuid();
                        var now = DateTime.UtcNow;
                        
                        // Get actual column names from the database to ensure we match the exact schema
                        var columnQuery = @"
                            SELECT column_name 
                            FROM information_schema.columns 
                            WHERE table_schema = 'app' AND table_name = 'Users'
                            ORDER BY ordinal_position";
                        
                        using var command = dbContext.Database.GetDbConnection().CreateCommand();
                        command.CommandText = columnQuery;
                        
                        if (dbContext.Database.GetDbConnection().State != System.Data.ConnectionState.Open)
                            await dbContext.Database.GetDbConnection().OpenAsync();
                        
                        List<string> columns = new List<string>();
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                columns.Add(reader.GetString(0));
                            }
                        }
                        
                        logger.LogInformation($"Found {columns.Count} columns in Users table: {string.Join(", ", columns)}");
                        
                        // Build the SQL statement using the exact column names from the database
                        var columnNames = string.Join(", ", columns.Select(c => $"\"{c}\""));
                        var paramPlaceholders = string.Join(", ", Enumerable.Range(0, columns.Count).Select(i => $"{{{i}}}"));
                        
                        var sql = $@"
                            INSERT INTO app.""Users"" ({columnNames})
                            VALUES ({paramPlaceholders})";
                        
                        // Prepare parameter values in the same order as columns
                        var parameters = new object[columns.Count];
                        var columnMap = new Dictionary<string, object>
                        {
                            { "Id", id },
                            { "UserId", userId },
                            { "FirstName", firstName },
                            { "LastName", lastName },
                            { "EmailAddress", emailAddress },
                            { "Password", password },
                            { "Address", address },
                            { "PhoneNumber", phoneNumber },
                            { "Gender", gender },
                            { "Created", now },
                            { "LastModified", now },
                            { "DepartmentID", Guid.Parse(departmentId) },
                            { "UserRoleID", Guid.Parse(roleId) },
                            { "UserTypeID", Guid.Parse(userTypeId) }
                        };
                        
                        // Handle potential shadow property issues
                        if (columns.Contains("DepartmentId") && !columns.Contains("DepartmentID"))
                        {
                            // The case in DB is different than our model
                            columnMap["DepartmentId"] = Guid.Parse(departmentId);
                        }
                        
                        if (columns.Contains("UserRoleId") && !columns.Contains("UserRoleID"))
                        {
                            columnMap["UserRoleId"] = Guid.Parse(roleId);
                        }
                        
                        if (columns.Contains("UserTypeId") && !columns.Contains("UserTypeID"))
                        {
                            columnMap["UserTypeId"] = Guid.Parse(userTypeId);
                        }
                        
                        // Set parameter values in the correct order
                        for (int i = 0; i < columns.Count; i++)
                        {
                            if (columnMap.TryGetValue(columns[i], out var value))
                            {
                                parameters[i] = value;
                            }
                            else
                            {
                                // Default null for columns we don't have a value for
                                parameters[i] = DBNull.Value;
                            }
                        }
                        
                        // Execute the SQL with parameters
                        var result = await dbContext.Database.ExecuteSqlRawAsync(sql, parameters);
                        
                        if (result > 0)
                        {
                            logger.LogInformation("Successfully created test user using direct SQL");
                        }
                        else
                        {
                            logger.LogWarning("Failed to create test user using direct SQL");
                        }
                    }
                    else
                    {
                        logger.LogInformation("Test user already exists in the database");
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error querying database or creating test user");
                    
                    // Alternative: Try to create the user via EF Core directly
                    logger.LogInformation("Attempting to create user via EF Core");
                    
                    var user = new ChronoTrack.Models.User
                    {
                        Id = Guid.NewGuid(),
                        UserId = userId,
                        FirstName = firstName,
                        LastName = lastName,
                        EmailAddress = emailAddress,
                        Password = password,
                        Address = address,
                        PhoneNumber = phoneNumber,
                        Gender = (ChronoTrack.Models.GenderEnum)gender,
                        DepartmentID = Guid.Parse(departmentId),
                        UserRoleID = Guid.Parse(roleId),
                        UserTypeID = Guid.Parse(userTypeId)
                    };

                    try
                    {
                        dbContext.Users.Add(user);
                        await dbContext.SaveChangesAsync();
                        logger.LogInformation("Successfully created test user via EF Core");
                    }
                    catch (Exception efEx)
                    {
                        logger.LogError(efEx, "Failed to create test user via EF Core");
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in CreateTestUser method");
            }
        }
    }
} 