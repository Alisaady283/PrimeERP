using PrimeERP.Domain.Enums;

namespace PrimeERP.Platform.Audit
{
    public interface IAuditLogger
    {
        void Log(string tableName, int recordId, AuditAction action,
                 object oldValue = null, object newValue = null, string details = null);
    }
}
