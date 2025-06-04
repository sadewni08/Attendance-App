using Microsoft.Maui.Controls;
using System;
using System.Timers;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using ChronoTrack_ViewLayer.Services;
using ChronoTrack_ViewLayer.Models;
using System.Collections.ObjectModel;
using System.Linq;

namespace ChronoTrack_ViewLayer.Pages.User
{
    public partial class UserDashboardPage : ContentPage, INotifyPropertyChanged
    {
        private System.Timers.Timer timer;
        private string currentTime;
        private string currentDate;
        private readonly AttendanceService _attendanceService;
        private string _currentUserId;
        private string _currentAttendanceId;
        private bool _isCheckedIn = false;
        private readonly ObservableCollection<AttendanceDto> _attendanceHistory = new ObservableCollection<AttendanceDto>();
        private bool _isPageActive = false;

        public string CurrentTime
        {
            get => currentTime;
            set
            {
                if (currentTime != value)
                {
                    currentTime = value;
                    OnPropertyChanged();
                }
            }
        }

        public string CurrentDate
        {
            get => currentDate;
            set
            {
                if (currentDate != value)
                {
                    currentDate = value;
                    OnPropertyChanged();
                }
            }
        }

        public UserDashboardPage()
        {
            try
            {
                InitializeComponent();
                AttendanceHistoryList.ItemsSource = _attendanceHistory;
                _attendanceService = new AttendanceService();

                // Get the current logged-in user ID from preferences
                _currentUserId = Preferences.Get("UserId", string.Empty);
                if (string.IsNullOrEmpty(_currentUserId))
                {
                    // Get user ID from UserName if UserId is not available
                    // This is a fallback mechanism since the header is showing user info correctly
                    var userName = Preferences.Get("UserName", string.Empty);
                    if (!string.IsNullOrEmpty(userName))
                    {
                        Console.WriteLine($"Using user name as fallback: {userName}");
                        // If needed, we could lookup the userId from the userName here
                    }

                    // For testing purposes, use a default user ID if nothing is available
                    if (string.IsNullOrEmpty(_currentUserId))
                    {
                        _currentUserId = "000001";
                        Console.WriteLine($"Using default user ID: {_currentUserId}");
                    }
                }
                else
                {
                    Console.WriteLine($"Current user ID: {_currentUserId}");
                }

                SetupEventHandlers();
                UpdateTimeAndDate();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in UserDashboardPage constructor: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        private void SetupEventHandlers()
        {
            try
            {
                // Wire up header events through FindByName since it might not be directly accessible by name
                var header = this.FindByName<Components.HeaderComponent>("HeaderComponent");
                if (header != null)
                {
                    header.LogoutRequested += OnHeaderLogoutRequested;
                }

                // Wire up button events if necessary
                if (this.FindByName<Button>("ViewHistoryButton") is Button viewHistoryButton)
                {
                    viewHistoryButton.Clicked += OnViewHistoryClicked;
                }

                if (this.FindByName<Button>("CheckInButton") is Button checkInButton)
                {
                    checkInButton.Clicked += OnCheckInClicked;
                }

                if (this.FindByName<Button>("CheckOutButton") is Button checkOutButton)
                {
                    checkOutButton.Clicked += OnCheckOutClicked;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in SetupEventHandlers: {ex.Message}");
            }
        }

        private void OnHeaderLogoutRequested(object sender, EventArgs e)
        {
            // The header component will handle clearing preferences and navigation
            // We just need to ensure it's allowed to proceed
        }

        private void UpdateTimeAndDate()
        {
            try
            {
                var now = DateTime.Now;
                DateLabel.Text = now.ToString("dd MMMM yyyy");
                TimeLabel.Text = now.ToString("h:mm:ss tt");
                Today.Text = "Today (Local Time)";

                // Display user info for verification (can be removed in production)
                var userName = Preferences.Get("UserName", "Unknown User");
                var userId = Preferences.Get("UserId", "Unknown ID");
                if (RealTimeInsight != null)
                {
                    // Get local timezone information to display
                    string timeZoneInfo = TimeZoneInfo.Local.DisplayName;
                    RealTimeInsight.Text = $"Local Time ({TimeZoneInfo.Local.StandardName})";
                }

                // Dispose of any existing timer
                if (timer != null)
                {
                    timer.Stop();
                    timer.Elapsed -= OnTimerElapsed;
                    timer.Dispose();
                    timer = null;
                }

                // Create new timer
                timer = new System.Timers.Timer(1000);
                timer.Elapsed += OnTimerElapsed;
                timer.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in UpdateTimeAndDate: {ex.Message}");
            }
        }

        private void OnTimerElapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                if (!_isPageActive)
                {
                    return;
                }

                var time = DateTime.Now;
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    try
                    {
                        if (TimeLabel != null)
                        {
                            TimeLabel.Text = time.ToString("h:mm:ss tt");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error updating time label: {ex.Message}");
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in timer elapsed: {ex.Message}");
            }
        }

        private async Task CheckAttendanceStatusAsync()
        {
            try
            {
                if (!_isPageActive)
                {
                    Console.WriteLine("CheckAttendanceStatusAsync: Page is not active, aborting");
                    return;
                }

                // Get current user ID
                var userId = Preferences.Get("UserId", string.Empty);
                if (string.IsNullOrEmpty(userId))
                {
                    userId = _currentUserId; // Fallback to the stored ID
                }

                if (string.IsNullOrEmpty(userId))
                {
                    Console.WriteLine("Error: User ID not found during CheckAttendanceStatusAsync");
                    if (WorkStatusMessage != null)
                    {
                        WorkStatusMessage.Text = "Error: User ID not found";
                        WorkStatusMessage.IsVisible = true;
                    }
                    return;
                }

                Console.WriteLine($"CheckAttendanceStatusAsync: Checking status for user ID: {userId}");

                // Update the stored user ID
                _currentUserId = userId;

                // Show loading indicator or message
                if (WorkStatusMessage != null)
                {
                    WorkStatusMessage.Text = "Checking attendance status...";
                    WorkStatusMessage.IsVisible = true;
                }

                // Ensure the checkout button is always visible
                if (CheckOutButton != null)
                {
                    CheckOutButton.IsVisible = true;
                }

                // Get current date for date range filtering
                var today = DateTime.UtcNow.Date;
                var startDate = today.AddDays(-7); // Get last week's attendance for recent activity

                // First, get recent attendance history for the Recent Activity section
                Console.WriteLine("Calling GetUserAttendanceHistoryAsync for recent activity...");
                var historyResult = await _attendanceService.GetUserAttendanceHistoryAsync(
                    userId,
                    startDate,
                    today.AddDays(1),  // Include today
                    1,                 // First page
                    10                 // Up to 10 records
                );

                Console.WriteLine($"GetUserAttendanceHistoryAsync result: Success={historyResult.Success}, Message={historyResult.Message ?? "null"}");

                // Update recent activity list regardless of status check
                if (historyResult.Success && historyResult.Data?.Items != null)
                {
                    // Clear existing history
                    _attendanceHistory.Clear();

                    // Add new items, avoiding duplicates
                    var uniqueAttendances = historyResult.Data.Items
                        .GroupBy(x => x.AttendanceId)
                        .Select(g => g.First())
                        .OrderByDescending(x => x.AttendanceDate)
                        .ToList();

                    foreach (var item in uniqueAttendances)
                    {
                        // Ensure IsUtc is set to true for proper time zone conversion
                        item.IsUtc = true;
                        _attendanceHistory.Add(item);
                    }
                }

                // Now call the dedicated check-in/check-out status API
                Console.WriteLine("Calling GetCheckInCheckOutStatusAsync...");
                var statusResult = await _attendanceService.GetCheckInCheckOutStatusAsync(userId);

                Console.WriteLine($"GetCheckInCheckOutStatusAsync result: Success={statusResult.Success}, Message={statusResult.Message ?? "null"}");

                if (!_isPageActive) return;

                if (statusResult.Success && statusResult.Data != null)
                {
                    var status = statusResult.Data;
                    Console.WriteLine($"Status: IsCheckedIn={status.IsCheckedIn}, IsCheckedOut={status.IsCheckedOut}, AttendanceId={status.CurrentAttendanceId}");

                    _isCheckedIn = status.IsCheckedIn;
                    _currentAttendanceId = status.CurrentAttendanceId;

                    // Get button states from the status
                    var (checkInEnabled, checkOutEnabled, checkInVisible, checkOutVisible) = status.GetButtonStates();

                    // Update button states
                    if (CheckInButton != null)
                    {
                        CheckInButton.IsEnabled = checkInEnabled;
                        CheckInButton.IsVisible = checkInVisible;
                    }
                    if (CheckOutButton != null)
                    {
                        CheckOutButton.IsEnabled = checkOutEnabled;
                        CheckOutButton.IsVisible = true; // Always keep checkout button visible
                    }

                    // Update status badge based on the actual state
                    if (AttendanceStatusBadge != null && AttendanceStatusBadge.Parent is Border badge)
                    {
                        string badgeText;
                        string textColor;
                        string bgColor;

                        if (status.IsCheckedIn && !status.IsCheckedOut)
                        {
                            // Active status - user is checked in but not checked out
                            badgeText = "Active";
                            textColor = "#166534"; // Dark green
                            bgColor = "#DCFCE7"; // Light green
                        }
                        else if (status.IsCheckedIn && status.IsCheckedOut)
                        {
                            // Closed status - user has checked in and out
                            badgeText = "Closed";
                            textColor = "#1E40AF"; // Dark blue
                            bgColor = "#DBEAFE"; // Light blue
                        }
                        else
                        {
                            // Waiting status - user has not checked in
                            badgeText = "Waiting";
                            textColor = "#10B981"; // Green
                            bgColor = "#DCFCE7"; // Light green
                        }

                        AttendanceStatusBadge.Text = badgeText;
                        AttendanceStatusBadge.TextColor = Color.FromArgb(textColor);
                        badge.BackgroundColor = Color.FromArgb(bgColor);

                        Console.WriteLine($"Updated status badge: Text={badgeText}, Actual Status={status.Status}");
                    }

                    // Update attendance status label
                    if (AttendanceStatusLabel != null)
                    {
                        if (status.IsCheckedIn && !status.IsCheckedOut)
                        {
                            AttendanceStatusLabel.Text = "You are checked in for today";
                        }
                        else if (status.IsCheckedIn && status.IsCheckedOut)
                        {
                            AttendanceStatusLabel.Text = "Your work is closed for today";
                        }
                        else
                        {
                            AttendanceStatusLabel.Text = "You haven't checked in today";
                        }
                    }

                    // Update work status message
                    if (WorkStatusMessage != null)
                    {
                        if (status.IsCheckedIn && !status.IsCheckedOut)
                        {
                            WorkStatusMessage.Text = $"Work session active. Checked in at {status.FormattedCheckInTime}";
                        }
                        else if (status.IsCheckedIn && status.IsCheckedOut)
                        {
                            // Calculate duration
                            TimeSpan duration = status.Duration ?? TimeSpan.Zero;
                            string durationText = duration.Hours > 0
                                ? $"{duration.Hours} hours and {duration.Minutes} minutes"
                                : $"{duration.Minutes} minutes";

                            WorkStatusMessage.Text = $"Work session closed. Today's work duration: {durationText}";
                        }
                        else
                        {
                            WorkStatusMessage.Text = "You haven't checked in today";
                        }

                        WorkStatusMessage.IsVisible = true;
                    }

                    // Update check-in time label
                    if (CheckInTimeLabel != null)
                    {
                        if (status.IsCheckedIn)
                        {
                            if (status.IsCheckedOut)
                            {
                                CheckInTimeLabel.Text = $"Checked in at: {status.FormattedCheckInTime}, Checked out at: {status.FormattedCheckOutTime}";
                            }
                            else
                            {
                                CheckInTimeLabel.Text = $"Check-in time: {status.FormattedCheckInTime}";
                            }
                            CheckInTimeLabel.IsVisible = true;
                        }
                        else
                        {
                            CheckInTimeLabel.IsVisible = false;
                        }
                    }
                }
                else
                {
                    _isCheckedIn = false;
                    _currentAttendanceId = null;

                    Console.WriteLine("Failed to get attendance status: " + (statusResult.Message ?? "Unknown error"));

                    // Reset button states for no attendance
                    if (CheckInButton != null)
                    {
                        CheckInButton.IsEnabled = true;
                        CheckInButton.IsVisible = true;
                    }
                    if (CheckOutButton != null)
                    {
                        CheckOutButton.IsEnabled = false;
                        CheckOutButton.IsVisible = true; // Always keep checkout button visible
                    }

                    // Reset status badge to "Waiting"
                    if (AttendanceStatusBadge != null && AttendanceStatusBadge.Parent is Border badge)
                    {
                        AttendanceStatusBadge.Text = "Waiting";
                        AttendanceStatusBadge.TextColor = Color.FromArgb("#10B981");
                        badge.BackgroundColor = Color.FromArgb("#DCFCE7");
                    }

                    // Reset status label
                    if (AttendanceStatusLabel != null)
                    {
                        AttendanceStatusLabel.Text = "You haven't checked in today";
                    }

                    // Update work status message with error
                    if (WorkStatusMessage != null)
                    {
                        WorkStatusMessage.Text = "Could not retrieve attendance status";
                        WorkStatusMessage.IsVisible = true;
                    }

                    // Hide check-in time label
                    if (CheckInTimeLabel != null)
                    {
                        CheckInTimeLabel.IsVisible = false;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in CheckAttendanceStatusAsync: {ex.Message}");
                Console.WriteLine(ex.StackTrace);

                if (WorkStatusMessage != null)
                {
                    WorkStatusMessage.Text = "Error checking attendance status";
                    WorkStatusMessage.IsVisible = true;
                }
            }
        }

        private async void OnViewHistoryClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new UserHistoryPage());
        }

        private async void OnCheckInClicked(object sender, EventArgs e)
        {
            try
            {
                // Update work status message
                if (WorkStatusMessage != null)
                {
                    WorkStatusMessage.Text = "Checking in...";
                    WorkStatusMessage.IsVisible = true;
                }

                // Disable check-in button during API call
                if (CheckInButton != null) CheckInButton.IsEnabled = false;
                if (CheckOutButton != null)
                {
                    CheckOutButton.IsVisible = true;
                    CheckOutButton.IsEnabled = false;
                }

                // Get current user ID
                var userId = Preferences.Get("UserId", string.Empty);
                if (string.IsNullOrEmpty(userId))
                {
                    userId = _currentUserId; // Fallback to the stored ID
                }

                if (string.IsNullOrEmpty(userId))
                {
                    Console.WriteLine("User ID not found for check-in");
                    if (WorkStatusMessage != null)
                    {
                        WorkStatusMessage.Text = "Error: User ID not found";
                    }
                    if (CheckInButton != null) CheckInButton.IsEnabled = true;
                    await DisplayAlert("Error", "User ID not found. Please log in again.", "OK");
                    return;
                }

                Console.WriteLine($"Checking in user: {userId}");

                // Use the new CreateWithLocalTime method to ensure the date is handled properly for Sri Lanka time zone
                var checkInDto = CreateAttendanceDto.CreateWithLocalTime();

                // Call the API to check in
                var result = await _attendanceService.CheckInAsync(userId, checkInDto);

                if (!_isPageActive) return;

                if (result.Success && result.Data != null)
                {
                    // Store the attendance ID for future check-out operation
                    _currentAttendanceId = result.Data.AttendanceId;
                    _isCheckedIn = true;
                    _currentUserId = userId;

                    Console.WriteLine($"Check-in successful. AttendanceId: {_currentAttendanceId}");

                    // Update UI to show "Active" status immediately
                    if (AttendanceStatusLabel != null)
                    {
                        AttendanceStatusLabel.Text = "You are checked in for today";
                    }

                    if (CheckInTimeLabel != null)
                    {
                        CheckInTimeLabel.Text = $"Check-in time: {result.Data.FormattedCheckInTime}";
                        CheckInTimeLabel.IsVisible = true;
                    }

                    // Update status badge to "Active"
                    if (AttendanceStatusBadge != null && AttendanceStatusBadge.Parent is Border badge)
                    {
                        AttendanceStatusBadge.Text = "Active";
                        AttendanceStatusBadge.TextColor = Color.FromArgb("#166534"); // Dark green
                        badge.BackgroundColor = Color.FromArgb("#DCFCE7"); // Light green
                    }

                    // Update button states - enable checkout, disable check-in
                    if (CheckInButton != null)
                    {
                        CheckInButton.IsEnabled = false;
                        CheckInButton.IsVisible = false;
                    }
                    if (CheckOutButton != null)
                    {
                        CheckOutButton.IsVisible = true;
                        CheckOutButton.IsEnabled = true;
                    }

                    // Update work status message
                    if (WorkStatusMessage != null)
                    {
                        WorkStatusMessage.Text = $"Work session active. Checked in at {result.Data.FormattedCheckInTime}";
                        WorkStatusMessage.IsVisible = true;
                    }

                    // Add the new attendance record to the history collection and refresh
                    await CheckAttendanceStatusAsync();

                    if (_isPageActive)
                    {
                        await DisplayAlert("Check In Successful",
                            $"You have been checked in at {result.Data.FormattedCheckInTime}. Your status is now Active.",
                            "OK");
                    }
                }
                else
                {
                    Console.WriteLine($"Check-in failed: {result.Message}");

                    // Re-enable check-in button
                    if (CheckInButton != null) CheckInButton.IsEnabled = true;

                    // Update work status message
                    if (WorkStatusMessage != null)
                    {
                        WorkStatusMessage.Text = "Check in failed. Please try again.";
                    }

                    if (_isPageActive)
                    {
                        await DisplayAlert("Check In Failed", result.Message ?? "Failed to check in. Please try again.", "OK");
                        await CheckAttendanceStatusAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during check-in: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");

                if (CheckInButton != null) CheckInButton.IsEnabled = true;

                if (WorkStatusMessage != null)
                {
                    WorkStatusMessage.Text = "Error during check in. Please try again.";
                }

                if (_isPageActive)
                {
                    await DisplayAlert("Error", "An error occurred during check-in. Please try again.", "OK");
                    await CheckAttendanceStatusAsync();
                }
            }
        }

        private async void OnCheckOutClicked(object sender, EventArgs e)
        {
            try
            {
                // Update work status message
                if (WorkStatusMessage != null)
                {
                    WorkStatusMessage.Text = "Checking out...";
                    WorkStatusMessage.IsVisible = true;
                }

                // Disable both buttons during API call
                if (CheckInButton != null) CheckInButton.IsEnabled = false;
                if (CheckOutButton != null) CheckOutButton.IsEnabled = false;

                // Get current user ID
                var userId = Preferences.Get("UserId", string.Empty);
                if (string.IsNullOrEmpty(userId))
                {
                    userId = _currentUserId; // Fallback to the stored ID
                }

                if (string.IsNullOrEmpty(userId))
                {
                    Console.WriteLine("User ID not found for check-out");
                    if (WorkStatusMessage != null)
                    {
                        WorkStatusMessage.Text = "Error: User ID not found";
                    }
                    if (CheckOutButton != null) CheckOutButton.IsEnabled = true;

                    if (_isPageActive)
                    {
                        await DisplayAlert("Error", "User ID not found. Please log in again.", "OK");
                    }
                    return;
                }

                // Get current attendance status to ensure we have the correct attendance ID
                Console.WriteLine("Getting current check-in/check-out status before checkout");
                var statusResult = await _attendanceService.GetCheckInCheckOutStatusAsync(userId);

                if (!statusResult.Success || statusResult.Data == null || !statusResult.Data.IsCheckedIn)
                {
                    Console.WriteLine("Cannot check-out: User is not checked in or status check failed");
                    if (WorkStatusMessage != null)
                    {
                        WorkStatusMessage.Text = "Error: You need to check in first";
                    }
                    if (CheckOutButton != null) CheckOutButton.IsEnabled = true;

                    if (_isPageActive)
                    {
                        await DisplayAlert("Not Checked In", "You need to check in first before checking out.", "OK");
                        await CheckAttendanceStatusAsync();
                    }
                    return;
                }

                // If already checked out
                if (statusResult.Data.IsCheckedOut)
                {
                    Console.WriteLine("User already checked out");
                    if (WorkStatusMessage != null)
                    {
                        WorkStatusMessage.Text = "You have already checked out today";
                    }

                    if (_isPageActive)
                    {
                        await DisplayAlert("Already Checked Out",
                            $"You are already checked out for today.", "OK");
                        await CheckAttendanceStatusAsync();
                    }
                    return;
                }

                // Get the attendance ID from the status
                string attendanceId = statusResult.Data.CurrentAttendanceId;

                // Check if attendance ID is valid
                if (string.IsNullOrEmpty(attendanceId))
                {
                    Console.WriteLine("Attendance ID not found in status for check-out");
                    if (WorkStatusMessage != null)
                    {
                        WorkStatusMessage.Text = "Error: Attendance record not found";
                    }
                    if (CheckOutButton != null) CheckOutButton.IsEnabled = true;

                    if (_isPageActive)
                    {
                        await DisplayAlert("Error", "Attendance record not found. Please refresh the page.", "OK");
                        await CheckAttendanceStatusAsync();
                    }
                    return;
                }

                // Extract the attendance date from the status response for proper checkout
                var checkoutAttendanceDate = DateTime.Today; // Default as a fallback

                // Create the check-out DTO using local time for proper timezone handling
                var checkoutDto = UpdateAttendanceDto.CreateWithLocalTime(checkoutAttendanceDate);

                Console.WriteLine($"Checking out user: {userId}, attendance ID: {attendanceId}");
                var result = await _attendanceService.CheckOutAsync(userId, attendanceId, checkoutDto);

                if (!_isPageActive) return;

                if (result.Success && result.Data != null)
                {
                    Console.WriteLine("Check-out successful");
                    _isCheckedIn = false;
                    _currentAttendanceId = null;

                    // Update UI to show "Closed" status
                    if (AttendanceStatusLabel != null)
                    {
                        AttendanceStatusLabel.Text = "Your work is closed for today";
                    }

                    // Update status badge to "Closed"
                    if (AttendanceStatusBadge != null && AttendanceStatusBadge.Parent is Border badge)
                    {
                        AttendanceStatusBadge.Text = "Closed";
                        AttendanceStatusBadge.TextColor = Color.FromArgb("#1E40AF");
                        badge.BackgroundColor = Color.FromArgb("#DBEAFE");
                    }

                    // Update button states - disable both buttons since work is completed for the day
                    if (CheckInButton != null)
                    {
                        CheckInButton.IsEnabled = false;
                        CheckInButton.IsVisible = false;
                    }
                    if (CheckOutButton != null)
                    {
                        CheckOutButton.IsEnabled = false;
                        CheckOutButton.IsVisible = true;
                    }

                    // Calculate duration for work status message
                    TimeSpan duration = result.Data.Duration;
                    string durationText = duration.Hours > 0
                        ? $"{duration.Hours} hours and {duration.Minutes} minutes"
                        : $"{duration.Minutes} minutes";

                    // Update work status message
                    if (WorkStatusMessage != null)
                    {
                        WorkStatusMessage.Text = $"Work session closed. Today's work duration: {durationText}. Check-out time: {result.Data.FormattedCheckOutTime}";
                        WorkStatusMessage.IsVisible = true;
                    }

                    // Ensure check-in time is still displayed
                    if (CheckInTimeLabel != null)
                    {
                        CheckInTimeLabel.Text = $"Checked in at: {result.Data.FormattedCheckInTime}, Checked out at: {result.Data.FormattedCheckOutTime}";
                        CheckInTimeLabel.IsVisible = true;
                    }

                    // Refresh attendance data
                    await CheckAttendanceStatusAsync();

                    if (_isPageActive)
                    {
                        await DisplayAlert("Check Out Successful",
                            $"You have been checked out at {result.Data.FormattedCheckOutTime}.\nTotal duration: {durationText}\nYour work status is now Closed for today.",
                            "OK");
                    }
                }
                else
                {
                    Console.WriteLine($"Check-out failed: {result.Message}");

                    if (CheckOutButton != null) CheckOutButton.IsEnabled = true;

                    if (WorkStatusMessage != null)
                    {
                        WorkStatusMessage.Text = "Check out failed. Please try again.";
                    }

                    if (_isPageActive)
                    {
                        await DisplayAlert("Check Out Failed", result.Message ?? "Failed to check out. Please try again.", "OK");
                        await CheckAttendanceStatusAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during check-out: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");

                if (CheckOutButton != null) CheckOutButton.IsEnabled = true;

                if (WorkStatusMessage != null)
                {
                    WorkStatusMessage.Text = "Error during check out. Please try again.";
                }

                if (_isPageActive)
                {
                    await DisplayAlert("Error", "An error occurred during check-out. Please try again.", "OK");
                    await CheckAttendanceStatusAsync();
                }
            }
        }

        protected override bool OnBackButtonPressed()
        {
            // Prevent back navigation to login page
            return true;
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();

            _isPageActive = false;

            // Stop the timer to prevent memory leaks
            if (timer != null)
            {
                timer.Stop();
                timer.Elapsed -= OnTimerElapsed;
                timer.Dispose();
                timer = null;
            }

            // Unsubscribe from events
            var header = this.FindByName<Components.HeaderComponent>("HeaderComponent");
            if (header != null)
            {
                header.LogoutRequested -= OnHeaderLogoutRequested;
            }

            // Clean up button event handlers if needed
            if (this.FindByName<Button>("ViewHistoryButton") is Button viewHistoryButton)
            {
                viewHistoryButton.Clicked -= OnViewHistoryClicked;
            }

            if (this.FindByName<Button>("CheckInButton") is Button checkInButton)
            {
                checkInButton.Clicked -= OnCheckInClicked;
            }

            if (this.FindByName<Button>("CheckOutButton") is Button checkOutButton)
            {
                checkOutButton.Clicked -= OnCheckOutClicked;
            }

            // Explicitly clear collections
            _attendanceHistory.Clear();

            // Force GC collection
            GC.Collect();
        }

        protected override async void OnAppearing()
        {
            try
            {
                base.OnAppearing();

                _isPageActive = true;

                // Start the timer
                if (timer != null && !timer.Enabled)
                {
                    timer.Start();
                }

                // Update time and date
                UpdateTimeAndDate();

                // Ensure the CheckOut button is always visible
                if (CheckOutButton != null)
                {
                    CheckOutButton.IsVisible = true;
                }

                // Refresh attendance status when page appears
                Console.WriteLine("OnAppearing: Refreshing attendance status");
                await CheckAttendanceStatusAsync();

                if (!_isPageActive)
                {
                    return;
                }

                // Check if it's a new day since last check-out
                // This ensures the auto check-out logic is triggered
                if (_isCheckedIn && !string.IsNullOrEmpty(_currentAttendanceId))
                {
                    var todayAttendance = _attendanceHistory.FirstOrDefault(a => a.AttendanceId == _currentAttendanceId);
                    if (todayAttendance != null)
                    {
                        var isCheckedOut = todayAttendance.CheckOutTime != TimeSpan.Zero;

                        // If auto check-out was applied, reflect it in the UI
                        if (isCheckedOut && todayAttendance.AttendanceDate.Date < DateTime.Now.Date)
                        {
                            // Update UI to show checked-out state
                            if (AttendanceStatusLabel != null)
                            {
                                AttendanceStatusLabel.Text = "You have been automatically checked out";
                            }

                            if (AttendanceStatusBadge != null && AttendanceStatusBadge.Parent is Border badge)
                            {
                                AttendanceStatusBadge.Text = "Closed";
                                AttendanceStatusBadge.TextColor = Color.FromArgb("#1E40AF"); // Dark blue
                                badge.BackgroundColor = Color.FromArgb("#DBEAFE"); // Light blue
                            }

                            // Disable check-out button but keep it visible
                            if (CheckOutButton != null)
                            {
                                CheckOutButton.IsEnabled = false;
                                CheckOutButton.IsVisible = true;
                            }
                            if (CheckInButton != null) CheckInButton.IsEnabled = false;

                            // Calculate and show duration in work status message
                            TimeSpan duration = todayAttendance.Duration;
                            string durationText = duration.Hours > 0
                                ? $"{duration.Hours} hours and {duration.Minutes} minutes"
                                : $"{duration.Minutes} minutes";

                            if (WorkStatusMessage != null)
                            {
                                WorkStatusMessage.Text = $"Auto check-out applied: {durationText} total work duration";
                                WorkStatusMessage.IsVisible = true;
                            }

                            if (_isPageActive)
                            {
                                // Show a notification about the auto check-out
                                await DisplayAlert("Auto Check-Out",
                                    $"You have been automatically checked out at 9 hours after your check-in time, as you didn't check out before midnight.\nTotal duration: {durationText}\nYour work status is now Closed.",
                                    "OK");

                                // Refresh attendance history again to ensure UI is updated
                                await CheckAttendanceStatusAsync();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in OnAppearing: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}