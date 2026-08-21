using System;
using System.Collections.Generic;
using System.Windows.Controls;
using PrimeERP.Core.Permissions;

namespace PrimeERP.Services
{
    /// <summary>
    /// ينقل بين صفحات مسجَّلة بمفتاح نصي بلا معرفة بأي صفحة بعينها — AppShell/AppSidebar يسجّلان
    /// الصفحات بمفاتيحها عند الإقلاع ثم يستدعيان NavigateTo فقط. كل مفتاح صفحة يقابل صلاحية
    /// "{key}.View" بنفس تسمية PermissionKeys (مثال: "Accounts" ↔ PermissionKeys.Accounts.View).
    /// </summary>
    public class NavigationService : INavigationService
    {
        public static readonly NavigationService Instance = new();

        private readonly Dictionary<string, Func<UserControl>> _pages = new();

        public UserControl CurrentPage { get; private set; }
        public event EventHandler CurrentPageChanged;

        public void RegisterPage(string key, Func<UserControl> factory) => _pages[key] = factory;

        public bool CanNavigateTo(string key) => PermissionService.Instance.Can($"{key}.View");

        public void NavigateTo(string key)
        {
            if (!_pages.TryGetValue(key, out var factory))
                throw new InvalidOperationException($"لا صفحة مسجّلة بالمفتاح '{key}'");

            if (!CanNavigateTo(key))
                return;

            CurrentPage = factory();
            CurrentPageChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
