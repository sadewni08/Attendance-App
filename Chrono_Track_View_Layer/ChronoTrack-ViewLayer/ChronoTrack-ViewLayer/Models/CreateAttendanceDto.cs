using System.Text.Json.Serialization;

namespace ChronoTrack_ViewLayer.Models
{
    public class CreateAttendanceDto
    {
        [JsonConstructor]
        public CreateAttendanceDto(DateTime attendanceDate, TimeSpan checkInTime, TimeSpan? checkOutTime = null)
        {
            // Ensure the date is stored as UTC and has correct format
            AttendanceDate = DateTime.SpecifyKind(attendanceDate.Date, DateTimeKind.Utc);
            CheckInTime = checkInTime;
            CheckOutTime = checkOutTime;
            
            // Log for debugging
            Console.WriteLine($"CreateAttendanceDto: Date={AttendanceDate:yyyy-MM-dd}, Time={CheckInTime}");
        }

        // Default constructor for deserialization
        public CreateAttendanceDto() 
        {
            // Set default values to ensure valid data
            AttendanceDate = DateTime.UtcNow.Date;
            CheckInTime = DateTime.UtcNow.TimeOfDay;
        }

        // Static method to create DTO with current Sri Lanka time
        public static CreateAttendanceDto CreateWithLocalTime()
        {
            // Get current time in UTC
            var utcNow = DateTime.UtcNow;
            
            // Convert to Sri Lanka time for logging/debugging
            var sriLankaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Colombo");
            var sriLankaDateTime = TimeZoneInfo.ConvertTimeFromUtc(utcNow, sriLankaTimeZone);
            
            Console.WriteLine($"Creating attendance with: UTC={utcNow:yyyy-MM-dd HH:mm:ss}, Sri Lanka={sriLankaDateTime:yyyy-MM-dd HH:mm:ss}");
            
            // However, we still store in UTC for consistent database storage
            return new CreateAttendanceDto(
                utcNow.Date,
                utcNow.TimeOfDay,
                null
            );
        }

        [JsonPropertyName("attendanceDate")]
        public DateTime AttendanceDate { get; set; }

        [JsonPropertyName("checkInTime")]
        public TimeSpan CheckInTime { get; set; }

        [JsonPropertyName("checkOutTime")]
        public TimeSpan? CheckOutTime { get; set; }
    }
} 