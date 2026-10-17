using System.Collections.Generic;
using System.Linq;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Localization;
using PrimeERP.Platform.Permissions;
using PrimeERP.Platform.Security;

namespace PrimeERP.Application.PageServices.Security
{
    /// <summary>ط§ظ„ظ…ط³طھط®ط¯ظ… ط¨ط¹ط¯ ط§ظ„طھط­ظ‚ظ‚: ظ‡ظˆظٹطھظ‡ ظˆطµظ„ط§ط­ظٹط§طھظ‡ ط§ظ„ظپط¹ظ„ظٹط©</summary>
    public sealed record SignedInUser(int Id, string Username, string DisplayName, int RoleId, string RoleName, int? BranchId, IReadOnlyList<string> Permissions);

    /// <summary>ط§ظ„طھط­ظ‚ظ‚ ظ…ظ† ط§ظ„ط¯ط®ظˆظ„ â€” ظ…ط¯ط®ظ„ظŒ ظˆط§ط­ط¯ ظ„ظ†ط§ظپط°ط© ط³ط·ط­ ط§ظ„ظ…ظƒطھط¨ ظˆظ„ظ„ط®ط§ط¯ظ…</summary>
    public interface IAuthService
    {
        Result<SignedInUser> Verify(string username, string password);
        /// <summary>مفتاح نظامٍ خارجي ← مستخدمه بصلاحياته</summary>
        Result<SignedInUser> VerifyKey(string key);
    }

    public class AuthService : IAuthService
    {
        private readonly IPermissionStore _store;
        private readonly IPermissionService _permissions;
        private readonly PrimeERP.Data.Repositories.IApiKeyRepository _keys;

        public AuthService(IPermissionStore store, IPermissionService permissions, PrimeERP.Data.Repositories.IApiKeyRepository keys)
        {
            _store = store;
            _permissions = permissions;
            _keys = keys;
        }

        public Result<SignedInUser> VerifyKey(string key)
        {
            var found = string.IsNullOrWhiteSpace(key) ? null : _keys.ByHash(PrimeERP.Application.Services.Integration.ApiKeys.Hash(key.Trim()));
            var user = found == null || found.ExpiresAt < System.DateTime.Now ? null : _store.GetAllUsers().FirstOrDefault(u => u.Id == found.UserId);
            if (user == null || !user.IsActive)
                return Result.Fail<SignedInUser>(LocalizationService.Get("Str.ApiKey.Invalid"), ErrorCode.Unauthorized);

            _keys.Touch(found.Id, System.DateTime.Now);
            return Result.Ok(new SignedInUser(user.Id, user.Username, user.DisplayName, user.RoleId, user.RoleName, user.BranchId,
                _permissions.GetUserPermissions(user.Id).ToList()));
        }

        public Result<SignedInUser> Verify(string username, string password)
        {
            var user = _store.FindByUsername(username?.Trim() ?? "");
            if (user == null || !PasswordHasher.Verify(password ?? "", user.PasswordHash, user.Salt))
                return Result.Fail<SignedInUser>(LocalizationService.Get("Str.Login.InvalidCredentials"), ErrorCode.Unauthorized);
            if (!user.IsActive)
                return Result.Fail<SignedInUser>(LocalizationService.Get("Str.Login.AccountInactive"), ErrorCode.Unauthorized);

            var permissions = _permissions.GetUserPermissions(user.Id).ToList();
            _store.UpdateLastLogin(user.Id);
            return Result.Ok(new SignedInUser(user.Id, user.Username, user.DisplayName, user.RoleId, user.RoleName, user.BranchId, permissions));
        }
    }
}
