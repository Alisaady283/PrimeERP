using System;

namespace PrimeERP.Domain.Entities
{
    /// <summary>كيان Account</summary>
    public class Account
    {
        public int      Id         { get; set; }
        public string   Code       { get; set; }
        public string   Name       { get; set; }
        public string   ParentCode { get; set; }
        public int      Level      { get; set; } = 1;
        public bool     IsLeaf     { get; set; } = true;
        public int      Type       { get; set; }
        public decimal  Balance    { get; set; }
        public string   Notes      { get; set; }
        public bool     IsActive   { get; set; } = true;
        public DateTime CreatedAt  { get; set; }
        public DateTime UpdatedAt  { get; set; }
    }
}
