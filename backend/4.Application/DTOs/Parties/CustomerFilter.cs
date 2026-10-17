namespace PrimeERP.Application.DTOs.Parties
{
    /// <summary>مرشّح العملاء</summary>
    public class CustomerFilter
    {
        public string SearchText     { get; set; }
        public bool?  IsActive       { get; set; }
        public bool?  HasBalance     { get; set; }
        public bool?  OverCreditLimit{ get; set; }
        public int?   CategoryId     { get; set; }
        public string SortBy         { get; set; } = "Code";
        public bool   SortDescending { get; set; }
    }

    public class CreditCheckResult
    {
        public bool    IsAllowed       { get; set; }
        public decimal CurrentBalance  { get; set; }
        public decimal CreditLimit     { get; set; }
        public decimal AvailableCredit { get; set; }
        public decimal ExceededBy      { get; set; }
    }
}
