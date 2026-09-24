using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace PrimeERP.UI.Converters
{
    /// <summary>محوّل عرض BoolToVisibility</summary>
    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType,
                              object parameter, CultureInfo culture)
        {
            bool val = value is bool b && b;
            if (parameter?.ToString() == "inverse")
                val = !val;
            return val ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType,
                                  object parameter, CultureInfo culture)
        {
            return value is Visibility v && v == Visibility.Visible;
        }
    }
}