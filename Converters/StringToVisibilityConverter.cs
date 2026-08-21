using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace PrimeERP.Converters
{
    /// <summary>نص فارغ/فارغ تماماً يعني Collapsed — غير ذلك Visible.</summary>
    public class StringToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType,
                              object parameter, CultureInfo culture)
        {
            return string.IsNullOrEmpty(value as string) ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType,
                                  object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
