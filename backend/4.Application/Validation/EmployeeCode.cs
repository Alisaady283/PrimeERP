using PrimeERP.Domain.Entities.Common;

namespace PrimeERP.Application.Validation
{
    /// <summary>الموظف بكوده: مطلوبٌ وموجود</summary>
    public static class EmployeeCode
    {
        public static Field<T>[] Rules<T>() where T : IEmployeeLine => new Field<T>[]
        {
            new(x => x.EmployeeCode, "", Required: true, Message: "Str.Employee.Required"),
            new(x => x.EmployeeCode, "", Must: x => string.IsNullOrWhiteSpace(x.EmployeeCode) || x.EmployeeId > 0,
                Message: "Str.Employee.CodeNotFound", Args: x => new object[] { x.EmployeeCode }),
        };
    }
}
