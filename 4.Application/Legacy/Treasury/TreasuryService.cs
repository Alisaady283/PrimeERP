using PrimeERP.Application.Services.Ledger.Accounts;
using PrimeERP.Application.Services.Core;
using PrimeERP.Data.Core;
using System.Collections.Generic;
using System.Linq;
using PrimeERP.Application.Validation;
using PrimeERP.Data.Repositories;
using PrimeERP.Domain.Entities;
using PrimeERP.Domain.Enums;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Audit;
using PrimeERP.Platform.Localization;
using PrimeERP.Platform.Permissions;
using PrimeERP.Platform.Settings;
using Entity = PrimeERP.Domain.Entities.Treasury;

namespace PrimeERP.Application.Legacy.Treasury
{
    /// <summary>الخزائن والبنوك وحساباتها</summary>
    public class TreasuryService : EntityService<Entity, Entity, Entity, Entity, object>,
        ITreasuryService, IAccountLinkedService
    {
        protected override string PermissionPrefix => "Treasuries";
        protected override string StringPrefix => "Str.Treasury";
        protected override string EntityName => "Treasuries";
        protected override string SequenceKey => "Treasury";

        private static readonly Field<Entity>[] Names = { new(t => t.Name, "Str.Treasury.Name", Required: true) };
        protected override Field<Entity>[] Fields => Names;

        private readonly ITreasuryRepository _repo;
        private readonly IAccountRepository _accountRows;
        private readonly AccountSpec<Entity>[] _account;

        public TreasuryService(IPermissionService permissions, ISettingsProvider settings, ILocalizationService localization,
            IAuditLogger audit, ITreasuryRepository repo, INumberSequenceService numbers, IAccountRepository accountRows,
            AccountCases tree)
            : base(permissions, settings, localization, audit, numbers, tree)
        {
            _repo = repo;
            _accountRows = accountRows;
            _account = new[] { new AccountSpec<Entity>(Root, t => t.Name, t => t.AccountCode, (t, code) => t.AccountCode = code) };
        }

        protected override IReadOnlyList<AccountSpec<Entity>> Accounts => _account;

        private string RootCode(bool bank) =>
            Setting(bank ? SettingKeys.Accounts.Bank : SettingKeys.Accounts.Cash, bank ? "1203" : "1204");

        private Result<Account> Root(PrimeDbContext db, Entity t)
        {
            var bank = t.Kind == TreasuryKind.Bank;
            var code = RootCode(bank);
            var label = bank ? Msg("Banks") : Msg("Funds");

            return Tree.Root(db, code, "Str.Treasury.RootMissing", "Str.Treasury.RootIsLeaf", label, code);
        }

        protected override Entity FindById(int id) => _repo.GetById(id);

        protected override (List<Entity> Items, int Total) FindPaged(int page, int pageSize, object filter)
        {
            var all = _repo.GetAll(includeInactive: true);
            return (all, all.Count);
        }

        protected override List<Entity> FindSearch(string term, int maxResults) =>
            _repo.GetAll().Where(t => (t.Name ?? "").Contains(term)).Take(maxResults).ToList();

        public override Result<Entity> GetById(int id) =>
            FindById(id) is { } entity ? Result.Ok(ToDto(entity)) : Fail<Entity>("NotFound", ErrorCode.NotFound);

        public Result<List<Entity>> GetAll(bool includeInactive = false) => Result.Ok(ToDtos(_repo.GetAll(includeInactive)));

        protected override void Number(Entity t, string code) => t.Code = code;

        protected override int Insert(PrimeDbContext db, Entity t) => _repo.Insert(t, db);
        protected override void Save(PrimeDbContext db, Entity t) => _repo.Update(t, db);
        protected override void Erase(PrimeDbContext db, Entity t) => _repo.Delete(t.Id, db);

        protected override object AuditValue(Entity t) => new { t.Code, t.Name, t.AccountCode };

        public string[] RootKeys => new[] { SettingKeys.Accounts.Cash, SettingKeys.Accounts.Bank };

        public Result CreateFromAccount(PrimeDbContext db, string accountCode, string name, string rootCode)
        {
            if (_repo.GetByAccountCode(accountCode, db) != null) return Result.Ok();

            _repo.Insert(new Entity
            {
                Code = Numbers.Next(db, "Treasury"), Name = name,
                Kind = rootCode == RootCode(bank: true) ? TreasuryKind.Bank : TreasuryKind.Cash,
                AccountCode = accountCode, IsActive = true
            }, db);
            return Result.Ok();
        }

        public Result UpdateNameFromAccount(PrimeDbContext db, string accountCode, string name)
        {
            _repo.UpdateNameByAccountCode(db, accountCode, name);
            return Result.Ok();
        }

        public Result DeleteByAccountCode(PrimeDbContext db, string accountCode)
        {
            _repo.DeleteByAccountCode(db, accountCode);
            return Result.Ok();
        }

        public Result RepairMissingAccounts()
        {
            var treasuries = _repo.GetAll(includeInactive: true);
            var missing = treasuries.Where(t => t.IsActive && string.IsNullOrWhiteSpace(t.AccountCode)).ToList();
            if (missing.Count == 0) return Result.Ok();

            var linked = treasuries.Select(t => t.AccountCode).Where(c => !string.IsNullOrWhiteSpace(c)).ToHashSet();

            foreach (var treasury in missing)
            {
                var root = RootCode(treasury.Kind == TreasuryKind.Bank);
                treasury.AccountCode = _accountRows.LeafNamed(root, treasury.Name, linked)?.Code;

                var repaired = Commit(db => Tree.Add.Run(db, treasury, Accounts).Then(() =>
                {
                    _repo.Update(treasury, db);
                    return Result.Ok();
                }));

                if (repaired.IsSuccess) linked.Add(treasury.AccountCode);
            }

            return Result.Ok();
        }

        public Result SeedDefaults()
        {
            if (_repo.GetAll(includeInactive: true).Count > 0) return Result.Ok();

            Add(new Entity { Name = Msg("MainFund"), Kind = TreasuryKind.Cash, IsActive = true });
            Add(new Entity { Name = Msg("MainBank"), Kind = TreasuryKind.Bank, IsActive = true });
            return Result.Ok();
        }

        protected override Entity ToDto(Entity t) => ToDtos(new List<Entity> { t })[0];

        protected override List<Entity> ToDtos(List<Entity> treasuries)
        {
            var balances = _accountRows.GetByCodes(treasuries.Select(t => t.AccountCode).Where(c => !string.IsNullOrWhiteSpace(c)))
                                       .ToDictionary(a => a.Code, a => a.Balance);
            foreach (var t in treasuries)
            {
                t.KindName = LocalizationService.Get(t.Kind == TreasuryKind.Bank ? "Str.Treasury.Kind.Bank" : "Str.Treasury.Kind.Fund");
                t.AccountBalance = !string.IsNullOrWhiteSpace(t.AccountCode) ? balances.GetValueOrDefault(t.AccountCode) : 0m;
            }
            return treasuries;
        }
    }
}
