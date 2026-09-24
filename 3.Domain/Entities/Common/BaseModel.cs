using System;

namespace PrimeERP.Domain.Entities.Common
{
    /// <summary>القاعدة المشتركة لكل الكيانات الرئيسية</summary>
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

        public long RowVersion { get; set; }
    }
}
