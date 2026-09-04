using System;
using System.Collections.Generic;
using System.Data.Common;
using PrimeERP.Platform.Permissions;
using PrimeERP.Domain.Enums;
using PrimeERP.Domain.Results;
using PrimeERP.Application.DTOs.Accounting;

namespace PrimeERP.Application.Services.Accounting
{
    public interface IAccountService
    {
        // ===== القراءة =====
        Result<List<AccountTreeNode>> GetTree(AccountTreeFilter filter = null);
        Result<PagedResult<AccountDto>> GetPaged(int page, int pageSize, AccountTreeFilter filter = null);
        Result<List<AccountDto>> GetLeaves(AccountType? type = null);
        Result<AccountDto> GetById(int id);
        Result<AccountDto> GetByCode(string code);
        Result<string> GenerateChildCode(int parentId);

        // ===== الكتابة =====
        Result<AccountDto> Create(CreateAccountDto dto);

        /// <summary>بمعاملة خارجية — يخدم CustomerService/SupplierService.Create (dto.SkipAutoLink يجب أن يكون true دائماً هنا، راجع CreateAccountDto.SkipAutoLink). بلا تحقق صلاحية Accounts.Create (المستدعي تحقق صلاحيته الخاصة Customers.Create/Suppliers.Create).</summary>
        Result<AccountDto> Create(DbConnection conn, DbTransaction tx, CreateAccountDto dto);

        Result Update(UpdateAccountDto dto);
        Result Delete(int id);

        /// <summary>بمعاملة خارجية، بالكود مباشرة (لا Id — المستدعي يملكه بالفعل) — يخدم CustomerService/SupplierService.Delete. بلا تحقق صلاحية/قيود/أبناء (المستدعي تحقق ذلك بنفسه قبل فتح معاملته)، وبلا استدعاء DeleteByAccountCode عكسياً (يمنع حلقة ping-pong — الطرف المرتبط يحذف نفسه هو، لا AccountService ينوب عنه).</summary>
        Result Delete(DbConnection conn, DbTransaction tx, string accountCode);

        /// <summary>يزامن اسم حساب من تعديل الطرف المرتبط (عميل/مورد) — الاسم فقط، بلا Update(UpdateAccountDto) الكاملة. يخدم CustomerService/SupplierService.Update ضمن معاملتهما.</summary>
        Result UpdateName(DbConnection conn, DbTransaction tx, string accountCode, string name);

        // ===== الأرصدة =====
        Result RecalculateBalance(string code);

        /// <summary>لخدمات أخرى ضمن معاملتها الخاصة (JournalService.Post/Unpost) — بلا صلاحية منفصلة (العملية الأصلية محكومة بصلاحيتها) وبلا Audit مستقل (Audit العملية الأصلية يوثّق الأثر).</summary>
        Result RecalculateBalance(DbConnection conn, DbTransaction tx, string code);

        Result RecalculateAllBalances();
        Result<decimal> GetBalanceAsOf(string code, DateTime date);
        Result<List<AccountStatementLine>> GetStatement(string code, DateTime from, DateTime to);
        bool IsLinkedRoot(string accountCode);

        // ===== المساعدات =====
        Result<bool> CanAcceptEntries(string code);
        Result<bool> CanHaveChildren(string code);
        Result<AccountType> GetTypeOf(string code);
    }
}
