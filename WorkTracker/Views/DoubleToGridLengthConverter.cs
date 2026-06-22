using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace WorkTracker.Views
{
    public class DoubleToGridLengthConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double val)
            {
                // Ensure value is positive, and return a star GridLength
                // Use a tiny positive epsilon if the value is zero, so the column width is close to 0
                double width = Math.Max(0.0001, val);
                return new GridLength(width, GridUnitType.Star);
            }
            return new GridLength(0.0001, GridUnitType.Star);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
