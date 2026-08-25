using System.Data.Common;

namespace PrimeERP.Data.Repositories
{
    /// <summary>الشكل المشترك بين ICustomerRepository/ISupplierRepository الذي يحتاجه PartyServiceBase فقط — لا يستبدل الواجهة الكاملة لكل مستودع.</summary>
    public interface IPartyRepository<TEntity>
    {
        TEntity GetByAccountCode(string accountCode, DbConnection conn = null, DbTransaction tx = null);
        int Insert(TEntity entity, DbConnection conn = null, DbTransaction tx = null);
        void UpdateNameByAccountCode(DbConnection conn, DbTransaction tx, string accountCode, string name);
        void Delete(int id, string deletedBy, DbConnection conn = null, DbTransaction tx = null);
        void SetBalance(int id, decimal balance, DbConnection conn = null, DbTransaction tx = null);
    }
}
