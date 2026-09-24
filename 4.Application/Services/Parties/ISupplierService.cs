using PrimeERP.Data.Core;
using System;
using System.Collections.Generic;
using PrimeERP.Domain.Results;
using PrimeERP.Application.DTOs.Accounting;
using PrimeERP.Application.DTOs.Parties;

namespace PrimeERP.Application.Services.Parties
{
    /// <summary>المالك الوحيد لمنطق الموردين</summary>
    public interface ISupplierService
    {
        Result<PagedResult<SupplierDto>> GetPaged(int page, int pageSize, SupplierFilter filter = null);
        Result<SupplierDto> GetById(int id);
        Result<SupplierDto> GetByCode(string code);

        Result<List<SupplierDto>> Search(string term, int maxResults = 50);

        Result<List<AccountStatementLine>> GetStatement(int id, DateTime from, DateTime to);

        Result<SupplierDto> Create(CreateSupplierDto dto);
        Result<SupplierDto> Create(PrimeDbContext db, CreateSupplierDto dto);

        Result<SupplierDto> CreateFromAccount(PrimeDbContext db, string accountCode, string name);

        Result Update(UpdateSupplierDto dto);
        Result UpdateNameFromAccount(PrimeDbContext db, string accountCode, string name);

        Result Delete(int id);
        Result DeleteByAccountCode(PrimeDbContext db, string accountCode);

        Result RecalculateBalance(int id);
        Result RecalculateAllBalances();

        Result<CreditCheckResult> CheckCreditLimit(int id, decimal additional);
    }
}
