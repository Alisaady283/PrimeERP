using System.Collections.Generic;

namespace PrimeERP.Core.Permissions
{
    public interface IPermissionService
    {
        bool Can(string key);
        bool CanAny(params string[] keys);
        bool CanAll(params string[] keys);
        void LoadForUser(int userId);
        IEnumerable<string> GetUserPermissions(int userId);
    }
}
