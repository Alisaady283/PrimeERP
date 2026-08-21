using System.Data.Common;
using PrimeERP.Domain.Results;

namespace PrimeERP.Application.Services.Parties
{
    // TEMPORARY — يُحذف ويُعاد بناؤه كاملاً في R6 (مع PartyServiceBase). لا تبنِ عليه أي منطق جديد الآن.
    /// <summary>
    /// عقد جزئي حتى F.3.2 — التوقيعات هنا حُدِّثت في F.3.1 لتطابق ICustomerService.cs (raise بالكود+الاسم لا
    /// Account كامل) حتى تبقى AccountService.Create/Update/Delete قادرة على معاملة ICustomerService وISupplierService
    /// بشكل متماثل تماماً في نفس مسار الكود. التنفيذ الفعلي (SupplierService.cs الكامل) يُبنى في F.3.2 (R6).
    /// </summary>
    public interface ISupplierService
    {
        Result<int> CreateFromAccount(DbConnection conn, DbTransaction tx, string accountCode, string name);

        /// <summary>يحذف المورد المرتبط بهذا الحساب — تستدعيها AccountService.Delete ضمن نفس معاملتها.</summary>
        Result DeleteByAccountCode(DbConnection conn, DbTransaction tx, string accountCode);

        /// <summary>يزامن اسم المورد من تعديل الحساب — تستدعيها AccountService.Update ضمن نفس معاملتها.</summary>
        Result UpdateNameFromAccount(DbConnection conn, DbTransaction tx, string accountCode, string name);
    }
}
