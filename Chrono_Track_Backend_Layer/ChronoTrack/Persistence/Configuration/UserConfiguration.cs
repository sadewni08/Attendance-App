using ChronoTrack.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChronoTrack.Persistence.Configuration
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.ToTable("Users");

            // Unique constraints
            builder.HasIndex(u => u.UserId)
                .IsUnique();

            builder.HasIndex(u => u.EmailAddress)
                .IsUnique();

            // Property configurations
            builder.Property(u => u.UserId)
                .HasMaxLength(20)
                .IsRequired();

            builder.Property(u => u.FirstName)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(u => u.LastName)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(u => u.EmailAddress)
                .HasMaxLength(255)
                .IsRequired();

            builder.Property(u => u.PhoneNumber)
                .HasMaxLength(20)
                .IsRequired();

            builder.Property(u => u.Password)
                .HasMaxLength(255)
                .IsRequired();

            builder.Property(u => u.Address)
                .HasMaxLength(500)
                .IsRequired();

            // Relationships
            builder.HasOne(u => u.UserType)
                .WithMany()
                .HasForeignKey(u => u.UserTypeID)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(u => u.UserRole)
                .WithMany()
                .HasForeignKey(u => u.UserRoleID)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(u => u.Department)
                .WithMany()
                .HasForeignKey(u => u.DepartmentID)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}