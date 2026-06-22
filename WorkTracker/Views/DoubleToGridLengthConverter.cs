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
            if (value is double val && val > 0)
            {
                return new GridLength(val, GridUnitType.Star);
            }
            return new GridLength(0, GridUnitType.Pixel);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
