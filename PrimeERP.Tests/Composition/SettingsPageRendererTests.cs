using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Application.Services;
using PrimeERP.Composition.Registry;
using PrimeERP.Composition.Renderers;
using PrimeERP.Platform.Design;
using PrimeERP.Platform.Permissions;
using PrimeERP.Platform.Settings;
using PrimeERP.UI.Components.Actions;
using PrimeERP.UI.Components.Inputs;
using PrimeERP.UI.Services;
using Xunit;
using PrimeERP.Application.Services.Admin;

namespace PrimeERP.Tests.Composition
{
    /// <summary>صفحة الإعدادات</summary>
    [Collection("WpfApplication")]
    public class SettingsPageRendererTests : System.IDisposable
    {
        private readonly TestDatabaseFixture _db = new();
        public SettingsPageRendererTests() => AppSession.DevMode = true;
        public void Dispose() => _db.Dispose();

        [Fact]
        public void ChangingCompanyName_AndSaving_PersistsThroughSettingsService()
        {
            WpfApplicationFixture.Run(() =>
            {
                UIServices.Initialize(_db.Services);
                _db.Services.GetRequiredService<IIdentityService>().Initialize();

                var registry = _db.Services.GetRequiredService<IModuleRegistry>();
                var definition = registry.Get("Settings");

                var element = PageRenderer.Render(definition, _db.Services);
                element.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));

                var window = new Window { Content = element, Width = 1200, Height = 900, ShowInTaskbar = false, WindowStyle = WindowStyle.None, ShowActivated = false };
                window.Show();
                window.UpdateLayout();

                var nameBox = FindAllVisualChildren<AppTextBox>(element).First(b => b.Label == "اسم الشركة");
                nameBox.Text = "شركة الاختبار";

                var saveText = PrimeERP.Platform.Localization.LocalizationService.Get("Str.Save");
                var saveButton = FindAllVisualChildren<AppButton>(element).First(b => b.Text == saveText);
                var innerButton = FindVisualChild<Button>(saveButton);
                innerButton.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));

                var settings = _db.Services.GetRequiredService<ISettingsService>();
                Assert.Equal("شركة الاختبار", settings.Get<string>(SettingKeys.Company.Name, ""));
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

        private static System.Collections.Generic.List<T> FindAllVisualChildren<T>(DependencyObject parent) where T : DependencyObject
        {
            var results = new System.Collections.Generic.List<T>();
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T typed) results.Add(typed);
                results.AddRange(FindAllVisualChildren<T>(child));
            }
            return results;
        }
    }
}
