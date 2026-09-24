using PrimeERP.Data.Core;
using System.Collections.Generic;
using System.Linq;
using PrimeERP.Application.DTOs.Treasury;
using PrimeERP.Data.Repositories;
using PrimeERP.Domain.Enums;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Audit;
using PrimeERP.Platform.Localization;
using PrimeERP.Platform.Permissions;
using PrimeERP.Platform.Settings;
using Entity = PrimeERP.Domain.Entities.Treasury;

namespace PrimeERP.Application.Services.Treasury
{
    /// <summary>الخزائن والبنوك وحساباتها</summary>
    public class TreasuryService : ServiceBase, ITreasuryService, IAccountLinkedService
    {
        protected override string PermissionPrefix => "Treasuries";
        protected override string StringPrefix => "Str.Treasury";
        protected override string EntityName => "Treasuries";

        private readonly ITreasuryRepository _repo;
        private readonly INumberSequenceService _numbers;
        private readonly PrimeERP.Application.Services.Accounting.IAccountService _accounts;
        private readonly IJournalRepository _journalRepo;
        private readonly ISettingsProvider _settingsProvider;

        public TreasuryService(IPermissionService permissions, ISettingsProvider settings, ILocalizationService localization,
            IAuditLogger audit, ITreasuryRepository repo, INumberSequenceService numbers,
            PrimeERP.Application.Services.Accounting.IAccountService accounts, IJournalRepository journalRepo)
            : base(permissions, settings, localization, audit)
        {
            _repo = repo; _numbers = numbers; _accounts = accounts; _journalRepo = journalRepo; _settingsProvider = settings;
        }

        private Result<string> EnsureAccount(string accountCode, string name, bool isBank)
        {
            if (!string.IsNullOrWhiteSpace(accountCode)) return Result.Ok(accountCode);

            var rootLabel = isBank ? "البنوك" : "الصناديق";
            var parentCode = _settingsProvider.Get(isBank ? SettingKeys.Accounts.Bank : SettingKeys.Accounts.Cash, isBank ? "1203" : "1204");

            var parent = _accounts.GetByCode(parentCode);
            if (parent.IsFailure)
                return Result.Fail<string>($"حساب «{rootLabel}» المضبوط في الإعدادات ({parentCode}) غير موجود في شجرة الحسابات", ErrorCode.ValidationFailed);

            if (parent.Value.IsLeaf)
                return Result.Fail<string>($"حساب «{rootLabel}» ({parentCode}) ورقي — لا يقبل حسابات تحته. اجعله تجميعياً أو غيّره من الإعدادات", ErrorCode.ValidationFailed);

            var created = _accounts.Create(new PrimeERP.Application.DTOs.Accounting.CreateAccountDto
            { ParentId = parent.Value.Id, Name = name, IsLeaf = true, SkipAutoLink = true });

            return created.IsSuccess
                ? Result.Ok(created.Value.Code)
                : Result.Fail<string>($"تعذّر إنشاء حساب «{name}» تحت {rootLabel}: {created.ErrorMessage}", created.ErrorCode);
        }

        public Result CreateFromAccount(PrimeDbContext db, string accountCode, string name, string rootCode)
        {
            if (_repo.GetByAccountCode(accountCode, db) != null) return Result.Ok();

            var bankRoot = _settingsProvider.Get(SettingKeys.Accounts.Bank, "1203");

            var entity = new Entity
            {
                Code = _numbers.Next(db, "Treasury"), Name = name,
                Kind = rootCode == bankRoot ? TreasuryKind.Bank : TreasuryKind.Cash,
                AccountCode = accountCode, IsActive = true
            };
            entity.Id = _repo.Insert(entity, db);
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

        public Result RepairLinkedRoots()
        {
            foreach (var key in SettingKeys.Accounts.LinkedRoots)
            {
                var code = _settingsProvider.Get(key, "");
                if (string.IsNullOrWhiteSpace(code)) continue;

                var account = _accounts.GetByCode(code);
                if (account.IsFailure || !account.Value.IsLeaf || string.IsNullOrWhiteSpace(account.Value.ParentCode)) continue;

                _settingsProvider.SetRaw(key, account.Value.ParentCode);
            }
            return Result.Ok();
        }

        public Result RepairMissingAccounts()
        {
            foreach (var treasury in _repo.GetAll().Where(t => string.IsNullOrWhiteSpace(t.AccountCode)))
            {
                var isBank = treasury.Kind == TreasuryKind.Bank;
                var root = _settingsProvider.Get(isBank ? SettingKeys.Accounts.Bank : SettingKeys.Accounts.Cash, isBank ? "1203" : "1204");

                var existing = _accounts.GetLeaves().Value?
                    .FirstOrDefault(leaf => leaf.Name == treasury.Name
                                         && (leaf.Code ?? "").StartsWith(root)
                                         && _repo.GetByAccountCode(leaf.Code) == null);

                var account = existing != null ? Result.Ok(existing.Code) : EnsureAccount(null, treasury.Name, isBank);
                if (account.IsFailure) continue;

                treasury.AccountCode = account.Value;
                _repo.Update(treasury);
            }

            return Result.Ok();
        }

        public Result SeedDefaults()
        {
            if (_repo.GetAll(includeInactive: true).Count > 0) return Result.Ok();

            Create(new CreateTreasuryDto { Name = "الصندوق الرئيسي", IsBank = false, IsActive = true });
            Create(new CreateTreasuryDto { Name = "البنك الرئيسي",  IsBank = true,  IsActive = true });
            return Result.Ok();
        }

        public Result<List<TreasuryDto>> GetAll(bool includeInactive = false) =>
            Result.Ok(_repo.GetAll(includeInactive).Select(ToDto).ToList());

        public Result<TreasuryDto> GetById(int id)
        {
            var entity = _repo.GetById(id);
            return entity == null ? Result.Fail<TreasuryDto>("الخزينة غير موجودة", ErrorCode.NotFound) : Result.Ok(ToDto(entity));
        }

        public Result<TreasuryDto> Create(CreateTreasuryDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name)) return Result.Fail<TreasuryDto>("اسم الخزينة مطلوب", ErrorCode.ValidationFailed);

            var account = EnsureAccount(dto.AccountCode, dto.Name, dto.IsBank);
            if (account.IsFailure) return Result.Fail<TreasuryDto>(account.ErrorMessage, account.ErrorCode);

            var entity = new Entity
            {
                Code = _numbers.Next("Treasury"), Name = dto.Name,
                Kind = dto.IsBank ? TreasuryKind.Bank : TreasuryKind.Cash,
                AccountCode = account.Value,
                BankName = dto.BankName, AccountNumber = dto.AccountNumber,
                Notes = dto.Notes, IsActive = dto.IsActive
            };
            entity.Id = _repo.Insert(entity);

            Audit.Log(EntityName, entity.Id, AuditAction.Insert, newValue: new { entity.Code, entity.Name, entity.AccountCode });
            return Result.Ok(ToDto(entity));
        }

        public Result Update(UpdateTreasuryDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name)) return Result.Fail("اسم الخزينة مطلوب", ErrorCode.ValidationFailed);

            var entity = _repo.GetById(dto.Id);
            if (entity == null) return Result.Fail("الخزينة غير موجودة", ErrorCode.NotFound);

            var nameChanged = entity.Name != dto.Name;

            entity.Name = dto.Name;
            entity.Kind = dto.IsBank ? TreasuryKind.Bank : TreasuryKind.Cash;
            var updatedAccount = EnsureAccount(dto.AccountCode ?? entity.AccountCode, dto.Name, dto.IsBank);
            if (updatedAccount.IsFailure) return Result.Fail(updatedAccount.ErrorMessage, updatedAccount.ErrorCode);

            entity.AccountCode = updatedAccount.Value; entity.BankName = dto.BankName;
            entity.AccountNumber = dto.AccountNumber; entity.Notes = dto.Notes; entity.IsActive = dto.IsActive;

            Tx(db =>
            {
                _repo.Update(entity, db);

                if (nameChanged && !string.IsNullOrWhiteSpace(entity.AccountCode))
                    _accounts.UpdateName(db, entity.AccountCode, entity.Name);
            });

            Audit.Log(EntityName, entity.Id, AuditAction.Update, newValue: new { entity.Name, entity.AccountCode });
            return Result.Ok();
        }

        public Result Delete(int id)
        {
            var entity = _repo.GetById(id);
            if (entity == null) return Result.Fail("الخزينة غير موجودة", ErrorCode.NotFound);

            if (!string.IsNullOrWhiteSpace(entity.AccountCode) && _journalRepo.HasLinesForAccount(entity.AccountCode))
                return Result.Fail("لا يمكن حذف خزينة لها قيود مسجَّلة", ErrorCode.ValidationFailed);

            Tx(db =>
            {
                _repo.Delete(id, db);

                if (!string.IsNullOrWhiteSpace(entity.AccountCode))
                    _accounts.Delete(db, entity.AccountCode);
            });

            Audit.Log(EntityName, id, AuditAction.Delete);
            return Result.Ok();
        }

        private TreasuryDto ToDto(Entity t) => new()
        {
            Id = t.Id, Code = t.Code, Name = t.Name, Kind = t.Kind,
            KindName = t.Kind == TreasuryKind.Bank ? "بنك" : "صندوق",
            AccountCode = t.AccountCode, BankName = t.BankName, AccountNumber = t.AccountNumber,
            Balance = BalanceOf(t.AccountCode),
            Notes = t.Notes, IsActive = t.IsActive
        };

        private decimal BalanceOf(string accountCode)
        {
            if (string.IsNullOrWhiteSpace(accountCode)) return 0m;

            var account = _accounts.GetByCode(accountCode);
            return account.IsSuccess ? account.Value.Balance : 0m;
        }
    }
}
