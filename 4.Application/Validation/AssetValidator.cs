using PrimeERP.Domain.Contracts;
using PrimeERP.Domain.Entities;

namespace PrimeERP.Application.Validation
{
    /// <summary>تحقّق الأصل</summary>
    public class AssetValidator : ValidatorBase, IValidator<Asset>
    {
        public ValidationResult Validate(Asset asset)
        {
            var result = new ValidationResult();

            Required(result, "Code", asset.Code, "كود الأصل");
            Required(result, "Name", asset.Name, "اسم الأصل");
            MaxLength(result, "Name", asset.Name, 200, "اسم الأصل");
            Positive(result, "PurchaseCost", asset.PurchaseCost, "تكلفة الشراء");

            return result;
        }
    }
}
