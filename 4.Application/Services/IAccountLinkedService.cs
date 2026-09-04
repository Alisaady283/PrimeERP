using System.Data.Common;
using PrimeERP.Domain.Results;

namespace PrimeERP.Application.Services
{
    /// <summary>كيان يعيش كورقة تحت أصل معيّن في شجرة الحسابات (عميل، مورد، خزينة، بنك): إنشاؤه من الشجرة
    /// وإنشاء حسابه منه وجهان لنفس السجل. AccountService يستدعي هذه الأوجه الثلاثة عبر هذا العقد وحده، فلا
    /// يعرف نوع الكيان — إضافة كيان مرتبط جديد لا تُدخِل أي فرع في AccountService.</summary>
    public interface IAccountLinkedService
    {
        /// <summary>rootCode = كود الأصل الذي وقع الحساب تحته مباشرة. يُمرَّر ولا يُقرأ من القاعدة: الحساب
        /// لم يُثبَّت بعد خارج معاملته، فقراءته هنا تُرجع لا شيء.</summary>
        Result CreateFromAccount(DbConnection conn, DbTransaction tx, string accountCode, string name, string rootCode);
        Result UpdateNameFromAccount(DbConnection conn, DbTransaction tx, string accountCode, string name);
        Result DeleteByAccountCode(DbConnection conn, DbTransaction tx, string accountCode);
    }
}
