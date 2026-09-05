using System.Collections.Generic;
using PrimeERP.Application.DTOs.Assets;
using PrimeERP.Application.Validation;
using PrimeERP.Data.Repositories;
using PrimeERP.Domain.Entities;
using PrimeERP.Domain.Enums;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Audit;
using PrimeERP.Platform.Localization;
using PrimeERP.Platform.Permissions;
using PrimeERP.Platform.Settings;

namespace PrimeERP.Application.Services.Assets
{
    // بنفس بنية ProductService حرفياً — CrudServiceBase + Create/Update/Delete خاصة بالكيان.
    public class AssetService : CrudServiceBase<Asset, AssetDto, AssetFilter>, IAssetService
    {
        protected override string PermissionPrefix => "Assets";
        protected override string StringPrefix => "Str.Asset";
        protected override string EntityName => "Assets";

        private readonly IAssetRepository _assets;
        private readonly ICategoryRepository _categories;
        private readonly INumberSequenceService _numbers;

        public AssetService(IPermissionService permissions, ISettingsProvider settings, ILocalizationService localization,
            IAuditLogger audit, IAssetRepository assets, ICategoryRepository categories, INumberSequenceService numbers)
            : base(permissions, settings, localization, audit)
        {
            _assets = assets;
            _categories = categories;
            _numbers = numbers;
        }

        protected override Asset FindById(int id) => _assets.GetById(id);

        protected override (List<Asset> Items, int Total) FindPaged(int page, int pageSize, AssetFilter filter)
        {
            filter ??= new AssetFilter();
            return _assets.GetPaged(page, pageSize, filter.SearchText, filter.IsActive, filter.CategoryId, filter.SortBy, filter.SortDescending);
        }

        protected override List<Asset> FindSearch(string term, int maxResults) => _assets.Search(term, maxResults);

        public Result<AssetDto> Create(CreateAssetDto dto)
        {
            if (!Can("Create")) return FailDenied<AssetDto>();

            var asset = new Asset
            {
                Code = _numbers.Next("Asset"), Name = dto.Name, CategoryId = dto.CategoryId, PurchaseDate = dto.PurchaseDate,
                PurchaseCost = dto.PurchaseCost, CurrentValue = dto.CurrentValue, Location = dto.Location,
                UsefulLifeYears = dto.UsefulLifeYears, SalvageValue = dto.SalvageValue,
                Notes = dto.Notes, IsActive = dto.IsActive, CreatedBy = CurrentUser
            };

            var validation = new AssetValidator().Validate(asset);
            if (!validation.IsValid) return Result.Fail<AssetDto>(string.Join("; ", validation.Errors.Values), ErrorCode.ValidationFailed);

            var id = _assets.Insert(asset);
            asset.Id = id;

            Audit.Log(EntityName, id, AuditAction.Insert, newValue: new { asset.Code, asset.Name });
            return Result.Ok(ToDto(asset));
        }

        public Result Update(UpdateAssetDto dto)
        {
            if (!Can("Edit")) return FailDenied();

            var asset = _assets.GetById(dto.Id);
            if (asset == null) return Fail("NotFound", ErrorCode.NotFound);

            asset.Name = dto.Name; asset.CategoryId = dto.CategoryId; asset.PurchaseDate = dto.PurchaseDate;
            asset.PurchaseCost = dto.PurchaseCost; asset.CurrentValue = dto.CurrentValue; asset.Location = dto.Location;
            asset.UsefulLifeYears = dto.UsefulLifeYears; asset.SalvageValue = dto.SalvageValue;
            asset.Notes = dto.Notes; asset.IsActive = dto.IsActive; asset.UpdatedBy = CurrentUser;

            var validation = new AssetValidator().Validate(asset);
            if (!validation.IsValid) return Result.Fail(string.Join("; ", validation.Errors.Values), ErrorCode.ValidationFailed);

            _assets.Update(asset);
            Audit.Log(EntityName, asset.Id, AuditAction.Update, newValue: new { asset.Name });
            return Result.Ok();
        }

        public Result Delete(int id)
        {
            if (!Can("Delete")) return FailDenied();

            var asset = _assets.GetById(id);
            if (asset == null) return Fail("NotFound", ErrorCode.NotFound);

            _assets.Delete(id, CurrentUser);
            Audit.Log(EntityName, id, AuditAction.Delete, details: asset.Code);
            return Result.Ok();
        }

        protected override AssetDto ToDto(Asset a)
        {
            var (variant, statusKey) = (a.IsActive ? StatusVariant.Success : StatusVariant.Danger, a.IsActive ? "Active" : "Inactive");
            return new AssetDto
            {
                Id = a.Id, Code = a.Code, Name = a.Name,
                CategoryId = a.CategoryId, CategoryName = a.CategoryId != null ? _categories.GetById(a.CategoryId.Value)?.Name : null,
                PurchaseDate = a.PurchaseDate, PurchaseCost = a.PurchaseCost, CurrentValue = a.CurrentValue, Location = a.Location, Notes = a.Notes,
                UsefulLifeYears = a.UsefulLifeYears, SalvageValue = a.SalvageValue, AccumulatedDepreciation = a.AccumulatedDepreciation,
                IsActive = a.IsActive, StatusVariant = variant, StatusText = LocalizationService.Get($"Str.{statusKey}"),
                CreatedAt = a.CreatedAt, UpdatedAt = a.UpdatedAt,
                CanEdit = Can("Edit"), CanDelete = Can("Delete")
            };
        }
    }
}
