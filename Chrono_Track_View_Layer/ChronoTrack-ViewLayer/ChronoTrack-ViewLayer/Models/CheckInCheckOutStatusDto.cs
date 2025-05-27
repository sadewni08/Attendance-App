using System;
using System.Text.Json.Serialization;

namespace ChronoTrack_ViewLayer.Models
{
    public class CheckInCheckOutStatusDto
    {
        [JsonPropertyName("userId")]
        public string UserId { get; set; } = string.Empty;
        
        [JsonPropertyName("userFullName")]
        public string UserFullName { get; set; } = string.Empty;
        
        [JsonPropertyName("isCheckedIn")]
        public bool IsCheckedIn { get; set; }
        
        [JsonPropertyName("currentAttendanceId")]
        public string CurrentAttendanceId { get; set; } = string.Empty;
        
        [JsonPropertyName("attendanceDate")]
        public DateTime? AttendanceDate { get; set; }
        
        [JsonPropertyName("checkInTime")]
        public TimeSpan? CheckInTime { get; set; }
        
        [JsonPropertyName("checkOutTime")]
        public TimeSpan? CheckOutTime { get; set; }
        
        [JsonPropertyName("duration")]
        public TimeSpan? Duration { get; set; }

        [JsonPropertyName("isUtc")]
        public bool IsUtc { get; set; } = true; // Default to true for new records
        
        // Helper properties for UI display
        public string FormattedCheckInTime => CheckInTime.HasValue ? GetLocalTimeString(CheckInTime.Value) : "--:--:--";
        public string FormattedCheckOutTime => CheckOutTime.HasValue ? GetLocalTimeString(CheckOutTime.Value) : "--:--:--";
        public string FormattedDuration => Duration.HasValue ? Duration.Value.ToString(@"hh\:mm\:ss") : "--:--:--";
        
        // Helper for status display
        public bool IsCheckedOut => CheckOutTime.HasValue && CheckOutTime.Value != TimeSpan.Zero;
        public string Status => IsCheckedIn ? (IsCheckedOut ? "Closed" : "Active") : "Waiting";
        
        // Get local time for the given TimeSpan
        private string GetLocalTimeString(TimeSpan timeSpan)
        {
            try
            {
                if (timeSpan == TimeSpan.Zero)
                    return "--:--:--";

                if (!AttendanceDate.HasValue)
                    return timeSpan.ToString(@"hh\:mm\:ss");

                // Convert UTC date and time to Sri Lanka time zone
                if (IsUtc)
                {
                    try
                    {
                        var sriLankaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Colombo");
                        var utcDateTime = AttendanceDate.Value.Date.Add(timeSpan);
                        var sriLankaDateTime = TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, sriLankaTimeZone);
                        
                        Console.WriteLine($"Converting time from UTC: {utcDateTime:HH:mm:ss} to Sri Lanka time: {sriLankaDateTime:HH:mm:ss}");
                        return sriLankaDateTime.ToString("hh:mm:ss tt");
                    }
                    catch (Exception ex)
                    {
                        // Fallback to general ToLocalTime if timezone conversion fails
                        Console.WriteLine($"Error converting to Sri Lanka time: {ex.Message}. Using local time instead.");
                        return AttendanceDate.Value.Date.Add(timeSpan).ToLocalTime().ToString("hh:mm:ss tt");
                    }
                }

                // If not UTC, use as is
                return AttendanceDate.Value.Date.Add(timeSpan).ToString("hh:mm:ss tt");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error formatting time: {ex.Message}");
                return "--:--:--";
            }
        }
        
        // Helper method for determining button states
        public (bool checkInEnabled, bool checkOutEnabled, bool checkInVisible, bool checkOutVisible) GetButtonStates()
        {
            if (!IsCheckedIn)
            {
                // Not checked in yet - can check in, cannot check out
                return (true, false, true, true);
            }
            else if (IsCheckedIn && !IsCheckedOut)
            {
                // Checked in but not checked out - cannot check in, can check out
                return (false, true, false, true);
            }
            else
            {
                // Checked in and checked out - cannot check in or check out
                return (false, false, false, true);
            }
        }
    }
} 