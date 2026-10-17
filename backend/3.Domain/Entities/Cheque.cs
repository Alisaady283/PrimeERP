using System;
using PrimeERP.Domain.Entities.Common;
using PrimeERP.Domain.Enums;

namespace PrimeERP.Domain.Entities
{
    /// <summary>شيك وارد أو صادر</summary>
    public class Cheque : BaseModel
    {
        public string          ChequeNo   { get; set; }
        public ChequeDirection Direction  { get; set; }
        public PartyKind       PartyKind  { get; set; }
        public int?            PartyId    { get; set; }
        public decimal         Amount     { get; set; }
        public DateTime        IssueDate  { get; set; }
        public DateTime        DueDate    { get; set; }
        public string          BankName   { get; set; }
        public ChequeStatus    Status     { get; set; }
        public int?            TreasuryId { get; set; }
        public int?            VoucherId  { get; set; }
        public string          Notes      { get; set; }
    }

    public class ChequeMovement
    {
        public int          Id             { get; set; }
        public int          ChequeId       { get; set; }
        public DateTime     MovementDate   { get; set; }
        public ChequeStatus FromStatus     { get; set; }
        public ChequeStatus ToStatus       { get; set; }
        public int?         TreasuryId     { get; set; }
        public int?         JournalEntryId { get; set; }
        public string       Notes          { get; set; }
        public DateTime     CreatedAt      { get; set; }
        public string       CreatedBy      { get; set; }
    }
}
