using System;

namespace PrimeERP.Models.Common
{
    /// <summary>القاعدة المشتركة لكل الكيانات الرئيسية — تتبع الإنشاء/التعديل، حذف منطقي، وعمود تزامن.</summary>
    public abstract class BaseModel
    {
        public int       Id         { get; set; }
        public DateTime  CreatedAt  { get; set; }
        public string    CreatedBy  { get; set; }
        public DateTime  UpdatedAt  { get; set; }
        public string    UpdatedBy  { get; set; }
        public bool      IsDeleted  { get; set; }
        public DateTime? DeletedAt  { get; set; }
        public string    DeletedBy  { get; set; }

        /// <summary>عدّاد تزامن متفائل (Optimistic Concurrency) — راجع SchemaBuilder.Concurrency وDbHelper.ExecuteWithConcurrencyCheck.</summary>
        public long RowVersion { get; set; }
    }
}
