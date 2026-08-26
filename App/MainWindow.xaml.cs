using System;
using System.Linq;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Application.Services;
using PrimeERP.Composition.Registry;
using PrimeERP.Composition.Renderers;
using PrimeERP.Platform.Localization;
using PrimeERP.Platform.Permissions;
using PrimeERP.Platform.Settings;
using PrimeERP.UI.Components.Shell;

namespace PrimeERP.App
{
    /// <summary>القطعة الوحيدة هنا AppShell — التنقل بين الوحدات المسجَّلة عبر IModuleRegistry، كل صفحة تُبنى
    /// عند الطلب فقط (لا كل الوحدات دفعة واحدة) عبر PageRenderer (يوزّع حسب ModuleDefinition.LayoutKind على
    /// CrudPageRenderer أو TreeRenderer — R11). مُنشأة يدوياً من App.xaml.cs.OnStartup (لا StartupUri)، فحقن
    /// IServiceProvider مباشر عبر المُنشئ ممكن — لا استدعاء XAML ضمني هنا.</summary>
    public partial class MainWindow : Window
    {
        private readonly IServiceProvider _services;
        private readonly IModuleRegistry _registry;

        public MainWindow(IServiceProvider services)
        {
            InitializeComponent();
            _services = services;
            _registry = services.GetRequiredService<IModuleRegistry>();

            shell.CompanyName = services.GetRequiredService<ISettingsService>().Get(SettingKeys.Company.Name, "PrimeERP");
            shell.UserName = AppSession.DisplayName;
            shell.UserRole = AppSession.RoleName;

            shell.NavItems = _registry.All().Select(m => new NavItem
            {
                Key = m.Key,
                Text = LocalizationService.Get(m.TitleKey),
                PermissionKey = $"{m.PermissionPrefix}.View"
            }).ToList();

            var first = _registry.All().FirstOrDefault();
            if (first != null)
            {
                shell.SelectedKey = first.Key;
                shell.CurrentPage = PageRenderer.Render(first, _services);
            }
        }

        private void Shell_NavigationRequested(object sender, string key)
        {
            var definition = _registry.Get(key);
            if (definition == null) return;

            shell.CurrentPage = PageRenderer.Render(definition, _services);
        }

        private void Shell_LogoutRequested(object sender, EventArgs e)
        {
            // بسيط عمداً — إعادة تسجيل دخول بلا إعادة تشغيل التطبيق كاملاً تحتاج إعادة بناء نافذة تسجيل
            // الدخول من الصفر داخل نفس العملية؛ خارج نطاق R9. الإغلاق هنا كافٍ ومطلوب أمنياً (صفر بيانات
            // جلسة سابقة متبقية بعد Close — AppSession.SignOut يُفرّغها قبل الإغلاق).
            AppSession.SignOut();
            Close();
        }
    }
}
