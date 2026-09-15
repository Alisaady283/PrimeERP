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

        /// <summary>بيان القيد الافتتاحي ثابت: قيدٌ واحد للمنشأة لا معنى لبيانٍ يكتبه المستخدم فيه.
        /// تفرضه الخدمة والشاشة تعرضه للقراءة — فالقيمة واحدة أياً كان مصدر الاستدعاء.</summary>
        public const string FixedDescription = "رصيد أول المدة";

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

        /// <summary>يُنشئ ويُرحّل معاً: القيد المسودّة لا تقرأه التقارير (تقرأ المُرحَّل فقط)، فرصيد
        /// افتتاحي غير مُرحَّل يبقى غائباً عن الميزان والكشوف وكأنه لم يُدخَل.</summary>
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

            return _journals.UpdateOwned(balanced.Value, SourceKey);
        }

        public Result Delete(int id) => _journals.DeleteOwned(id, SourceKey);

        /// <summary>
        /// التوازن شرط لا تسوية: القيد غير المتزن يُرفض ويُعرَض فرقه. كان الفرق يُرحَّل تلقائياً لحقوق
        /// الملكية، فيدخل حسابٌ لم يختره المستخدم بمبلغ لم يقصده — وهو كسرٌ صامت في الميزانية.
        /// والتاريخ من إعداد بدء العمل لا من كتابة المستخدم: تاريخ حديث يجعل الافتتاحي حركةَ فترة.
        /// </summary>
        private Result<CreateJournalDto> Balance(CreateJournalDto dto)
        {
            dto.Source = SourceKey;
            dto.Description = FixedDescription;
            dto.Lines = dto.Lines?.Where(l => !string.IsNullOrWhiteSpace(l.AccountCode)).ToList() ?? new List<CreateJournalLineDto>();

            if (dto.Lines.Count == 0)
                return Result.Fail<CreateJournalDto>("أضف سطراً واحداً على الأقل", ErrorCode.ValidationFailed);

            dto.EntryDate = _settingsService.Get(SettingKeys.Company.StartDate, dto.EntryDate);

            var debit = dto.Lines.Sum(l => l.Debit);
            var credit = dto.Lines.Sum(l => l.Credit);
            if (debit != credit)
                return Result.Fail<CreateJournalDto>(
                    $"القيد غير متزن: المدين {debit:N2} والدائن {credit:N2}، والفرق {Math.Abs(debit - credit):N2}",
                    ErrorCode.ValidationFailed);

            return Result.Ok(dto);
        }

    }
}
