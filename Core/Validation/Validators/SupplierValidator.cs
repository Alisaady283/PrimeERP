using PrimeERP.Models;

namespace PrimeERP.Core.Validation.Validators
{
    public class SupplierValidator : ValidatorBase, IValidator<Supplier>
    {
        public ValidationResult Validate(Supplier supplier)
        {
            var result = new ValidationResult();

            Required(result, "Code", supplier.Code, "كود المورد");
            Required(result, "Name", supplier.Name, "اسم المورد");
            MaxLength(result, "Name", supplier.Name, 150, "اسم المورد");
            Phone(result, "Phone", supplier.Phone, "رقم الهاتف");
            Email(result, "Email", supplier.Email, "البريد الإلكتروني");
            Positive(result, "CreditLimit", supplier.CreditLimit, "الحد الائتماني");

            return result;
        }
    }
}
