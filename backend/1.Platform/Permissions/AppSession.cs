using System;
using System.Collections.Generic;
using System.Threading;

namespace PrimeERP.Platform.Permissions
{
    /// <summary>حالة المستخدم الحالي: مشتركةٌ في سطح المكتب، ولكل طلبٍ حالته على الخادم (Enter)</summary>
    public static class AppSession
    {
        private sealed class State
        {
            public int    UserId;
            public string Username;
            public string DisplayName;
            public int    RoleId;
            public string RoleName;
            public int?   BranchId;
            public bool   IsAuthenticated;
            public readonly HashSet<string> Permissions = new();
        }

        private static readonly State Shared = new();
        private static readonly AsyncLocal<State> Scoped = new();
        private static State Current => Scoped.Value ?? Shared;

        public static event EventHandler PermissionsChanged;

        public static void RaisePermissionsChanged() => PermissionsChanged?.Invoke(null, EventArgs.Empty);

        public static int    UserId      => Current.UserId;
        public static string Username    => Current.Username;
        public static string DisplayName => Current.DisplayName;
        public static int    RoleId      => Current.RoleId;
        public static string RoleName    => Current.RoleName;
        public static int?   BranchId    => Current.BranchId;
        public static bool   IsAuthenticated => Current.IsAuthenticated;

#if DEBUG
        public static bool DevMode { get; set; } = true;
#else
        public static bool DevMode { get; } = false;
#endif

        public static bool BypassPermissions => DevMode && !IsAuthenticated && Scoped.Value == null;

        public static HashSet<string> Permissions => Current.Permissions;

        public static IDisposable Enter()
        {
            var previous = Scoped.Value;
            Scoped.Value = new State();
            return new ScopeExit(() => Scoped.Value = previous);
        }

        public static void SignIn(int userId, string username, string displayName,
                                  int roleId, string roleName, IEnumerable<string> permissions, int? branchId = null)
        {
            var state = Current;
            state.BranchId        = branchId;
            state.UserId          = userId;
            state.Username        = username;
            state.DisplayName     = displayName;
            state.RoleId          = roleId;
            state.RoleName        = roleName;
            state.IsAuthenticated = true;

            var granted = new List<string>(permissions);
            state.Permissions.Clear();
            foreach (var p in granted)
                state.Permissions.Add(p);

            RaisePermissionsChanged();
        }

        public static void SignOut()
        {
            var state = Current;
            state.UserId          = 0;
            state.Username        = null;
            state.DisplayName     = null;
            state.RoleId          = 0;
            state.RoleName        = null;
            state.BranchId        = null;
            state.IsAuthenticated = false;
            state.Permissions.Clear();
            RaisePermissionsChanged();
        }
    }
}
