using System;
using System.Text.Json.Serialization;

namespace ChronoTrack_ViewLayer.Models
{
    public class AttendanceDto
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; }

        [JsonPropertyName("attendanceId")]
        public string AttendanceId 
        { 
            get => Id.ToString();
            set
            {
                if (Guid.TryParse(value, out Guid parsedId))
                {
                    Id = parsedId;
                }
                else
                {
                    Id = Guid.Empty;
                    Console.WriteLine($"Warning: Failed to parse AttendanceId: {value}");
                }
            }
        }
        
        [JsonPropertyName("userId")]
        public string UserId { get; set; } = string.Empty;
        
        [JsonPropertyName("userFullName")]
        public string UserFullName { get; set; } = string.Empty;
        
        [JsonPropertyName("attendanceDate")]
        public DateTime AttendanceDate { get; set; }
        
        [JsonPropertyName("checkInTime")]
        public TimeSpan CheckInTime { get; set; }
        
        [JsonPropertyName("checkOutTime")]
        public TimeSpan CheckOutTime { get; set; }
        
        [JsonPropertyName("duration")]
        public TimeSpan Duration { get; set; }
        
        [JsonPropertyName("isUtc")]
        public bool IsUtc { get; set; } = true; // Default to true for new records

        // Get check-in time converted to local time if needed
        public string FormattedCheckInTime { 
            get {
                // Just use our common method
                return GetLocalTimeString(CheckInTime);
            }
        }
        
        // Get check-out time converted to local time if needed
        public string FormattedCheckOutTime { 
            get {
                // Just use our common method
                return GetLocalTimeString(CheckOutTime);
            }
        }
        
        // Get local time for the given TimeSpan
        private string GetLocalTimeString(TimeSpan timeSpan) {
            if (timeSpan == TimeSpan.Zero) return "--:--:--";
            
            if (IsUtc) {
                try {
                    // Convert time to local machine time
                    DateTime utcDateTime = AttendanceDate.Date.Add(timeSpan);
                    DateTime localDateTime = utcDateTime.ToLocalTime();
                    
                    // Debug information
                    Console.WriteLine($"Converting time: UTC={utcDateTime:HH:mm:ss}, Local={localDateTime:HH:mm:ss}");
                    
                    return localDateTime.ToString("hh:mm:ss tt");
                }
                catch (Exception ex) {
                    // Fallback to standard local time if conversion fails
                    Console.WriteLine($"Error converting to local time: {ex.Message}. Using UTC time instead.");
                    DateTime utcDateTime = AttendanceDate.Date.Add(timeSpan);
                    return utcDateTime.ToString("hh:mm:ss tt");
                }
            }
            
            return timeSpan.ToString(@"hh\:mm\:ss tt");
        }
        
        public string FormattedDate {
            get {
                try {
                    if (AttendanceDate == default)
                        return "--";
                    
                    // Convert UTC date to local machine time
                    if (IsUtc) {
                        try {
                            var localDate = AttendanceDate.ToLocalTime();
                            
                            Console.WriteLine($"Converting date from UTC: {AttendanceDate:yyyy-MM-dd} to local time: {localDate:yyyy-MM-dd}");
                            return localDate.ToString("dd MMM yyyy");
                        }
                        catch (Exception ex) {
                            // Fallback to UTC if conversion fails
                            Console.WriteLine($"Error converting to local time: {ex.Message}. Using UTC time instead.");
                            return AttendanceDate.ToString("dd MMM yyyy");
                        }
                    }
                    
                    // If date is not UTC, use as is
                    return AttendanceDate.ToString("dd MMM yyyy");
                } catch (Exception ex) {
                    Console.WriteLine($"Error formatting date: {ex.Message}");
                    return "--";
                }
            }
        }
        
        [JsonPropertyName("status")]
        public string Status => CheckOutTime == TimeSpan.Zero ? "Active" : "Completed";

        // Helper methods for attendance status
        public bool IsCheckedIn => CheckInTime != TimeSpan.Zero;
        public bool IsCheckedOut => CheckOutTime != TimeSpan.Zero;
        public bool IsActive => IsCheckedIn && !IsCheckedOut;
        public bool IsCompleted => IsCheckedIn && IsCheckedOut;
        public bool CanCheckIn => !IsCheckedIn;
        public bool CanCheckOut => IsCheckedIn && !IsCheckedOut;

        // Get button states
        public (bool checkInEnabled, bool checkOutEnabled, bool checkInVisible, bool checkOutVisible) GetButtonStates()
        {
            if (!IsCheckedIn)
            {
                return (true, false, true, true); // Can only check in, but checkout button is visible
            }
            else if (IsCheckedIn && !IsCheckedOut)
            {
                return (false, true, true, true); // Can only check out
            }
            else
            {
                return (false, false, true, true); // Both disabled, both visible
            }
        }

        // Get status badge info
        public (string text, string textColor, string backgroundColor) GetStatusBadgeInfo()
        {
            if (!IsCheckedIn)
            {
                return ("Waiting", "#10B981", "#DCFCE7"); // Green for waiting
            }
            else if (IsCheckedIn && !IsCheckedOut)
            {
                return ("Active", "#166534", "#DCFCE7"); // Dark green for active
            }
            else
            {
                return ("Completed", "#1E40AF", "#DBEAFE"); // Blue for completed
            }
        }

        // Get status message
        public string GetStatusMessage()
        {
            if (!IsCheckedIn)
            {
                return "You haven't checked in today";
            }
            else if (IsCheckedIn && !IsCheckedOut)
            {
                return $"Work session active. Checked in at {FormattedCheckInTime}";
            }
            else
            {
                return $"Work session closed. Checked out at {FormattedCheckOutTime}";
            }
        }

        // Auto-generate checkout time if not checked out by midnight (9 hours after check-in)
        public void EnsureCheckOutTime()
        {
            if (CheckOutTime == TimeSpan.Zero)
            {
                DateTime referenceDate = IsUtc ? DateTime.UtcNow : DateTime.Now;
                
                // If current time is past midnight, set checkout time to 9 hours after check-in
                if (referenceDate.Date > AttendanceDate.Date)
                {
                    // Calculate 9 hours from check-in time
                    CheckOutTime = CheckInTime.Add(TimeSpan.FromHours(9));
                    // Recalculate duration
                    Duration = CheckOutTime - CheckInTime;
                }
            }
        }
    }

    public class AttendanceHistoryResponse
    {
        [JsonPropertyName("data")]
        public PagedResult<AttendanceDto> Data { get; set; } = new PagedResult<AttendanceDto>();
        
        [JsonPropertyName("todaysAttendance")]
        public AttendanceDto? TodaysAttendance { get; set; }
    }

    public class CreateAttendanceResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }
        
        [JsonPropertyName("message")]
        public string? Message { get; set; }
        
        [JsonPropertyName("data")]
        public AttendanceDto? Data { get; set; }
    }

    public class UpdateAttendanceResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }
        
        [JsonPropertyName("message")]
        public string? Message { get; set; }
        
        [JsonPropertyName("data")]
        public AttendanceDto? Data { get; set; }
    }

    public class DetailedAttendanceDto
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; }
        
        [JsonPropertyName("attendanceId")]
        public string AttendanceId 
        { 
            get => Id.ToString();
            set
            {
                if (Guid.TryParse(value, out Guid parsedId))
                {
                    Id = parsedId;
                }
                else
                {
                    Id = Guid.Empty;
                    Console.WriteLine($"Warning: Failed to parse AttendanceId: {value}");
                }
            }
        }
        
        [JsonPropertyName("userId")]
        public string UserId { get; set; } = string.Empty;
        
        [JsonPropertyName("userFullName")]
        public string UserFullName { get; set; } = string.Empty;
        
        [JsonPropertyName("department")]
        public string Department { get; set; } = string.Empty;
        
        [JsonPropertyName("userRole")]
        public string UserRole { get; set; } = string.Empty;
        
        [JsonPropertyName("attendanceDate")]
        public DateTime AttendanceDate { get; set; }
        
        [JsonPropertyName("checkInTime")]
        public TimeSpan CheckInTime { get; set; }
        
        [JsonPropertyName("checkOutTime")]
        public TimeSpan CheckOutTime { get; set; }
        
        [JsonPropertyName("duration")]
        public TimeSpan Duration { get; set; }
        
        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;
        
        [JsonPropertyName("isUtc")]
        public bool IsUtc { get; set; } = true;

        public string FormattedCheckInTime => GetLocalTimeString(CheckInTime);
        public string FormattedCheckOutTime => GetLocalTimeString(CheckOutTime);
        public string FormattedDuration => Duration.ToString(@"hh\:mm\:ss");
        
        // Get local time for the given TimeSpan
        private string GetLocalTimeString(TimeSpan timeSpan) {
            if (timeSpan == TimeSpan.Zero) return "--:--:--";
            
            if (IsUtc) {
                try {
                    // Convert time to local machine time
                    DateTime utcDateTime = AttendanceDate.Date.Add(timeSpan);
                    DateTime localDateTime = utcDateTime.ToLocalTime();
                    
                    // Debug information
                    Console.WriteLine($"Converting time: UTC={utcDateTime:HH:mm:ss}, Local={localDateTime:HH:mm:ss}");
                    
                    return localDateTime.ToString("hh:mm:ss tt");
                }
                catch (Exception ex) {
                    // Fallback to standard local time if conversion fails
                    Console.WriteLine($"Error converting to local time: {ex.Message}. Using UTC time instead.");
                    DateTime utcDateTime = AttendanceDate.Date.Add(timeSpan);
                    return utcDateTime.ToString("hh:mm:ss tt");
                }
            }
            
            return timeSpan.ToString(@"hh\:mm\:ss tt");
        }
        
        public string FormattedDate {
            get {
                try {
                    if (AttendanceDate == default)
                        return "--";
                    
                    // Convert UTC date to local machine time
                    if (IsUtc) {
                        try {
                            var localDate = AttendanceDate.ToLocalTime();
                            
                            Console.WriteLine($"Converting date from UTC: {AttendanceDate:yyyy-MM-dd} to local time: {localDate:yyyy-MM-dd}");
                            return localDate.ToString("dd MMM yyyy");
                        }
                        catch (Exception ex) {
                            // Fallback to UTC if conversion fails
                            Console.WriteLine($"Error converting to local time: {ex.Message}. Using UTC time instead.");
                            return AttendanceDate.ToString("dd MMM yyyy");
                        }
                    }
                    
                    // If date is not UTC, use as is
                    return AttendanceDate.ToString("dd MMM yyyy");
                } catch (Exception ex) {
                    Console.WriteLine($"Error formatting date: {ex.Message}");
                    return "--";
                }
            }
        }
    }
} 