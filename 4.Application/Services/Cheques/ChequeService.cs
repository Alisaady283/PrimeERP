using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using PrimeERP.Application.DTOs.Accounting;
using PrimeERP.Application.DTOs.Cheques;
using PrimeERP.Application.Services.Accounting;
using PrimeERP.Application.Services.Treasury;
using PrimeERP.Data.Repositories;
using PrimeERP.Domain.Entities;
using PrimeERP.Domain.Enums;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Audit;
using PrimeERP.Platform.Localization;
using PrimeERP.Platform.Permissions;
using PrimeERP.Platform.Settings;
using Db = PrimeERP.Data.Core.DbHelper;
using PrimeERP.Application.Validation;

namespace PrimeERP.Application.Services.Cheques
{
    public interface IChequeService
    {
        Result<PagedResult<ChequeDto>> GetPaged(int page, int pageSize, ChequeFilter filter = null);
        Result<ChequeDetailDto> GetById(int id);
        Result<List<ChequeStatus>> GetAllowedTransitions(int id);
        Result Move(MoveChequeDto dto);
        Result<ChequeDocumentResultDto> CreateBatch(CreateChequeDocumentDto dto, ChequeDirection direction);

        /// <summary>تعديل/حذف شيك لم يُحرَّك بعد. الشيك المُحرَّك يُصحَّح بحركة مقابلة لا بالكتابة فوقه.</summary>
        Result UpdateUnmoved(int id, CreateChequeLineDto line, DateTime docDate);
        Result DeleteUnmoved(int id);
        Result<List<ChequeDto>> GetOpenForParty(int partyId, DateTime from, DateTime to);
    }

    /// <summary>دورة الشيك كاملة. القاعدة الوحيدة: لا تتغيّر الحالة بلا سطر حركة وقيد مقابلين في نفس المعاملة —
    /// فيبقى "أين الشيك الآن ولماذا" مقروءاً من السجل لا مستنتَجاً.</summary>
    public class ChequeService : ServiceBase, IChequeService
    {
        private readonly IChequeRepository _repo;
        private readonly ITreasuryService _treasuries;
        // المستودعان لا الخدمتان عمداً: خدمة العملاء تستهلك خدمة الشيكات (أسطر الشيك في كشف الحساب)،
        // فاعتمادها هنا على الخدمة يغلق حلقة اعتماد. المطلوب هنا اسم الطرف وحسابه فقط — كلاهما على الكيان.
        private readonly ICustomerRepository _customers;
        private readonly ISupplierRepository _suppliers;
        private readonly IJournalService _journals;
        private readonly ISettingsService _settingsService;

        public ChequeService(IChequeRepository repo, ITreasuryService treasuries, ICustomerRepository customers,
            ISupplierRepository suppliers, IJournalService journals, ISettingsService settingsService,
            IPermissionService permissions, ISettingsProvider settings, ILocalizationService localization, IAuditLogger audit)
            : base(permissions, settings, localization, audit)
        {
            _repo = repo; _treasuries = treasuries; _customers = customers; _suppliers = suppliers;
            _journals = journals; _settingsService = settingsService;
        }

        protected override string PermissionPrefix => "Cheques";
        protected override string StringPrefix => "Str.Cheque";
        protected override string EntityName => "Cheque";

        // الانتقالات المسموحة — الوارد يسار، الصادر يمين. ولا حالة بلا رجعة: الحركة قد تكون خطأً، فيعيدها
        // من يملك الصلاحية بحركة مضادة تُسجَّل بقيدها المقابل — تصحيحٌ مقروء في السجل لا محوٌ له.
        private static readonly Dictionary<ChequeStatus, ChequeStatus[]> Allowed = new()
        {
            [ChequeStatus.InHand]    = new[] { ChequeStatus.Deposited, ChequeStatus.Collected, ChequeStatus.Returned },
            [ChequeStatus.Deposited] = new[] { ChequeStatus.Collected, ChequeStatus.Bounced, ChequeStatus.InHand },
            [ChequeStatus.Bounced]   = new[] { ChequeStatus.Deposited, ChequeStatus.Returned, ChequeStatus.InHand },
            [ChequeStatus.Collected] = new[] { ChequeStatus.Deposited, ChequeStatus.InHand },
            [ChequeStatus.Returned]  = new[] { ChequeStatus.InHand },
            [ChequeStatus.Issued]    = new[] { ChequeStatus.Paid, ChequeStatus.Bounced },
            [ChequeStatus.Paid]      = new[] { ChequeStatus.Issued },
        };

        // حالة الشيك عند إنشائه — إليها يعود بعد إلغاء حركة خاطئة، وعندها يقبل التعديل.
        private static ChequeStatus InitialStatus(ChequeDirection direction) =>
            direction == ChequeDirection.Incoming ? ChequeStatus.InHand : ChequeStatus.Issued;

        public Result<PagedResult<ChequeDto>> GetPaged(int page, int pageSize, ChequeFilter filter = null)
        {
            if (!Can("View")) return FailDenied<PagedResult<ChequeDto>>();
            filter ??= new ChequeFilter();

            var direction = filter.Direction == 0 ? (ChequeDirection?)null : (ChequeDirection)filter.Direction;
            var status = filter.Status == 0 ? (ChequeStatus?)null : (ChequeStatus)filter.Status;

            var (items, total) = _repo.GetPaged(direction, status, page, pageSize, filter.SearchText);
            return Result.Ok(new PagedResult<ChequeDto>
            { Items = items.Select(ToDto).ToList(), Page = page, PageSize = pageSize, TotalCount = total });
        }

        public Result<ChequeDetailDto> GetById(int id)
        {
            if (!Can("View")) return FailDenied<ChequeDetailDto>();

            var cheque = _repo.GetById(id);
            if (cheque == null) return Result.Fail<ChequeDetailDto>("الشيك غير موجود", ErrorCode.NotFound);

            // ToDto يبني التفصيلة مباشرة — نسخُ الحقول حقلاً حقلاً هنا كان يُسقط ما يُضاف لاحقاً (أسقط PartyId).
            var dto = ToDto<ChequeDetailDto>(cheque);
            dto.Status = cheque.Status;
            dto.Direction = cheque.Direction;
            dto.Movements = _repo.GetMovements(id).Select(m => new ChequeMovementDto
            {
                MovementDate = m.MovementDate, FromStatusName = StatusName(m.FromStatus), ToStatusName = StatusName(m.ToStatus),
                Notes = m.Notes, CreatedBy = m.CreatedBy
            }).ToList();

            return Result.Ok(dto);
        }

        public Result<List<ChequeStatus>> GetAllowedTransitions(int id)
        {
            var cheque = _repo.GetById(id);
            if (cheque == null) return Result.Fail<List<ChequeStatus>>("الشيك غير موجود", ErrorCode.NotFound);

            return Result.Ok(Allowed[cheque.Status].ToList());
        }

        public Result Move(MoveChequeDto dto)
        {
            if (!Can("Edit")) return FailDenied();

            var cheque = _repo.GetById(dto.ChequeId);
            if (cheque == null) return Result.Fail("الشيك غير موجود", ErrorCode.NotFound);

            var target = (ChequeStatus)dto.ToStatus;
            if (!Allowed[cheque.Status].Contains(target))
                return Result.Fail($"لا يمكن نقل الشيك من {StatusName(cheque.Status)} إلى {StatusName(target)}", ErrorCode.ValidationFailed);

            var (debit, credit) = ResolveAccounts(cheque, target, dto.TreasuryId);
            if (AffectsLedger(target) && (string.IsNullOrWhiteSpace(debit) || string.IsNullOrWhiteSpace(credit)))
                return Result.Fail("لا حساب مرتبط بالخزينة أو بالطرف — اربطهما بحسابيهما أولاً", ErrorCode.ValidationFailed);

            try
            {
                Db.RunTransaction((conn, tx) =>
                {
                    var entryId = AffectsLedger(target) ? PostEntry(conn, tx, cheque, target, debit, credit, dto.MovementDate) : (int?)null;

                    _repo.InsertMovement(conn, tx, new ChequeMovement
                    {
                        ChequeId = cheque.Id, MovementDate = dto.MovementDate, FromStatus = cheque.Status, ToStatus = target,
                        TreasuryId = dto.TreasuryId ?? cheque.TreasuryId, JournalEntryId = entryId,
                        Notes = dto.Notes, CreatedBy = AppSession.Username
                    });

                    _repo.SetStatus(conn, tx, cheque.Id, target, dto.TreasuryId ?? cheque.TreasuryId);
                });
            }
            catch (InvalidOperationException ex)
            {
                return Result.Fail(ex.Message, ErrorCode.ValidationFailed);
            }

            Audit.Log(EntityName, cheque.Id, AuditAction.Update, newValue: new { From = cheque.Status, To = target });
            return Result.Ok();
        }

        /// <summary>الشيك لا يمسّ الأرصدة قبل أن يُسدَّد من البنك فعلاً — لذلك القيد يقع عند التحصيل/الصرف
        /// وحدهما، وباقي الحركات (استلام/إيداع/ارتداد/رد) تُسجَّل كحركة بلا قيد. قبلها يظهر الشيك بكشف
        /// الحساب كقيمة استعلامية فقط (راجع GetOpenForParty).</summary>
        private (string Debit, string Credit) ResolveAccounts(Cheque cheque, ChequeStatus target, int? treasuryId)
        {
            var cash  = TreasuryAccount(treasuryId ?? cheque.TreasuryId);
            var party = PartyAccount(cheque);

            return target switch
            {
                ChequeStatus.Collected => (cash, party),
                ChequeStatus.Paid      => (party, cash),
                _ => (null, null)
            };
        }

        private static bool AffectsLedger(ChequeStatus target) =>
            target is ChequeStatus.Collected or ChequeStatus.Paid;

        private int PostEntry(DbConnection conn, DbTransaction tx, Cheque cheque, ChequeStatus target,
            string debitAccount, string creditAccount, DateTime date)
        {
            var description = $"شيك {cheque.ChequeNo} — {StatusName(target)}";
            var entry = new CreateJournalDto
            {
                EntryDate = date, Description = description, Source = EntityName,
                Lines = new List<CreateJournalLineDto>
                {
                    new() { LineNo = 1, AccountCode = debitAccount,  Debit = cheque.Amount, Credit = 0, Notes = description },
                    new() { LineNo = 2, AccountCode = creditAccount, Debit = 0, Credit = cheque.Amount, Notes = description },
                }
            };

            var created = _journals.Create(conn, tx, entry);
            if (created.IsFailure) throw new InvalidOperationException(created.ErrorMessage);

            var posted = _journals.Post(conn, tx, created.Value.Id);
            if (posted.IsFailure) throw new InvalidOperationException(posted.ErrorMessage);

            return created.Value.Id;
        }

        private string TreasuryAccount(int? treasuryId)
        {
            if (treasuryId == null) return _settingsService.Get<string>(SettingKeys.Accounts.Bank, "");

            var treasury = _treasuries.GetById(treasuryId.Value);
            if (treasury.IsFailure) return _settingsService.Get<string>(SettingKeys.Accounts.Bank, "");

            return !string.IsNullOrWhiteSpace(treasury.Value.AccountCode)
                ? treasury.Value.AccountCode
                : _settingsService.Get<string>(treasury.Value.Kind == TreasuryKind.Bank ? SettingKeys.Accounts.Bank : SettingKeys.Accounts.Cash, "");
        }

        private string PartyAccount(Cheque cheque)
        {
            if (cheque.PartyId == null) return null;

            return cheque.PartyKind == PartyKind.Customer
                ? _customers.GetById(cheque.PartyId.Value)?.AccountCode
                : _suppliers.GetById(cheque.PartyId.Value)?.AccountCode;
        }

        private ChequeDto ToDto(Cheque c) => ToDto<ChequeDto>(c);

        /// <summary>مصدر واحد لحقول الشيك: القائمة والتفصيلة تُبنيان منه، فلا يسقط حقل من إحداهما.</summary>
        private T ToDto<T>(Cheque c) where T : ChequeDto, new() => new()
        {
            Id = c.Id, ChequeNo = c.ChequeNo, Amount = c.Amount, IssueDate = c.IssueDate, DueDate = c.DueDate,
            BankName = c.BankName, Notes = c.Notes, PartyId = c.PartyId,
            DirectionName = c.Direction == ChequeDirection.Incoming ? "وارد" : "صادر",
            StatusName = StatusName(c.Status),
            PartyName = PartyNameOf(c),
            TreasuryName = c.TreasuryId == null ? null : _treasuries.GetById(c.TreasuryId.Value).Value?.Name
        };

        private string PartyNameOf(Cheque c)
        {
            if (c.PartyId == null) return null;

            return c.PartyKind == PartyKind.Customer
                ? _customers.GetById(c.PartyId.Value)?.Name
                : _suppliers.GetById(c.PartyId.Value)?.Name;
        }

        /// <summary>مستند استلام/صرف شيكات: رأس واحد وعدة شيكات — بلا قيد، الشيك يبقى استعلامياً حتى يُسدَّد.</summary>
        public Result<ChequeDocumentResultDto> CreateBatch(CreateChequeDocumentDto dto, ChequeDirection direction)
        {
            if (!Can("Create")) return FailDenied<ChequeDocumentResultDto>();
            if (dto.Lines == null || dto.Lines.Count == 0)
                return Result.Fail<ChequeDocumentResultDto>("المستند يحتاج شيكاً واحداً على الأقل", ErrorCode.ValidationFailed);

            foreach (var line in dto.Lines)
            {
                var valid = Check(LineRules, line);
                if (valid.IsFailure) return Result.Fail<ChequeDocumentResultDto>(valid.ErrorMessage, valid.ErrorCode);
            }

            var status = direction == ChequeDirection.Incoming ? ChequeStatus.InHand : ChequeStatus.Issued;
            var partyKind = direction == ChequeDirection.Incoming ? PartyKind.Customer : PartyKind.Supplier;
            var ids = new List<int>();

            Db.RunTransaction((conn, tx) =>
            {
                foreach (var line in dto.Lines)
                {
                    var cheque = new Cheque
                    {
                        ChequeNo = line.ChequeNo, Direction = direction, PartyKind = partyKind,
                        PartyId = line.PartyId, Amount = line.Amount,
                        IssueDate = dto.DocDate, DueDate = line.DueDate ?? dto.DocDate,
                        BankName = line.BankName, Status = status, Notes = line.Notes, CreatedBy = AppSession.Username
                    };
                    var id = _repo.Insert(conn, tx, cheque);
                    ids.Add(id);

                    _repo.InsertMovement(conn, tx, new ChequeMovement
                    {
                        ChequeId = id, MovementDate = dto.DocDate, FromStatus = status, ToStatus = status,
                        Notes = dto.Notes, CreatedBy = AppSession.Username
                    });
                }
            });

            Audit.Log(EntityName, ids.FirstOrDefault(), AuditAction.Insert, newValue: new { Count = ids.Count, Direction = direction });
            return Result.Ok(new ChequeDocumentResultDto { ChequeIds = ids });
        }

        /// <summary>
        /// الحالة الراهنة لا تاريخ الحركات: ما دام الشيك بحالته الأولى فتصحيحه تصحيحُ إدخال، ولو كان قد
        /// تحرّك وأُعيد — فقيود الحركتين تلاشت آثارها. أما وهو مُودَع أو محصَّل أو مرتدّ فله قيدٌ قائم،
        /// فيُعاد أولاً بحركة مضادة (وهي متاحة من كل حالة) ثم يُصحَّح — وإلا تناقض السجل مع الدفاتر.
        /// </summary>
        private static Result EnsureAtInitialStatus(Cheque cheque, string action) =>
            cheque.Status == InitialStatus(cheque.Direction)
                ? Result.Ok()
                : Result.Fail($"لا يمكن {action} شيك تحرّك — أعِده أولاً إلى {StatusName(InitialStatus(cheque.Direction))} بحركة مضادة", ErrorCode.ValidationFailed);

        // التحقق من طبقة التحقق لا من شروط مكتوبة هنا — Check يترجم نتيجتها إلى Result كأي خدمة أخرى.
        private static readonly ChequeLineValidator LineRules = new();

        public Result UpdateUnmoved(int id, CreateChequeLineDto line, DateTime docDate)
        {
            if (!Can("Edit")) return FailDenied();

            var cheque = _repo.GetById(id);
            if (cheque == null) return Result.Fail("الشيك غير موجود", ErrorCode.NotFound);

            var editable = EnsureAtInitialStatus(cheque, "تعديل");
            if (editable.IsFailure) return editable;

            var valid = Check(LineRules, line);
            if (valid.IsFailure) return valid;

            cheque.ChequeNo = line.ChequeNo;
            cheque.PartyId  = line.PartyId;
            cheque.Amount   = line.Amount;
            cheque.IssueDate = docDate;
            cheque.DueDate  = line.DueDate ?? docDate;
            cheque.BankName = line.BankName;
            cheque.Notes    = line.Notes;

            Db.RunTransaction((conn, tx) => _repo.Update(conn, tx, cheque));
            Audit.Log(EntityName, id, AuditAction.Update, newValue: cheque);
            return Result.Ok();
        }

        public Result DeleteUnmoved(int id)
        {
            if (!Can("Delete")) return FailDenied();

            var cheque = _repo.GetById(id);
            if (cheque == null) return Result.Fail("الشيك غير موجود", ErrorCode.NotFound);

            var deletable = EnsureAtInitialStatus(cheque, "حذف");
            if (deletable.IsFailure) return deletable;

            Db.RunTransaction((conn, tx) =>
            {
                _repo.DeleteMovements(conn, tx, id);
                _repo.Delete(conn, tx, id);
            });

            Audit.Log(EntityName, id, AuditAction.Delete, oldValue: cheque);
            return Result.Ok();
        }

        public Result<List<ChequeDto>> GetOpenForParty(int partyId, DateTime from, DateTime to)
        {
            if (!Can("View")) return FailDenied<List<ChequeDto>>();
            return Result.Ok(_repo.GetOpenForParty(partyId, from, to).Select(ToDto).ToList());
        }

        public static string StatusName(ChequeStatus status) => status switch
        {
            ChequeStatus.InHand    => "بالمحفظة",
            ChequeStatus.Deposited => "مودع بالبنك",
            ChequeStatus.Collected => "محصّل",
            ChequeStatus.Bounced   => "مرتد",
            ChequeStatus.Returned  => "مردود للطرف",
            ChequeStatus.Issued    => "صادر",
            ChequeStatus.Paid      => "مصروف",
            _ => status.ToString()
        };
    }
}
