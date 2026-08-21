using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using PrimeERP.Platform.Permissions;
using PrimeERP.UI.Services;

namespace PrimeERP.UI.ViewModels.Base
{
    /// <summary>قاعدة لأي ViewModel يحتاج التحقق من الصلاحيات قبل تنفيذ أوامره أو إظهار أعمدة معيّنة.</summary>
    public abstract class PermissionAwareViewModel : BaseViewModel
    {
        // خاصية لا حقل static readonly عمداً — تُقرأ عند كل استخدام لا عند تحميل الكلاس، لتفادي قراءة
        // UIServices.Provider قبل تهيئته في App.xaml.cs.OnStartup (ترتيب تحميل static غير مضمون).
        protected static IPermissionService Permissions => UIServices.Permissions;

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
