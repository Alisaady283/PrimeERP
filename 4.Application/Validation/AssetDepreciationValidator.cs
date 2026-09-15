using PrimeERP.Domain.Contracts;
using PrimeERP.Domain.Entities;

namespace PrimeERP.Application.Validation
{
    public class AssetDepreciationValidator : ValidatorBase, IValidator<AssetDepreciation>
    {
        public ValidationResult Validate(AssetDepreciation charge)
        {
            var result = new ValidationResult();

            if (charge.AssetId <= 0) result.AddError("AssetId", "اختر الأصل");
            if (charge.PeriodDate == default) result.AddError("PeriodDate", "شهر الإهلاك مطلوب");
            Positive(result, "Amount", charge.Amount, "قيمة القسط");

            return result;
        }
    }
}
