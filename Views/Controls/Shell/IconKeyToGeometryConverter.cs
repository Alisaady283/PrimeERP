using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace PrimeERP.Views.Controls.Shell
{
    /// <summary>يحوّل NavItem.IconKey (اسم نصي مثل "IconAccounts") إلى Geometry الفعلية من Icons.xaml.</summary>
    public class IconKeyToGeometryConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var key = value as string;
            if (string.IsNullOrEmpty(key)) return null;
            return Application.Current.TryFindResource(key);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }
}
