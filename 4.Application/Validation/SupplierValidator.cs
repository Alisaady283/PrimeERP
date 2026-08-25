using PrimeERP.Domain.Contracts;
using PrimeERP.Domain.Entities;
using PrimeERP.Domain.Rules;

namespace PrimeERP.Application.Validation
{
    public class SupplierValidator : IValidator<Supplier>
    {
        public ValidationResult Validate(Supplier supplier) =>
            Rules.For<Supplier>()
                .Required(x => x.Code, "كود المورد")
                .Required(x => x.Name, "اسم المورد")
                .MaxLength(x => x.Name, 150, "اسم المورد")
                .Phone(x => x.Phone, "رقم الهاتف")
                .Email(x => x.Email, "البريد الإلكتروني")
                .Positive(x => x.CreditLimit, "الحد الائتماني")
                .Validate(supplier);
    }
}
