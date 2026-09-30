using System.Collections.Generic;
using PrimeERP.Data.Core;
using PrimeERP.Data.Repositories;
using PrimeERP.Platform.Settings;

namespace PrimeERP.Application.Services.Ledger.Accounts
{
    /// <summary>حسابات الإعدادات</summary>
    public sealed class SettingAccounts
    {
        private readonly IAccountRepository _accounts;
        private readonly AddTreeAccount _add;
        private readonly ISettingsProvider _settings;

        public SettingAccounts(IAccountRepository accounts, AddTreeAccount add, ISettingsProvider settings)
        {
            _accounts = accounts;
            _add = add;
            _settings = settings;
        }

        /// <summary>يُعتمد الحساب إن وُجد</summary>
        public void Adopt(string key, string code)
        {
            if (!string.IsNullOrWhiteSpace(_settings.Get(key, ""))) return;
            if (_accounts.GetByCode(code) != null) _settings.SetRaw(key, code);
        }

        /// <summary>يُنشأ الحساب إن غاب</summary>
        public void Ensure(string key, string parentCode, string name)
        {
            if (!string.IsNullOrWhiteSpace(_settings.Get(key, ""))) return;

            var parent = _accounts.GetByCode(parentCode);
            if (parent == null) return;

            var code = _accounts.LeafNamed(parentCode, name)?.Code
                       ?? DbContextFactory.RunTransaction(db => _add.Run(db, parent, name)).Value?.Code;
            if (!string.IsNullOrWhiteSpace(code)) _settings.SetRaw(key, code);
        }

        /// <summary>جذرٌ صار ورقةً يعود لأبيه</summary>
        public void RepairRoots(IEnumerable<string> keys)
        {
            foreach (var key in keys)
            {
                var account = _accounts.GetByCode(_settings.Get(key, ""));
                if (account == null || !account.IsLeaf || string.IsNullOrWhiteSpace(account.ParentCode)) continue;
                _settings.SetRaw(key, account.ParentCode);
            }
        }
    }
}
