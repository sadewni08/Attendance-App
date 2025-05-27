using ChronoTrack.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChronoTrack.Persistence.Configuration
{
    public class DepartmentConfiguration : IEntityTypeConfiguration<Department>
    {
        public void Configure(EntityTypeBuilder<Department> builder)
        {
            builder.ToTable("Departments");

            builder.HasIndex(d => d.DepartmentName)
                .IsUnique();

            builder.Property(d => d.DepartmentName)
                .HasMaxLength(100)
                .IsRequired();

            // Seed initial data
            builder.HasData(
                new Department { Id = Guid.Parse("3d490a70-94ce-4d15-9494-5248280c2ce3"), DepartmentName = "IT" },
                new Department { Id = Guid.Parse("7c9e6679-7425-40de-944b-e07fc1f90ae7"), DepartmentName = "HR" },
                new Department { Id = Guid.Parse("98a52f9d-16be-4a4f-a6c9-6e9df8e1e6eb"), DepartmentName = "Marketing" },
                new Department { Id = Guid.Parse("f8c3de3d-1fea-4d7c-a8b0-29f63c4c3454"), DepartmentName = "Business" },
                new Department { Id = Guid.Parse("6a534922-c788-4386-a38c-aabc856bdca7"), DepartmentName = "Design" },
                new Department { Id = Guid.Parse("f4ed6c3a-c6d3-47b9-b7e5-a67893a8b3a2"), DepartmentName = "Sales" },
                new Department { Id = Guid.Parse("74b2c633-f052-4e50-b00c-9a4f6a2599d6"), DepartmentName = "Management" }
            );
        }
    }
}
