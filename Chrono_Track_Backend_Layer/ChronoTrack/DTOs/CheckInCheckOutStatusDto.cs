using System;
using System.Text.Json.Serialization;

namespace ChronoTrack.DTOs
{
    public class CheckInCheckOutStatusDto
    {
        [JsonPropertyName("userId")]
        public string UserId { get; set; }
        
        [JsonPropertyName("userFullName")]
        public string UserFullName { get; set; }
        
        [JsonPropertyName("isCheckedIn")]
        public bool IsCheckedIn { get; set; }
        
        [JsonPropertyName("currentAttendanceId")]
        public string CurrentAttendanceId { get; set; }
        
        [JsonPropertyName("attendanceDate")]
        public DateTime? AttendanceDate { get; set; }
        
        [JsonPropertyName("checkInTime")]
        public TimeSpan? CheckInTime { get; set; }
        
        [JsonPropertyName("checkOutTime")]
        public TimeSpan? CheckOutTime { get; set; }
        
        [JsonPropertyName("duration")]
        public TimeSpan? Duration { get; set; }
        
        public CheckInCheckOutStatusDto()
        {
        }
        
        public CheckInCheckOutStatusDto(
            string userId,
            string userFullName,
            bool isCheckedIn,
            string currentAttendanceId,
            DateTime? attendanceDate,
            TimeSpan? checkInTime,
            TimeSpan? checkOutTime,
            TimeSpan? duration)
        {
            UserId = userId;
            UserFullName = userFullName;
            IsCheckedIn = isCheckedIn;
            CurrentAttendanceId = currentAttendanceId;
            AttendanceDate = attendanceDate;
            CheckInTime = checkInTime;
            CheckOutTime = checkOutTime;
            Duration = duration;
        }
    }
} 