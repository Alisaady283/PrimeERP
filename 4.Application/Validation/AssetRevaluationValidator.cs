using PrimeERP.Domain.Contracts;
using PrimeERP.Domain.Entities;

namespace PrimeERP.Application.Validation
{
    /// <summary>تحقّق إعادة التقييم</summary>
    public class AssetRevaluationValidator : ValidatorBase, IValidator<AssetRevaluation>
    {
        public ValidationResult Validate(AssetRevaluation revaluation)
        {
            var result = new ValidationResult();

            if (revaluation.AssetId <= 0) result.AddError("AssetId", "اختر الأصل");
            if (revaluation.RevaluationDate == default) result.AddError("RevaluationDate", "تاريخ إعادة التقييم مطلوب");
            Positive(result, "NewValue", revaluation.NewValue, "القيمة بعد إعادة التقييم");

            if (revaluation.Difference == 0)
                result.AddError("NewValue", "القيمة الجديدة تساوي القيمة الحالية — لا فرق يُرحَّل");

            return result;
        }
    }
}
