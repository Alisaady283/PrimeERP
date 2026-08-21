using PrimeERP.Domain.Contracts;
using PrimeERP.Domain.Entities;

namespace PrimeERP.Application.Validation
{
    public class EmployeeValidator : ValidatorBase, IValidator<Employee>
    {
        public ValidationResult Validate(Employee employee)
        {
            var result = new ValidationResult();

            Required(result, "Code", employee.Code, "كود الموظف");
            Required(result, "Name", employee.Name, "اسم الموظف");
            MaxLength(result, "Name", employee.Name, 150, "اسم الموظف");
            Phone(result, "Phone", employee.Phone, "رقم الهاتف");
            Email(result, "Email", employee.Email, "البريد الإلكتروني");
            Positive(result, "BasicSalary", employee.BasicSalary, "الراتب الأساسي");
            DateValid(result, "HireDate", employee.HireDate.ToString("yyyy-MM-dd"), "تاريخ التعيين");

            return result;
        }
    }
}
