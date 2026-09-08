using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Composition.Registry;
using PrimeERP.Composition.Renderers;
using PrimeERP.Platform.Design;
using PrimeERP.Platform.Permissions;
using PrimeERP.UI.Components.Actions;
using PrimeERP.UI.Services;
using Xunit;

namespace PrimeERP.Tests.Composition
{
    /// <summary>
    /// زرّ «جديد» المُصيَّر فعلياً — لا نموذج الإجراء المجرّد. من يملك الصلاحية يجده مفعَّلاً في كل وحدة.
    /// كان أربع وحدات تعرضه معطَّلاً: بادئة صلاحيتها حقلٌ يُضبط بعد مُنشئ القاعدة، فتجمّد المفتاح ناقصاً.
    /// </summary>
    [Collection("WpfApplication")]
    public class AddButtonEnabledTests : IDisposable
    {
        private readonly TestDatabaseFixture _db = new();
        public void Dispose() => _db.Dispose();

        [Fact]
        public void EveryModule_EnablesAddForWhoeverHoldsThePermission()
        {
            WpfApplicationFixture.Run(() =>
            {
                UIServices.Initialize(_db.Services);
                _db.Services.GetRequiredService<IIdentityService>().Apply("Default");

                // صلاحيات حقيقية لا وضع تطوير: البوابة نفسها هي المفحوصة. AppSession حالة ساكنة يتقاسمها
                // كل الاختبارات، فما يُستعار منها يُعاد — وإلا وجدت اختبارات «بلا صلاحية» صلاحياتٍ ممنوحة.
                var previousDevMode = AppSession.DevMode;
                var previousPermissions = AppSession.Permissions.ToList();
                AppSession.DevMode = false;
                AppSession.Permissions.Clear();
                foreach (var key in PermissionKeys.All()) AppSession.Permissions.Add(key);

                try
                {
                    var disabled = new List<string>();

                    foreach (var definition in _db.Services.GetRequiredService<IModuleRegistry>().All()
                                 .Where(d => d.Dialog != null || d.DocumentDialog != null))
                    {
                        var page = CrudPageRenderer.Render(definition, _db.Services);
                        page.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));
                        page.Measure(new Size(1400, 900));
                        page.Arrange(new Rect(0, 0, 1400, 900));
                        page.UpdateLayout();

                        var add = Descendants<AppButton>(page).FirstOrDefault(b => b.Text == "جديد");
                        Assert.True(add != null, $"{definition.Key}: زرّ جديد غائب رغم امتلاك الصلاحية");

                        // الزرّ الداخلي هو ما يراه المستخدم؛ IsEnabled عليه يتبع CanExecute للأمر.
                        if (!Descendants<Button>(add).First().IsEnabled)
                            disabled.Add(definition.Key);
                    }

                    Assert.True(disabled.Count == 0,
                        "زرّ جديد معطَّل رغم امتلاك الصلاحية في: " + string.Join("، ", disabled));
                }
                finally
                {
                    AppSession.DevMode = previousDevMode;
                    AppSession.Permissions.Clear();
                    foreach (var key in previousPermissions) AppSession.Permissions.Add(key);
                }
            });
        }

        private static IEnumerable<T> Descendants<T>(DependencyObject root) where T : DependencyObject
        {
            if (root is T typed) yield return typed;

            for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
                foreach (var found in Descendants<T>(VisualTreeHelper.GetChild(root, i)))
                    yield return found;
        }
    }
}
