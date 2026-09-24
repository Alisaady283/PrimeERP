using PrimeERP.Data.Core;
using System;
using System.Collections.Generic;
using PrimeERP.Domain.Results;
using PrimeERP.Application.DTOs.Accounting;
using PrimeERP.Application.DTOs.Parties;

namespace PrimeERP.Application.Services.Parties
{
    /// <summary>المالك الوحيد لمنطق العملاء</summary>
    public interface ICustomerService
    {
        Result<PagedResult<CustomerDto>> GetPaged(int page, int pageSize, CustomerFilter filter = null);
        Result<CustomerDto> GetById(int id);
        Result<CustomerDto> GetByCode(string code);

        Result<List<CustomerDto>> Search(string term, int maxResults = 50);

        Result<List<AccountStatementLine>> GetStatement(int id, DateTime from, DateTime to);

        Result<CustomerDto> Create(CreateCustomerDto dto);
        Result<CustomerDto> Create(PrimeDbContext db, CreateCustomerDto dto);

        Result<CustomerDto> CreateFromAccount(PrimeDbContext db, string accountCode, string name);

        Result Update(UpdateCustomerDto dto);

        Result UpdateNameFromAccount(PrimeDbContext db, string accountCode, string name);

        Result Delete(int id);

        Result DeleteByAccountCode(PrimeDbContext db, string accountCode);


        Result RecalculateBalance(int id);
        Result RecalculateAllBalances();

        Result<CreditCheckResult> CheckCreditLimit(int id, decimal additional);
    }
}
