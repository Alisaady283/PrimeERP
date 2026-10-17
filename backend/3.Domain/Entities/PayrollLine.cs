namespace PrimeERP.Domain.Entities
{
    /// <summary>استحقاق موظفٍ في مسير</summary>
    public class PayrollLine
    {
        public int     Id           { get; set; }
        public int     PayrollId    { get; set; }
        public int     EmployeeId   { get; set; }
        public string  EmployeeName { get; set; }

        public decimal BasicSalary  { get; set; }
        public decimal Allowances   { get; set; }
        public decimal Overtime     { get; set; }

        public decimal Deductions   { get; set; }

        public decimal Advances     { get; set; }
        public decimal Insurance    { get; set; }
        public decimal Tax          { get; set; }

        public decimal NetSalary    { get; set; }
        public string  Notes        { get; set; }
    }
}
