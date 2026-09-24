using System;
using System.Collections.Generic;

namespace PrimeERP.Domain.Entities
{
    /// <summary>كيان JournalEntry</summary>
    public class JournalEntry
    {
        public int      Id          { get; set; }
        public string   EntryNo     { get; set; }
        public string   EntryDate   { get; set; }
        public string   Description { get; set; }
        public decimal  TotalDebit  { get; set; }
        public decimal  TotalCredit { get; set; }
        public string   Source      { get; set; } = "يدوي";
        public bool     IsPosted    { get; set; }
        public DateTime? PostedAt   { get; set; }
        public string   PostedBy    { get; set; }
        public string   CreatedBy   { get; set; }
        public DateTime CreatedAt   { get; set; }
        public DateTime UpdatedAt   { get; set; }

        public List<JournalLine> Lines { get; set; } = new();
    }
}
