using System.Text.Json.Serialization;

namespace ChronoTrack.DTOs
{
    // Use both constructor and properties to make it more flexible for deserialization
    public class CreateAttendanceDto
    {
        [JsonConstructor]
        public CreateAttendanceDto(DateTime attendanceDate, TimeSpan checkInTime, TimeSpan? checkOutTime = null)
        {
            AttendanceDate = attendanceDate;
            CheckInTime = checkInTime;
            CheckOutTime = checkOutTime;
        }
        
        // Default constructor for deserialization
        public CreateAttendanceDto() { }
        
        public DateTime AttendanceDate { get; set; }
        public TimeSpan CheckInTime { get; set; }
        public TimeSpan? CheckOutTime { get; set; }
    }

    // Use both constructor and properties to make it more flexible for deserialization
    public class UpdateAttendanceDto
    {
        [JsonConstructor]
        public UpdateAttendanceDto(DateTime attendanceDate, TimeSpan checkOutTime)
        {
            AttendanceDate = attendanceDate;
            CheckOutTime = checkOutTime;
        }
        
        // Default constructor for deserialization
        public UpdateAttendanceDto() { }
        
        public DateTime AttendanceDate { get; set; }
        public TimeSpan CheckOutTime { get; set; }
    }

    public record AttendanceDto(
        Guid Id,
        string UserId,
        string UserFullName,
        DateTime AttendanceDate,
        TimeSpan CheckInTime,
        TimeSpan CheckOutTime,
        TimeSpan Duration
    )
    {
        public IEnumerable<char>? EmployeeName { get; set; }
    }

    public record AttendanceSummaryDto(
        DateTime Date,
        int TotalEmployees,
        int PresentEmployees,
        int LateArrivals,
        double AverageWorkingHours
    );

    public record UserAttendanceReportDto(
        string UserId,
        string UserFullName,
        string Department,
        int TotalDays,
        int PresentDays,
        int LateDays,
        double AverageWorkingHours,
        List<AttendanceDto> AttendanceDetails
    );

    public record DetailedAttendanceDto(
        Guid Id,
        string UserId,
        string UserFullName,
        string Department,
        string UserRole,
        DateTime AttendanceDate,
        TimeSpan CheckInTime,
        TimeSpan CheckOutTime,
        TimeSpan Duration,
        string Status
    )
    {
        // Formatted properties for UI display
        public string FormattedDate => AttendanceDate.ToString("yyyy-MM-dd");
        public string FormattedCheckInTime => CheckInTime.ToString(@"hh\:mm\:ss");
        public string FormattedCheckOutTime => CheckOutTime == TimeSpan.Zero ? "--:--:--" : CheckOutTime.ToString(@"hh\:mm\:ss");
        public string FormattedDuration => Duration.ToString(@"hh\:mm\:ss");
        
        // Default constructor for deserialization
        public DetailedAttendanceDto() : this(
            Guid.Empty, 
            string.Empty, 
            string.Empty, 
            string.Empty, 
            string.Empty, 
            DateTime.UtcNow, 
            TimeSpan.Zero, 
            TimeSpan.Zero, 
            TimeSpan.Zero, 
            string.Empty) { }
    }
}