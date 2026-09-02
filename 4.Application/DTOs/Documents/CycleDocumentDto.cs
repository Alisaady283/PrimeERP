using System;
using System.Collections.Generic;

namespace PrimeERP.Application.DTOs.Documents
{
    public class CycleDocumentDto
    {
        public int      Id        { get; set; }
        public string   DocNo     { get; set; }
        public DateTime DocDate   { get; set; }
        public int?     PartyId   { get; set; }
        public string   PartyName { get; set; }
        public decimal  TotalQty  { get; set; }
        public decimal  Total     { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CycleDocumentDetailDto : CycleDocumentDto
    {
        public List<CycleDocumentLineDto> Lines { get; set; } = new();
    }

    public class CycleDocumentLineDto
    {
        public int     LineNo      { get; set; }
        public string  ProductCode { get; set; }
        public string  ProductName { get; set; }
        public decimal Qty         { get; set; }
        public decimal UnitPrice   { get; set; }
        public string  Notes       { get; set; }
    }

    public class CreateCycleDocumentLineDto
    {
        public int     LineNo      { get; set; }
        public string  ProductCode { get; set; }
        public decimal Qty         { get; set; }
        public decimal UnitPrice   { get; set; }
        public string  Notes       { get; set; }
    }

    public class CreateCycleDocumentDto
    {
        public int      Id      { get; set; }
        public DateTime DocDate { get; set; } = DateTime.Today;
        public int?     PartyId { get; set; }
        public string   Notes   { get; set; }
        public List<CreateCycleDocumentLineDto> Lines { get; set; } = new();
    }

    public class CycleDocumentFilter
    {
        public string SearchText     { get; set; }
        public string SortBy         { get; set; } = "DocDate";
        public bool   SortDescending { get; set; } = true;
    }
}
