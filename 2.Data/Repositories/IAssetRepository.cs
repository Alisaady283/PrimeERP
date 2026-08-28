using System.Collections.Generic;
using System.Data.Common;
using PrimeERP.Domain.Entities;

namespace PrimeERP.Data.Repositories
{
    public interface IAssetRepository
    {
        void CreateTable();
        Asset GetById(int id, DbConnection conn = null, DbTransaction tx = null);
        List<Asset> Search(string term, int maxResults);
        (List<Asset> Items, int Total) GetPaged(
            int page, int pageSize, string searchText = null, bool? isActive = null, int? categoryId = null,
            string sortColumn = "Name", bool sortDescending = false);

        int Insert(Asset a, DbConnection conn = null, DbTransaction tx = null);
        void Update(Asset a, DbConnection conn = null, DbTransaction tx = null);
        void Delete(int id, string deletedBy, DbConnection conn = null, DbTransaction tx = null);
    }
}
