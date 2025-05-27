using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace ChronoTrack_ViewLayer.Converters
{
    public class BoolToFontAttributesConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue && parameter is string attributes)
            {
                string[] attributeStrings = attributes.Split(',');
                string attributeString = boolValue ? attributeStrings[0] : attributeStrings[1];
                
                return attributeString == "Bold" ? FontAttributes.Bold : FontAttributes.None;
            }
            
            return FontAttributes.None;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 