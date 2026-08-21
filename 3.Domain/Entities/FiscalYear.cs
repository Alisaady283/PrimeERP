using System;

namespace PrimeERP.Domain.Entities
{
    public class FiscalYear
    {
        public int       Id             { get; set; }
        public string    Name           { get; set; }
        public string    StartDate      { get; set; }
        public string    EndDate        { get; set; }
        public bool      IsClosed       { get; set; }
        public DateTime? ClosedAt       { get; set; }
        public string    ClosedBy       { get; set; }
        public int?      ClosingEntryId { get; set; }
        public bool      IsCurrent      { get; set; }
        public bool      IsDeleted      { get; set; }
        public long      RowVersion     { get; set; }
    }
}
