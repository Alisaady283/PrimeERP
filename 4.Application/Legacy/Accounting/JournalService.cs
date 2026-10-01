using PrimeERP.Domain.Entities;
using PrimeERP.Application.Services.Entities;
using PrimeERP.Application.Services.Ledger;
using PrimeERP.Application.Services.Core;
using PrimeERP.Platform.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using PrimeERP.Platform.Permissions;
using PrimeERP.Domain.Calculations;
using PrimeERP.Domain.Enums;
using PrimeERP.Domain.Results;
using PrimeERP.Data.Repositories;
using PrimeERP.Platform.Settings;
using PrimeERP.Application.DTOs.Accounting;
using PrimeERP.Platform.Audit;
using AuditAction = PrimeERP.Domain.Enums.AuditAction;

namespace PrimeERP.Application.Legacy.Accounting
{
    /// <summary>صفحة قيود اليومية</summary>
    public class JournalService : ServiceBase, IJournalService
    {
        protected override string PermissionPrefix => "Journal";
        protected override string StringPrefix => "Str.Journal";
        protected override string EntityName => "JournalEntries";

        private readonly IJournalRepository _journal;
        private readonly IAccountRepository _accountRepo;
        private readonly Entries _entries;
        private readonly IFiscalPeriodRepository _periods;
        private readonly TrialBalance _trial;

        public JournalService(IPermissionService permissions, ISettingsProvider settings, ILocalizationService localization, IAuditLogger audit,
            IJournalRepository journal, IAccountRepository accountRepo, Entries entries, IFiscalPeriodRepository periods, TrialBalance trial)
            : base(permissions, settings, localization, audit)
        {
            _journal = journal;
            _accountRepo = accountRepo;
            _entries = entries;
            _periods = periods;
            _trial = trial;
        }


        public Result<PagedResult<JournalEntryDto>> GetPaged(int page, int pageSize, JournalFilter filter = null) =>
            Can("View") ? Result.Ok(Page(page, pageSize, filter ?? new JournalFilter())) : FailDenied<PagedResult<JournalEntryDto>>();

        public Result<JournalEntryDetailDto> GetById(int id)
        {
            if (!Can("View")) return FailDenied<JournalEntryDetailDto>();
            var entry = _journal.GetById(id);
            return entry == null ? NotFound().As<JournalEntryDetailDto>() : Result.Ok(Detail(entry));
        }

        public Result<JournalEntryDto> GetByEntryNo(string entryNo)
        {
            if (!Can("View")) return FailDenied<JournalEntryDto>();
            var entry = _journal.GetByEntryNo(entryNo);
            return entry == null ? NotFound().As<JournalEntryDto>() : Result.Ok(Row(entry, _journal.GetLines(entry.Id).Count, _periods.GetAllPeriods()));
        }


        public Result<JournalEntryDto> Create(CreateJournalDto dto)
        {
            if (!Can("Create")) return FailDenied<JournalEntryDto>();

            var created = Commit(db => _entries.CreatePosted(db, dto, CurrentUser));
            if (created.IsFailure) return created.As<JournalEntryDto>();

            Audit.Log(EntityName, created.Value.Id, AuditAction.Insert, details: Msg("CreatedLog", created.Value.EntryNo, dto.Lines.Count));
            return Result.Ok(Row(_journal.GetById(created.Value.Id), dto.Lines.Count, _periods.GetAllPeriods()));
        }

        public Result Update(CreateJournalDto dto) => UpdateOwned(dto, null);

        public Result UpdateOwned(CreateJournalDto dto, string ownerSource)
        {
            if (!Can("Edit")) return FailDenied();

            var existing = _journal.GetById(dto.Id);
            if (existing == null) return NotFound();

            var accounts = _entries.CanUpdate(existing, dto, ownerSource);
            if (accounts.IsFailure) return accounts;

            Tx(db => _entries.Replace(db, dto, accounts.Value));

            Audit.Log(EntityName, dto.Id, AuditAction.Update, details: Msg("EditedLog", existing.EntryNo));
            return Result.Ok();
        }

        public Result Delete(int id) => DeleteOwned(id, null);

        public Result DeleteOwned(int id, string ownerSource)
        {
            if (!Can("Delete")) return FailDenied();

            var entry = _journal.GetById(id);
            if (entry == null) return NotFound();

            var deletable = _entries.CanDelete(entry, ownerSource);
            if (deletable.IsFailure) return deletable;

            Tx(db => _entries.Delete(db, id));

            Audit.Log(EntityName, id, AuditAction.Delete, details: Msg("DeletedLog", entry.EntryNo));
            return Result.Ok();
        }

        public Result Post(int id)
        {
            if (!Can("Post")) return FailDenied();

            var entry = _journal.GetById(id);
            var postable = _entries.CanPost(entry);
            if (postable.IsFailure) return postable;

            Tx(db => _entries.MarkPosted(db, id, CurrentUser));

            Audit.Log(EntityName, id, AuditAction.Update, details: Msg("PostedLog", entry.EntryNo));
            return Result.Ok();
        }

        public Result Unpost(int id)
        {
            if (!Can("Unpost")) return FailDenied();

            var entry = _journal.GetById(id);
            if (entry == null) return NotFound();

            var unpostable = _entries.CanUnpost(entry);
            if (unpostable.IsFailure) return unpostable;

            Tx(db => _entries.MarkUnposted(db, id));

            Audit.Log(EntityName, id, AuditAction.Update, details: Msg("UnpostedLog", entry.EntryNo));
            return Result.Ok();
        }

        public Result<JournalBatchResult> PostBatch(List<int> ids)
        {
            if (!Can("Post")) return FailDenied<JournalBatchResult>();

            var batch = new JournalBatchResult();
            var toPost = new List<int>();

            foreach (var id in ids)
            {
                var postable = _entries.CanPost(_journal.GetById(id));
                if (postable.IsFailure) batch.Failures.Add((id, postable.ErrorMessage));
                else toPost.Add(id);
            }

            batch.FailedCount = batch.Failures.Count;
            if (batch.FailedCount > 0) return Result.Ok(batch);

            Tx(db =>
            {
                foreach (var id in toPost) _entries.MarkPosted(db, id, CurrentUser);
            });

            batch.SuccessCount = toPost.Count;
            Audit.Log(EntityName, 0, AuditAction.Update, details: Msg("BatchPostedLog", batch.SuccessCount));
            return Result.Ok(batch);
        }

        public Result<List<TrialBalanceLine>> GetTrialBalance(DateTime from, DateTime to, bool includeZero = false, bool postedOnly = true) =>
            Can("View") ? _trial.Of(from, to, includeZero, postedOnly) : FailDenied<List<TrialBalanceLine>>();

        public Result<int> CountUnpostedBetween(DateTime from, DateTime to) =>
            Result.Ok(_journal.CountUnpostedBetween(from, to));

        private PagedResult<JournalEntryDto> Page(int page, int pageSize, JournalFilter filter)
        {
            var (items, total) = _journal.GetPaged(page, pageSize, filter.SearchText, filter.DateFrom, filter.DateTo, filter.Source,
                filter.IsPosted, filter.AccountCode, filter.MinAmount, filter.MaxAmount, filter.SortBy, filter.SortDescending);
            var counts = _journal.GetLineCounts(items.Select(e => e.Id));
            var periods = _periods.GetAllPeriods();

            return Paged(items, total, page, pageSize, rows => rows.Select(e => Row(e, counts.GetValueOrDefault(e.Id), periods)).ToList());
        }

        private JournalEntryDetailDto Detail(JournalEntry e)
        {
            var lines = _journal.GetLines(e.Id);
            var accounts = _accountRepo.GetByCodes(lines.Select(l => l.AccountCode)).ToDictionary(a => a.Code);
            return Rows.Copy(Row(e, lines.Count, _periods.GetAllPeriods()),
                new JournalEntryDetailDto { Lines = lines.Select(l => Line(l, accounts)).ToList() });
        }

        private JournalEntryDto Row(JournalEntry e, int linesCount, List<FiscalPeriod> periods)
        {
            var balanced = e.TotalDebit == e.TotalCredit;
            var (variant, status) = Rows.State(
                (e.IsPosted, StatusVariant.Success, "Str.Journal.Status.Posted"),
                (balanced, StatusVariant.Warning, "Str.Journal.Status.Draft"),
                (true, StatusVariant.Danger, "Str.Journal.Status.Unbalanced"));

            var dto = Rows.Copy(e, new JournalEntryDto());
            dto.EntryDate        = Entries.ParseDate(e.EntryDate);
            dto.SourceText       = SourceText(e.Source);
            dto.StatusVariant    = variant;
            dto.StatusText       = status;
            dto.LinesCount       = linesCount;
            dto.FiscalPeriodName = periods.FirstOrDefault(p => string.CompareOrdinal(e.EntryDate, p.StartDate) >= 0
                                                            && string.CompareOrdinal(e.EntryDate, p.EndDate) <= 0)?.Name;
            dto.CanEdit          = Can("Edit") && !e.IsPosted;
            dto.CanDelete        = Can("Delete") && !e.IsPosted;
            dto.CanPost          = Can("Post") && !e.IsPosted && balanced;
            dto.CanUnpost        = Can("Unpost") && e.IsPosted && e.Source != Entries.ClosingSource;
            return dto;
        }

        private static JournalLineDto Line(JournalLine l, IReadOnlyDictionary<string, Account> accounts)
        {
            var account = accounts.GetValueOrDefault(l.AccountCode);
            var dto = Rows.Copy(l, new JournalLineDto());
            dto.AccountId   = account?.Id;
            dto.AccountName = l.AccountName ?? account?.Name;
            dto.AccountType = account != null ? (AccountType)account.Type : default;
            return dto;
        }

        private string SourceText(string source) => Msg($"Source.{Entries.NormalizeSource(source)}");

        private Result NotFound() => Result.Fail(Msg("NotFound"), ErrorCode.NotFound);
    }
}
