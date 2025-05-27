using System.Timers;
using Microsoft.Maui.Controls;
using Microsoft.Maui.ApplicationModel;
using ChronoTrack_ViewLayer.Components;
using System;
using ChronoTrack_ViewLayer.Services.Interfaces;
using ChronoTrack_ViewLayer.Services;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using ChronoTrack_ViewLayer.Models;
using System.Linq;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using ChronoTrack_ViewLayer.Pages;

namespace ChronoTrack_ViewLayer.Pages
{
    public partial class DashboardPage : ContentPage, INotifyPropertyChanged
    {
        private System.Timers.Timer timer;
        private SidebarComponent sidebarComponent;
        private readonly IEmployeeService _employeeService;
        private readonly IAttendanceService _attendanceService;
        private ObservableCollection<EmployeeSelectionItem> _employees = new ObservableCollection<EmployeeSelectionItem>();
        private bool _isEmployeeListVisible = false;
        private bool _isAllSelected = false;
        private ICommand _onEmployeeSelectionBorderTappedCommand;
        private HeaderComponent _headerComponent;

        public ICommand OnEmployeeSelectionBorderTappedCommand => 
            _onEmployeeSelectionBorderTappedCommand ??= new Command(async () => await OnEmployeeSelectionBorderTappedAsync());

        public ObservableCollection<EmployeeSelectionItem> Employees
        {
            get => _employees;
            set
            {
                if (_employees != value)
                {
                    _employees = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(HasSelectedEmployees));
                    OnPropertyChanged(nameof(HasCheckedInEmployees));
                }
            }
        }

        public bool IsEmployeeListVisible
        {
            get => _isEmployeeListVisible;
            set
            {
                if (_isEmployeeListVisible != value)
                {
                    _isEmployeeListVisible = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsAllSelected
        {
            get => _isAllSelected;
            set
            {
                if (_isAllSelected != value)
                {
                    _isAllSelected = value;
                    OnPropertyChanged();

                    // Update all employee checkboxes that are not already checked in
                    foreach (var employee in Employees.Where(e => !e.IsCheckedIn))
                    {
                        employee.IsSelected = value;
                    }
                }
            }
        }

        public bool HasSelectedEmployees => Employees.Any(e => e.IsSelected);
        public bool HasCheckedInEmployees => Employees.Any(e => e.IsCheckedIn && !string.IsNullOrEmpty(e.AttendanceId));

        public DashboardPage()
            : this(null, null)
        {
            // This parameterless constructor is needed for navigation
        }

        public DashboardPage(IEmployeeService employeeService = null, IAttendanceService attendanceService = null)
        {
            InitializeComponent();
            
            // Initialize the services
            _employeeService = employeeService ?? new EmployeeService();
            _attendanceService = attendanceService ?? new AttendanceService();
            
            // Set the binding context to this for property binding
            this.BindingContext = this;
            
            SetupEventHandlers();
            UpdateTimeAndDate();
            
            // Load employee count and attendance stats when the page loads
            LoadEmployeeCountAsync();
            LoadAttendanceStatsAsync();
            
            // Setup sidebar component first
            sidebarComponent = this.FindByName<SidebarComponent>("SidebarComponent");
            
            // Ensure the Dashboard icon is active when this page loads
            if (sidebarComponent != null)
            {
                sidebarComponent.IsDashboardActive = true;
                sidebarComponent.IsHistoryActive = false;
                sidebarComponent.IsEmployeeDetailsActive = false;
            }
            
            // Wire up header component events
            _headerComponent = this.FindByName<HeaderComponent>("HeaderComponent");
            if (_headerComponent != null)
            {
                _headerComponent.CalendarClicked += OnCalendarClicked;
                _headerComponent.NotificationsClicked += OnNotificationsClicked;
            }
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            
            // Refresh data when page appears
            LoadEmployeeCountAsync();
            LoadAttendanceStatsAsync();
            
            // Update sidebar active states when page appears
            if (sidebarComponent != null)
            {
                sidebarComponent.IsDashboardActive = true;
                sidebarComponent.IsHistoryActive = false;
                sidebarComponent.IsEmployeeDetailsActive = false;
            }
        }

        // Method to load attendance statistics from the API
        private async Task LoadAttendanceStatsAsync()
        {
            try
            {
                if (_attendanceService == null)
                {
                    Console.WriteLine("ERROR: AttendanceService is null");
                    return;
                }

                Console.WriteLine("Loading attendance statistics from API...");
                var response = await _attendanceService.GetTodayAttendanceStatsAsync();
                
                if (response?.Success == true && response.Data != null)
                {
                    var stats = response.Data;
                    Console.WriteLine($"Retrieved attendance stats from API: Total={stats.TotalEmployees}, Arrived={stats.TotalArrived}, OnTime={stats.OnTime}, Late={stats.LateArrivals}, Absent={stats.Absent}, Early={stats.EarlyDeparture}");
                    
                    // Update the UI with the stats
                    MainThread.BeginInvokeOnMainThread(() => 
                    {
                        // Update total count (also done in LoadEmployeeCountAsync)
                        if (this.FindByName<Label>("TotalEmployeeCountLabel") is Label totalEmployeeLabel)
                        {
                            totalEmployeeLabel.Text = stats.TotalEmployees.ToString();
                        }
                        
                        // Update total attendance count
                        if (this.FindByName<Label>("TotalAttendanceCountLabel") is Label totalAttendanceLabel)
                        {
                            totalAttendanceLabel.Text = stats.TotalArrived.ToString();
                        }
                        
                        // Update on-time count
                        if (this.FindByName<Label>("OnTimeLabel") is Label onTimeLabel)
                        {
                            onTimeLabel.Text = stats.OnTime.ToString();
                        }
                        
                        // Update late arrivals count
                        if (this.FindByName<Label>("LateArrivalsLabel") is Label lateArrivalsLabel)
                        {
                            lateArrivalsLabel.Text = stats.LateArrivals.ToString();
                        }
                        
                        // Update early departure count
                        if (this.FindByName<Label>("EarlyDepartureLabel") is Label earlyDepartureLabel)
                        {
                            earlyDepartureLabel.Text = stats.EarlyDeparture.ToString();
                        }
                        
                        // Update absent count
                        if (this.FindByName<Label>("AbsentLabel") is Label absentLabel)
                        {
                            absentLabel.Text = stats.Absent.ToString();
                        }
                    });
                }
                else
                {
                    Console.WriteLine($"Failed to get attendance stats: {response?.Message ?? "Unknown error"}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading attendance stats: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        // Method to load employee count from API
        private async void LoadEmployeeCountAsync()
        {
            try
            {
                if (_employeeService == null)
                {
                    Console.WriteLine("ERROR: EmployeeService is null");
                    return;
                }

                Console.WriteLine("Loading employee count from API...");
                var response = await _employeeService.GetEmployeeCountAsync();
                
                if (response?.Success == true)
                {
                    int employeeCount = response.Data;
                    Console.WriteLine($"Employee count from API: {employeeCount}");
                    
                    // Update the UI with the employee count
                    MainThread.BeginInvokeOnMainThread(() => 
                    {
                        // Find all labels that display employee counts and update them
                        if (this.FindByName<Label>("TotalEmployeeCountLabel") is Label totalLabel)
                        {
                            totalLabel.Text = employeeCount.ToString();
                        }
                        
                        // No need to update TotalAttendanceCountLabel here as it's handled by LoadAttendanceStatsAsync
                    });
                }
                else
                {
                    Console.WriteLine($"Failed to get employee count: {response?.Message ?? "Unknown error"}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading employee count: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        private void SetupEventHandlers()
        {
            // Set up header component events
            if (this.FindByName<Components.HeaderComponent>("HeaderComponent") is Components.HeaderComponent header)
            {
                header.LogoutRequested += OnHeaderLogoutRequested;
            }

            // Wire up other events
            ViewSummaryButton.Clicked += OnViewSummaryClicked;
            AddEmployeesButton.Clicked += OnAddEmployeesClicked;
        }

        private void OnHeaderLogoutRequested(object sender, EventArgs e)
        {
            // The header component will handle clearing preferences and navigation
            // We just need to ensure it's allowed to proceed
        }

        private void UpdateTimeAndDate()
        {
            var now = DateTime.Now;
            DateLabel.Text = now.ToString("dd MMMM yyyy");
            TimeLabel.Text = now.ToString("h:mm:ss tt");

            // Update time every second
            timer = new System.Timers.Timer(1000);
            timer.Elapsed += (sender, e) =>
            {
                var time = DateTime.Now;
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    TimeLabel.Text = time.ToString("h:mm:ss tt");
                });
            };
            timer.Start();
        }

        private async void OnViewSummaryClicked(object sender, EventArgs e)
        {
            // Navigate to the Summary page
            await Navigation.PushAsync(new SummaryPage());
        }

        private async void OnAddEmployeesClicked(object sender, EventArgs e)
        {
            // Simply navigate to Employee Details page
            await Navigation.PushAsync(new EmployeeDetailsPage());
        }

        protected override bool OnBackButtonPressed()
        {
            // Prevent back navigation to login page
            return true;
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            
            // Clean up timer
            timer?.Stop();
            timer?.Dispose();
            
            // Unsubscribe from events
            if (_headerComponent != null)
            {
                _headerComponent.CalendarClicked -= OnCalendarClicked;
                _headerComponent.NotificationsClicked -= OnNotificationsClicked;
            }
        }

        private async Task OnEmployeeSelectionBorderTappedAsync()
        {
            if (!IsEmployeeListVisible)
            {
                // First time opening - load employee list
                await LoadEmployeeListAsync();
            }
            
            // Toggle visibility
            IsEmployeeListVisible = !IsEmployeeListVisible;
        }

        private async Task LoadEmployeeListAsync()
        {
            try
            {
                Console.WriteLine("Loading employee list for bulk actions...");
                
                // Clear existing list
                Employees.Clear();
                
                // Load all employees from the API
                var result = await _employeeService.GetAllEmployeesAsync(1, 100); // Get up to 100 employees
                
                if (result.Success && result.Data?.Items != null)
                {
                    var employees = result.Data.Items;
                    Console.WriteLine($"Retrieved {employees.Count} employees from API");
                    
                    // Create EmployeeSelectionItem for each employee
                    foreach (var employee in employees)
                    {
                        var selectionItem = new EmployeeSelectionItem
                        {
                            UserId = employee.UserId,
                            FullName = employee.EmployeeName,
                            Department = employee.Department,
                            Role = employee.Role,
                            IsSelected = false
                        };
                        
                        // Get current check-in status for this employee
                        await CheckEmployeeAttendanceStatusAsync(selectionItem);
                        
                        Employees.Add(selectionItem);
                    }
                    
                    // Update property for button enablement
                    OnPropertyChanged(nameof(HasSelectedEmployees));
                    OnPropertyChanged(nameof(HasCheckedInEmployees));
                }
                else
                {
                    Console.WriteLine($"Failed to get employees: {result?.Message ?? "Unknown error"}");
                    await DisplayAlert("Error", "Failed to load employee list. Please try again.", "OK");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading employee list: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                await DisplayAlert("Error", "An error occurred while loading employees. Please try again.", "OK");
            }
        }

        private async Task CheckEmployeeAttendanceStatusAsync(EmployeeSelectionItem employee)
        {
            try
            {
                // Get current check-in status for this employee
                var statusResult = await _attendanceService.GetCheckInCheckOutStatusAsync(employee.UserId);
                
                if (statusResult.Success && statusResult.Data != null)
                {
                    var status = statusResult.Data;
                    employee.IsCheckedIn = status.IsCheckedIn;
                    
                    if (status.IsCheckedIn)
                    {
                        employee.AttendanceId = status.CurrentAttendanceId;
                        employee.Status = status.IsCheckedOut ? "Checked Out" : "Checked In";
                    }
                    else
                    {
                        employee.Status = "Not Checked In";
                    }
                }
                else
                {
                    Console.WriteLine($"Failed to get status for employee {employee.UserId}: {statusResult?.Message ?? "Unknown error"}");
                    employee.Status = "Status Unknown";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting status for employee {employee.UserId}: {ex.Message}");
                employee.Status = "Status Error";
            }
        }

        private void OnSelectAllCheckBoxChanged(object sender, CheckedChangedEventArgs e)
        {
            // Handled by IsAllSelected property setter
        }

        private void OnEmployeeCheckBoxChanged(object sender, CheckedChangedEventArgs e)
        {
            OnPropertyChanged(nameof(HasSelectedEmployees));
        }

        private async void OnBulkSignInClicked(object sender, EventArgs e)
        {
            try
            {
                // Get selected employees that are not checked in
                var selectedEmployees = Employees.Where(emp => emp.IsSelected && !emp.IsCheckedIn).ToList();
                
                if (!selectedEmployees.Any())
                {
                    await DisplayAlert("No Selection", "Please select at least one employee to check in.", "OK");
                    return;
                }
                
                bool confirmed = await DisplayAlert("Confirm Check-In", 
                    $"Are you sure you want to check in {selectedEmployees.Count} employee(s)?", 
                    "Yes", "No");
                
                if (!confirmed) return;
                
                int successCount = 0;
                int failCount = 0;
                
                foreach (var employee in selectedEmployees)
                {
                    try
                    {
                        // Create check-in DTO with current time
                        var checkInDto = CreateAttendanceDto.CreateWithLocalTime();
                        
                        // Call API to check in
                        var result = await _attendanceService.CheckInAsync(employee.UserId, checkInDto);
                        
                        if (result.Success && result.Data != null)
                        {
                            // Update employee status
                            employee.IsCheckedIn = true;
                            employee.IsSelected = false;
                            employee.AttendanceId = result.Data.AttendanceId;
                            employee.Status = "Checked In";
                            successCount++;
                        }
                        else
                        {
                            Console.WriteLine($"Failed to check in employee {employee.UserId}: {result.Message}");
                            failCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error checking in employee {employee.UserId}: {ex.Message}");
                        failCount++;
                    }
                }
                
                // Update property for button enablement
                OnPropertyChanged(nameof(HasSelectedEmployees));
                OnPropertyChanged(nameof(HasCheckedInEmployees));
                
                // Show result
                string message = successCount > 0
                    ? $"Successfully checked in {successCount} employee(s)."
                    : "No employees were checked in.";
                
                if (failCount > 0)
                {
                    message += $" Failed to check in {failCount} employee(s).";
                }
                
                await DisplayAlert("Check-In Result", message, "OK");
                
                // Refresh attendance stats
                await LoadAttendanceStatsAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error performing bulk check-in: {ex.Message}");
                await DisplayAlert("Error", "An error occurred during the check-in process.", "OK");
            }
        }

        private async void OnBulkSignOutClicked(object sender, EventArgs e)
        {
            try
            {
                // Get employees that are checked in
                var checkedInEmployees = Employees.Where(emp => emp.IsCheckedIn && !string.IsNullOrEmpty(emp.AttendanceId)).ToList();
                
                if (!checkedInEmployees.Any())
                {
                    await DisplayAlert("No Check-Ins", "There are no employees checked in to check out.", "OK");
                    return;
                }
                
                bool confirmed = await DisplayAlert("Confirm Check-Out", 
                    $"Are you sure you want to check out {checkedInEmployees.Count} employee(s)?", 
                    "Yes", "No");
                
                if (!confirmed) return;
                
                int successCount = 0;
                int failCount = 0;
                
                foreach (var employee in checkedInEmployees)
                {
                    try
                    {
                        // Only check out if we have an attendance ID
                        if (string.IsNullOrEmpty(employee.AttendanceId))
                        {
                            failCount++;
                            continue;
                        }
                        
                        // Create check-out DTO with current time
                        var checkoutDto = UpdateAttendanceDto.CreateWithLocalTime(DateTime.Today);
                        
                        // Call API to check out
                        var result = await _attendanceService.CheckOutAsync(employee.UserId, employee.AttendanceId, checkoutDto);
                        
                        if (result.Success && result.Data != null)
                        {
                            // Update employee status
                            employee.IsCheckedIn = true; // Still checked in but with check-out time
                            employee.Status = "Checked Out";
                            successCount++;
                        }
                        else
                        {
                            Console.WriteLine($"Failed to check out employee {employee.UserId}: {result.Message}");
                            failCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error checking out employee {employee.UserId}: {ex.Message}");
                        failCount++;
                    }
                }
                
                // Update property for button enablement
                OnPropertyChanged(nameof(HasCheckedInEmployees));
                
                // Show result
                string message = successCount > 0
                    ? $"Successfully checked out {successCount} employee(s)."
                    : "No employees were checked out.";
                
                if (failCount > 0)
                {
                    message += $" Failed to check out {failCount} employee(s).";
                }
                
                await DisplayAlert("Check-Out Result", message, "OK");
                
                // Refresh attendance stats
                await LoadAttendanceStatsAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error performing bulk check-out: {ex.Message}");
                await DisplayAlert("Error", "An error occurred during the check-out process.", "OK");
            }
        }

        private async void OnIndividualCheckOutClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is string userId)
            {
                var employee = Employees.FirstOrDefault(emp => emp.UserId == userId);
                
                if (employee == null || !employee.IsCheckedIn || string.IsNullOrEmpty(employee.AttendanceId))
                {
                    await DisplayAlert("Error", "Cannot check out this employee. Invalid state.", "OK");
                    return;
                }
                
                bool confirmed = await DisplayAlert("Confirm Individual Check-Out", 
                    $"Are you sure you want to check out {employee.FullName}?", 
                    "Yes", "No");
                
                if (!confirmed) return;
                
                try
                {
                    // Create check-out DTO with current time
                    var checkoutDto = UpdateAttendanceDto.CreateWithLocalTime(DateTime.Today);
                    
                    // Call API to check out
                    var result = await _attendanceService.CheckOutAsync(employee.UserId, employee.AttendanceId, checkoutDto);
                    
                    if (result.Success && result.Data != null)
                    {
                        // Update employee status
                        employee.Status = "Checked Out";
                        await DisplayAlert("Success", $"{employee.FullName} has been checked out successfully.", "OK");
                        
                        // Refresh attendance stats
                        await LoadAttendanceStatsAsync();
                    }
                    else
                    {
                        Console.WriteLine($"Failed to check out employee {employee.UserId}: {result.Message}");
                        await DisplayAlert("Error", $"Failed to check out {employee.FullName}: {result.Message}", "OK");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error checking out employee {employee.UserId}: {ex.Message}");
                    await DisplayAlert("Error", "An error occurred during the check-out process.", "OK");
                }
            }
        }

        // INotifyPropertyChanged implementation
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // Handle calendar clicked event 
        private async void OnCalendarClicked(object sender, EventArgs e)
        {
            try
            {
                Console.WriteLine("DashboardPage: Calendar icon clicked - showing calendar popup");
                await Application.Current.MainPage.DisplayAlert("Calendar", 
                    "Calendar functionality is now integrated in the header popup.", "OK");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error showing calendar alert: {ex.Message}");
                await DisplayAlert("Error", "Could not open calendar.", "OK");
            }
        }

        // Handle notifications clicked event
        private async void OnNotificationsClicked(object sender, EventArgs e)
        {
            try
            {
                Console.WriteLine("DashboardPage: Notification icon clicked - showing notifications popup");
                await Application.Current.MainPage.DisplayAlert("Notifications", 
                    "Notifications are now integrated in the header popup.", "OK");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error showing notifications alert: {ex.Message}");
                await DisplayAlert("Error", "Could not open notifications.", "OK");
            }
        }

        // Find and modify methods referencing CalendarPage or NotificationsPage
        private void OnCalendarIconTapped(object sender, EventArgs e)
        {
            try
            {
                // Instead of navigating to a separate page
                Application.Current.MainPage.DisplayAlert("Calendar", 
                    "Calendar functionality is now integrated in the header popup.", "OK");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error displaying calendar alert: {ex.Message}");
            }
        }

        private void OnNotificationsIconTapped(object sender, EventArgs e)
        {
            try
            {
                // Instead of navigating to a separate page
                Application.Current.MainPage.DisplayAlert("Notifications", 
                    "Notifications are now integrated in the header popup.", "OK");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error displaying notifications alert: {ex.Message}");
            }
        }
    }
} 

