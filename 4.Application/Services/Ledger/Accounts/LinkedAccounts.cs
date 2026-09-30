using System;
using System.Collections.Generic;
using System.Linq;
using PrimeERP.Platform.Settings;

namespace PrimeERP.Application.Services.Ledger.Accounts
{
    /// <summary>كيان الجذر المرتبط</summary>
    public sealed class LinkedAccounts
    {
        private readonly IServiceProvider _services;
        private readonly ISettingsProvider _settings;

        public LinkedAccounts(IServiceProvider services, ISettingsProvider settings)
        {
            _services = services;
            _settings = settings;
        }

        public (IAccountLinkedService Linked, string Root) Of(string rootCode, bool skip = false)
        {
            if (skip || string.IsNullOrWhiteSpace(rootCode) || !_settings.Get(SettingKeys.Accounts.AutoLinkEnabled, true)) return default;

            var all = _services.GetService(typeof(IEnumerable<IAccountLinkedService>)) as IEnumerable<IAccountLinkedService>;
            return (all ?? Enumerable.Empty<IAccountLinkedService>())
                .SelectMany(linked => linked.RootKeys.Select(key => (linked, _settings.Get(key, ""))))
                .FirstOrDefault(pair => pair.Item2 == rootCode);
        }

        public bool IsRoot(string code) =>
            !string.IsNullOrWhiteSpace(code) && SettingKeys.Accounts.LinkedRoots.Any(key => _settings.Get(key, "") == code);
    }
}
