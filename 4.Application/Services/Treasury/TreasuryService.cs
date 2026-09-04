using System.Data.Common;
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
    public class TreasuryService : ServiceBase, ITreasuryService, IAccountLinkedService
    {
        protected override string PermissionPrefix => "Treasuries";
        protected override string StringPrefix => "Str.Treasury";
        protected override string EntityName => "Treasuries";

        private readonly ITreasuryRepository _repo;
        private readonly INumberSequenceService _numbers;
        private readonly PrimeERP.Application.Services.Accounting.IAccountService _accounts;
        private readonly ISettingsProvider _settingsProvider;

        public TreasuryService(IPermissionService permissions, ISettingsProvider settings, ILocalizationService localization,
            IAuditLogger audit, ITreasuryRepository repo, INumberSequenceService numbers,
            PrimeERP.Application.Services.Accounting.IAccountService accounts) : base(permissions, settings, localization, audit)
        {
            _repo = repo; _numbers = numbers; _accounts = accounts; _settingsProvider = settings;
        }

        /// <summary>الخزينة حساب ورقي تحت "الصناديق" والبنك تحت "البنوك" — يُنشأ تلقائياً عند ترك الحساب فارغاً،
        /// فلا يضطر المستخدم لبناء الحساب يدوياً قبل إنشاء الخزينة.</summary>
        private string EnsureAccount(string accountCode, string name, bool isBank)
        {
            if (!string.IsNullOrWhiteSpace(accountCode)) return accountCode;

            var parentCode = _settingsProvider.Get(isBank ? SettingKeys.Accounts.Bank : SettingKeys.Accounts.Cash, isBank ? "1203" : "1204");
            var parent = _accounts.GetByCode(parentCode);
            if (parent.IsFailure) return null;

            var created = _accounts.Create(new PrimeERP.Application.DTOs.Accounting.CreateAccountDto
            { ParentId = parent.Value.Id, Name = name, IsLeaf = true, SkipAutoLink = true });

            return created.IsSuccess ? created.Value.Code : null;
        }

        /// <summary>الاتجاه المعاكس — إنشاء حساب ورقي تحت "الصناديق"/"البنوك" في الشجرة يُنشئ خزينته هنا.
        /// النوع يُستنتَج من الأصل الذي وقع تحته الحساب، فلا يحتاج المستخدم لتكرار الاختيار.</summary>
        public Result CreateFromAccount(DbConnection conn, DbTransaction tx, string accountCode, string name, string rootCode)
        {
            if (_repo.GetByAccountCode(accountCode, conn, tx) != null) return Result.Ok();

            var bankRoot = _settingsProvider.Get(SettingKeys.Accounts.Bank, "1203");

            var entity = new Entity
            {
                Code = _numbers.Next(conn, tx, "Treasury"), Name = name,
                Kind = rootCode == bankRoot ? TreasuryKind.Bank : TreasuryKind.Cash,
                AccountCode = accountCode, IsActive = true
            };
            entity.Id = _repo.Insert(entity, conn, tx);
            return Result.Ok();
        }

        public Result UpdateNameFromAccount(DbConnection conn, DbTransaction tx, string accountCode, string name)
        {
            _repo.UpdateNameByAccountCode(conn, tx, accountCode, name);
            return Result.Ok();
        }

        public Result DeleteByAccountCode(DbConnection conn, DbTransaction tx, string accountCode)
        {
            _repo.DeleteByAccountCode(conn, tx, accountCode);
            return Result.Ok();
        }

        /// <summary>خزينة وبنك افتراضيان عند أول تشغيل — بلا هذا تبقى قوائم السندات فارغة فيبدو أنها لا تعمل.</summary>
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

            var entity = new Entity
            {
                Code = _numbers.Next("Treasury"), Name = dto.Name,
                Kind = dto.IsBank ? TreasuryKind.Bank : TreasuryKind.Cash,
                AccountCode = EnsureAccount(dto.AccountCode, dto.Name, dto.IsBank),
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

            entity.Name = dto.Name;
            entity.Kind = dto.IsBank ? TreasuryKind.Bank : TreasuryKind.Cash;
            entity.AccountCode = EnsureAccount(dto.AccountCode, dto.Name, dto.IsBank); entity.BankName = dto.BankName;
            entity.AccountNumber = dto.AccountNumber; entity.Notes = dto.Notes; entity.IsActive = dto.IsActive;

            _repo.Update(entity);
            Audit.Log(EntityName, entity.Id, AuditAction.Update, newValue: new { entity.Name, entity.AccountCode });
            return Result.Ok();
        }

        public Result Delete(int id)
        {
            if (_repo.GetById(id) == null) return Result.Fail("الخزينة غير موجودة", ErrorCode.NotFound);
            _repo.Delete(id);
            Audit.Log(EntityName, id, AuditAction.Delete);
            return Result.Ok();
        }

        private static TreasuryDto ToDto(Entity t) => new()
        {
            Id = t.Id, Code = t.Code, Name = t.Name, Kind = t.Kind,
            KindName = t.Kind == TreasuryKind.Bank ? "بنك" : "صندوق",
            AccountCode = t.AccountCode, BankName = t.BankName, AccountNumber = t.AccountNumber,
            Notes = t.Notes, IsActive = t.IsActive
        };
    }
}
