using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using PrimeERP.Platform.Permissions;

namespace PrimeERP.UI.ViewModels.Base
{
    /// <summary>
    /// قاعدة لأي ViewModel يحتاج التحقق من الصلاحيات قبل تنفيذ أوامره أو إظهار أعمدة معيّنة. IPermissionService
    /// يُحقَن بارامتر بناء (من DI أو من المُنشئ المباشر للـ VM) — لا UIServices هنا (راجع القيد الملزم في
    /// ARCHITECTURE.md § UIServices: صفر استهلاك من Service/VM، حصراً من code-behind).
    /// </summary>
    public abstract class PermissionAwareViewModel : BaseViewModel
    {
        protected readonly IPermissionService Permissions;

        protected PermissionAwareViewModel(IPermissionService permissions) => Permissions = permissions;

        protected bool Can(string key) => Permissions.Can(key);

        protected ICommand GuardedCommand(Action action, string permissionKey) =>
            new RelayCommand(
                () => { if (Can(permissionKey)) action(); },
                () => Can(permissionKey));

        protected ICommand GuardedCommand(Action<object> action, string permissionKey) =>
            new RelayCommand(
                param => { if (Can(permissionKey)) action(param); },
                param => Can(permissionKey));

        /// <summary>يخفي أعمدة الجدول التي لا يملك المستخدم صلاحية عرضها — المفتاح Header نص العمود كما ظهر في الـ XAML.</summary>
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
