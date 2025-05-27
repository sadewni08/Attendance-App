using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ChronoTrack.Models;

namespace ChronoTrack.Persistence.Configuration
{
    public class AttendanceConfiguration : IEntityTypeConfiguration<Attendance>
    {
        public void Configure(EntityTypeBuilder<Attendance> builder)
        {
            builder.ToTable("Attendances");

            builder.HasIndex(a => new { a.UserID, a.AttendanceDate })
                .IsUnique();

            builder.Property(a => a.AttendanceDate)
                .IsRequired();

            builder.Property(a => a.CheckInTime)
                .IsRequired();

            builder.Property(a => a.CheckOutTime)
                .IsRequired();
        }
    }
}
