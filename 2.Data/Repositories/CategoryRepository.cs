using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using PrimeERP.Data.Repositories.Base;
using PrimeERP.Data.Core;
using PrimeERP.Domain.Entities;

namespace PrimeERP.Data.Repositories
{
    /// <summary>مستودع Category</summary>
    public interface ICategoryRepository
    {
        List<Category> GetAll(string moduleKey, bool includeInactive = false);
        Category GetById(int id, PrimeDbContext db = null);
        bool HasChildren(int id);
        int Insert(Category c, PrimeDbContext db = null);
        void Update(Category c, PrimeDbContext db = null);
        void Delete(int id, PrimeDbContext db = null);
    }

    public class CategoryRepository : RepositoryBase<Category>, ICategoryRepository
    {
        protected override string TableName => "Categories";


        public List<Category> GetAll(string moduleKey, bool includeInactive = false) =>
            Fetch(q => q.Where(c => c.ModuleKey == moduleKey && (includeInactive || c.IsActive))
                       .OrderBy(c => c.Name));


        public bool HasChildren(int id) => Any(q => q.Where(c => c.ParentId == id && c.IsActive));

        public int Insert(Category c, PrimeDbContext db = null) => Add(c, db);

        public void Update(Category c, PrimeDbContext db = null) =>
            Edit(x => x.Id == c.Id, row =>
            {
                row.Name = c.Name;
                row.ParentId = c.ParentId;
                row.IsActive = c.IsActive;
                row.Notes = c.Notes ?? "";
                row.AccountCode = c.AccountCode ?? "";
                row.DepreciationAccountCode = c.DepreciationAccountCode ?? "";
            }, db);

        public void Delete(int id, PrimeDbContext db = null) =>
            Set(c => c.Id == id, s => s.SetProperty(r => r.IsActive, false), db);
    }
}
