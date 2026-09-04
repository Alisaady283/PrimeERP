using System;
using System.Collections.Generic;
using PrimeERP.Domain.Enums;

namespace PrimeERP.Application.DTOs.Vouchers
{
    public class VoucherDto
    {
        public int       Id          { get; set; }
        public string    VoucherNo   { get; set; }
        public DateTime  VoucherDate { get; set; }
        public string    PartyName   { get; set; }
        public string    TreasuryName{ get; set; }
        public decimal   Amount      { get; set; }
        public string    MethodName  { get; set; }
        public string    Reference   { get; set; }
        public string    Notes       { get; set; }
        public DateTime  CreatedAt   { get; set; }
    }

    public class VoucherDetailDto : VoucherDto
    {
        public int?          PartyId    { get; set; }
        public int           TreasuryId { get; set; }
        public PaymentMethod Method     { get; set; }
        public List<VoucherAllocationDto> Allocations { get; set; } = new();
    }

    public class VoucherAllocationDto
    {
        public int     Id          { get; set; }
        public int     LineNo      { get; set; }
        public string  InvoiceNo   { get; set; }
        public decimal Amount      { get; set; }
        public string  Notes       { get; set; }
    }

    public class CreateVoucherAllocationDto
    {
        public int     LineNo    { get; set; }
        public string  InvoiceNo { get; set; }
        public decimal Amount    { get; set; }
        public string  Notes     { get; set; }
    }

    /// <summary>سند قبض أو صرف — الاتجاه من الخدمة المستدعاة لا من الحقول، فالنموذج واحد للشاشتين.</summary>
    public class CreateVoucherDto
    {
        public int       Id          { get; set; }
        public DateTime  VoucherDate { get; set; } = DateTime.Today;
        public int?      PartyId     { get; set; }

        public int       TreasuryId  { get; set; }
        public decimal   Amount      { get; set; }
        public int       Method      { get; set; } = (int)PaymentMethod.Cash;
        public string    Reference   { get; set; }
        public string    Notes       { get; set; }

        // تُقرأ فقط عندما Method = شيك (3) — عندها يُنشأ الشيك تلقائياً بلا شاشة إدخال ثانية.
        public string    ChequeNo    { get; set; }
        public DateTime? ChequeDueDate { get; set; }
        public string    ChequeBank  { get; set; }

        public List<CreateVoucherAllocationDto> Allocations { get; set; } = new();
    }

    public class VoucherFilter
    {
        public string SearchText     { get; set; }
        public bool   SortDescending { get; set; } = true;
    }
}
