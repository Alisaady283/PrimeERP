using System;
using System.Collections.Generic;
using System.Data.Common;
using PrimeERP.Domain.Results;
using PrimeERP.Application.DTOs.Accounting;

namespace PrimeERP.Application.Services.Accounting
{
    /// <summary>
    /// المالك الوحيد لمنطق قيود اليومية — Repository تحته CRUD صرف فقط. Create/Post/Unpost/Delete لها نسخة
    /// (conn,tx) بجانب النسخة العادية، لتُنفَّذ ذرّياً ضمن معاملة مستدعٍ آخر (FiscalPeriodService.CloseYear/
    /// ReopenYear).
    /// </summary>
    public interface IJournalService
    {
        // ===== القراءة =====
        Result<PagedResult<JournalEntryDto>> GetPaged(int page, int pageSize, JournalFilter filter = null);
        Result<JournalEntryDetailDto> GetById(int id);
        Result<JournalEntryDto> GetByEntryNo(string entryNo);

        // ===== الإنشاء =====
        Result<JournalEntryDto> Create(CreateJournalDto dto);
        Result<JournalEntryDto> Create(DbConnection conn, DbTransaction tx, CreateJournalDto dto);

        // ===== التعديل والحذف =====
        Result Update(CreateJournalDto dto);
        Result Delete(int id);

        /// <summary>تعديل/حذف قيدٍ من المستند الذي يملكه — يمرّر المستند مصدره. شاشة القيود تستعمل
        /// Update/Delete أعلاه ولا تملك إلا اليدوي. الاسم مستقلّ لا معامل اختياري: محرِّر المستندات يجد
        /// Update(Dto) بالانعكاس، وزيادة معامل ولو اختيارياً تُفقده المطابقة.</summary>
        Result UpdateOwned(CreateJournalDto dto, string ownerSource);
        Result DeleteOwned(int id, string ownerSource);
        Result Delete(DbConnection conn, DbTransaction tx, int id);

        /// <summary>الخزينة والبنك لا يقبلان سالباً: يُسأل قبل فتح المعاملة، فيُرفض العمل كلّه أو يمضي كلّه.</summary>
        Result EnsureRemovable(int entryId);
        Result EnsureAffordable(IEnumerable<CreateJournalLineDto> lines);
        Result EnsureReplaceable(int entryId, IEnumerable<CreateJournalLineDto> lines);

        // ===== الترحيل =====
        Result Post(int id);
        Result Post(DbConnection conn, DbTransaction tx, int id);
        Result Unpost(int id);
        Result Unpost(DbConnection conn, DbTransaction tx, int id);
        Result<JournalBatchResult> PostBatch(List<int> ids);

        // ===== التقارير =====
        Result<List<TrialBalanceLine>> GetTrialBalance(DateTime from, DateTime to, bool includeZero = false, bool postedOnly = true);

        /// <summary>عدد القيود غير المرحّلة بين تاريخين — تستخدمه FiscalPeriodService.ClosePeriod.</summary>
        Result<int> CountUnpostedBetween(DateTime from, DateTime to);
    }
}
