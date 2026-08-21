using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Localization;
using PrimeERP.UI.Services;
using PrimeERP.Application;
using PrimeERP.Application.Services;
using Btn = PrimeERP.UI.Components.Actions.AppButton;

namespace PrimeERP.UI.Components.Feedback
{
    public class AppMessageDialog : AppDialogWindow
    {
        public AppMessageDialog(string title, string message, StatusVariant variant = StatusVariant.Info)
        {
            Title = title;
            HeaderTitle = title;
            HeaderVariant = variant;
            HeaderIcon = (Geometry)FindResource(variant switch
            {
                StatusVariant.Danger  => "IconX",
                StatusVariant.Success => "IconCheck",
                StatusVariant.Warning => "IconWarning",
                _                     => "IconInfo"
            });

            Body = new TextBlock
            {
                Text = message,
                TextWrapping = TextWrapping.Wrap,
                FontSize = (double)FindResource("FontSizeSm"),
                FontFamily = (FontFamily)FindResource("FontFamilyPrimary"),
                Foreground = (Brush)FindResource("TextPrimary")
            };

            var btnOk = new Btn { Text = LocalizationService.Get("Str.Close"), Variant = "primary", Size = "sm" };
            btnOk.Click += (s, e) => { DialogResult = true; Close(); };
            Footer = btnOk;
        }

        protected override void OnEnterPressed()
        {
            DialogResult = true;
            Close();
        }
    }
}
