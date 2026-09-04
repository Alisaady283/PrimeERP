using System;
using System.Collections.Generic;
using PrimeERP.Platform.Permissions;
using PrimeERP.Domain.Enums;
using PrimeERP.Domain.Results;

namespace PrimeERP.Application.DTOs.Accounting
{
    /// <summary>أي حساب مرتبط به تلقائياً (عميل/مورد) — تُقرأ لا تُكتب هنا؛ إنشاؤه الفعلي عبر ICustomerService/ISupplierService.</summary>
    public enum LinkedEntityType { None, Customer, Supplier }

    /// <summary>للعرض في الجداول — لا Model خام (Account) يخرج من الخدمة إطلاقاً.</summary>
    public class AccountDto
    {
        public int    Id         { get; set; }
        public string Code       { get; set; }
        public string Name       { get; set; }
        public int?   ParentId   { get; set; }
        public string ParentCode { get; set; }
        public string ParentName { get; set; }
        public int    Level      { get; set; }
        public bool   IsLeaf     { get; set; }
        public bool   IsActive   { get; set; }
        public AccountType Type  { get; set; }
        public string TypeName   { get; set; }
        public decimal Balance   { get; set; }

        public LinkedEntityType LinkedEntityType { get; set; }
        public int?             LinkedEntityId   { get; set; }

        public bool HasChildren     { get; set; }
        public bool HasTransactions { get; set; }
        public bool IsSystem        { get; set; }

        /// <summary>Active → Success، Inactive → Danger — من مفردات الحالة الستة فقط.</summary>
        public StatusVariant StatusVariant { get; set; }
        public string   StatusText { get; set; }
        public DateTime CreatedAt  { get; set; }
        public DateTime UpdatedAt  { get; set; }
    }

    /// <summary>للشجرة — نفس حقول AccountDto + أبناء.</summary>
    public class AccountTreeNode : AccountDto
    {
        public List<AccountTreeNode> Children { get; set; } = new();
    }

    public class AccountTreeFilter
    {
        public string SearchText      { get; set; }
        public int?   LevelFilter     { get; set; }
        public AccountType? TypeFilter { get; set; }
        public bool   LeafOnly        { get; set; }
        public bool   IncludeInactive { get; set; }
    }

    public class CreateAccountDto
    {
        public int    ParentId { get; set; }
        public string Name     { get; set; }
        public bool   IsLeaf   { get; set; } = true;
        public bool   IsActive { get; set; } = true;
        public string Notes    { get; set; }
        public LinkedEntityType LinkedEntityType { get; set; } = LinkedEntityType.None;

        /// <summary>
        /// true يتخطّى الربط التلقائي (حساب↔عميل/مورد) كلياً بصرف النظر عن SettingKeys.Accounts.AutoLinkEnabled —
        /// إلزامي عند true من CustomerService/SupplierService.Create (اللتين تُنشئان الحساب أولاً ثم العميل/المورد
        /// نفسه): بدونه AccountService.Create يحاول استدعاء CustomerService.CreateFromAccount ثانية → حلقة لا
        /// نهائية (CustomerService.Create → AccountService.Create → CustomerService.CreateFromAccount → ...).
        /// </summary>
        public bool SkipAutoLink { get; set; } = false;
    }

    public class UpdateAccountDto
    {
        public int    Id       { get; set; }
        public string Name     { get; set; }
        public bool   IsLeaf   { get; set; }
        public bool   IsActive { get; set; }
        public string Notes    { get; set; }
    }

    public class AccountStatementLine
    {
        public string Date         { get; set; }
        public string EntryNo      { get; set; }
        public string Description  { get; set; }
        public decimal Debit       { get; set; }
        public decimal Credit      { get; set; }
        public decimal RunningBalance { get; set; }
        public string SourceType   { get; set; }

        /// <summary>قيمة استعلامية تظهر بالكشف بلا أثر على الرصيد — شيك لم يُسدَّد من البنك بعد.</summary>
        public decimal MemoAmount  { get; set; }
    }
}
