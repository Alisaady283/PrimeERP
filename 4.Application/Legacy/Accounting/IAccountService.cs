using PrimeERP.Data.Core;
using System;
using System.Collections.Generic;
using PrimeERP.Platform.Permissions;
using PrimeERP.Domain.Enums;
using PrimeERP.Domain.Results;
using PrimeERP.Application.DTOs.Accounting;

namespace PrimeERP.Application.Legacy.Accounting
{
    /// <summary>عقد خدمة الحسابات</summary>
    public interface IAccountService
    {
        Result<List<AccountTreeNode>> GetTree(AccountTreeFilter filter = null);
        Result<PagedResult<AccountDto>> GetPaged(int page, int pageSize, AccountTreeFilter filter = null);
        Result<List<AccountDto>> GetLeaves(AccountType? type = null);
        Result<AccountDto> GetById(int id);
        Result<AccountDto> GetByCode(string code);
        Result<string> GenerateChildCode(int parentId);

        Result<AccountDto> Create(CreateAccountDto dto);


        Result Update(UpdateAccountDto dto);
        Result Delete(int id);



        Result RecalculateBalance(string code);

        Result RecalculateBalance(PrimeDbContext db, string code);

        Result RecalculateAllBalances();
        Result<decimal> GetBalanceAsOf(string code, DateTime date);
        Result<List<AccountStatementLine>> GetStatement(string code, DateTime from, DateTime to);
        bool IsLinkedRoot(string accountCode);

        Result<bool> CanAcceptEntries(string code);
        Result<bool> CanHaveChildren(string code);
        Result<AccountType> GetTypeOf(string code);
    }
}
