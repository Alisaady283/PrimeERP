using System;
using PrimeERP.Platform.Permissions;
using PrimeERP.Domain.Enums;
using PrimeERP.Domain.Entities.Common;

namespace PrimeERP.Domain.Entities
{
    public class Employee : BaseModel
    {
        public string    Code            { get; set; }
        public string    Name            { get; set; }
        public string    NameEn          { get; set; }
        public string    NationalId      { get; set; }
        public string    Phone           { get; set; }
        public string    Email           { get; set; }
        public string    Address         { get; set; }
        public int?       DepartmentId    { get; set; }
        public int?       JobTitleId      { get; set; }
        public DateTime   HireDate        { get; set; }
        public DateTime?  TerminationDate { get; set; }
        public decimal    BasicSalary     { get; set; }
        public int?       AccountId       { get; set; }
        public string     BankAccount     { get; set; }
        public EmployeeStatus Status      { get; set; } = EmployeeStatus.Active;
        public string      Notes           { get; set; }

        /// <summary>اسم القسم/الوظيفة الظاهر — ليس عموداً في Employees (مرتبط بـ DepartmentId/JobTitleId)، يملؤه مصدر البيانات وقت الجلب للعرض فقط (نفس نمط Product.CurrentStock/UnitName).</summary>
        public string DepartmentName { get; set; }
        public string JobTitleName   { get; set; }
    }
}
