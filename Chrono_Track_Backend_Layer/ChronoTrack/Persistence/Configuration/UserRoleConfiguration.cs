using ChronoTrack.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChronoTrack.Persistence.Configuration
{
    public class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
    {
        public void Configure(EntityTypeBuilder<UserRole> builder)
        {
            builder.ToTable("UserRoles");
            builder.HasIndex(ur => ur.UserRoleName)
                .IsUnique();
            builder.Property(ur => ur.UserRoleName)
                .HasMaxLength(50)
                .IsRequired();
            // Seed initial data
            builder.HasData(
                // Administrative roles
                new UserRole { Id = Guid.Parse("2c5e174e-3b0e-446f-86af-483d56fd7210"), UserRoleName = "Administrator" },
                new UserRole { Id = Guid.Parse("e34e5f87-e2c9-4a3a-b234-77d5d7066987"), UserRoleName = "Manager" },
                new UserRole { Id = Guid.Parse("a9f1d24f-5b5a-4a87-a1ed-e6541b4a6fec"), UserRoleName = "Employee" },
                
                // IT Department roles
                new UserRole { Id = Guid.Parse("11111111-1111-1111-2222-111111111111"), UserRoleName = "Software Developer" },
                new UserRole { Id = Guid.Parse("66666666-6666-6666-2222-666666666666"), UserRoleName = "DevOps Engineer" },
                new UserRole { Id = Guid.Parse("88888888-8888-8888-2222-888888888888"), UserRoleName = "System Analyst" },
                new UserRole { Id = Guid.Parse("55555555-5555-5555-2222-555555555555"), UserRoleName = "QA Engineer" },
                
                // Design Department roles
                new UserRole { Id = Guid.Parse("22222222-2222-2222-2222-222222222222"), UserRoleName = "UI/UX Designer" },
                
                // Management Department roles
                new UserRole { Id = Guid.Parse("33333333-3333-3333-2222-333333333333"), UserRoleName = "Project Manager" },
                new UserRole { Id = Guid.Parse("77777777-7777-7777-2222-777777777777"), UserRoleName = "Product Manager" },
                
                // Business Department roles
                new UserRole { Id = Guid.Parse("44444444-4444-4444-2222-444444444444"), UserRoleName = "Business Analyst" },
                
                // HR Department roles
                new UserRole { Id = Guid.Parse("99999999-9999-9999-2222-999999999999"), UserRoleName = "HR Specialist" },
                
                // Marketing Department roles
                new UserRole { Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-2222-aaaaaaaaaaaa"), UserRoleName = "Marketing Specialist" },
                
                // Sales Department roles
                new UserRole { Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-2222-bbbbbbbbbbbb"), UserRoleName = "Sales Representative" }
            );
        }
    }
}



