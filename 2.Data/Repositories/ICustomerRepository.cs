using System.Collections.Generic;
using System.Data.Common;
using PrimeERP.Domain.Entities;

namespace PrimeERP.Data.Repositories
{
    public interface ICustomerRepository : IPartyRepository<Customer>
    {
        void CreateTable();
        Customer GetById(int id, DbConnection conn = null, DbTransaction tx = null);
        Customer GetByCode(string code);
        new Customer GetByAccountCode(string accountCode, DbConnection conn = null, DbTransaction tx = null);
        List<Customer> GetAll(bool activeOnly = true);
        List<Customer> Search(string term, int maxResults);
        int CountAll(bool activeOnly = true);
        bool ExistsCode(string code, int? excludeId = null);
        bool ExistsPhone(string phone, int? excludeId = null);
        bool ExistsName(string name, int? excludeId = null);
        (List<Customer> Items, int Total) GetPaged(
            int page, int pageSize,
            string searchText = null, bool? isActive = null, bool? hasBalance = null, bool? overCreditLimit = null,
            int? categoryId = null, string sortColumn = "Name", bool sortDescending = false);

        new int Insert(Customer c, DbConnection conn = null, DbTransaction tx = null);
        void Update(Customer c, DbConnection conn = null, DbTransaction tx = null);
        new void UpdateNameByAccountCode(DbConnection conn, DbTransaction tx, string accountCode, string name);
        new void Delete(int id, string deletedBy, DbConnection conn = null, DbTransaction tx = null);
        new void SetBalance(int id, decimal balance, DbConnection conn = null, DbTransaction tx = null);
    }
}
