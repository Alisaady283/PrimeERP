using PrimeERP.Domain.Contracts;
using PrimeERP.Domain.Entities;
using PrimeERP.Domain.Rules;

namespace PrimeERP.Application.Validation
{
    /// <summary>تحقّق العميل</summary>
    public class CustomerValidator : IValidator<Customer>
    {
        public ValidationResult Validate(Customer customer) =>
            Rules.For<Customer>()
                .Required(x => x.Code, "كود العميل")
                .Required(x => x.Name, "اسم العميل")
                .MaxLength(x => x.Name, 150, "اسم العميل")
                .Phone(x => x.Phone, "رقم الهاتف")
                .Email(x => x.Email, "البريد الإلكتروني")
                .Positive(x => x.CreditLimit, "الحد الائتماني")
                .Validate(customer);
    }
}
