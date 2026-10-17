using PrimeERP.Domain.Entities.Common;
using PrimeERP.Domain.Enums;

namespace PrimeERP.Domain.Entities
{
    /// <summary>خزينة/صندوق أو حساب بنكي</summary>
    public class Treasury : BaseModel
    {
        public string       Code          { get; set; }
        public string       Name          { get; set; }
        public TreasuryKind Kind          { get; set; } = TreasuryKind.Cash;
        public string       AccountCode   { get; set; }
        public string       BankName      { get; set; }
        public string       AccountNumber { get; set; }
        public string       Notes         { get; set; }
        public bool         IsActive      { get; set; } = true;

        public decimal      AccountBalance { get; set; }
        public string       KindName       { get; set; }
    }
}
