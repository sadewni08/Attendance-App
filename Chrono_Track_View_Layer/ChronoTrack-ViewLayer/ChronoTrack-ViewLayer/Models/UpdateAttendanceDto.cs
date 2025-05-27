using System.Text.Json.Serialization;

namespace ChronoTrack_ViewLayer.Models
{
    public class UpdateAttendanceDto
    {
        [JsonConstructor]
        public UpdateAttendanceDto(DateTime attendanceDate, TimeSpan checkOutTime)
        {
            // Ensure the date is stored as UTC
            AttendanceDate = DateTime.SpecifyKind(attendanceDate.Date, DateTimeKind.Utc);
            CheckOutTime = checkOutTime;
            
            // Log for debugging
            Console.WriteLine($"Created UpdateAttendanceDto: Date={AttendanceDate:yyyy-MM-dd} ({AttendanceDate.Kind}), CheckOutTime={CheckOutTime}");
        }
        
        // Constructor with only checkout time for backward compatibility
        public UpdateAttendanceDto(TimeSpan checkOutTime)
        {
            AttendanceDate = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc);
            CheckOutTime = checkOutTime;
            
            Console.WriteLine($"Created UpdateAttendanceDto with default date: Date={AttendanceDate:yyyy-MM-dd}, CheckOutTime={CheckOutTime}");
        }

        // Default constructor for deserialization
        public UpdateAttendanceDto() 
        {
            AttendanceDate = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc);
            CheckOutTime = DateTime.UtcNow.TimeOfDay;
        }
        
        // Static method to create DTO with current Sri Lanka time
        public static UpdateAttendanceDto CreateWithLocalTime(DateTime attendanceDate)
        {
            // Get current time in UTC for check-out time
            var utcNow = DateTime.UtcNow;
            
            // Convert to Sri Lanka time for logging/debugging
            var sriLankaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Colombo");
            var sriLankaDateTime = TimeZoneInfo.ConvertTimeFromUtc(utcNow, sriLankaTimeZone);
            
            Console.WriteLine($"Creating checkout with: UTC Date={attendanceDate:yyyy-MM-dd}, UTC Time={utcNow.TimeOfDay}, " + 
                             $"Sri Lanka={sriLankaDateTime:yyyy-MM-dd HH:mm:ss}");
            
            // However, we still store in UTC for consistent database storage
            return new UpdateAttendanceDto(
                attendanceDate.Date,
                utcNow.TimeOfDay
            );
        }

        [JsonPropertyName("attendanceDate")]
        public DateTime AttendanceDate { get; set; }
        
        [JsonPropertyName("checkOutTime")]
        public TimeSpan CheckOutTime { get; set; }
    }
} 