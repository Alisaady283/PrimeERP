using System;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Collections.Generic;
using PrimeERP.Data.Core;
using PrimeERP.Data.Repositories.Base;
using PrimeERP.Domain.Entities;
using PrimeERP.Domain.Enums;

namespace PrimeERP.Data.Repositories
{
    /// <summary>مستودع Product</summary>
    public interface IProductRepository
    {
        Product GetById(int id, PrimeDbContext db = null);
        Product GetByCode(string code, PrimeDbContext db = null);
        Dictionary<string, Product> ByCodes(IEnumerable<string> codes, PrimeDbContext db = null);
        List<Product> GetByIds(IEnumerable<int> ids, PrimeDbContext db = null);
        /// <summary>أصناف التصنيف — لتصفية التحليل به</summary>
        List<int> IdsInCategory(int categoryId);
        List<Product> Search(string term, int maxResults);
        /// <summary>الأصناف النشطة التي لها حدّ طلب</summary>
        List<Product> WithReorderPoint();
        bool ExistsCode(string code, int? excludeId = null);
        bool ExistsBarcode(string barcode, int excludeId);
        Dictionary<string, Product> ByBarcodes(IEnumerable<string> barcodes, PrimeDbContext db = null);
        (List<Product> Items, int Total) GetPaged(
            int page, int pageSize, string searchText = null, bool? isActive = null, int? categoryId = null,
            string sortColumn = "Name", bool sortDescending = false);

        int Insert(Product p, PrimeDbContext db = null);
        void Update(Product p, PrimeDbContext db = null);
        void Delete(int id, string deletedBy, PrimeDbContext db = null);
    }

    public class ProductRepository : RepositoryBase<Product>, IProductRepository
    {
        protected override string TableName => "Products";



        public List<int> IdsInCategory(int categoryId) =>
            Fetch(q => q.Where(p => p.CategoryId == categoryId)).Select(p => p.Id).ToList();

        public Product GetByCode(string code, PrimeDbContext db = null) =>
            One(q => q.Where(p => p.Code == code), db);

        public List<Product> WithReorderPoint() => Fetch(q => q.Where(p => p.IsActive && p.ReorderPoint > 0));

        public List<Product> Search(string term, int maxResults) =>
            Fetch(q => q.Where(p => p.IsActive
                                 && (EF.Functions.Like(p.Name, $"%{term}%")
                                  || EF.Functions.Like(p.Code, $"%{term}%")
                                  || EF.Functions.Like(p.Barcode, $"%{term}%")))
                        .OrderBy(p => p.Name).Take(maxResults));

        public bool ExistsCode(string code, int? excludeId = null) =>
            Count(q => q.Where(p => p.Code == code
                                 && (excludeId == null || p.Id != excludeId))) > 0;

        public bool ExistsBarcode(string barcode, int excludeId) =>
            Count(q => q.Where(p => p.Barcode == barcode && p.Id != excludeId)) > 0;

        public Dictionary<string, Product> ByBarcodes(IEnumerable<string> barcodes, PrimeDbContext db = null) => By(nameof(Product.Barcode), barcodes, db);

        public (List<Product> Items, int Total) GetPaged(
            int page, int pageSize, string searchText = null, bool? isActive = null, int? categoryId = null,
            string sortColumn = "Name", bool sortDescending = false)
        {
            IQueryable<Product> Shape(IQueryable<Product> rows)
            {
                var q = rows;
                if (!string.IsNullOrWhiteSpace(searchText))
                    q = q.Where(p => EF.Functions.Like(p.Name, $"%{searchText}%")
                                  || EF.Functions.Like(p.Code, $"%{searchText}%")
                                  || EF.Functions.Like(p.Barcode, $"%{searchText}%"));
                if (isActive != null) q = q.Where(p => p.IsActive == isActive);
                if (categoryId != null) q = q.Where(p => p.CategoryId == categoryId);
                return q;
            }

            var (items, total) = Page(page, pageSize, Shape, sortColumn switch
            {
                "Name"      => By(p => p.Name, sortDescending),
                "SalePrice" => By(p => p.SalePrice, sortDescending),
                "CreatedAt" => By(p => p.CreatedAt, sortDescending),
                _           => By(p => p.Code, sortDescending),
            });

            return (Named(items), total);
        }

        /// <summary>اسم الفئة والعلامة</summary>
        private static List<Product> Named(List<Product> rows) =>
            WithCategoryNames(rows,
                (p => p.CategoryId, (p, name) => p.CategoryName = name),
                (p => p.BrandId,    (p, name) => p.BrandName = name));

        public int Insert(Product p, PrimeDbContext db = null) => Add(p, db);

        public void Update(Product p, PrimeDbContext db = null) =>
            Modify(p, db, nameof(Product.Code));

        public void Delete(int id, string deletedBy, PrimeDbContext db = null) =>
            SoftDelete(id, deletedBy, db);
    }
}
