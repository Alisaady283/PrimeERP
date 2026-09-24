using PrimeERP.Domain.Contracts;
using PrimeERP.Domain.Entities;

namespace PrimeERP.Application.Validation
{
    /// <summary>تحقّق استبعاد الأصل</summary>
    public class AssetDisposalValidator : ValidatorBase, IValidator<AssetDisposal>
    {
        public ValidationResult Validate(AssetDisposal disposal)
        {
            var result = new ValidationResult();

            if (disposal.AssetId <= 0) result.AddError("AssetId", "اختر الأصل");
            if (disposal.TreasuryId <= 0) result.AddError("TreasuryId", "اختر الخزينة التي قُبض فيها الثمن");
            if (disposal.DisposalDate == default) result.AddError("DisposalDate", "تاريخ البيع مطلوب");

            if (disposal.SalePrice < 0) result.AddError("SalePrice", "ثمن البيع لا يكون سالباً");

            return result;
        }
    }
}
