namespace PrimeERP.Models
{
    public class PayrollLine
    {
        public int     Id           { get; set; }
        public int     PayrollId    { get; set; }
        public int     EmployeeId   { get; set; }
        public string  EmployeeName { get; set; }
        public decimal BasicSalary  { get; set; }
        public decimal Allowances   { get; set; }
        public decimal Deductions   { get; set; }
        public decimal NetSalary    { get; set; }
        public string  Notes        { get; set; }
    }
}
