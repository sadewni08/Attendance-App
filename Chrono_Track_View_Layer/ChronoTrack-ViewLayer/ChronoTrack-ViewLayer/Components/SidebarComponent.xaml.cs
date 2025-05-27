using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using ChronoTrack_ViewLayer.Pages;

namespace ChronoTrack_ViewLayer.Components
{
    public partial class SidebarComponent : ContentView, INotifyPropertyChanged
    {
        private bool _isDashboardActive;
        public bool IsDashboardActive
        {
            get => _isDashboardActive;
            set
            {
                if (_isDashboardActive != value)
                {
                    _isDashboardActive = value;
                    OnPropertyChanged();
                    UpdateBackgroundColors();
                }
            }
        }

        private bool _isHistoryActive;
        public bool IsHistoryActive
        {
            get => _isHistoryActive;
            set
            {
                if (_isHistoryActive != value)
                {
                    _isHistoryActive = value;
                    OnPropertyChanged();
                    UpdateBackgroundColors();
                }
            }
        }

        private bool _isEmployeeDetailsActive;
        public bool IsEmployeeDetailsActive
        {
            get => _isEmployeeDetailsActive;
            set
            {
                if (_isEmployeeDetailsActive != value)
                {
                    _isEmployeeDetailsActive = value;
                    OnPropertyChanged();
                    UpdateBackgroundColors();
                }
            }
        }

        private bool _isAttendanceDetailsActive;
        public bool IsAttendanceDetailsActive
        {
            get => _isAttendanceDetailsActive;
            set
            {
                if (_isAttendanceDetailsActive != value)
                {
                    _isAttendanceDetailsActive = value;
                    OnPropertyChanged();
                    UpdateBackgroundColors();
                }
            }
        }

        private bool _isReportsActive;
        public bool IsReportsActive
        {
            get => _isReportsActive;
            set
            {
                if (_isReportsActive != value)
                {
                    _isReportsActive = value;
                    OnPropertyChanged();
                    UpdateBackgroundColors();
                }
            }
        }

        private bool _isSettingsActive;
        public bool IsSettingsActive
        {
            get => _isSettingsActive;
            set
            {
                if (_isSettingsActive != value)
                {
                    _isSettingsActive = value;
                    OnPropertyChanged();
                    UpdateBackgroundColors();
                }
            }
        }

        public SidebarComponent()
        {
            InitializeComponent();
            BindingContext = this;
            
            // Default to Dashboard being active
            IsDashboardActive = true;
            IsHistoryActive = false;
            IsEmployeeDetailsActive = false;
            IsAttendanceDetailsActive = false;
            IsReportsActive = false;
            IsSettingsActive = false;
            
            UpdateBackgroundColors();
        }

        private void UpdateBackgroundColors()
        {
            // Set background colors directly
            DashboardBorder.BackgroundColor = IsDashboardActive ? Color.FromArgb("#0066FF") : Colors.Transparent;
            HistoryBorder.BackgroundColor = IsHistoryActive ? Color.FromArgb("#0066FF") : Colors.Transparent;
            EmployeeDetailsBorder.BackgroundColor = IsEmployeeDetailsActive ? Color.FromArgb("#0066FF") : Colors.Transparent;
            //AttendanceDetailsBorder.BackgroundColor = IsAttendanceDetailsActive ? Color.FromArgb("#0066FF") : Colors.Transparent;
            
            // Update the Reports and Settings borders if they exist
            if (FindByName("ReportsBorder") is Border reportsBorder)
            {
                reportsBorder.BackgroundColor = IsReportsActive ? Color.FromArgb("#0066FF") : Colors.Transparent;
            }
            
            if (FindByName("SettingsBorder") is Border settingsBorder)
            {
                settingsBorder.BackgroundColor = IsSettingsActive ? Color.FromArgb("#0066FF") : Colors.Transparent;
            }
        }

        private async void OnDashboardClicked(object sender, EventArgs e)
        {
            // Only navigate if not already on this page
            if (!IsDashboardActive)
            {
                // Update active states
                IsDashboardActive = true;
                IsHistoryActive = false;
                IsEmployeeDetailsActive = false;
                IsAttendanceDetailsActive = false;
                IsReportsActive = false;
                IsSettingsActive = false;
                
                // Navigate to Dashboard page if it exists
                try
                {
                    // Check if the page type exists in your project
                    Type dashboardPageType = Type.GetType("ChronoTrack_ViewLayer.Pages.DashboardPage, ChronoTrack-ViewLayer");
                    if (dashboardPageType != null)
                    {
                        var dashboardPage = Activator.CreateInstance(dashboardPageType) as Page;
                        if (dashboardPage != null)
                        {
                            await Application.Current.MainPage.Navigation.PushAsync(dashboardPage);
                        }
                    }
                    else
                    {
                        // If the page doesn't exist, show a message
                        await Application.Current.MainPage.DisplayAlert("Navigation", "Dashboard page is not implemented yet", "OK");
                    }
                }
                catch (Exception ex)
                {
                    await Application.Current.MainPage.DisplayAlert("Error", $"Could not navigate to Dashboard: {ex.Message}", "OK");
                }
            }
        }

        private async void OnHistoryClicked(object sender, EventArgs e)
        {
            // Only navigate if not already on this page
            if (!IsHistoryActive)
            {
                // Update active states
                IsDashboardActive = false;
                IsHistoryActive = true;
                IsEmployeeDetailsActive = false;
                IsAttendanceDetailsActive = false;
                IsReportsActive = false;
                IsSettingsActive = false;
                
                // Navigate to Summary/History page if it exists
                try
                {
                    // Check if the page type exists in your project
                    // Try different possible names for the history/summary page
                    Type historyPageType = Type.GetType("ChronoTrack_ViewLayer.Pages.SummaryPage, ChronoTrack-ViewLayer") ?? 
                                          Type.GetType("ChronoTrack_ViewLayer.Pages.HistoryPage, ChronoTrack-ViewLayer");
                    
                    if (historyPageType != null)
                    {
                        var historyPage = Activator.CreateInstance(historyPageType) as Page;
                        if (historyPage != null)
                        {
                            await Application.Current.MainPage.Navigation.PushAsync(historyPage);
                        }
                    }
                    else
                    {
                        // If the page doesn't exist, show a message
                        await Application.Current.MainPage.DisplayAlert("Navigation", "Summary/History page is not implemented yet", "OK");
                    }
                }
                catch (Exception ex)
                {
                    await Application.Current.MainPage.DisplayAlert("Error", $"Could not navigate to Summary/History: {ex.Message}", "OK");
                }
            }
        }

        private async void OnEmployeeDetailsClicked(object sender, EventArgs e)
        {
            // Only navigate if not already on this page
            if (!IsEmployeeDetailsActive)
            {
                // Update active states
                IsDashboardActive = false;
                IsHistoryActive = false;
                IsEmployeeDetailsActive = true;
                IsAttendanceDetailsActive = false;
                IsReportsActive = false;
                IsSettingsActive = false;
                
                // Navigate to Employee Details page
                try
                {
                    await Application.Current.MainPage.Navigation.PushAsync(new EmployeeDetailsPage());
                }
                catch (Exception ex)
                {
                    await Application.Current.MainPage.DisplayAlert("Error", $"Could not navigate to Employee Details: {ex.Message}", "OK");
                }
            }
        }

        private async void OnAttendanceDetailsClicked(object sender, EventArgs e)
        {
            // Only navigate if not already on this page
            if (!IsAttendanceDetailsActive)
            {
                // Update active states
                IsDashboardActive = false;
                IsHistoryActive = false;
                IsEmployeeDetailsActive = false;
                IsAttendanceDetailsActive = true;
                IsReportsActive = false;
                IsSettingsActive = false;
                
                // Navigate to Summary Page instead of Attendance Details page
                try
                {
                    if (Application.Current != null && Application.Current.MainPage != null)
                    {
                        await Application.Current.MainPage.Navigation.PushAsync(new Pages.SummaryPage());
                    }
                    else
                    {
                        Console.WriteLine("Cannot navigate: Application.Current.MainPage is null");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Navigation error: {ex.Message}");
                    Console.WriteLine($"Stack trace: {ex.StackTrace}");
                    
                    if (Application.Current?.MainPage != null)
                    {
                        await Application.Current.MainPage.DisplayAlert("Error", $"Could not navigate to Summary Page: {ex.Message}", "OK");
                    }
                }
            }
        }

        public new event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
} 