using Microsoft.Maui.Controls;
using System;
using System.Globalization;

namespace ChronoTrack_ViewLayer.Converters
{
    public class TimeSpanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // If the value is a TimeSpan
            if (value is TimeSpan timeSpan)
            {
                // Return true (visible) if the TimeSpan is not zero
                return timeSpan != TimeSpan.Zero;
            }

            // Default to visible if null or not a TimeSpan
            return true;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // This converter doesn't support converting back
            throw new NotImplementedException();
        }
    }
} 