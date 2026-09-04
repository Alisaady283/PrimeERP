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
    public class TreasuryService : ServiceBase, ITreasuryService
    {
        protected override string PermissionPrefix => "Treasuries";
        protected override string StringPrefix => "Str.Treasury";
        protected override string EntityName => "Treasuries";

        private readonly ITreasuryRepository _repo;
        private readonly INumberSequenceService _numbers;

        public TreasuryService(IPermissionService permissions, ISettingsProvider settings, ILocalizationService localization,
            IAuditLogger audit, ITreasuryRepository repo, INumberSequenceService numbers) : base(permissions, settings, localization, audit)
        {
            _repo = repo; _numbers = numbers;
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
                AccountCode = dto.AccountCode, BankName = dto.BankName, AccountNumber = dto.AccountNumber,
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
            entity.AccountCode = dto.AccountCode; entity.BankName = dto.BankName;
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
