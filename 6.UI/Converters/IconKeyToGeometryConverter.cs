using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace PrimeERP.UI.Converters
{
    /// <summary>يحوّل NavItem</summary>
    public class IconKeyToGeometryConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var key = value as string;
            if (string.IsNullOrEmpty(key)) return null;
            return System.Windows.Application.Current.TryFindResource(key);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }
}
