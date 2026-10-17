using PrimeERP.Domain.Entities;

namespace PrimeERP.Platform.Audit
{
    /// <summary>مخزن جدول AuditLog</summary>
    public interface IAuditStore
    {
        void Insert(AuditEntry entry);
    }
}
