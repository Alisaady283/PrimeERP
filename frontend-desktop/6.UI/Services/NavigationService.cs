using System;
using System.Collections.Generic;
using System.Windows.Controls;
using PrimeERP.Platform.Permissions;

namespace PrimeERP.UI.Services
{
    /// <summary>ينقل بين صفحات مسجَّلة بمفتاح</summary>
    public class NavigationService : INavigationService
    {
        private readonly IPermissionService _permissions;
        private readonly Dictionary<string, Func<UserControl>> _pages = new();

        public NavigationService(IPermissionService permissions) => _permissions = permissions;

        public UserControl CurrentPage { get; private set; }
        public event EventHandler CurrentPageChanged;

        public void RegisterPage(string key, Func<UserControl> factory) => _pages[key] = factory;

        public bool CanNavigateTo(string key) => _permissions.Can($"{key}.View");

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
