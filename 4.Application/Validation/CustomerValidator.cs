using PrimeERP.Domain.Contracts;
using PrimeERP.Domain.Entities;

namespace PrimeERP.Application.Validation
{
    public class CustomerValidator : ValidatorBase, IValidator<Customer>
    {
        public ValidationResult Validate(Customer customer)
        {
            var result = new ValidationResult();

            Required(result, "Code", customer.Code, "كود العميل");
            Required(result, "Name", customer.Name, "اسم العميل");
            MaxLength(result, "Name", customer.Name, 150, "اسم العميل");
            Phone(result, "Phone", customer.Phone, "رقم الهاتف");
            Email(result, "Email", customer.Email, "البريد الإلكتروني");
            Positive(result, "CreditLimit", customer.CreditLimit, "الحد الائتماني");

            return result;
        }
    }
}
