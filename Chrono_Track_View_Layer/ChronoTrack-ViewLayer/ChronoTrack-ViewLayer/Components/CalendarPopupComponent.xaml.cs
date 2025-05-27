using System;
using System.Collections.ObjectModel;
using System.Globalization;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace ChronoTrack_ViewLayer.Components
{
    public partial class CalendarPopupComponent : ContentView
    {
        // Event to notify when a date is selected
        public event EventHandler<DateTime> DateSelected;
        
        // Event to notify when the popup should be closed
        public event EventHandler CloseRequested;
        
        private DateTime _currentMonth;
        private ObservableCollection<CalendarDayViewModel> _calendarDays;

        public CalendarPopupComponent()
        {
            InitializeComponent();
            _currentMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            _calendarDays = new ObservableCollection<CalendarDayViewModel>();
            
            // Update the calendar display on initialization
            UpdateCalendarTitle();
            GenerateCalendarDays();
        }
        
        public void RefreshCalendar()
        {
            // Reset to current month
            _currentMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            UpdateCalendarTitle();
            GenerateCalendarDays();
        }
        
        private void OnPreviousMonthClicked(object sender, EventArgs e)
        {
            _currentMonth = _currentMonth.AddMonths(-1);
            UpdateCalendarTitle();
            GenerateCalendarDays();
        }
        
        private void OnNextMonthClicked(object sender, EventArgs e)
        {
            _currentMonth = _currentMonth.AddMonths(1);
            UpdateCalendarTitle();
            GenerateCalendarDays();
        }
        
        private void UpdateCalendarTitle()
        {
            // Set the calendar title
            CalendarTitle.Text = _currentMonth.ToString("MMMM yyyy");
        }
        
        private void GenerateCalendarDays()
        {
            try
            {
                // Clear previous calendar days
                _calendarDays = new ObservableCollection<CalendarDayViewModel>();
                
                // Get first day of month and number of days in month
                DateTime firstDayOfMonth = new DateTime(_currentMonth.Year, _currentMonth.Month, 1);
                int daysInMonth = DateTime.DaysInMonth(_currentMonth.Year, _currentMonth.Month);
                
                // Get day of week for the first day (0 = Sunday, 6 = Saturday)
                int firstDayOfWeek = (int)firstDayOfMonth.DayOfWeek;
                
                // Get current date for highlighting today
                DateTime today = DateTime.Today;
                bool isCurrentMonth = today.Year == _currentMonth.Year && today.Month == _currentMonth.Month;
                
                // Add days from previous month to fill the first row
                if (firstDayOfWeek > 0)
                {
                    DateTime prevMonth = firstDayOfMonth.AddMonths(-1);
                    int daysInPrevMonth = DateTime.DaysInMonth(prevMonth.Year, prevMonth.Month);
                    
                    for (int i = firstDayOfWeek - 1; i >= 0; i--)
                    {
                        int day = daysInPrevMonth - i;
                        _calendarDays.Add(new CalendarDayViewModel 
                        { 
                            Day = day,
                            IsCurrentMonth = false,
                            IsToday = false
                        });
                    }
                }
                
                // Add days for current month
                for (int day = 1; day <= daysInMonth; day++)
                {
                    _calendarDays.Add(new CalendarDayViewModel 
                    { 
                        Day = day,
                        IsCurrentMonth = true,
                        IsToday = isCurrentMonth && day == today.Day
                    });
                }
                
                // Calculate how many days from next month we need to show
                int totalDaysShown = _calendarDays.Count;
                int remainingCells = 42 - totalDaysShown; // 6 weeks * 7 days = 42 cells total
                
                // Add days from next month to complete the grid
                for (int day = 1; day <= remainingCells; day++)
                {
                    _calendarDays.Add(new CalendarDayViewModel 
                    { 
                        Day = day,
                        IsCurrentMonth = false,
                        IsToday = false
                    });
                }
                
                // Bind to the collection view
                CalendarDaysCollectionView.ItemsSource = _calendarDays;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error generating calendar: {ex.Message}");
            }
        }
        
        private void OnDayTapped(object sender, TappedEventArgs e)
        {
            if (e.Parameter is int day)
            {
                try
                {
                    if (!IsCurrentMonthDay(day))
                    {
                        // For simplicity, we're not handling selection of days outside the current month
                        return;
                    }
                    
                    // Create a date object for the selected day
                    DateTime selectedDate = new DateTime(_currentMonth.Year, _currentMonth.Month, day);
                    
                    // Notify subscribers that a date was selected
                    DateSelected?.Invoke(this, selectedDate);
                    
                    // Request to close the popup
                    CloseRequested?.Invoke(this, EventArgs.Empty);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error selecting day: {ex.Message}");
                }
            }
        }
        
        private bool IsCurrentMonthDay(int day)
        {
            // Find the calendar day item
            foreach (var item in _calendarDays)
            {
                if (item.Day == day && item.IsCurrentMonth)
                {
                    return true;
                }
            }
            return false;
        }
        
        private void OnCloseButtonClicked(object sender, EventArgs e)
        {
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }
    }
    
    // Helper class for calendar days
    public class CalendarDayViewModel
    {
        public int Day { get; set; }
        public bool IsCurrentMonth { get; set; }
        public bool IsToday { get; set; }
        public Color BackgroundColor => IsToday ? Color.FromArgb("#3354F4") : Colors.Transparent;
        public Color TextColor => IsToday ? Colors.White : (IsCurrentMonth ? Color.FromArgb("#111827") : Color.FromArgb("#9CA3AF"));
        public FontAttributes FontAttributes => IsToday ? FontAttributes.Bold : FontAttributes.None;
        public double Opacity => IsCurrentMonth ? 1.0 : 0.5;
    }
} 