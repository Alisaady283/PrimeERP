using PrimeERP.Application.Services.Entities;
using PrimeERP.Application.Services.Ledger;
using PrimeERP.Application.Services.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using PrimeERP.Application.DTOs.Accounting;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Audit;
using PrimeERP.Platform.Localization;
using PrimeERP.Platform.Permissions;
using PrimeERP.Platform.Settings;
using PrimeERP.Application.PageServices.Inventory;

namespace PrimeERP.Application.PageServices.Accounting
{
    /// <summary>الأرصدة الافتتاحية ليست جدولاً موازياً</summary>
    public interface IOpeningBalanceService
    {
        Result<PagedResult<JournalEntryDto>> GetPaged(int page, int pageSize, JournalFilter filter = null);
        Result<CreateJournalDto> GetById(int id);
        Result<JournalEntryDto> Create(CreateJournalDto dto);
        Result Update(CreateJournalDto dto);
        Result Delete(int id);
    }

    public class OpeningBalanceService : ServiceBase, IOpeningBalanceService
    {
        private const string SourceKey = "OpeningBalance";

        public static string FixedDescription => LocalizationService.Get("Str.Journal.OpeningDescription");

        private readonly IJournalService _journals;
        private readonly IOpeningStockService _stock;

        public OpeningBalanceService(IJournalService journals, IOpeningStockService stock,
            IPermissionService permissions, ISettingsProvider settings, ILocalizationService localization, IAuditLogger audit)
            : base(permissions, settings, localization, audit)
        {
            _journals = journals;
            _stock = stock;
        }

        protected override string PermissionPrefix => "Journal";
        protected override string StringPrefix => "Str.Journal";
        protected override string EntityName => "OpeningBalance";

        public Result<PagedResult<JournalEntryDto>> GetPaged(int page, int pageSize, JournalFilter filter = null)
        {
            filter ??= new JournalFilter();
            filter.Sources = new[] { SourceKey, OpeningStockService.SourceKey };
            return _journals.GetPaged(page, pageSize, filter);
        }

        public Result<CreateJournalDto> GetById(int id)
        {
            var entry = _journals.GetById(id);
            if (entry.IsFailure) return entry.As<CreateJournalDto>();

            return Result.Ok(Rows.Copy(entry.Value, new CreateJournalDto
            {
                Lines = entry.Value.Lines.Select(l => Rows.Copy(l, new CreateJournalLineDto())).ToList()
            }));
        }

        public Result<JournalEntryDto> Create(CreateJournalDto dto) => _journals.Create(Opening(dto));

        public Result Update(CreateJournalDto dto) => _journals.UpdateOwned(Opening(dto), SourceKey);

        public Result Delete(int id) =>
            _journals.GetById(id) is { IsSuccess: true, Value.Source: OpeningStockService.SourceKey }
                ? _stock.Delete(id)
                : _journals.DeleteOwned(id, SourceKey);

        private CreateJournalDto Opening(CreateJournalDto dto) =>
            OpeningEntry.Prepare(dto, Setting(SettingKeys.Company.StartDate, dto.EntryDate), SourceKey, FixedDescription);
    }
}
