using PrimeERP.Data.Repositories.Base;
using PrimeERP.Domain.Entities;
using PrimeERP.Platform.Audit;

namespace PrimeERP.Data.Repositories
{
    /// <summary>مستودع AuditLog</summary>
    public class AuditRepository : RepositoryBase<AuditEntry>, IAuditStore
    {
        protected override string TableName => "AuditLog";

        public void Insert(AuditEntry entry) => Add(entry);
    }
}
