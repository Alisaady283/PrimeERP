using System.Collections.Generic;
using System.Linq;
using PrimeERP.Application.DTOs.Inventory;
using PrimeERP.Application.Validation;
using PrimeERP.Data.Repositories;
using PrimeERP.Domain.Entities;
using PrimeERP.Domain.Enums;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Audit;
using PrimeERP.Platform.Localization;
using PrimeERP.Platform.Permissions;
using PrimeERP.Platform.Settings;

namespace PrimeERP.Application.Services.Inventory
{
    public class ProductService : CrudServiceBase<Product, ProductDto, ProductFilter>, IProductService
    {
        protected override string PermissionPrefix => "Products";
        protected override string StringPrefix => "Str.Product";
        protected override string EntityName => "Products";

        private readonly IProductRepository _products;
        private readonly ICategoryRepository _categories;
        private readonly INumberSequenceService _numbers;

        public ProductService(IPermissionService permissions, ISettingsProvider settings, ILocalizationService localization,
            IAuditLogger audit, IProductRepository products, ICategoryRepository categories, INumberSequenceService numbers)
            : base(permissions, settings, localization, audit)
        {
            _products = products;
            _categories = categories;
            _numbers = numbers;
        }

        protected override Product FindById(int id) => _products.GetById(id);

        public Result<ProductDto> GetByCode(string code)
        {
            if (!Can("View")) return FailDenied<ProductDto>();

            var product = _products.GetByCode(code);
            if (product == null) return Fail<ProductDto>("NotFound", ErrorCode.NotFound);

            return Ok(ToDto(product));
        }

        protected override (List<Product> Items, int Total) FindPaged(int page, int pageSize, ProductFilter filter)
        {
            filter ??= new ProductFilter();
            return _products.GetPaged(page, pageSize, filter.SearchText, filter.IsActive, filter.CategoryId, filter.SortBy, filter.SortDescending);
        }

        protected override List<Product> FindSearch(string term, int maxResults) => _products.Search(term, maxResults);

        public Result<ProductDto> Create(CreateProductDto dto)
        {
            if (!Can("Create")) return FailDenied<ProductDto>();

            var product = new Product
            {
                Code = _numbers.Next("Product"), Barcode = dto.Barcode, Name = dto.Name, NameEn = dto.NameEn,
                CategoryId = dto.CategoryId, BrandId = dto.BrandId, CostPrice = dto.CostPrice, SalePrice = dto.SalePrice, MinPrice = dto.MinPrice,
                Notes = dto.Notes, IsActive = dto.IsActive, CreatedBy = CurrentUser
            };

            var validation = new ProductValidator().Validate(product);
            if (!validation.IsValid) return Result.Fail<ProductDto>(string.Join("; ", validation.Errors.Values), ErrorCode.ValidationFailed);

            var id = _products.Insert(product);
            product.Id = id;

            Audit.Log(EntityName, id, AuditAction.Insert, newValue: new { product.Code, product.Name });
            return Result.Ok(ToDto(product));
        }

        public Result Update(UpdateProductDto dto)
        {
            if (!Can("Edit")) return FailDenied();

            var product = _products.GetById(dto.Id);
            if (product == null) return Fail("NotFound", ErrorCode.NotFound);

            product.Name = dto.Name; product.NameEn = dto.NameEn; product.CategoryId = dto.CategoryId; product.BrandId = dto.BrandId;
            product.CostPrice = dto.CostPrice; product.SalePrice = dto.SalePrice; product.MinPrice = dto.MinPrice;
            product.Notes = dto.Notes; product.IsActive = dto.IsActive; product.UpdatedBy = CurrentUser;

            var validation = new ProductValidator().Validate(product);
            if (!validation.IsValid) return Result.Fail(string.Join("; ", validation.Errors.Values), ErrorCode.ValidationFailed);

            _products.Update(product);
            Audit.Log(EntityName, product.Id, AuditAction.Update, newValue: new { product.Name });
            return Result.Ok();
        }

        public Result Delete(int id)
        {
            if (!Can("Delete")) return FailDenied();

            var product = _products.GetById(id);
            if (product == null) return Fail("NotFound", ErrorCode.NotFound);

            _products.Delete(id, CurrentUser);
            Audit.Log(EntityName, id, AuditAction.Delete, details: product.Code);
            return Result.Ok();
        }

        protected override ProductDto ToDto(Product p)
        {
            var (variant, statusKey) = (p.IsActive ? StatusVariant.Success : StatusVariant.Danger, p.IsActive ? "Active" : "Inactive");
            return new ProductDto
            {
                Id = p.Id, Code = p.Code, Barcode = p.Barcode, Name = p.Name, NameEn = p.NameEn,
                CategoryId = p.CategoryId, CategoryName = p.CategoryId != null ? _categories.GetById(p.CategoryId.Value)?.Name : null,
                BrandId = p.BrandId, BrandName = p.BrandId != null ? _categories.GetById(p.BrandId.Value)?.Name : null,
                CostPrice = p.CostPrice, SalePrice = p.SalePrice, MinPrice = p.MinPrice, Notes = p.Notes,
                IsActive = p.IsActive, StatusVariant = variant, StatusText = LocalizationService.Get($"Str.{statusKey}"),
                CreatedAt = p.CreatedAt, UpdatedAt = p.UpdatedAt,
                CanEdit = Can("Edit"), CanDelete = Can("Delete")
            };
        }
    }
}
