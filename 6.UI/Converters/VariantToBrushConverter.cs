using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using PrimeERP.Domain.Results;

namespace PrimeERP.UI.Converters
{
    /// <summary>
    /// StatusVariant → Brush من Resources/Themes فقط (الشاشة — يتبدّل فاتح/داكن تلقائياً لأنه DynamicResource).
    /// parameter: "Solid" (افتراضي) / "Soft" / "SoftText" / "Hover" / "Border".
    /// كل القيم من مفاتيح Colors.xaml/Colors.Dark.xaml الموجودة فعلاً — لا مفاتيح جديدة، لا تكرار.
    /// </summary>
    public class VariantToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not StatusVariant variant)
                return null;

            var part = parameter as string ?? "Solid";
            var key = ResolveKey(variant, part);
            return System.Windows.Application.Current?.TryFindResource(key) as Brush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotImplementedException();

        private static string ResolveKey(StatusVariant variant, string part) => (variant, part) switch
        {
            (StatusVariant.Brand, "Solid")    => "BrandDefault",
            (StatusVariant.Brand, "Soft")     => "BrandSoft",
            (StatusVariant.Brand, "SoftText") => "BrandSoftText",
            (StatusVariant.Brand, "Hover")    => "BrandHover",
            (StatusVariant.Brand, "Border")   => "BrandSoftBorder",

            (StatusVariant.Success, "Solid")    => "Success",
            (StatusVariant.Success, "Soft")     => "SuccessSoft",
            (StatusVariant.Success, "SoftText") => "SuccessSoftText",
            (StatusVariant.Success, "Hover")    => "SuccessHover",
            (StatusVariant.Success, "Border")   => "Success",

            (StatusVariant.Warning, "Solid")    => "Warning",
            (StatusVariant.Warning, "Soft")     => "WarningSoft",
            (StatusVariant.Warning, "SoftText") => "WarningSoftText",
            (StatusVariant.Warning, "Hover")    => "WarningHover",
            (StatusVariant.Warning, "Border")   => "Warning",

            (StatusVariant.Danger, "Solid")    => "Danger",
            (StatusVariant.Danger, "Soft")     => "DangerSoft",
            (StatusVariant.Danger, "SoftText") => "DangerSoftText",
            (StatusVariant.Danger, "Hover")    => "DangerHover",
            (StatusVariant.Danger, "Border")   => "Danger",

            (StatusVariant.Info, "Solid")    => "Info",
            (StatusVariant.Info, "Soft")     => "InfoSoft",
            (StatusVariant.Info, "SoftText") => "InfoSoftText",
            (StatusVariant.Info, "Hover")    => "InfoHover",
            (StatusVariant.Info, "Border")   => "Info",

            (StatusVariant.Neutral, "Solid")    => "TextTertiary",
            (StatusVariant.Neutral, "Soft")     => "SurfaceCanvas",
            (StatusVariant.Neutral, "SoftText") => "TextSecondary",
            (StatusVariant.Neutral, "Hover")    => "TextSecondary",
            (StatusVariant.Neutral, "Border")   => "OutlineDefault",

            _ => "TextMuted"
        };
    }
}
