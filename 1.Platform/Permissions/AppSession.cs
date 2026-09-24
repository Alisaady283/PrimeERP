using System;
using System.Collections.Generic;

namespace PrimeERP.Platform.Permissions
{
    /// <summary>حالة المستخدم الحالي</summary>
    public static class AppSession
    {
        public static event EventHandler PermissionsChanged;

        public static void RaisePermissionsChanged() => PermissionsChanged?.Invoke(null, EventArgs.Empty);

        public static int    UserId      { get; private set; }
        public static string Username    { get; private set; }
        public static string DisplayName { get; private set; }
        public static int    RoleId      { get; private set; }
        public static string RoleName    { get; private set; }
        public static bool   IsAuthenticated { get; private set; }

#if DEBUG
        public static bool DevMode { get; set; } = true;
#else
        public static bool DevMode { get; } = false;
#endif

        public static bool BypassPermissions => DevMode && !IsAuthenticated;

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
