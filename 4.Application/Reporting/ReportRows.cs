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

    public class PayrollSummaryRow
    {
        public string  PayrollNo   { get; set; }
        public string  Period      { get; set; }
        public decimal Basic       { get; set; }
        public decimal Allowances  { get; set; }
        public decimal Deductions  { get; set; }
        public decimal Net         { get; set; }
    }

    public class AttendanceSummaryRow
    {
        public string Code            { get; set; }
        public string Name            { get; set; }
        public int    Present         { get; set; }
        public int    Absent          { get; set; }
        public int    Leave           { get; set; }
        public int    LateMinutes     { get; set; }
        public int    OvertimeMinutes { get; set; }
    }

    public class AdvanceRow
    {
        public string  Date     { get; set; }
        public string  Code     { get; set; }
        public string  Name     { get; set; }
        public decimal Amount   { get; set; }
        public string  Status   { get; set; }
    }

    public class CashierRow
    {
        public string  Cashier    { get; set; }
        public int     Shifts     { get; set; }
        public int     Sales      { get; set; }
        public int     Returns    { get; set; }
        public decimal Collected  { get; set; }
        public decimal Refunded   { get; set; }
        public decimal Difference { get; set; }
    }

    public class ProductionRow
    {
        public string  OrderNo    { get; set; }
        public string  Date       { get; set; }
        public string  Product    { get; set; }
        public decimal Qty        { get; set; }
        public decimal Materials  { get; set; }
        public decimal Operations { get; set; }
        public decimal Total      { get; set; }
        public decimal UnitCost   { get; set; }
    }

    public class QuantityCostRow
    {
        public string  Code { get; set; }
        public string  Name { get; set; }
        public decimal Qty  { get; set; }
        public decimal Cost { get; set; }
    }

    public class BatchRow
    {
        public string  Code      { get; set; }
        public string  Name      { get; set; }
        public string  Warehouse { get; set; }
        public string  Batch     { get; set; }
        public string  Expiry    { get; set; }
        public decimal Qty       { get; set; }
        public string  State     { get; set; }
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
        public int     No              { get; set; }
        public string  SupplierName    { get; set; }
        public decimal Additions       { get; set; }
        public decimal Reductions      { get; set; }
        public decimal Rate            { get; set; }
        public decimal AccumulatedStart { get; set; }
        public decimal Charge          { get; set; }
        public decimal AccumulatedEnd  { get; set; }
        public decimal Net             { get; set; }
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

    /// <summary>سطر مستندٍ ومقدار ما سُحب منه</summary>
    public class FlowRow
    {
        public string  DocNo     { get; set; }
        public string  Date      { get; set; }
        public string  Party     { get; set; }
        public string  Code      { get; set; }
        public string  Name      { get; set; }
        public decimal Qty       { get; set; }
        public decimal Done      { get; set; }
        public decimal Closed    { get; set; }
        public decimal Remaining { get; set; }
        public string  Status    { get; set; }
    }

    /// <summary>طرفٌ وأعمار متبقيه</summary>
    public class AgingRow
    {
        public string  Code    { get; set; }
        public string  Name    { get; set; }
        public decimal NotDue  { get; set; }
        public decimal Days30  { get; set; }
        public decimal Days60  { get; set; }
        public decimal Days90  { get; set; }
        public decimal Days120 { get; set; }
        public decimal Days150 { get; set; }
        public decimal Over150 { get; set; }
        public decimal Total   { get; set; }
    }

    /// <summary>فاتورةٌ وسدادها</summary>
    public class InvoiceSettlementRow
    {
        public string  InvoiceNo   { get; set; }
        public string  Date        { get; set; }
        public string  DueDate     { get; set; }
        public string  Party       { get; set; }
        public decimal Net         { get; set; }
        public decimal Paid        { get; set; }
        public decimal Remaining   { get; set; }
        public string  LastPayment { get; set; }
        public int     Payments    { get; set; }
        public string  Status      { get; set; }
    }
}
