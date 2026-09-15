namespace PrimeERP.Domain.Entities
{
    /// <summary>
    /// استحقاق موظفٍ في مسير. الاستحقاقات تُجمع والاستقطاعات تُطرح، والصافي مشتقٌّ منهما لا مُدخَل:
    /// (أساسي + بدلات + إضافي) − (خصومات + سلف + تأمينات + ضرائب).
    /// </summary>
    public class PayrollLine
    {
        public int     Id           { get; set; }
        public int     PayrollId    { get; set; }
        public int     EmployeeId   { get; set; }
        public string  EmployeeName { get; set; }

        // الاستحقاقات
        public decimal BasicSalary  { get; set; }
        public decimal Allowances   { get; set; }
        public decimal Overtime     { get; set; }

        // الاستقطاعات
        public decimal Deductions   { get; set; }

        /// <summary>قسط السلفة المقتطَع هذا الشهر — يُقيَّد دائناً على حساب سلفة الموظف فيُنقص رصيدها.</summary>
        public decimal Advances     { get; set; }
        public decimal Insurance    { get; set; }
        public decimal Tax          { get; set; }

        public decimal NetSalary    { get; set; }
        public string  Notes        { get; set; }

        public decimal GrossPay    => BasicSalary + Allowances + Overtime;
        public decimal TotalWithheld => Deductions + Advances + Insurance + Tax;
    }
}
