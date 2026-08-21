using System;
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
    public class AppProgressDialog : AppDialogWindow
    {
        private readonly TextBlock _txtMessage;
        private readonly ProgressBar _bar;

        public event EventHandler CancelRequested;

        public AppProgressDialog(string title, string message, bool allowCancel = false)
        {
            Title = title;
            HeaderTitle = title;
            HeaderVariant = StatusVariant.Brand;
            HeaderIcon = (Geometry)FindResource("IconRefresh");

            _txtMessage = new TextBlock
            {
                Text = message,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 14),
                FontSize = (double)FindResource("FontSizeSm"),
                FontFamily = (FontFamily)FindResource("FontFamilyPrimary"),
                Foreground = (Brush)FindResource("TextPrimary")
            };

            _bar = new ProgressBar
            {
                Minimum = 0,
                Maximum = 100,
                IsIndeterminate = true,
                Foreground = (Brush)FindResource("BrandDefault")
            };

            Body = new StackPanel { Children = { _txtMessage, _bar } };

            if (allowCancel)
            {
                var btnCancel = new Btn { Text = LocalizationService.Get("Str.Cancel"), Variant = "secondary", Size = "sm" };
                btnCancel.Click += (s, e) => CancelRequested?.Invoke(this, EventArgs.Empty);
                Footer = btnCancel;
            }
            else
            {
                Footer = new Border { Height = 1 };
            }
        }

        public void SetProgress(double percent, string message = null)
        {
            _bar.IsIndeterminate = false;
            _bar.Value = Math.Clamp(percent, 0, 100);
            if (message != null) _txtMessage.Text = message;
        }

        protected override void OnEscapePressed()
        {
            // لا يُغلق بـ ESC تلقائياً — يُترك التحكم لـ IProgressHandle.Close() أو زر الإلغاء إن وُجد
        }
    }
}
