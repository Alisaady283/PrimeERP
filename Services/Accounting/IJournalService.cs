using System;
using System.Collections.Generic;
using System.Data.Common;
using PrimeERP.Core.Common;
using PrimeERP.Services.Accounting.DTOs;

namespace PrimeERP.Services.Accounting
{
    /// <summary>
    /// المالك الوحيد لمنطق قيود اليومية — Repository تحته CRUD صرف فقط. Create/Post/Unpost/Delete لها نسخة
    /// (conn,tx) بجانب النسخة العادية — عمداً، حتى تُنفَّذ ذرّياً ضمن معاملة مستدعٍ آخر (FiscalPeriodService.
    /// CloseYear/ReopenYear الآن، مستندات F.4 لاحقاً عبر بديل IPostable الذي يُبنى حينها ويستدعي هذه الخدمة
    /// لا يكررها — راجع MIGRATION_INVENTORY.md).
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
        Result Delete(DbConnection conn, DbTransaction tx, int id);

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
