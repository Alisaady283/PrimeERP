using System;
using System.Collections.Generic;
using System.Data.Common;
using PrimeERP.Domain.Results;
using PrimeERP.Application.DTOs.Accounting;
using PrimeERP.Application.DTOs.Parties;

namespace PrimeERP.Application.Services.Parties
{
    /// <summary>
    /// المالك الوحيد لمنطق الموردين — Repository تحته CRUD صرف فقط. كل حساب مورد يُنشأ/يُحدَّث/يُحذف عبر
    /// IAccountService حصراً (لا SupplierRepository يلمس جدول Accounts). نفس شكل ICustomerService حرفياً —
    /// PartyServiceBase يحمل المنطق المشترك بين الاثنين.
    /// </summary>
    public interface ISupplierService
    {
        Result<PagedResult<SupplierDto>> GetPaged(int page, int pageSize, SupplierFilter filter = null);
        Result<SupplierDto> GetById(int id);
        Result<SupplierDto> GetByCode(string code);

        Result<List<SupplierDto>> Search(string term, int maxResults = 50);

        Result<List<AccountStatementLine>> GetStatement(int id, DateTime from, DateTime to);

        Result<SupplierDto> Create(CreateSupplierDto dto);
        Result<SupplierDto> Create(DbConnection conn, DbTransaction tx, CreateSupplierDto dto);

        Result<SupplierDto> CreateFromAccount(DbConnection conn, DbTransaction tx, string accountCode, string name);

        Result Update(UpdateSupplierDto dto);
        Result UpdateNameFromAccount(DbConnection conn, DbTransaction tx, string accountCode, string name);

        Result Delete(int id);
        Result DeleteByAccountCode(DbConnection conn, DbTransaction tx, string accountCode);

        Result RecalculateBalance(int id);
        Result RecalculateAllBalances();

        Result<CreditCheckResult> CheckCreditLimit(int id, decimal additional);
    }
}
