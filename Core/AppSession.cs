using System;
using System.Collections.Generic;

namespace PrimeERP.Core
{
    /// <summary>حالة المستخدم الحالي الجارية — من سجّل دخوله وما صلاحياته.</summary>
    public static class AppSession
    {
        /// <summary>يُطلق عند أي تغيير في DevMode أو Permissions — القطع المرتبطة بالصلاحيات تعيد تقييم حالتها عند سماعه.</summary>
        public static event EventHandler PermissionsChanged;

        public static void RaisePermissionsChanged() => PermissionsChanged?.Invoke(null, EventArgs.Empty);

        public static int    UserId      { get; private set; }
        public static string Username    { get; private set; }
        public static string DisplayName { get; private set; }
        public static int    RoleId      { get; private set; }
        public static string RoleName    { get; private set; }
        public static bool   IsAuthenticated { get; private set; }

        /// <summary>في وضع التطوير يمنح كل الصلاحيات بدون تسجيل دخول — مضبوط بشرط الـ build فلا يتسرّب لنسخة الإنتاج.</summary>
#if DEBUG
        public static bool DevMode { get; set; } = true;
#else
        public static bool DevMode { get; } = false;
#endif

        public static HashSet<string> Permissions { get; } = new();

        public static void SignIn(int userId, string username, string displayName,
                                  int roleId, string roleName, IEnumerable<string> permissions)
        {
            UserId          = userId;
            Username        = username;
            DisplayName     = displayName;
            RoleId          = roleId;
            RoleName        = roleName;
            IsAuthenticated = true;

            Permissions.Clear();
            foreach (var p in permissions)
                Permissions.Add(p);

            RaisePermissionsChanged();
        }

        public static void SignOut()
        {
            UserId          = 0;
            Username        = null;
            DisplayName     = null;
            RoleId          = 0;
            RoleName        = null;
            IsAuthenticated = false;
            Permissions.Clear();
            RaisePermissionsChanged();
        }
    }
}
