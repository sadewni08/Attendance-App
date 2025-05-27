using System.Text.Json.Serialization;

namespace ChronoTrack.DTOs
{
    public class AttendanceStatusDto
    {
        [JsonPropertyName("isCheckedIn")]
        public bool IsCheckedIn { get; set; }
        
        [JsonPropertyName("checkedOutToday")]
        public bool CheckedOutToday { get; set; }
        
        [JsonPropertyName("attendanceId")]
        public string AttendanceId { get; set; } = string.Empty;
    }
} 