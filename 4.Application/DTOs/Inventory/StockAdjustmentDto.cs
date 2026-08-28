using System;
using System.Collections.Generic;

namespace PrimeERP.Application.DTOs.Inventory
{
    // نفس الشكل لِـStockIn وStockOut — الفرق في الاتجاه فقط (Service).
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

    public class StockAdjustmentDetailDto : StockAdjustmentDto
    {
        public List<StockAdjustmentLineDto> Lines { get; set; } = new();
    }

    public class StockAdjustmentLineDto
    {
        public int     LineNo      { get; set; }
        public string  ProductCode { get; set; }
        public string  ProductName { get; set; }
        public decimal Qty         { get; set; }
        public decimal UnitCost    { get; set; }
        public string  Notes       { get; set; }
    }

    public class CreateStockAdjustmentLineDto
    {
        public int     LineNo      { get; set; }
        public string  ProductCode { get; set; }
        public decimal Qty         { get; set; }
        public decimal UnitCost    { get; set; }
        public string  Notes       { get; set; }
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
