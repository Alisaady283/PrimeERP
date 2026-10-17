using PrimeERP.Application.Services.Core;
using System.Collections.Generic;
using PrimeERP.Application.DTOs.Inventory;
using PrimeERP.Application.Validation;
using PrimeERP.Data.Core;
using PrimeERP.Data.Repositories;
using PrimeERP.Domain.Calculations;
using PrimeERP.Domain.Entities;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Audit;
using PrimeERP.Platform.Localization;
using PrimeERP.Platform.Permissions;
using PrimeERP.Platform.Settings;

namespace PrimeERP.Application.PageServices.Inventory
{
    /// <summary>خدمة الأصناف</summary>
    public class ProductService : EntityService<Product, Product, Product, Product, ProductFilter>, IProductService
    {
        protected override string PermissionPrefix => "Products";
        protected override string StringPrefix => "Str.Product";
        protected override string EntityName => "Products";
        protected override string SequenceKey => "Product";

        public static readonly Field<Product>[] Rules =
        {
            new(x => x.Code, "Str.Field.ProductCode", Required: true),
            new(x => x.Name, "Str.Field.ProductName", Required: true, Max: 200),
            new(x => x.Description, "Str.Product.Description", Max: 1000),
            new(x => x.CostPrice, "Str.CostPrice", From: 0),
            new(x => x.SalePrice, "Str.SalePrice", From: 0),
            new(x => x.SalePrice, "Str.SalePrice", Must: p => p.MinPrice == 0 || p.SalePrice >= p.MinPrice, Message: "Str.Product.BelowMinPrice"),
        };

        protected override Field<Product>[] Fields => Rules;

        private readonly IProductRepository _products;
        private readonly IStockMovementRepository _movements;

        public ProductService(IPermissionService permissions, ISettingsProvider settings, ILocalizationService localization,
            IAuditLogger audit, IProductRepository products, INumberSequenceService numbers, IStockMovementRepository movements)
            : base(permissions, settings, localization, audit, numbers)
        {
            _products = products;
            _movements = movements;
        }

        protected override Product FindById(int id) => _products.GetById(id);

        public Result<Product> GetByCode(string code)
        {
            if (!Can("View")) return FailDenied<Product>();

            var product = _products.GetByCode(code);
            return product == null ? Fail<Product>("NotFound", ErrorCode.NotFound) : Ok(product);
        }

        protected override (List<Product> Items, int Total) FindPaged(int page, int pageSize, ProductFilter filter)
        {
            filter ??= new ProductFilter();
            return _products.GetPaged(page, pageSize, filter.SearchText, filter.IsActive, filter.CategoryId, filter.SortBy, filter.SortDescending);
        }

        protected override List<Product> FindSearch(string term, int maxResults) => _products.Search(term, maxResults);

        protected override void Number(Product p, string code) => p.Code = code;

        protected override Result Prepare(Product p, Product stored)
        {
            p.Barcode = string.IsNullOrWhiteSpace(p.Barcode)
                ? Setting(SettingKeys.Documents.AutoBarcode, false) ? BarcodeCalc.Internal(Setting(SettingKeys.Documents.BarcodePrefix, "20"), p.Code) : null
                : p.Barcode.Trim();

            if (p.Barcode == null) return Result.Ok();

            if (BarcodeCalc.IsNumeric(p.Barcode) && BarcodeCalc.IsGtinLength(p.Barcode) && p.Barcode[0] != '2' && !BarcodeCalc.IsValidGtin(p.Barcode))
                return Fail("BarcodeCheckDigit", ErrorCode.ValidationFailed);

            return _products.ExistsBarcode(p.Barcode, p.Id) ? Fail("BarcodeTaken", ErrorCode.ValidationFailed) : Result.Ok();
        }

        public Result<(Product Product, ScannedCode Code)> Scan(string raw)
        {
            if (!Can("View")) return FailDenied<(Product, ScannedCode)>();

            var scale = new ScaleFormat(Setting(SettingKeys.Documents.ScalePrefix, "21"), Setting(SettingKeys.Documents.ScaleItemDigits, 5),
                Setting(SettingKeys.Documents.ScaleWeighted, true), Setting(SettingKeys.Documents.ScaleDecimals, 3));
            var code = BarcodeCalc.Read(raw, scale);

            var product = _products.ByBarcodes(new[] { code.Key }).GetValueOrDefault(code.Key) ?? _products.GetByCode(code.Key);
            return product == null
                ? Fail<(Product, ScannedCode)>("BarcodeNotFound", ErrorCode.NotFound, code.Key)
                : Ok((product, code));
        }

        protected override int Insert(PrimeDbContext db, Product p) => _products.Insert(p, db);
        protected override void Save(PrimeDbContext db, Product p) => _products.Update(p, db);
        protected override void Erase(PrimeDbContext db, Product p) => _products.Delete(p.Id, CurrentUser, db);

        protected override Result CanErase(Product p) =>
            _movements.AnyForProduct(p.Id) ? Fail("HasMovements", ErrorCode.ValidationFailed) : Result.Ok();

        protected override object AuditValue(Product p) => new { p.Code, p.Name };
        protected override string DeleteDetails(Product p) => p.Code;

        protected override Product ToDto(Product p) => p;
    }
}
