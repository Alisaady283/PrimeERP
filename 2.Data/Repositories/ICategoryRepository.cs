using System.Collections.Generic;
using System.Data.Common;
using PrimeERP.Domain.Entities;

namespace PrimeERP.Data.Repositories
{
    public interface ICategoryRepository
    {
        void CreateTable();
        List<Category> GetAll(string moduleKey, bool includeInactive = false);
        Category GetById(int id, DbConnection conn = null, DbTransaction tx = null);
        bool HasChildren(int id);
        int Insert(Category c, DbConnection conn = null, DbTransaction tx = null);
        void Update(Category c, DbConnection conn = null, DbTransaction tx = null);
        void Delete(int id, DbConnection conn = null, DbTransaction tx = null);
    }
}
