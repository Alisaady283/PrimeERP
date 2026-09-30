using PrimeERP.Data.Core;
using System.Collections.Generic;

namespace PrimeERP.Data.Repositories
{
    /// <summary>الشكل المشترك بين ICustomerRepository/ISupplierReposito</summary>
    public interface IPartyRepository<TEntity>
    {
        TEntity GetById(int id, PrimeDbContext db = null);
        Dictionary<int, string> NamesOf(IEnumerable<int> ids, PrimeDbContext db = null);
        TEntity GetByCode(string code);
        TEntity GetByAccountCode(string accountCode, PrimeDbContext db = null);
        List<TEntity> GetAll(bool activeOnly = true);
        List<TEntity> Search(string term, int maxResults);
        int CountAll(bool activeOnly = true);

        bool ExistsCode(string code, int? excludeId = null);
        bool ExistsPhone(string phone, int? excludeId = null);
        bool ExistsName(string name, int? excludeId = null);

        (List<TEntity> Items, int Total) GetPaged(
            int page, int pageSize,
            string searchText = null, bool? isActive = null, bool? hasBalance = null, bool? overCreditLimit = null,
            int? categoryId = null, string sortColumn = "Name", bool sortDescending = false);

        int Insert(TEntity entity, PrimeDbContext db = null);
        void Update(TEntity entity, PrimeDbContext db = null);
        void UpdateNameByAccountCode(PrimeDbContext db, string accountCode, string name);
        void Delete(int id, string deletedBy, PrimeDbContext db = null);
        void SetBalance(int id, decimal balance, PrimeDbContext db = null);
    }
}
