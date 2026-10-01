using PrimeERP.Application.Validation;
using PrimeERP.Application.Legacy.Documents;
using PrimeERP.Application.Services.Core;
using System;
using System.Collections.Generic;
using PrimeERP.Application.Legacy.Accounting;
using PrimeERP.Application.Services.Ledger;
using PrimeERP.Data.Core;
using PrimeERP.Domain.Enums;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Audit;
using PrimeERP.Platform.Localization;
using PrimeERP.Platform.Permissions;
using PrimeERP.Platform.Settings;

namespace PrimeERP.Application.Services.Documents
{
    /// <summary>المستند: قراءةً وإنشاءً واستبدالاً وحذفاً</summary>
    public abstract class DocumentService<THead, TRow, TDetail, TCreate, TFilter> : ServiceBase
        where THead : class
    {
        protected readonly IDocumentPull Links;
        protected readonly Entries Journals;

        protected DocumentService(IPermissionService permissions, ISettingsProvider settings, ILocalizationService localization,
            IAuditLogger audit, IDocumentPull links = null, Entries journals = null)
            : base(permissions, settings, localization, audit)
        {
            Links = links;
            Journals = journals;
        }

        protected abstract THead FindHead(int id);
        protected abstract (List<THead> Items, int Total) FindPage(int page, int pageSize, TFilter filter);
        protected abstract List<TRow> ToRows(List<THead> heads);
        protected abstract TDetail ToDetail(THead head);
        protected abstract int IdOf(THead head);

        /// <summary>التحقق ثم دالة الكتابة</summary>
        protected abstract Result<Func<PrimeDbContext, int>> Plan(TCreate dto);
        protected abstract void Remove(PrimeDbContext db, THead head);

        protected virtual bool CanDo(string action) => Can(action);
        protected virtual int IdOf(TCreate dto) => 0;
        protected virtual bool Editable => false;
        protected virtual string PullType => null;
        protected virtual int? EntryOf(THead head) => null;
        protected virtual Result Guard(THead head) => Result.Ok();
        protected virtual object AuditValue(THead head) => null;

        public Result<PagedResult<TRow>> GetPaged(int page, int pageSize, TFilter filter = default)
        {
            if (!CanDo("View")) return FailDenied<PagedResult<TRow>>();

            var (items, total) = FindPage(page, pageSize, filter);
            return Ok(Paged(items, total, page, pageSize, ToRows));
        }

        public Result<TDetail> GetById(int id)
        {
            if (!CanDo("View")) return FailDenied<TDetail>();

            var head = FindHead(id);
            return head == null ? Fail<TDetail>("NotFound", ErrorCode.NotFound) : Ok(ToDetail(head));
        }

        public Result<TDetail> Create(TCreate dto)
        {
            if (!CanDo("Create")) return FailDenied<TDetail>();

            var created = Plan(dto).Then(write => Commit(db => Result.Ok(write(db))));
            if (created.IsFailure) return created.As<TDetail>();

            Audit.Log(EntityName, created.Value, AuditAction.Insert, newValue: AuditValue(FindHead(created.Value)));
            return GetById(created.Value);
        }

        public Result Update(TCreate dto)
        {
            if (!Editable) return Fail("EditRefused", ErrorCode.ValidationFailed);
            if (!CanDo("Edit")) return FailDenied();

            var head = FindHead(IdOf(dto));
            if (head == null) return Fail("NotFound", ErrorCode.NotFound);

            var replaced = Removable(head).Then(() => Plan(dto)).Then(write => Commit(db =>
            {
                Remove(db, head);
                return Result.Ok(write(db));
            }));
            if (replaced.IsFailure) return replaced;

            Audit.Log(EntityName, replaced.Value, AuditAction.Update, newValue: AuditValue(FindHead(replaced.Value)));
            return Result.Ok();
        }

        public Result Delete(int id)
        {
            if (!CanDo("Delete")) return FailDenied();

            var head = FindHead(id);
            if (head == null) return Fail("NotFound", ErrorCode.NotFound);

            var removed = Removable(head).Then(() => Commit(db =>
            {
                Remove(db, head);
                return Result.Ok();
            }));
            if (removed.IsFailure) return removed;

            Audit.Log(EntityName, id, AuditAction.Delete);
            return Result.Ok();
        }

        /// <summary>حرّاس الحذف</summary>
        private Result Removable(THead head)
        {
            if (PullType != null && Links.IsPulledFrom(PullType, IdOf(head)))
                return Fail("PulledFrom", ErrorCode.ValidationFailed);

            return Posting.EnsureReversible(Journals, EntryOf(head)).Then(() => Guard(head));
        }
    }
}
