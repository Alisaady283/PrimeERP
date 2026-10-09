using PrimeERP.Data.Core;
using System;
using System.Collections.Generic;
using PrimeERP.Domain.Entities;
using PrimeERP.Domain.Results;
using PrimeERP.Application.DTOs.Accounting;
using PrimeERP.Application.DTOs.Parties;

namespace PrimeERP.Application.PageServices.Parties
{
    /// <summary>المالك الوحيد لمنطق العملاء</summary>
    public interface ICustomerService
    {
        Result<PagedResult<Customer>> GetPaged(int page, int pageSize, CustomerFilter filter = null);
        Result<Customer> GetById(int id);
        Result<Customer> GetByCode(string code);

        Result<List<Customer>> Search(string term, int maxResults = 50);

        Result<List<AccountStatementLine>> GetStatement(int id, DateTime from, DateTime to);

        Result<Customer> Create(Customer customer);
        Result<Customer> Create(PrimeDbContext db, Customer customer);

        Result Update(Customer customer);

        Result UpdateNameFromAccount(PrimeDbContext db, string accountCode, string name);

        Result Delete(int id);

        Result DeleteByAccountCode(PrimeDbContext db, string accountCode);

        Result RecalculateBalance(int id);
        Result RecalculateAllBalances();

        Result<CreditCheckResult> CheckCreditLimit(int id, decimal additional);
    }
}
