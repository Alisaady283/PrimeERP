using System.Collections.Generic;
using System.Data.Common;
using PrimeERP.Domain.Entities;

namespace PrimeERP.Data.Repositories
{
    public interface IProductRepository
    {
        void CreateTable();
        Product GetById(int id, DbConnection conn = null, DbTransaction tx = null);
        List<Product> Search(string term, int maxResults);
        bool ExistsCode(string code, int? excludeId = null);
        (List<Product> Items, int Total) GetPaged(
            int page, int pageSize, string searchText = null, bool? isActive = null, int? categoryId = null,
            string sortColumn = "Name", bool sortDescending = false);

        int Insert(Product p, DbConnection conn = null, DbTransaction tx = null);
        void Update(Product p, DbConnection conn = null, DbTransaction tx = null);
        void Delete(int id, string deletedBy, DbConnection conn = null, DbTransaction tx = null);
    }
}
