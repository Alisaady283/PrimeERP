namespace PrimeERP.Application.Services.Ledger
{
    /// <summary>طرفا القيد باتجاهه</summary>
    public static class TwoSided
    {
        /// <summary>قبضٌ أو صرف</summary>
        public static (string Debit, string Credit) Cash(bool incoming, string cash, string party) =>
            incoming ? (cash, party) : (party, cash);

        /// <summary>زيادةٌ أو نقص</summary>
        public static (string Debit, string Credit) BySign(bool increase, string account, string counter) =>
            increase ? (account, counter) : (counter, account);
    }
}
