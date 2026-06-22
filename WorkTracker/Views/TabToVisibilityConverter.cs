using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace WorkTracker.Views
{
    public class TabToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string currentTab && parameter is string targetTab)
            {
                return string.Equals(currentTab, targetTab, StringComparison.OrdinalIgnoreCase) 
                    ? Visibility.Visible 
                    : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
