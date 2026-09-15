using System;
using System.Collections.Generic;
using System.Data.Common;
using PrimeERP.Application.DTOs.Accounting;
using PrimeERP.Application.Services.Accounting;
using PrimeERP.Domain.Entities.Common;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Audit;
using PrimeERP.Platform.Localization;
using PrimeERP.Platform.Permissions;
using PrimeERP.Platform.Settings;

namespace PrimeERP.Application.Services.Assets
{
    /// <summary>
    /// أساس حركات الأصول: اقتناءٌ وإهلاكٌ وإعادة تقييمٍ وبيع. كلٌّ منها مستندٌ يُرحّل قيداً من سطرين
    /// ويعكسه عند الحذف — تماماً كما يفعل <see cref="Vouchers.VoucherServiceBase"/> بسندَي القبض والصرف.
    ///
    /// حسابات الأصول من الإعدادات لا مكتوبة، والقيد يُنشأ ويُرحَّل في معاملة المستدعي — فلا خدمةٌ تكتب
    /// جملة ترحيلٍ لنفسها، ولا صنفٌ مساعد خارج شجرة الوراثة.
    /// </summary>
    public abstract class AssetMovementServiceBase<TEntity, TDto, TFilter>
        : CrudServiceBase<TEntity, TDto, TFilter> where TEntity : BaseModel
    {
        protected readonly IJournalService Journals;
        private readonly ISettingsService _settings;

        protected AssetMovementServiceBase(IPermissionService permissions, ISettingsProvider settings,
            ILocalizationService localization, IAuditLogger audit,
            IJournalService journals, ISettingsService settingsService)
            : base(permissions, settings, localization, audit)
        {
            Journals = journals;
            _settings = settingsService;
        }

        protected override string PermissionPrefix => "Assets";
        protected override string StringPrefix => "Str.Asset";

        /// <summary>كود حسابٍ من الإعدادات — غيابه يُوقف الحركة برسالةٍ واحدة لا برسالةٍ لكل خدمة.</summary>
        protected Result<string> Account(string key) => Required(_settings.Get<string>(key, ""), "AccountsMissing");

        /// <summary>
        /// قيد الحركة: سطرٌ مدين وسطرٌ دائن، يُنشأ ويُرحَّل في معاملة المستدعي. يرمي عند الفشل — نسخة
        /// <see cref="Vouchers.VoucherServiceBase"/> حرفياً: <c>DbHelper.RunTransaction</c> لا يتراجع إلا
        /// باستثناء، فنتيجةٌ فاشلة تُعاد بهدوء كانت تُثبِت السجلّ بلا قيده.
        /// </summary>
        protected int PostEntry(DbConnection conn, DbTransaction tx, DateTime date, string description,
            string debitAccount, string creditAccount, decimal amount, string lineNote = null) =>
            PostEntry(conn, tx, date, description, new List<CreateJournalLineDto>
            {
                new() { LineNo = 1, AccountCode = debitAccount,  Debit  = amount, Notes = lineNote ?? description },
                new() { LineNo = 2, AccountCode = creditAccount, Credit = amount, Notes = lineNote ?? description }
            });

        /// <summary>
        /// قيدٌ بأي عدد من السطور — البيع يُغلق حساب الأصل ومجمّعه ويقبض ثمنه ويُقيّد فرقه في قيدٍ
        /// واحد. نفس مسار الإنشاء والترحيل، فلا خدمةٌ تكتب ترحيلها.
        /// </summary>
        protected int PostEntry(DbConnection conn, DbTransaction tx, DateTime date, string description,
            List<CreateJournalLineDto> lines)
        {
            var entry = Journals.Create(conn, tx, new CreateJournalDto
            {
                EntryDate = date,
                Description = description,
                Source = EntityName,
                Lines = lines
            });
            if (entry.IsFailure) throw new InvalidOperationException(entry.ErrorMessage);

            var posted = Journals.Post(conn, tx, entry.Value.Id);
            if (posted.IsFailure) throw new InvalidOperationException(posted.ErrorMessage);

            return entry.Value.Id;
        }

        /// <summary>
        /// الطرف المقابل لا يكون فارغاً: حسابٌ فارغ يُسقِط سطره فيولد قيدٌ غير متزن — نفس حراسة السند
        /// «لا حساب مرتبط بالخزينة المختارة».
        /// </summary>
        protected Result<string> Required(string accountCode, string messageKey)
            => string.IsNullOrWhiteSpace(accountCode)
                ? Result.Fail<string>(Msg(messageKey), ErrorCode.ValidationFailed)
                : Result.Ok(accountCode);

        /// <summary>يُسأل قبل فتح المعاملة: عكسُ القيد لا يُنزل خزينةً ولا بنكاً تحت الصفر.</summary>
        protected Result EnsureReversible(int? entryId) =>
            entryId == null ? Result.Ok() : Journals.EnsureRemovable(entryId.Value);

        /// <summary>عكس قيد الحركة — مسار المالك، فلا يمنعه كونُ القيد مرحَّلاً.</summary>
        protected void ReverseEntry(DbConnection conn, DbTransaction tx, int? entryId)
        {
            if (entryId != null) Journals.Delete(conn, tx, entryId.Value);
        }
    }
}
