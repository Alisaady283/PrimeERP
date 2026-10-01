namespace PrimeERP.Application.Services.Ledger
{
    /// <summary>طرفا القيد باتجاهه</summary>
    public static class TwoSided
    {
        public static (T Debit, T Credit) By<T>(bool forward, T first, T second) => forward ? (first, second) : (second, first);
    }
}
