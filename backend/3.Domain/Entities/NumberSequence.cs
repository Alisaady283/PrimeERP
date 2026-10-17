namespace PrimeERP.Domain.Entities
{
    /// <summary>عدّاد ترقيم لكل مفتاح</summary>
    public class NumberSequence
    {
        public int    Id          { get; set; }
        public string Key         { get; set; }
        public string Prefix      { get; set; }
        public int    NextNumber  { get; set; } = 1;
        public int    Padding     { get; set; } = 5;
        public bool   ResetYearly { get; set; } = true;
        public int?   LastYear    { get; set; }
    }
}
