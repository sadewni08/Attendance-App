using ChronoTrack.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChronoTrack.Persistence.Configuration
{
    public class UserTypeConfiguration : IEntityTypeConfiguration<UserType>
    {
        public void Configure(EntityTypeBuilder<UserType> builder)
        {
            builder.ToTable("UserTypes");

            builder.HasIndex(ut => ut.TypeName)
                .IsUnique();

            builder.Property(ut => ut.TypeName)
                .IsRequired();

            // Seed initial data
            builder.HasData(
                new UserType { Id = Guid.Parse("8d04dce2-969a-435d-bba4-df3f325983dc"), TypeName = UserTypeEnum.Admin },
                new UserType { Id = Guid.Parse("c7b013f0-5201-4317-abd8-c211f91b7330"), TypeName = UserTypeEnum.User }
            );
        }
    }
}
