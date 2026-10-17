using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using PrimeERP.Platform.Permissions;

namespace PrimeERP.UI.ViewModels.Base
{
    /// <summary>قاعدة لأي ViewModel يحتاج التحقق</summary>
    public abstract class PermissionAwareViewModel : BaseViewModel
    {
        protected readonly IPermissionService Permissions;

        protected PermissionAwareViewModel(IPermissionService permissions) => Permissions = permissions;

        protected bool Can(string key) => Permissions.Can(key);

        protected ICommand GuardedCommand(Action action, Func<string> permissionKey) =>
            new RelayCommand(
                () => { if (Can(permissionKey())) action(); },
                () => Can(permissionKey()));

        protected ICommand GuardedCommand(Action<object> action, Func<string> permissionKey) =>
            new RelayCommand(
                param => { if (Can(permissionKey())) action(param); },
                param => Can(permissionKey()));

        protected void ApplyColumnPermissions(DataGrid grid, Dictionary<string, string> columnPermissionMap)
        {
            foreach (var column in grid.Columns)
            {
                var header = column.Header?.ToString();
                if (header != null && columnPermissionMap.TryGetValue(header, out var key))
                    column.Visibility = Can(key) ? Visibility.Visible : Visibility.Collapsed;
            }
        }
    }
}
