using System;
using System.Collections.Generic;
using System.Linq;
using PrimeERP.Application.DTOs.Accounting;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Audit;
using PrimeERP.Platform.Localization;
using PrimeERP.Platform.Permissions;
using PrimeERP.Platform.Settings;

namespace PrimeERP.Application.Services.Accounting
{
    public interface IOpeningBalanceService
    {
        Result<PagedResult<JournalEntryDto>> GetPaged(int page, int pageSize, JournalFilter filter = null);
        Result<CreateJournalDto> GetById(int id);
        Result<JournalEntryDto> Create(CreateJournalDto dto);
        Result Update(CreateJournalDto dto);
        Result Delete(int id);
    }

    /// <summary>الأرصدة الافتتاحية ليست جدولاً موازياً: الشاشة تُنشئ قيداً عادياً بمصدر OpeningBalance،
    /// فيُقرأ في كل كشف وتقرير كأي قيد. الفرق بين المدين والدائن يُرحَّل تلقائياً لحساب رأس المال — وهو
    /// ما يجعل القيد متوازناً بلا أن يحسب المستخدم الفرق بنفسه.</summary>
    public class OpeningBalanceService : ServiceBase, IOpeningBalanceService
    {
        private const string SourceKey = "OpeningBalance";

        private readonly IJournalService _journals;
        private readonly ISettingsService _settingsService;

        public OpeningBalanceService(IJournalService journals, ISettingsService settingsService,
            IPermissionService permissions, ISettingsProvider settings, ILocalizationService localization, IAuditLogger audit)
            : base(permissions, settings, localization, audit)
        {
            _journals = journals; _settingsService = settingsService;
        }

        protected override string PermissionPrefix => "Journal";
        protected override string StringPrefix => "Str.Journal";
        protected override string EntityName => "OpeningBalance";

        public Result<PagedResult<JournalEntryDto>> GetPaged(int page, int pageSize, JournalFilter filter = null)
        {
            filter ??= new JournalFilter();
            filter.Source = SourceKey;
            return _journals.GetPaged(page, pageSize, filter);
        }

        public Result<CreateJournalDto> GetById(int id)
        {
            var entry = _journals.GetById(id);
            if (entry.IsFailure) return Result.Fail<CreateJournalDto>(entry.ErrorMessage, entry.ErrorCode);

            return Result.Ok(new CreateJournalDto
            {
                Id = id, EntryDate = entry.Value.EntryDate, Description = entry.Value.Description,
                Lines = entry.Value.Lines.Select(l => new CreateJournalLineDto
                { LineNo = l.LineNo, AccountCode = l.AccountCode, Debit = l.Debit, Credit = l.Credit, Notes = l.Notes }).ToList()
            });
        }

        public Result<JournalEntryDto> Create(CreateJournalDto dto)
        {
            var balanced = Balance(dto);
            if (balanced.IsFailure) return Result.Fail<JournalEntryDto>(balanced.ErrorMessage, balanced.ErrorCode);

            return _journals.Create(balanced.Value);
        }

        public Result Update(CreateJournalDto dto)
        {
            var balanced = Balance(dto);
            if (balanced.IsFailure) return balanced;

            return _journals.Update(balanced.Value);
        }

        public Result Delete(int id) => _journals.Delete(id);

        /// <summary>يُلحق سطر رأس المال بفرق الطرفين — صفر الفرق يعني قيداً متوازناً أصلاً فلا سطر يُضاف.</summary>
        private Result<CreateJournalDto> Balance(CreateJournalDto dto)
        {
            dto.Source = SourceKey;
            dto.Lines = dto.Lines?.Where(l => !string.IsNullOrWhiteSpace(l.AccountCode)).ToList() ?? new List<CreateJournalLineDto>();

            if (dto.Lines.Count == 0)
                return Result.Fail<CreateJournalDto>("أضف سطراً واحداً على الأقل", ErrorCode.ValidationFailed);

            var difference = dto.Lines.Sum(l => l.Debit) - dto.Lines.Sum(l => l.Credit);
            if (difference == 0) return Result.Ok(dto);

            var capitalAccount = _settingsService.Get<string>(SettingKeys.Accounts.RetainedEarnings, "");
            if (string.IsNullOrWhiteSpace(capitalAccount))
                return Result.Fail<CreateJournalDto>("حساب حقوق الملكية غير مضبوط في الإعدادات — الفرق يُرحَّل إليه", ErrorCode.ValidationFailed);

            dto.Lines.Add(new CreateJournalLineDto
            {
                LineNo = dto.Lines.Max(l => l.LineNo) + 1,
                AccountCode = capitalAccount,
                Debit = difference < 0 ? -difference : 0,
                Credit = difference > 0 ? difference : 0,
                Notes = "فرق الأرصدة الافتتاحية"
            });

            return Result.Ok(dto);
        }

    }
}
