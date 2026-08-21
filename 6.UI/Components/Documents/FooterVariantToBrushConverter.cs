using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace PrimeERP.UI.Components.Documents
{
    /// <summary>يحوّل FooterTotal.Variant (نفس مفردات AppBadge) إلى Brush مناسب من قاموس السمة الحالية.</summary>
    public class FooterVariantToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var key = (value as string) switch
            {
                "success" => "OkBrush",
                "danger"  => "CriticalBrush",
                "warning" => "CautionBrush",
                "info"    => "InfoBrush",
                "brand"   => "BrandBrush",
                _         => "BodyTextBrush"
            };
            return System.Windows.Application.Current.TryFindResource(key);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }
}
