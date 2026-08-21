using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using PrimeERP.Core.Common;
using Btn = PrimeERP.Views.Controls.Actions.AppButton;

namespace PrimeERP.Views.Controls.Feedback
{
    public class AppConfirmDialog : AppDialogWindow
    {
        public bool Result { get; private set; }

        public AppConfirmDialog(string title, string message, string confirmText = null,
                                string cancelText = null, bool isDangerous = false, Geometry icon = null)
        {
            confirmText ??= Services.LocalizationService.Get("Str.Confirm");
            cancelText  ??= Services.LocalizationService.Get("Str.Cancel");

            Title = title;
            HeaderTitle = title;
            HeaderVariant = isDangerous ? StatusVariant.Danger : StatusVariant.Brand;
            HeaderIcon = icon ?? (Geometry)FindResource(isDangerous ? "IconWarning" : "IconInfo");

            Body = new TextBlock
            {
                Text = message,
                TextWrapping = TextWrapping.Wrap,
                FontSize = (double)FindResource("FontSizeSm"),
                FontFamily = (FontFamily)FindResource("FontFamilyPrimary"),
                Foreground = (Brush)FindResource("TextPrimary")
            };

            var btnCancel = new Btn { Text = cancelText, Variant = "secondary", Size = "sm" };
            btnCancel.Click += (s, e) => { Result = false; DialogResult = false; Close(); };

            var btnConfirm = new Btn
            {
                Text = confirmText,
                Variant = isDangerous ? "danger" : "primary",
                Size = "sm",
                Margin = (Thickness)FindResource("DialogButtonSpacing")
            };
            btnConfirm.Click += (s, e) => { Result = true; DialogResult = true; Close(); };

            Footer = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Children = { btnCancel, btnConfirm }
            };
        }

        protected override void OnEnterPressed()
        {
            Result = true;
            DialogResult = true;
            Close();
        }
    }
}
