using PrimeERP.UI.Components.Documents;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace PrimeERP.UI.Converters
{
    /// <summary>يحوّل FooterTotal</summary>
    public class FooterVariantToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var key = (value as string) switch
            {
                "success" => "Success",
                "danger"  => "Danger",
                "warning" => "Warning",
                "info"    => "Info",
                "brand"   => "BrandDefault",
                _         => "TextPrimary"
            };
            return System.Windows.Application.Current.TryFindResource(key);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }
}
