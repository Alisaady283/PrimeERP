using System.Collections.Generic;
using System.Data.Common;
using PrimeERP.Domain.Entities;

namespace PrimeERP.Data.Repositories
{
    public interface ISupplierRepository : IPartyRepository<Supplier>
    {
        void CreateTable();
        Supplier GetById(int id, DbConnection conn = null, DbTransaction tx = null);
        Supplier GetByCode(string code);
        new Supplier GetByAccountCode(string accountCode, DbConnection conn = null, DbTransaction tx = null);
        new List<Supplier> GetAll(bool activeOnly = true);
        List<Supplier> Search(string term, int maxResults);
        bool ExistsCode(string code, int? excludeId = null);
        bool ExistsPhone(string phone, int? excludeId = null);
        bool ExistsName(string name, int? excludeId = null);
        (List<Supplier> Items, int Total) GetPaged(
            int page, int pageSize,
            string searchText = null, bool? isActive = null, bool? hasBalance = null, bool? overCreditLimit = null,
            int? categoryId = null, string sortColumn = "Name", bool sortDescending = false);

        new int Insert(Supplier s, DbConnection conn = null, DbTransaction tx = null);
        void Update(Supplier s, DbConnection conn = null, DbTransaction tx = null);
        new void UpdateNameByAccountCode(DbConnection conn, DbTransaction tx, string accountCode, string name);
        new void Delete(int id, string deletedBy, DbConnection conn = null, DbTransaction tx = null);
        new void SetBalance(int id, decimal balance, DbConnection conn = null, DbTransaction tx = null);
    }
}
