using System.Collections.Generic;
using System.Linq;

namespace PrimeERP.Platform.Permissions
{
    /// <summary>لا فعل بلا عرض</summary>
    public static class PermissionRules
    {
        public const string ViewAction = "View";

        public static IEnumerable<string> WithImpliedView(IEnumerable<string> keys)
        {
            var result = keys.ToHashSet();
            var known = PermissionKeys.All().ToHashSet();

            foreach (var view in result.Select(ViewKeyOf).Where(v => v != null && known.Contains(v)).ToList())
                result.Add(view);

            return result;
        }

        public static string ViewKeyOf(string key)
        {
            var separator = key.IndexOf('.');
            return separator <= 0 ? null : key[..separator] + "." + ViewAction;
        }
    }
}
