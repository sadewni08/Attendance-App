using Microsoft.Maui.Controls;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.ComponentModel;
using System.Linq;
using System.Collections.Generic;
using ChronoTrack_ViewLayer.Services;
using ChronoTrack_ViewLayer.Models;
using Microsoft.Maui.ApplicationModel;

namespace ChronoTrack_ViewLayer.Pages.User
{
    public partial class UserHistoryPage : ContentPage
    {
        private List<HistoryItem> _historyItems;
        private bool _showFullDay = true;
        private const double MOBILE_WIDTH = 768;
        private const double TABLET_WIDTH = 1024;
        private readonly AttendanceService _attendanceService;
        private string _currentUserId;
        
        // Pagination properties
        private int _currentPage = 1;
        private int _itemsPerPage = 10;
        private int _totalItems = 0;
        private int _startItem = 1;
        private int _endItem = 10;

        public int StartItem
        {
            get => _startItem;
            set
            {
                if (_startItem != value)
                {
                    _startItem = value;
                    OnPropertyChanged(nameof(StartItem));
                }
            }
        }

        public int EndItem
        {
            get => _endItem;
            set
            {
                if (_endItem != value)
                {
                    _endItem = value;
                    OnPropertyChanged(nameof(EndItem));
                }
            }
        }

        public int TotalItems
        {
            get => _totalItems;
            set
            {
                if (_totalItems != value)
                {
                    _totalItems = value;
                    OnPropertyChanged(nameof(TotalItems));
                }
            }
        }

        public UserHistoryPage()
        {
            try
            {
                InitializeComponent();
                
                // Subscribe to unhandled exceptions
                AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
                {
                    Console.WriteLine($"Unhandled exception: {e.ExceptionObject}");
                };
                
                // Initialize services and data
                _attendanceService = new AttendanceService();
                _currentUserId = Preferences.Get("UserId", string.Empty);
                _historyItems = new List<HistoryItem>();
                
                // Set binding context before loading data
                BindingContext = this;
                
                // Load data asynchronously
                LoadHistoryDataAsync().ConfigureAwait(false);
                
                Console.WriteLine("UserHistoryPage initialized successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing UserHistoryPage: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        private async Task LoadHistoryDataAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(_currentUserId))
                {
                    Console.WriteLine("User ID is empty, cannot load attendance history");
                    await DisplayAlert("Error", "User ID not found. Please log in again.", "OK");
                    return;
                }

                Console.WriteLine($"Loading attendance history for user: {_currentUserId}");

                // Get current date for filtering
                var today = DateTime.UtcNow.Date;
                var startDate = new DateTime(today.Year, today.Month, 1); // Start of current month
                var endDate = today.AddDays(1); // Include today

                Console.WriteLine($"Date range: {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}");

                try 
                {
                    var result = await _attendanceService.GetUserAttendanceHistoryAsync(
                        _currentUserId,
                        startDate,
                        endDate
                    );
                    
                    Console.WriteLine($"API Response: Success={result.Success}, Message={result.Message ?? "null"}");
                    
                    if (result.Success && result.Data != null)
                    {
                        Console.WriteLine($"Received {result.Data.Items?.Count ?? 0} attendance records");
                        
                        _historyItems = result.Data.Items?.Select(attendance => new HistoryItem
                        {
                            AttendanceId = attendance.AttendanceId,
                            Date = attendance.AttendanceDate,
                            CheckInTime = attendance.CheckInTime,
                            CheckOutTime = attendance.CheckOutTime,
                            Duration = attendance.Duration,
                            Status = attendance.Status,
                            ShowFullDay = _showFullDay,
                            IsEvenRow = false,
                            IsUtc = attendance.IsUtc
                        }).ToList() ?? new List<HistoryItem>();

                        // Sort by date descending (newest first)
                        _historyItems = _historyItems.OrderByDescending(x => x.Date).ToList();

                        TotalItems = result.Data.TotalItems;
                        
                        // Safe update of UI components
                        MainThread.BeginInvokeOnMainThread(() => {
                            UpdatePagination();

                            // Update the date display in the header
                            if (MainTitleLabel != null)
                            {
                                MainTitleLabel.Text = $"Attendance History ({startDate:MMM yyyy} - {today:MMM yyyy})";
                            }
                        });
                    }
                    else
                    {
                        Console.WriteLine($"Failed to load attendance history: {result.Message}");
                        await DisplayAlert("Error", result.Message ?? "Failed to load attendance history", "OK");
                    }
                }
                catch (Exception apiEx) 
                {
                    Console.WriteLine($"API call exception: {apiEx.Message}");
                    Console.WriteLine($"Stack trace: {apiEx.StackTrace}");
                    await DisplayAlert("API Error", $"Error calling attendance API: {apiEx.Message}", "OK");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading attendance history: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                await DisplayAlert("Error", $"An error occurred: {ex.Message}", "OK");
            }
        }

        private void UpdatePagination()
        {
            try
            {
                // Check if we have any data to display
                if (_historyItems == null || !_historyItems.Any())
                {
                    Console.WriteLine("No attendance data available for pagination");
                    
                    // Update pagination info with zeros
                    StartItem = 0;
                    EndItem = 0;
                    TotalItems = 0;
                    
                    // Clear the collection view
                    if (HistoryCollectionView != null)
                    {
                        HistoryCollectionView.ItemsSource = null;
                    }
                    
                    return;
                }
                
                Console.WriteLine($"Updating pagination for {_historyItems.Count} items, page {_currentPage}");
                
                var skip = (_currentPage - 1) * _itemsPerPage;
                var pageItems = _historyItems.Skip(skip).Take(_itemsPerPage).ToList();

                // Set alternating row colors
                for (int i = 0; i < pageItems.Count; i++)
                {
                    pageItems[i].IsEvenRow = i % 2 == 0;
                }

                StartItem = _historyItems.Any() ? skip + 1 : 0;
                EndItem = Math.Min(skip + _itemsPerPage, TotalItems);

                // Safely update the CollectionView
                if (HistoryCollectionView != null)
                {
                    MainThread.BeginInvokeOnMainThread(() => {
                        HistoryCollectionView.ItemsSource = null;
                        HistoryCollectionView.ItemsSource = pageItems;
                    });
                }
                else
                {
                    Console.WriteLine("HistoryCollectionView is null - cannot update");
                }

                // Update pagination buttons
                UpdatePaginationButtons();
                
                Console.WriteLine($"Pagination updated: {StartItem}-{EndItem} of {TotalItems}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating pagination: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        private void UpdatePaginationButtons()
        {
            try
            {
                var maxPages = Math.Ceiling((double)TotalItems / _itemsPerPage);
                var paginationStack = HistoryCollectionView.Parent?.Parent?.FindByName<HorizontalStackLayout>("PaginationControls");
                
                if (paginationStack != null)
                {
                    // Clear existing buttons except Previous and Next
                    var buttons = paginationStack.Children.OfType<Button>().ToList();
                    var previousButton = buttons.FirstOrDefault(b => b.Text == "Previous");
                    var nextButton = buttons.FirstOrDefault(b => b.Text == "Next");
                    
                    paginationStack.Children.Clear();
                    
                    // Add Previous button
                    if (previousButton != null)
                    {
                        previousButton.IsEnabled = _currentPage > 1;
                        paginationStack.Children.Add(previousButton);
                    }

                    // Add page number buttons
                    for (int i = 1; i <= maxPages; i++)
                    {
                        if (i == 1 || i == maxPages || (i >= _currentPage - 1 && i <= _currentPage + 1))
                        {
                            var button = new Button
                            {
                                Text = i.ToString(),
                                BackgroundColor = i == _currentPage ? Color.FromArgb("#0066FF") : Color.FromArgb("#F3F4F6"),
                                TextColor = i == _currentPage ? Colors.White : Color.FromArgb("#374151"),
                                HeightRequest = 40,
                                WidthRequest = 40,
                                FontSize = 14
                            };
                            button.Clicked += OnPageNumberClicked;
                            paginationStack.Children.Add(button);
                        }
                        else if (i == _currentPage - 2 || i == _currentPage + 2)
                        {
                            var label = new Label
                            {
                                Text = "...",
                                TextColor = Color.FromArgb("#374151"),
                                VerticalOptions = LayoutOptions.Center,
                                FontSize = 14
                            };
                            paginationStack.Children.Add(label);
                        }
                    }

                    // Add Next button
                    if (nextButton != null)
                    {
                        nextButton.IsEnabled = _currentPage < maxPages;
                        paginationStack.Children.Add(nextButton);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating pagination buttons: {ex.Message}");
            }
        }

        private void OnPreviousClicked(object sender, EventArgs e)
        {
            if (_currentPage > 1)
            {
                _currentPage--;
                UpdatePagination();
            }
        }

        private void OnNextClicked(object sender, EventArgs e)
        {
            var maxPages = Math.Ceiling((double)TotalItems / _itemsPerPage);
            if (_currentPage < maxPages)
            {
                _currentPage++;
                UpdatePagination();
            }
        }

        private void OnPageNumberClicked(object sender, EventArgs e)
        {
            if (sender is Button button && int.TryParse(button.Text, out int pageNumber))
            {
                var maxPages = Math.Ceiling((double)TotalItems / _itemsPerPage);
                if (pageNumber >= 1 && pageNumber <= maxPages)
                {
                    _currentPage = pageNumber;
                    UpdatePagination();
                }
            }
        }

        private void OnPageSizeChanged(object sender, EventArgs e)
        {
            var width = Width;

            if (width < MOBILE_WIDTH)
            {
                // Mobile view
                _showFullDay = false;
                AttendanceIdHeader.IsVisible = false;
                DateHeader.FontSize = 12;
                DayHeader.FontSize = 12;
                CheckInHeader.FontSize = 12;
                CheckOutHeader.FontSize = 12;
                WorkHoursHeader.FontSize = 12;

                CheckInHeader.Text = "In";
                CheckOutHeader.Text = "Out";
                WorkHoursHeader.Text = "Hours";

                // Adjust column widths for mobile
                TableHeaderGrid.ColumnDefinitions = new ColumnDefinitionCollection
                {
                    new ColumnDefinition { Width = new GridLength(0, GridUnitType.Absolute) }, // Hide AttendanceId
                    new ColumnDefinition { Width = new GridLength(3, GridUnitType.Star) }, // Date
                    new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) }, // Day
                    new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) }, // In
                    new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) }, // Out
                    new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) }, // Hours
                    new ColumnDefinition { Width = new GridLength(3, GridUnitType.Star) }  // Status
                };

                RootGrid.Padding = new Thickness(8);
                MainTitleLabel.FontSize = 18;
                DateHeader.Margin = new Thickness(0);
                TableHeaderGrid.Padding = new Thickness(8);

                // Adjust pagination for mobile
                foreach (var label in PaginationInfoStack.Children.OfType<Label>())
                {
                    label.FontSize = 12;
                    label.Margin = new Thickness(2, 0);
                }

                PreviousButton.WidthRequest = 80;
                PreviousButton.HeightRequest = 35;
                PreviousButton.FontSize = 12;
                PreviousButton.Margin = new Thickness(0, 0, 4, 4);
                
                NextButton.WidthRequest = 80;
                NextButton.HeightRequest = 35;
                NextButton.FontSize = 12;
                NextButton.Margin = new Thickness(4, 0, 0, 4);

                // Update all grid items to match the header grid columns
                foreach (var item in _historyItems)
                {
                    if (item is HistoryItem historyItem)
                    {
                        var grid = HistoryCollectionView.GetVisualTreeDescendants()
                            .FirstOrDefault(x => x is Grid && ((Grid)x).BindingContext == historyItem) as Grid;
                        
                        if (grid != null)
                        {
                            grid.ColumnDefinitions = TableHeaderGrid.ColumnDefinitions;
                            grid.Padding = new Thickness(8);
                        }
                    }
                }
            }
            else if (width < TABLET_WIDTH)
            {
                // Tablet view
                _showFullDay = true;
                AttendanceIdHeader.IsVisible = true;
                DateHeader.FontSize = 13;
                DayHeader.FontSize = 13;
                CheckInHeader.FontSize = 13;
                CheckOutHeader.FontSize = 13;
                WorkHoursHeader.FontSize = 13;

                CheckInHeader.Text = "Check-in";
                CheckOutHeader.Text = "Check-out";
                WorkHoursHeader.Text = "Work hours";

                // Reset column widths for tablet
                TableHeaderGrid.ColumnDefinitions = new ColumnDefinitionCollection
                {
                    new ColumnDefinition { Width = new GridLength(1.5, GridUnitType.Star) }, // AttendanceId
                    new ColumnDefinition { Width = new GridLength(1.5, GridUnitType.Star) }, // Date
                    new ColumnDefinition { Width = new GridLength(0.8, GridUnitType.Star) }, // Day
                    new ColumnDefinition { Width = new GridLength(1.2, GridUnitType.Star) }, // Check-in
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }, // Check-out
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }, // Work hours
                    new ColumnDefinition { Width = new GridLength(1.5, GridUnitType.Star) }  // Status
                };

                RootGrid.Padding = new Thickness(16);
                MainTitleLabel.FontSize = 20;
                DateHeader.Margin = new Thickness(0);
                TableHeaderGrid.Padding = new Thickness(16);

                // Adjust pagination for tablet
                foreach (var label in PaginationInfoStack.Children.OfType<Label>())
                {
                    label.FontSize = 13;
                    label.Margin = new Thickness(3, 0);
                }

                PreviousButton.WidthRequest = 90;
                PreviousButton.HeightRequest = 38;
                PreviousButton.FontSize = 13;
                PreviousButton.Margin = new Thickness(0, 0, 6, 6);
                
                NextButton.WidthRequest = 90;
                NextButton.HeightRequest = 38;
                NextButton.FontSize = 13;
                NextButton.Margin = new Thickness(6, 0, 0, 6);
            }
            else
            {
                // Desktop view - reset to default values
                _showFullDay = true;
                AttendanceIdHeader.IsVisible = true;
                DateHeader.FontSize = 14;
                DayHeader.FontSize = 14;
                CheckInHeader.FontSize = 14;
                CheckOutHeader.FontSize = 14;
                WorkHoursHeader.FontSize = 14;

                CheckInHeader.Text = "Check-in";
                CheckOutHeader.Text = "Check-out";
                WorkHoursHeader.Text = "Work hours";

                // Reset column widths for desktop
                TableHeaderGrid.ColumnDefinitions = new ColumnDefinitionCollection
                {
                    new ColumnDefinition { Width = new GridLength(1.5, GridUnitType.Star) }, // AttendanceId
                    new ColumnDefinition { Width = new GridLength(1.5, GridUnitType.Star) }, // Date
                    new ColumnDefinition { Width = new GridLength(0.8, GridUnitType.Star) }, // Day
                    new ColumnDefinition { Width = new GridLength(1.2, GridUnitType.Star) }, // Check-in
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }, // Check-out
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }, // Work hours
                    new ColumnDefinition { Width = new GridLength(1.5, GridUnitType.Star) }  // Status
                };

                RootGrid.Padding = new Thickness(24);
                MainTitleLabel.FontSize = 24;
                DateHeader.Margin = new Thickness(0);
                TableHeaderGrid.Padding = new Thickness(20, 16);

                // Reset pagination for desktop
                foreach (var label in PaginationInfoStack.Children.OfType<Label>())
                {
                    label.FontSize = 14;
                    label.Margin = new Thickness(4, 0);
                }

                PreviousButton.WidthRequest = 100;
                PreviousButton.HeightRequest = 40;
                PreviousButton.FontSize = 14;
                PreviousButton.Margin = new Thickness(0, 0, 8, 8);
                
                NextButton.WidthRequest = 100;
                NextButton.HeightRequest = 40;
                NextButton.FontSize = 14;
                NextButton.Margin = new Thickness(8, 0, 0, 8);
            }

            // Update all items to reflect the new day format
            foreach (var item in _historyItems)
            {
                item.ShowFullDay = _showFullDay;
            }

            // Force CollectionView to refresh
            HistoryCollectionView.ItemsSource = null;
            HistoryCollectionView.ItemsSource = _historyItems;
        }
    }

    public class HistoryItem : INotifyPropertyChanged
    {
        private bool _showFullDay = true;
        public bool IsUtc { get; set; } = true;

        public string AttendanceId { get; set; }
        public DateTime Date { get; set; }
        public string Day => Date.ToString("dddd");
        public string ShortDay => Date.ToString("ddd");
        public TimeSpan CheckInTime { get; set; }
        public TimeSpan CheckOutTime { get; set; }
        public TimeSpan Duration { get; set; }
        public string Status { get; set; }
        public string FormattedDate => Date.ToString("dd MMM yyyy");
        
        public string FormattedCheckInTime { 
            get {
                if (CheckInTime == TimeSpan.Zero) return "--:--:--";
                
                if (IsUtc) {
                    // Create a DateTime at the attendance date with the check-in time
                    DateTime utcDateTime = Date.Date.Add(CheckInTime);
                    // Convert to local time
                    DateTime localDateTime = utcDateTime.ToLocalTime();
                    // Return the time portion
                    return localDateTime.ToString("hh:mm:ss tt");
                }
                
                return CheckInTime.ToString(@"hh\:mm\:ss tt");
            }
        }
        
        public string FormattedCheckOutTime { 
            get {
                if (CheckOutTime == TimeSpan.Zero) return "--:--:--";
                
                if (IsUtc) {
                    // Create a DateTime at the attendance date with the check-out time
                    DateTime utcDateTime = Date.Date.Add(CheckOutTime);
                    // Convert to local time
                    DateTime localDateTime = utcDateTime.ToLocalTime();
                    // Return the time portion
                    return localDateTime.ToString("hh:mm:ss tt");
                }
                
                return CheckOutTime.ToString(@"hh\:mm\:ss tt");
            }
        }
        
        public string FormattedDuration => Duration == TimeSpan.Zero ? "--:--:--" : Duration.ToString(@"hh\:mm\:ss");
        public bool IsEvenRow { get; set; }

        public bool ShowFullDay
        {
            get => _showFullDay;
            set
            {
                if (_showFullDay != value)
                {
                    _showFullDay = value;
                    OnPropertyChanged(nameof(ShowFullDay));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class StatusColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return Colors.Transparent;

            string status = value.ToString();

            // Return color based on status
            switch (status)
            {
                case "Checked in":
                    return Color.FromArgb("#E0F2FE"); // Light blue
                case "Checked out":
                    return Color.FromArgb("#FEF3C7"); // Light yellow
                case "Completed":
                    return Color.FromArgb("#DCFCE7"); // Light green
                case "Absent":
                    return Color.FromArgb("#FEE2E2"); // Light red
                case "Active":
                    return Color.FromArgb("#E0F2FE"); // Light blue
                case "Waiting":
                    return Color.FromArgb("#F3F4F6"); // Light gray
                default:
                    return Colors.Transparent;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class BoolToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isEven)
            {
                return isEven ? Colors.White : Color.FromArgb("#F9FAFB");
            }
            return Colors.White;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class InverseBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return !boolValue;
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 