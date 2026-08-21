using System;
using System.Globalization;
using System.Windows.Data;

namespace PrimeERP.Converters
{
    public class DecimalFormatConverter : IValueConverter
    {
        public object Convert(object value, Type targetType,
                              object parameter, CultureInfo culture)
        {
            if (value == null) return "0.00";
            string format = parameter?.ToString() ?? "F2";
            if (decimal.TryParse(value.ToString(), out decimal d))
                return d.ToString(format);
            return "0.00";
        }

        public object ConvertBack(object value, Type targetType,
                                  object parameter, CultureInfo culture)
        {
            if (decimal.TryParse(value?.ToString(), out decimal d))
                return d;
            return 0m;
        }
    }
}