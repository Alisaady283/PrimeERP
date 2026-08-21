namespace PrimeERP.Domain.Entities
{
    public class JournalLine
    {
        public int     Id          { get; set; }
        public int     EntryId     { get; set; }
        public int     LineNo      { get; set; }
        public string  AccountCode { get; set; }
        public string  AccountName { get; set; }
        public decimal Debit       { get; set; }
        public decimal Credit      { get; set; }
        public string  Notes       { get; set; }
    }
}
