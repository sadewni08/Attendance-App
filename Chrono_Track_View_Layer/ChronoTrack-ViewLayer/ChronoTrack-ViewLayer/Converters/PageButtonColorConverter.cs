using System;
using System.Globalization;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace ChronoTrack_ViewLayer.Converters
{
    public class PageButtonColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int currentPage && parameter is string pageStr && int.TryParse(pageStr, out int buttonPage))
            {
                // If the button page matches the current page, return active color
                return currentPage == buttonPage ? Color.FromArgb("#0066FF") : Color.FromArgb("#F3F4F6");
            }
            
            // Default to inactive color
            return Color.FromArgb("#F3F4F6");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    
    public class PageButtonTextColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int currentPage && parameter is string pageStr && int.TryParse(pageStr, out int buttonPage))
            {
                // If the button page matches the current page, return white text, otherwise dark text
                return currentPage == buttonPage ? Colors.White : Color.FromArgb("#374151");
            }
            
            // Default to dark text
            return Color.FromArgb("#374151");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 