namespace PrimeERP.Application.Reporting
{
    /// <summary>ميزان المراجعة القياسي: افتتاحي ثم حركة الفترة ثم ختامي، كلٌّ مدين ودائن.</summary>
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

    /// <summary>المخزون بشكل الفترة كالعملاء والموردين: أول المدة، الوارد، المنصرف، آخر المدة.</summary>
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

    /// <summary>رصيد طرف بشكل الفترة: أول المدة، ما تحمّله، ما سُدِّد، آخر المدة.</summary>
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

    public class ItemCardRow
    {
        public string  Date         { get; set; }
        public string  MovementType { get; set; }
        public decimal Qty          { get; set; }
        public decimal UnitCost     { get; set; }
        public decimal BalanceAfter { get; set; }
        public string  SourceDoc    { get; set; }
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
}
