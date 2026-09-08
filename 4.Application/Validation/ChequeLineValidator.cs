using PrimeERP.Application.DTOs.Cheques;
using PrimeERP.Domain.Contracts;
using PrimeERP.Domain.Rules;

namespace PrimeERP.Application.Validation
{
    /// <summary>
    /// ما يجعل سطر المستند شيكاً: رقمه ومبلغه وبنكه وطرفه. الشاشة تمنع النقص بإعلان الحقول مطلوبة،
    /// وهذا يضمنه لأي مستدعٍ آخر — استيراد، زرع بيانات، أو خدمة تستهلك خدمة الشيكات.
    /// </summary>
    public class ChequeLineValidator : IValidator<CreateChequeLineDto>
    {
        public ValidationResult Validate(CreateChequeLineDto line) =>
            Rules.For<CreateChequeLineDto>()
                .Required(x => x.ChequeNo, "رقم الشيك")
                .Required(x => x.BankName, "بنك الشيك")
                .Custom((x, result) =>
                {
                    // Positive تقبل الصفر (ترفض السالب فقط)، والشيك بصفر ليس شيكاً.
                    if (x.Amount <= 0) result.AddError(nameof(x.Amount), "مبلغ الشيك يجب أن يكون أكبر من صفر");
                    if (x.PartyId == null) result.AddError(nameof(x.PartyId), "طرف الشيك مطلوب");
                })
                .Validate(line);
    }
}
