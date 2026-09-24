using System;
using System.Collections.Generic;
using PrimeERP.Domain.Entities.Common;
using PrimeERP.Domain.Enums;

namespace PrimeERP.Domain.Entities
{
    /// <summary>سند قبض/صرف</summary>
    public class Voucher : BaseModel
    {
        public string        VoucherNo   { get; set; }
        public DateTime      VoucherDate { get; set; }
        public VoucherKind   Kind        { get; set; }
        public PartyKind     PartyKind   { get; set; }
        public int?          PartyId     { get; set; }
        public int           TreasuryId  { get; set; }
        public decimal       Amount      { get; set; }
        public PaymentMethod Method      { get; set; }
        public string        Reference   { get; set; }
        public string        Notes       { get; set; }
        public int?          JournalEntryId { get; set; }
        public int?          ChequeId       { get; set; }

        public List<VoucherAllocation> Allocations { get; set; } = new();
    }

    public class VoucherAllocation
    {
        public int     Id          { get; set; }
        public int     VoucherId   { get; set; }
        public int     LineNo      { get; set; }
        public string  InvoiceType { get; set; }
        public string  InvoiceNo   { get; set; }
        public decimal Amount      { get; set; }
        public string  Notes       { get; set; }
    }
}
