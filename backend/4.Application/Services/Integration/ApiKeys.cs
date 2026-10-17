using System;
using System.Security.Cryptography;
using System.Text;
using PrimeERP.Application.Services.Core;
using PrimeERP.Data.Repositories;
using PrimeERP.Domain.Enums;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Audit;
using PrimeERP.Platform.Localization;
using PrimeERP.Platform.Permissions;
using PrimeERP.Platform.Settings;

namespace PrimeERP.Application.Services.Integration
{
    /// <summary>مفتاحٌ عشوائي (32 بايت) يُعرض مرةً، وتُحفظ بصمته SHA-256 وأوّله للتعرّف</summary>
    public static class ApiKeys
    {
        public const string Header = "X-Api-Key";

        public static (string Plain, string Prefix, string Hash) Generate()
        {
            var plain = "pk_" + Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
            return (plain, plain[..10], Hash(plain));
        }

        public static string Hash(string plain) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(plain))).ToLowerInvariant();
    }

    public interface IApiKeyService
    {
        /// <summary>مفتاحٌ جديد للصف (يُبطل السابق) — يُعاد نصّه مرةً واحدة</summary>
        Result<string> Issue(int id);
    }

    public sealed class ApiKeyService : ServiceBase, IApiKeyService
    {
        private readonly IApiKeyRepository _keys;

        public ApiKeyService(IApiKeyRepository keys, IPermissionService permissions, ISettingsProvider settings,
            ILocalizationService localization, IAuditLogger audit) : base(permissions, settings, localization, audit) => _keys = keys;

        protected override string PermissionPrefix => PermissionKeys.GateOf("ApiKeys", "ApiKeys");
        protected override string StringPrefix => "Str.ApiKey";
        protected override string EntityName => "ApiKeys";

        public Result<string> Issue(int id)
        {
            if (!Can("Edit")) return FailDenied<string>();

            var (plain, prefix, hash) = ApiKeys.Generate();
            _keys.SetKey(id, prefix, hash);
            Audit.Log(EntityName, id, AuditAction.Update, details: Msg("Issued", prefix));
            return Result.Ok(plain);
        }
    }
}
