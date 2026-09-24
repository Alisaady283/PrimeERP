namespace PrimeERP.Application.Reporting
{
    /// <summary>ميزان المراجعة القياسي</summary>
    public class TrialBalanceRow
    {
        public string  Code { get; set; }
        public string  Name { get; set; }
        public decimal OpeningDebit  { get; set; }
        public decimal OpeningCredit { get; set; }
        public decimal PeriodDebit   { get; set; }
        public decimal PeriodCredit  { get; set; }
        public decimal ClosingDebit  { get; set; }
        public decimal ClosingCredit { get; set; }
    }

    /// <summary>المخزون بشكل الفترة كالعملاء والموردين</summary>
    public class StockBalanceRow
    {
        public string  ProductCode   { get; set; }
        public string  ProductName   { get; set; }
        public string  WarehouseName { get; set; }
        public decimal Opening       { get; set; }
        public decimal In            { get; set; }
        public decimal Out           { get; set; }
        public decimal Closing       { get; set; }
    }

    /// <summary>رصيد طرف بشكل الفترة</summary>
    public class PartyBalanceRow
    {
        public string  Code    { get; set; }
        public string  Name    { get; set; }
        public decimal Opening { get; set; }
        public decimal Charged { get; set; }
        public decimal Settled { get; set; }
        public decimal Closing { get; set; }
    }

    public class StatementRow
    {
        public string  Date           { get; set; }
        public string  EntryNo        { get; set; }
        public string  Description    { get; set; }
        public decimal Debit          { get; set; }
        public decimal Credit         { get; set; }
        public decimal RunningBalance { get; set; }
    }

    /// <summary>سطرٌ في حركة الصنف</summary>
    public class ItemCardRow
    {
        public string  Date         { get; set; }
        public string  MovementType { get; set; }
        public string  SourceDoc    { get; set; }

        public decimal InQty        { get; set; }
        public decimal InPrice      { get; set; }
        public decimal InValue      { get; set; }

        public decimal OutQty       { get; set; }
        public decimal OutPrice     { get; set; }
        public decimal OutValue     { get; set; }

        public decimal BalanceQty   { get; set; }
        public decimal BalancePrice { get; set; }
        public decimal BalanceValue { get; set; }
    }

    /// <summary>راتب موظفٍ في مسير</summary>
    public class PayslipRow
    {
        public string   PayrollNo   { get; set; }
        public DateTime PaymentDate { get; set; }
        public string   Period      { get; set; }

        public decimal  BasicSalary { get; set; }
        public decimal  Allowances  { get; set; }
        public decimal  Overtime    { get; set; }
        public decimal  Gross       { get; set; }

        public decimal  Deductions  { get; set; }
        public decimal  Advances    { get; set; }
        public decimal  Insurance   { get; set; }
        public decimal  Tax         { get; set; }
        public decimal  Withheld    { get; set; }

        public decimal  NetSalary   { get; set; }
    }

    public class StockMovementRow
    {
        public string  Date          { get; set; }
        public string  ProductCode   { get; set; }
        public string  ProductName   { get; set; }
        public string  WarehouseName { get; set; }
        public string  MovementType  { get; set; }
        public decimal Qty           { get; set; }
        public decimal BalanceAfter  { get; set; }
    }

    public class SalesReportRow
    {
        public string  InvoiceNo { get; set; }
        public string  Date      { get; set; }
        public string  PartyName { get; set; }
        public decimal NetTotal  { get; set; }
    }

    /// <summary>سطر سجلّ الأصول</summary>
    public class AssetRegisterRow
    {
        public string  Code            { get; set; }
        public string  Name            { get; set; }
        public string  CategoryName    { get; set; }
        public string  PurchaseDate    { get; set; }
        public decimal PurchaseCost    { get; set; }

        public decimal Revalued        { get; set; }
        public decimal SalvageValue    { get; set; }
        public int     UsefulLifeYears { get; set; }
        public decimal MonthlyAmount   { get; set; }
        public decimal Accumulated     { get; set; }
        public decimal BookValue       { get; set; }
        public string  Location        { get; set; }

        public string  Kind            { get; set; }
    }
}
