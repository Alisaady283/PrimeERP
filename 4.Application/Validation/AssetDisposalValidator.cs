using PrimeERP.Domain.Contracts;
using PrimeERP.Domain.Entities;

namespace PrimeERP.Application.Validation
{
    public class AssetDisposalValidator : ValidatorBase, IValidator<AssetDisposal>
    {
        public ValidationResult Validate(AssetDisposal disposal)
        {
            var result = new ValidationResult();

            if (disposal.AssetId <= 0) result.AddError("AssetId", "اختر الأصل");
            if (disposal.TreasuryId <= 0) result.AddError("TreasuryId", "اختر الخزينة التي قُبض فيها الثمن");
            if (disposal.DisposalDate == default) result.AddError("DisposalDate", "تاريخ البيع مطلوب");

            // بيعٌ بلا ثمن وارد (تبرّعٌ أو إعدام)، والسالب لا: قيدٌ بطرفٍ سالب لا معنى له.
            if (disposal.SalePrice < 0) result.AddError("SalePrice", "ثمن البيع لا يكون سالباً");

            return result;
        }
    }
}
