using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Platform.Design;
using PrimeERP.UI.Components.Feedback;
using Xunit;

namespace PrimeERP.Tests.Composition
{
    /// <summary>إغلاق التنبيه</summary>
    [Collection("WpfApplication")]
    public class ToastCloseTests : System.IDisposable
    {
        private readonly TestDatabaseFixture _db = new();
        public void Dispose() => _db.Dispose();

        [Fact]
        public void ClickingCloseButton_RaisesDismissed_OnErrorToast()
        {
            WpfApplicationFixture.Run(() =>
            {
                _db.Services.GetRequiredService<IIdentityService>().Initialize();

                var toast = new AppToast { Message = "القيد غير متوازن", Variant = "error", Duration = 0 };

                var window = new Window { Content = toast, Width = 320, Height = 100, ShowInTaskbar = false, WindowStyle = WindowStyle.None, ShowActivated = false };
                window.Show();
                window.UpdateLayout();

                var dismissed = false;
                toast.Dismissed += (_, __) => dismissed = true;

                var closeButton = FindVisualChild<Button>(toast);
                Assert.NotNull(closeButton);
                closeButton.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));

                Assert.True(dismissed);
                window.Close();
            });
        }

        private static T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T typed) return typed;
                var nested = FindVisualChild<T>(child);
                if (nested != null) return nested;
            }
            return null;
        }
    }
}
