using ChronoTrack.Models;
using ChronoTrack.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ChronoTrack.Utilities
{
    public static class MissingRoleFixer
    {
        public static async Task FixMissingRoles(ChronoTrackDbContext dbContext, ILogger logger)
        {
            // Check if the Software Developer role exists
            var softwareDevRole = await dbContext.UserRoles
                .Where(r => r.Id == Guid.Parse("11111111-1111-1111-2222-111111111111"))
                .FirstOrDefaultAsync();

            if (softwareDevRole == null)
            {
                logger.LogInformation("Adding missing Software Developer role with ID 11111111-1111-1111-2222-111111111111");
                
                // Create the role with the exact ID
                var newRole = new UserRole
                {
                    Id = Guid.Parse("11111111-1111-1111-2222-111111111111"),
                    UserRoleName = "Software Developer"
                };

                try
                {
                    // Add and save
                    dbContext.UserRoles.Add(newRole);
                    await dbContext.SaveChangesAsync();
                    logger.LogInformation("Successfully added Software Developer role");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to add Software Developer role");
                }
            }
            else
            {
                logger.LogInformation("Software Developer role already exists");
            }

            // Check for other potentially missing roles from RoleConfiguration
            var rolesToCheck = new List<(Guid Id, string Name)>
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

            foreach (var role in rolesToCheck)
            {
                var existingRole = await dbContext.UserRoles
                    .Where(r => r.Id == role.Id)
                    .FirstOrDefaultAsync();

                if (existingRole == null)
                {
                    logger.LogInformation($"Adding missing role {role.Name} with ID {role.Id}");
                    
                    var newRole = new UserRole
                    {
                        Id = role.Id,
                        UserRoleName = role.Name
                    };

                    try
                    {
                        dbContext.UserRoles.Add(newRole);
                        await dbContext.SaveChangesAsync();
                        logger.LogInformation($"Successfully added {role.Name} role");
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, $"Failed to add {role.Name} role");
                    }
                }
            }
        }
    }
} 