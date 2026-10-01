using PrimeERP.Data.Core;
using System;
using System.Collections.Generic;
using PrimeERP.Domain.Entities;
using PrimeERP.Domain.Results;
using PrimeERP.Application.DTOs.Accounting;
using PrimeERP.Application.DTOs.Parties;

namespace PrimeERP.Application.Legacy.Parties
{
    /// <summary>المالك الوحيد لمنطق الموردين</summary>
    public interface ISupplierService
    {
        Result<PagedResult<Supplier>> GetPaged(int page, int pageSize, SupplierFilter filter = null);
        Result<Supplier> GetById(int id);
        Result<Supplier> GetByCode(string code);

        Result<List<Supplier>> Search(string term, int maxResults = 50);

        Result<List<AccountStatementLine>> GetStatement(int id, DateTime from, DateTime to);

        Result<Supplier> Create(Supplier supplier);
        Result<Supplier> Create(PrimeDbContext db, Supplier supplier);

        Result Update(Supplier supplier);
        Result UpdateNameFromAccount(PrimeDbContext db, string accountCode, string name);

        Result Delete(int id);
        Result DeleteByAccountCode(PrimeDbContext db, string accountCode);

        Result RecalculateBalance(int id);
        Result RecalculateAllBalances();

        Result<CreditCheckResult> CheckCreditLimit(int id, decimal additional);
    }
}
