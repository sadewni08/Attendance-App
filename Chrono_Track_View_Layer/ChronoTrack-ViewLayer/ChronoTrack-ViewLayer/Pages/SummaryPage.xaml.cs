using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using ChronoTrack_ViewLayer.Services;
using ChronoTrack_ViewLayer.Models;
using System.Globalization;
using Microsoft.Maui.Controls;
using System.Linq;
using System.Collections.Generic;
using ChronoTrack_ViewLayer.Services.Interfaces;
using ChronoTrack_ViewLayer.Extensions;
using ChronoTrack_ViewLayer.Components;
using Microsoft.Maui.Graphics;
using System.Text;
using System.IO;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.ApplicationModel.DataTransfer;
using Microsoft.Maui.Storage;

namespace ChronoTrack_ViewLayer.Pages;

public partial class SummaryPage : ContentPage, INotifyPropertyChanged, IFindSidebarProvider
{
    private readonly IAttendanceService _attendanceService;
    private readonly IEmployeeService _employeeService;

    private DateTime _startDate = DateTime.Today.AddDays(-7);
    public DateTime StartDate
    {
        get => _startDate;
        set
        {
            if (_startDate != value)
            {
                _startDate = value;
                OnPropertyChanged();
                LoadAttendanceDataAsync();
            }
        }
    }

    private DateTime _endDate = DateTime.Today;
    public DateTime EndDate
    {
        get => _endDate;
        set
        {
            if (_endDate != value)
            {
                _endDate = value;
                OnPropertyChanged();
                LoadAttendanceDataAsync();
            }
        }
    }

    private ObservableCollection<string> _employees;
    public ObservableCollection<string> Employees
    {
        get => _employees;
        set
        {
            if (_employees != value)
            {
                _employees = value;
                OnPropertyChanged();
            }
        }
    }

    private string _selectedEmployee;
    public string SelectedEmployee
    {
        get => _selectedEmployee;
        set
        {
            if (_selectedEmployee != value)
            {
                _selectedEmployee = value;
                OnPropertyChanged();
                LoadAttendanceDataAsync();
            }
        }
    }

    // Statistics properties
    private int _totalEmployees;
    public int TotalEmployees
    {
        get => _totalEmployees;
        set
        {
            if (_totalEmployees != value)
            {
                _totalEmployees = value;
                OnPropertyChanged();
            }
        }
    }

    private int _presentEmployees;
    public int PresentEmployees
    {
        get => _presentEmployees;
        set
        {
            if (_presentEmployees != value)
            {
                _presentEmployees = value;
                OnPropertyChanged();
            }
        }
    }

    private int _lateEmployees;
    public int LateEmployees
    {
        get => _lateEmployees;
        set
        {
            if (_lateEmployees != value)
            {
                _lateEmployees = value;
                OnPropertyChanged();
            }
        }
    }

    private int _activeSessions;
    public int ActiveSessions
    {
        get => _activeSessions;
        set
        {
            if (_activeSessions != value)
            {
                _activeSessions = value;
                OnPropertyChanged();
            }
        }
    }

    private string _averageWorkingHours = "0.0";
    public string AverageWorkingHours
    {
        get => _averageWorkingHours;
        set
        {
            if (_averageWorkingHours != value)
            {
                _averageWorkingHours = value;
                OnPropertyChanged();
            }
        }
    }

    private ObservableCollection<AttendanceActivityViewModel> _allActivities;
    private ObservableCollection<AttendanceActivityViewModel> _currentPageActivities;
    private int _currentPage = 1;
    private const int ItemsPerPage = 10;
    private int _totalItems = 0;
    private int _totalPages = 1;
    private bool _isLoading = false;

    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            if (_isLoading != value)
            {
                _isLoading = value;
                OnPropertyChanged();
            }
        }
    }

    // Chart view properties and related fields have been removed

    public SummaryPage()
    {
        try
        {
            InitializeComponent();

            // Initialize services - using our new extension method
            _attendanceService = ChronoTrack_ViewLayer.Services.ServiceProviderExtensions.GetService<IAttendanceService>();
            _employeeService = ChronoTrack_ViewLayer.Services.ServiceProviderExtensions.GetService<IEmployeeService>();

            if (_attendanceService == null)
            {
                Console.WriteLine("CRITICAL ERROR: AttendanceService could not be initialized.");

                // Fallback - attempt to use direct instantiation if DI fails
                try
                {
                    // Check if you have a concrete implementation that can be created directly
                    var attendanceServiceType = Type.GetType("ChronoTrack_ViewLayer.Services.AttendanceService, ChronoTrack-ViewLayer");
                    if (attendanceServiceType != null)
                    {
                        _attendanceService = Activator.CreateInstance(attendanceServiceType) as IAttendanceService;
                        Console.WriteLine($"Created attendance service directly: {_attendanceService != null}");
                    }
                }
                catch (Exception fallbackEx)
                {
                    Console.WriteLine($"Fallback instantiation failed: {fallbackEx.Message}");
                }
            }

            if (_employeeService == null)
            {
                Console.WriteLine("CRITICAL ERROR: EmployeeService could not be initialized.");
                
                // Fallback creation attempt
                try
                {
                    var employeeServiceType = Type.GetType("ChronoTrack_ViewLayer.Services.EmployeeService, ChronoTrack-ViewLayer");
                    if (employeeServiceType != null)
                    {
                        _employeeService = Activator.CreateInstance(employeeServiceType) as IEmployeeService;
                        Console.WriteLine($"Created employee service directly: {_employeeService != null}");
                    }
                }
                catch (Exception fallbackEx)
                {
                    Console.WriteLine($"Fallback instantiation failed: {fallbackEx.Message}");
                }
            }

            _allActivities = new ObservableCollection<AttendanceActivityViewModel>();
            _currentPageActivities = new ObservableCollection<AttendanceActivityViewModel>();
            _employees = new ObservableCollection<string>();

            BindingContext = this;
            ActivitiesCollection.ItemsSource = _currentPageActivities;

            // Initialize date pickers
            StartDatePicker.Date = _startDate;
            EndDatePicker.Date = _endDate;

            // Initialize pagination buttons
            InitializePaginationButtons();

            LoadInitialDataAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in SummaryPage constructor: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }

    protected override async void OnAppearing()
    {
        try
        {
            base.OnAppearing();

            // Add event handlers for date changes
            if (StartDatePicker != null)
            {
                StartDatePicker.DateSelected += OnStartDateSelected;
            }

            if (EndDatePicker != null)
            {
                EndDatePicker.DateSelected += OnEndDateSelected;
            }

            // Add event handler for employee selection
            if (EmployeePicker != null)
            {
                EmployeePicker.SelectedIndexChanged += OnEmployeeSelected;
            }

            // Safely determine the route with proper null checking
            bool isAttendanceDetailsView = false;
            if (Shell.Current != null && Shell.Current.CurrentState != null &&
                Shell.Current.CurrentState.Location != null &&
                !string.IsNullOrEmpty(Shell.Current.CurrentState.Location.OriginalString))
            {
                string currentRoute = Shell.Current.CurrentState.Location.OriginalString;
                isAttendanceDetailsView = currentRoute.Contains("AttendanceDetailsPage");
            }

            // Update sidebar active states when page appears
            if (SidebarComponent != null)
            {
                SidebarComponent.IsDashboardActive = false;

                // Set the appropriate active state based on how the page was accessed
                if (isAttendanceDetailsView)
                {
                    SidebarComponent.IsHistoryActive = false;
                    SidebarComponent.IsAttendanceDetailsActive = true;
                }
                else
                {
                    SidebarComponent.IsHistoryActive = true;
                    SidebarComponent.IsAttendanceDetailsActive = false;
                }

                SidebarComponent.IsEmployeeDetailsActive = false;
                SidebarComponent.IsReportsActive = false;
                SidebarComponent.IsSettingsActive = false;
            }

            await LoadAttendanceDataAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in OnAppearing: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        // Remove event handlers to prevent memory leaks
        if (StartDatePicker != null)
        {
            StartDatePicker.DateSelected -= OnStartDateSelected;
        }

        if (EndDatePicker != null)
        {
            EndDatePicker.DateSelected -= OnEndDateSelected;
        }

        if (EmployeePicker != null)
        {
            EmployeePicker.SelectedIndexChanged -= OnEmployeeSelected;
        }
    }

    private async void OnStartDateSelected(object sender, DateChangedEventArgs e)
    {
        StartDate = e.NewDate;
        await LoadAttendanceDataAsync();
    }

    private async void OnEndDateSelected(object sender, DateChangedEventArgs e)
    {
        EndDate = e.NewDate;
        await LoadAttendanceDataAsync();
    }

    private async void OnEmployeeSelected(object sender, EventArgs e)
    {
        if (sender is Picker picker)
        {
            SelectedEmployee = picker.SelectedItem as string;
            await LoadAttendanceDataAsync();
        }
    }

    private async void OnExportClicked(object sender, EventArgs e)
    {
        try
        {
            // Confirm export action
            bool confirmExport = await DisplayAlert("Export Report", 
                $"Export attendance report for {(SelectedEmployee == "All Employees" ? "all employees" : SelectedEmployee)} from {StartDate:MMM dd, yyyy} to {EndDate:MMM dd, yyyy}?", 
                "Yes", "No");
                
            if (!confirmExport)
                return;
                
            // Ensure _allActivities is initialized
            if (_allActivities == null)
            {
                _allActivities = new ObservableCollection<AttendanceActivityViewModel>();
            }
            
            // Check if we have data to export
            if (_allActivities.Count == 0)
            {
                // No data available, try to load it
                await LoadAttendanceDataAsync();
                
                // Check again after loading
                if (_allActivities.Count == 0)
                {
                    await DisplayAlert("Export Error", "No attendance data available for the selected criteria.", "OK");
                    return;
                }
            }
            
            // Create CSV content with headers
            var csvContent = new StringBuilder();
            csvContent.AppendLine("ID,Employee Name,Department,Role,Date,Status,Check In,Check Out,Working Hours");
            
            // Add each attendance record as a row in the CSV
            foreach (var activity in _allActivities)
            {
                // Escape any commas in fields by enclosing in double quotes
                string employeeName = activity.EmployeeName ?? "";
                if (employeeName.Contains(",")) employeeName = $"\"{employeeName}\"";
                
                string department = activity.Department ?? "";
                if (department.Contains(",")) department = $"\"{department}\"";
                
                string role = activity.Role ?? "";
                if (role.Contains(",")) role = $"\"{role}\"";
                
                string status = activity.Status ?? "";
                if (status.Contains(",")) status = $"\"{status}\"";
                
                // Append activity data as CSV row
                csvContent.AppendLine($"{activity.Id},{employeeName},{department},{role},{activity.Date},{status},{activity.CheckIn},{activity.CheckOut},{activity.WorkingHours}");
            }
            
            // Format filename with date range and employee info
            string dateRange = $"{StartDate:yyyyMMdd}_to_{EndDate:yyyyMMdd}";
            string employeeInfo = SelectedEmployee == "All Employees" ? "AllEmployees" : SelectedEmployee.Replace(" ", "");
            string fileName = $"AttendanceReport_{dateRange}_{employeeInfo}.csv";
            
            // Get the file path for saving the CSV
            string filePath = Path.Combine(FileSystem.CacheDirectory, fileName);
            
            // Write CSV content to file
            File.WriteAllText(filePath, csvContent.ToString());
            
            // Share the file
            await Share.RequestAsync(new ShareFileRequest
            {
                Title = "Attendance Report Export",
                File = new ShareFile(filePath)
            });
            
            Console.WriteLine($"CSV file exported successfully: {filePath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error exporting attendance report: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            await DisplayAlert("Export Error", $"Failed to export report: {ex.Message}", "OK");
        }
    }

    // Helper method to load initial data asynchronously
    private async void LoadDataAsync()
    {
        try 
        {
            await LoadInitialDataAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in LoadDataAsync: {ex.Message}");
        }
    }

    private async Task LoadEmployeeListAsync()
    {
        try
        {
            // Get all employees
            var result = await _employeeService.GetAllEmployeesAsync(1, 100);
            if (result.Success && result.Data != null)
            {
                // Add "All Employees" option
                _employees.Clear();
                _employees.Add("All Employees");

                // Add employee names
                foreach (var employee in result.Data.Items)
                {
                    _employees.Add(employee.EmployeeName);
                }

                // Set default selection
                if (_employees.Count > 0)
                {
                    SelectedEmployee = _employees[0]; // All Employees
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading employee list: {ex.Message}");
        }
    }

    private async Task LoadAttendanceDataAsync()
    {
        try
        {
            // Check if services were properly initialized
            if (_attendanceService == null)
            {
                Console.WriteLine("ERROR: AttendanceService is null");
                await DisplayAlert("Error", "Attendance service not available", "OK");
                return;
            }

            IsLoading = true;
            _allActivities.Clear();

            // Reset to first page and statistics
            _currentPage = 1;
            ResetStatistics();

            // Get total employee count from API
            var employeeCountResponse = await _employeeService.GetEmployeeCountAsync();
            if (employeeCountResponse?.Success == true)
            {
                TotalEmployees = employeeCountResponse.Data;
                Console.WriteLine($"Retrieved total employee count from API: {TotalEmployees}");
            }
            else
            {
                Console.WriteLine($"Failed to get employee count: {employeeCountResponse?.Message ?? "Unknown error"}");
                // Keep current value or set to 0 if none
                TotalEmployees = 0;
            }

            // Make sure dates are in correct order
            if (_endDate < _startDate)
            {
                var temp = _endDate;
                _endDate = _startDate;
                _startDate = temp;
            }

            Console.WriteLine($"Loading attendance data for date range: {_startDate:yyyy-MM-dd} to {_endDate:yyyy-MM-dd}");

            // Get detailed attendance data for all employees or filtered by employee
            string userId = null;
            string employeeName = null;

            // If we filter by a specific employee
            if (!string.IsNullOrEmpty(SelectedEmployee) && SelectedEmployee != "All Employees")
            {
                employeeName = SelectedEmployee;

                try
                {
                    // Try to get the userId for the selected employee
                    if (_employeeService != null)
                    {
                        var employeeResult = await _employeeService.GetAllEmployeesAsync(1, 100);
                        if (employeeResult?.Success == true && employeeResult.Data != null)
                        {
                            var employee = employeeResult.Data.Items.FirstOrDefault(e => e.EmployeeName == SelectedEmployee);
                            if (employee != null)
                            {
                                userId = employee.UserId;
                                Console.WriteLine($"Found userId: {userId} for employee: {SelectedEmployee}");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error getting employee ID: {ex.Message}");
                }
            }

            // Use the detailed attendance API to get all the data we need
            var response = await _attendanceService.GetDetailedAttendanceAsync(
                userId: userId,
                employeeName: employeeName,
                startDate: _startDate,
                endDate: _endDate.AddDays(1).AddSeconds(-1), // Include all of end date
                pageSize: 100 // Get more items at once
            );

            if (response?.Success == true && response.Data != null && response.Data.Items != null)
            {
                Console.WriteLine($"Received {response.Data.Items.Count} attendance records");

                foreach (var item in response.Data.Items)
                {
                    try
                    {
                        // Create the view model to display
                        var activity = new AttendanceActivityViewModel
                        {
                            Id = item.Id.ToString().Substring(0, 8),
                            EmployeeName = item.UserFullName,
                            Role = item.UserRole,
                            Department = item.Department,
                            Date = item.FormattedDate,
                            Status = item.Status,
                            StatusColor = GetStatusColor(item.Status),
                            CheckIn = item.FormattedCheckInTime,
                            CheckOut = item.FormattedCheckOutTime,
                            WorkingHours = item.FormattedDuration
                        };

                        _allActivities.Add(activity);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error processing attendance item: {ex.Message}");
                    }
                }

                // Calculate statistics - don't update TotalEmployees as it comes from the API
                CalculateStatistics();

                // Update the current page
                UpdatePage(1);
            }
            else
            {
                Console.WriteLine($"Failed to get attendance data: {response?.Message ?? "Unknown error"}");
                // Don't reset TotalEmployees as it was set from the API earlier
                PresentEmployees = 0;
                LateEmployees = 0;
                ActiveSessions = 0;
                AverageWorkingHours = "0.0";
                
                if (response != null && !response.Success)
                {
                    Console.WriteLine($"API Error: {response.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading attendance data: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private string GetStatusColor(string status)
    {
        return status switch
        {
            "Present" => "#059669", // Green
            "Late" => "#DC2626",    // Red
            "Active" => "#0066FF",  // Blue
            _ => "#6B7280"          // Gray for unknown status
        };
    }

    private void UpdatePage(int pageNumber)
    {
        if (_allActivities.Count == 0)
        {
            _currentPageActivities.Clear();
            _totalItems = 0;
            _totalPages = 0;
            UpdatePaginationUI(1);
            return;
        }

        // Update total items and pages
        _totalItems = _allActivities.Count;
        _totalPages = (int)Math.Ceiling(_totalItems / (double)ItemsPerPage);

        // Ensure page number is within bounds
        if (pageNumber < 1)
            pageNumber = 1;
        if (pageNumber > _totalPages)
            pageNumber = _totalPages;

        _currentPage = pageNumber;

        // Calculate start and end index for the current page
        int startIndex = (pageNumber - 1) * ItemsPerPage;
        int endIndex = Math.Min(startIndex + ItemsPerPage, _allActivities.Count);

        // Update the current page items
        _currentPageActivities.Clear();
        for (int i = startIndex; i < endIndex; i++)
        {
            _currentPageActivities.Add(_allActivities[i]);
        }

        // Update pagination UI
        UpdatePaginationUI(pageNumber);
    }

    private void ResetStatistics()
    {
        // We don't reset TotalEmployees here anymore as it comes from the API
        PresentEmployees = 0;
        LateEmployees = 0;
        ActiveSessions = 0;
        AverageWorkingHours = "0.0";
    }

    private void CalculateStatistics()
    {
        if (_allActivities.Count == 0)
        {
            // Don't reset TotalEmployees as it comes from the API
            PresentEmployees = 0;
            LateEmployees = 0;
            ActiveSessions = 0;
            AverageWorkingHours = "0.0";
            return;
        }

        // Don't count unique employees anymore as we get the count from the API
        // TotalEmployees is now set from the API

        // Count by status
        PresentEmployees = _allActivities.Count(a => a.Status == "Present");
        LateEmployees = _allActivities.Count(a => a.Status == "Late");
        ActiveSessions = _allActivities.Count(a => a.Status == "Active");

        // Calculate average working hours
        var hoursSum = _allActivities.Sum(a => double.TryParse(a.WorkingHours, out double hours) ? hours : 0);
        var avgHours = _allActivities.Count > 0 ? hoursSum / _allActivities.Count : 0;
        AverageWorkingHours = avgHours.ToString("F1");
    }

    private void UpdatePaginationUI(int pageNumber)
    {
        _currentPage = pageNumber;

        // Update binding context for pagination text and statistics
        BindingContext = new
        {
            StartDate = _startDate,
            EndDate = _endDate,
            Employees = _employees,
            SelectedEmployee = _selectedEmployee,
            IsLoading = _isLoading,
            CurrentPage = _currentPage,
            StartItem = ((_currentPage - 1) * ItemsPerPage) + 1,
            EndItem = Math.Min(_currentPage * ItemsPerPage, _totalItems),
            TotalItems = _totalItems,

            // Statistics
            TotalEmployees = _totalEmployees,
            PresentEmployees = _presentEmployees,
            LateEmployees = _lateEmployees,
            ActiveSessions = _activeSessions,
            AverageWorkingHours = _averageWorkingHours
        };

        // Update pagination buttons
        UpdatePaginationButtons();
    }

    // Button click handlers for pagination
    private async void OnPreviousClicked(object sender, EventArgs e)
    {
        if (_currentPage > 1)
        {
            UpdatePage(_currentPage - 1);
        }
    }

    private async void OnNextClicked(object sender, EventArgs e)
    {
        if (_currentPage < _totalPages)
        {
            UpdatePage(_currentPage + 1);
        }
    }

    private async void OnPageNumberClicked(object sender, EventArgs e)
    {
        if (sender is Button button && int.TryParse(button.Text, out int pageNumber))
        {
            UpdatePage(pageNumber);
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private void InitializePaginationButtons()
    {
        var paginationLayout = this.FindByName<HorizontalStackLayout>("PaginationContainer");
        if (paginationLayout == null)
        {
            Console.WriteLine("ERROR: PaginationContainer not found in XAML");
            return;
        }

        // Clear existing pagination buttons
        paginationLayout.Children.Clear();

        // Add "Previous" button
        var prevButton = new Button
        {
            Text = "Previous",
            BackgroundColor = Color.FromArgb("#F3F4F6"),
            TextColor = Color.FromArgb("#374151"),
            CornerRadius = 6,
            Padding = new Thickness(12, 0),
            HeightRequest = 36
        };
        prevButton.Clicked += OnPreviousClicked;
        paginationLayout.Children.Add(prevButton);

        // Add page number buttons (initially just button 1)
        var pageButton = new Button
        {
            Text = "1",
            BackgroundColor = Color.FromArgb("#0066FF"),
            TextColor = Colors.White,
            CornerRadius = 6,
            Padding = new Thickness(12, 0),
            HeightRequest = 36,
            WidthRequest = 36
        };
        pageButton.Clicked += OnPageNumberClicked;
        paginationLayout.Children.Add(pageButton);

        // Add "Next" button
        var nextButton = new Button
        {
            Text = "Next",
            BackgroundColor = Color.FromArgb("#F3F4F6"),
            TextColor = Color.FromArgb("#374151"),
            CornerRadius = 6,
            Padding = new Thickness(12, 0),
            HeightRequest = 36
        };
        nextButton.Clicked += OnNextClicked;
        paginationLayout.Children.Add(nextButton);
    }

    private void UpdatePaginationButtons()
    {
        var paginationLayout = this.FindByName<HorizontalStackLayout>("PaginationContainer");
        if (paginationLayout == null)
        {
            Console.WriteLine("ERROR: PaginationContainer not found in XAML");
            return;
        }

        paginationLayout.Children.Clear();

        // Add "Previous" button
        var prevButton = new Button
        {
            Text = "Previous",
            BackgroundColor = Color.FromArgb("#F3F4F6"),
            TextColor = Color.FromArgb("#374151"),
            CornerRadius = 6,
            Padding = new Thickness(12, 0),
            HeightRequest = 36
        };
        prevButton.Clicked += OnPreviousClicked;
        paginationLayout.Children.Add(prevButton);

        // For simplicity, show up to 5 page buttons with current page in the middle when possible
        int startPage = Math.Max(1, _currentPage - 2);
        int endPage = Math.Min(_totalPages, startPage + 4);

        // Adjust startPage if we're near the end to always show 5 pages when possible
        if (_totalPages >= 5 && endPage == _totalPages)
        {
            startPage = Math.Max(1, _totalPages - 4);
        }

        // Add page number buttons
        for (int i = startPage; i <= endPage; i++)
        {
            var pageButton = new Button
            {
                Text = i.ToString(),
                BackgroundColor = (i == _currentPage) ? Color.FromArgb("#0066FF") : Color.FromArgb("#F3F4F6"),
                TextColor = (i == _currentPage) ? Colors.White : Color.FromArgb("#374151"),
                CornerRadius = 6,
                HeightRequest = 36,
                WidthRequest = 36
            };
            pageButton.Clicked += OnPageNumberClicked;
            paginationLayout.Children.Add(pageButton);
        }

        // Add "Next" button
        var nextButton = new Button
        {
            Text = "Next",
            BackgroundColor = Color.FromArgb("#F3F4F6"),
            TextColor = Color.FromArgb("#374151"),
            CornerRadius = 6,
            Padding = new Thickness(12, 0),
            HeightRequest = 36
        };
        nextButton.Clicked += OnNextClicked;
        paginationLayout.Children.Add(nextButton);
    }

    // Implement the IFindSidebarProvider interface
    public SidebarComponent GetSidebar()
    {
        return SidebarComponent;
    }

    private async Task LoadInitialDataAsync()
    {
        try
        {
            await LoadEmployeeListAsync();
            
            // Load today's attendance first for admin view
            await LoadTodayAttendanceAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading initial data: {ex.Message}");
            await DisplayAlert("Error", "Failed to load initial data.", "OK");
        }
    }

    // Method to load today's attendance for the admin dashboard
    private async Task LoadTodayAttendanceAsync()
    {
        try
        {
            if (_attendanceService == null)
            {
                Console.WriteLine("ERROR: AttendanceService is null");
                await DisplayAlert("Error", "Service not available", "OK");
                return;
            }
            
            IsLoading = true;
            Console.WriteLine("Loading today's attendance data...");
            
            // Set date range for today only
            DateTime today = DateTime.Today;
            DateTime tomorrow = today.AddDays(1);
            
            Console.WriteLine($"Date range for today's attendance: {today:yyyy-MM-dd} to {tomorrow:yyyy-MM-dd}");

            // Get total employee count from API
            var employeeCountResponse = await _employeeService.GetEmployeeCountAsync();
            if (employeeCountResponse?.Success == true)
            {
                TotalEmployees = employeeCountResponse.Data;
                Console.WriteLine($"Retrieved total employee count from API: {TotalEmployees}");
            }
            else
            {
                Console.WriteLine($"Failed to get employee count: {employeeCountResponse?.Message ?? "Unknown error"}");
                // Keep current value or set to 0 if none
                TotalEmployees = 0;
            }
            
            // Call the detailed attendance API for today's data
            var response = await _attendanceService.GetDetailedAttendanceAsync(
                startDate: today,
                endDate: tomorrow,
                pageSize: 100 // Enough for most workplaces for a single day
            );
            
            if (response?.Success == true && response.Data != null)
            {
                Console.WriteLine($"Received {response.Data.Items?.Count ?? 0} attendance records for today");
                
                // Update statistics
                if (response.Data.Items != null)
                {
                    // Count by status
                    PresentEmployees = response.Data.Items.Count(a => a.Status == "Present");
                    LateEmployees = response.Data.Items.Count(a => a.Status == "Late");
                    ActiveSessions = response.Data.Items.Count(a => a.Status == "Active");
                    
                    // Calculate average working hours for completed records
                    var completedRecords = response.Data.Items.Where(a => a.Status == "Present").ToList();
                    double avgHours = 0;
                    if (completedRecords.Count > 0)
                    {
                        double totalHours = 0;
                        foreach (var record in completedRecords)
                        {
                            TimeSpan duration = record.CheckOutTime - record.CheckInTime;
                            totalHours += duration.TotalHours;
                        }
                        avgHours = totalHours / completedRecords.Count;
                    }
                    AverageWorkingHours = avgHours.ToString("F1");
                    
                    // Create view models for the collection view
                    _allActivities.Clear();
                    foreach (var item in response.Data.Items)
                    {
                        var activity = new AttendanceActivityViewModel
                        {
                            Id = item.Id.ToString().Substring(0, 8),
                            EmployeeName = item.UserFullName,
                            Role = item.UserRole,
                            Department = item.Department,
                            Date = item.FormattedDate,
                            Status = item.Status,
                            StatusColor = GetStatusColor(item.Status),
                            CheckIn = item.FormattedCheckInTime,
                            CheckOut = item.FormattedCheckOutTime,
                            WorkingHours = item.FormattedDuration
                        };
                        
                        _allActivities.Add(activity);
                    }
                    
                    // Update the current page
                    UpdatePage(1);
                }
                else
                {
                    Console.WriteLine("No items found in the response");
                    // Don't reset TotalEmployees since we got it from the API
                    PresentEmployees = 0;
                    LateEmployees = 0;
                    ActiveSessions = 0;
                    AverageWorkingHours = "0.0";
                }
            }
            else
            {
                Console.WriteLine($"Failed to get attendance data: {response?.Message ?? "Unknown error"}");
                // Don't reset TotalEmployees since we got it from the API
                PresentEmployees = 0;
                LateEmployees = 0;
                ActiveSessions = 0;
                AverageWorkingHours = "0.0";
                await DisplayAlert("Error", $"Failed to get attendance records: {response?.Message ?? "Unknown error"}", "OK");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading today's attendance: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            // Don't reset TotalEmployees since we got it from the API earlier
            PresentEmployees = 0;
            LateEmployees = 0;
            ActiveSessions = 0;
            AverageWorkingHours = "0.0";
            await DisplayAlert("Error", $"An error occurred: {ex.Message}", "OK");
        }
        finally
        {
            IsLoading = false;
        }
    }
}