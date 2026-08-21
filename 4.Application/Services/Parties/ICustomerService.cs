using System;
using System.Collections.Generic;
using System.Data.Common;
using PrimeERP.Domain.Results;
using PrimeERP.Application.DTOs.Accounting;
using PrimeERP.Application.DTOs.Parties;

namespace PrimeERP.Application.Services.Parties
{
    /// <summary>
    /// المالك الوحيد لمنطق العملاء — Repository تحته CRUD صرف فقط. كل حساب عميل يُنشأ/يُحدَّث/يُحذف عبر
    /// IAccountService حصراً (لا CustomerRepository يلمس جدول Accounts إطلاقاً). راجع MIGRATION_INVENTORY.md
    /// لتوثيق حل التبعية الدائرية مع AccountService (CreateAccountDto.SkipAutoLink).
    /// </summary>
    public interface ICustomerService
    {
        // ===== القراءة =====
        Result<PagedResult<CustomerDto>> GetPaged(int page, int pageSize, CustomerFilter filter = null);
        Result<CustomerDto> GetById(int id);
        Result<CustomerDto> GetByCode(string code);

        /// <summary>يخدم CustomerPicker.</summary>
        Result<List<CustomerDto>> Search(string term, int maxResults = 50);

        /// <summary>يستدعي IAccountService.GetStatement على AccountCode العميل — لا يكرر منطق كشف الحساب.</summary>
        Result<List<AccountStatementLine>> GetStatement(int id, DateTime from, DateTime to);

        // ===== الإنشاء =====
        Result<CustomerDto> Create(CreateCustomerDto dto);
        Result<CustomerDto> Create(DbConnection conn, DbTransaction tx, CreateCustomerDto dto);

        /// <summary>الاتجاه المعاكس — يستدعيه AccountService.Create عند الربط التلقائي. لا ينشئ حساباً (موجود بالفعل)، ينشئ العميل فقط بـ AccountCode المُمرَّر.</summary>
        Result<CustomerDto> CreateFromAccount(DbConnection conn, DbTransaction tx, string accountCode, string name);

        // ===== التعديل =====
        Result Update(UpdateCustomerDto dto);

        /// <summary>الاتجاه المعاكس — يستدعيه AccountService.Update عند تعديل اسم الحساب. يحدّث اسم العميل فقط بلا مزامنة عكسية (يمنع حلقة ping-pong).</summary>
        Result UpdateNameFromAccount(DbConnection conn, DbTransaction tx, string accountCode, string name);

        // ===== الحذف =====
        Result Delete(int id);

        /// <summary>الاتجاه المعاكس — يستدعيه AccountService.Delete عند حذف الحساب مباشرة. يحذف العميل فقط بلا لمس الحساب (محذوف بالفعل من طرف الاستدعاء).</summary>
        Result DeleteByAccountCode(DbConnection conn, DbTransaction tx, string accountCode);

        // ===== الأرصدة والائتمان =====

        /// <summary>من رصيد الحساب المرتبط عبر IAccountService — لا حساب مزدوج، الحساب مصدر الحقيقة الوحيد.</summary>
        Result RecalculateBalance(int id);
        Result RecalculateAllBalances();

        Result<CreditCheckResult> CheckCreditLimit(int id, decimal additional);
    }
}
