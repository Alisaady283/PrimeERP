using System;
using System.Globalization;
using System.Windows.Data;

namespace PrimeERP.UI.Converters
{
    /// <summary>StatusVariant (اسماً أو رقماً) ← اسم نوع AppBadge بحروفٍ صغيرة</summary>
    public class VariantNameConverter : IValueConverter
    {
        private static readonly string[] Names = { "neutral", "info", "brand", "success", "warning", "danger" };

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => value switch
        {
            null => "neutral",
            int index when index >= 0 && index < Names.Length => Names[index],
            Enum e => e.ToString().ToLowerInvariant(),
            _ => value.ToString()!.ToLowerInvariant()
        };

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
    }
}
