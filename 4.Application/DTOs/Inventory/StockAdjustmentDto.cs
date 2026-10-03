using System;
using System.Collections.Generic;
using PrimeERP.Application.DTOs.Documents;

namespace PrimeERP.Application.DTOs.Inventory
{
    /// <summary>بيانات إذن المخزون</summary>
    public class StockAdjustmentDto
    {
        public int      Id            { get; set; }
        public string   DocNo         { get; set; }
        public DateTime MovementDate  { get; set; }
        public int      WarehouseId   { get; set; }
        public string   WarehouseName { get; set; }
        public decimal  TotalQty      { get; set; }
        public DateTime CreatedAt     { get; set; }
    }

    public class StockAdjustmentDetailDto : StockAdjustmentDto, ISourceDocument
    {
        public List<StockAdjustmentLineDto> Lines { get; set; } = new();

        DateTime ISourceDocument.DocDate   => MovementDate;
        string   ISourceDocument.PartyName => WarehouseName;
        IEnumerable<ISourceLine> ISourceDocument.Lines => Lines;
    }

    public class StockAdjustmentLineDto : ISourceLine, IPullableLine
    {
        public int     Id          { get; set; }
        public int     LineNo      { get; set; }
        public string  ProductCode { get; set; }
        public string  ProductName { get; set; }
        public decimal Qty         { get; set; }
        public decimal UnitCost    { get; set; }
        public string  Notes       { get; set; }

        public string  SourceType   { get; set; }
        public int     SourceId     { get; set; }
        public string  SourceNo     { get; set; }
        public int     SourceLineId { get; set; }

        decimal ISourceLine.UnitPrice => UnitCost;
    }

    public class CreateStockAdjustmentLineDto : PrimeERP.Application.DTOs.Documents.IPullableLine
    {
        public int     LineNo      { get; set; }
        public string  ProductCode { get; set; }
        public decimal Qty         { get; set; }
        public decimal UnitCost    { get; set; }
        public string  Notes       { get; set; }

        public string  SourceType   { get; set; }
        public int     SourceId     { get; set; }
        public string  SourceNo     { get; set; }
        public int     SourceLineId { get; set; }
    }

    public class CreateStockAdjustmentDto
    {
        public int      Id            { get; set; }
        public DateTime MovementDate  { get; set; } = DateTime.Today;
        public int      WarehouseId   { get; set; }
        public string   Notes         { get; set; }
        public List<CreateStockAdjustmentLineDto> Lines { get; set; } = new();
    }

    public class StockAdjustmentFilter
    {
        public string SearchText     { get; set; }
        public string SortBy         { get; set; } = "MovementDate";
        public bool   SortDescending { get; set; } = true;
    }
}
