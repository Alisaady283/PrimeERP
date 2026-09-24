using System;
using System.Collections.Generic;

namespace PrimeERP.Application.DTOs.Inventory
{
    /// <summary>بيانات التحويل المخزني</summary>
    public class StockTransferDto
    {
        public int      Id                { get; set; }
        public string   DocNo             { get; set; }
        public DateTime MovementDate      { get; set; }
        public int      FromWarehouseId   { get; set; }
        public string   FromWarehouseName { get; set; }
        public int      ToWarehouseId     { get; set; }
        public string   ToWarehouseName   { get; set; }
        public DateTime CreatedAt         { get; set; }
    }

    public class StockTransferDetailDto : StockTransferDto
    {
        public List<StockTransferLineDto> Lines { get; set; } = new();
    }

    public class StockTransferLineDto
    {
        public int     LineNo      { get; set; }
        public string  ProductCode { get; set; }
        public string  ProductName { get; set; }
        public decimal Qty         { get; set; }
        public string  Notes       { get; set; }
    }

    public class CreateStockTransferLineDto
    {
        public int     LineNo      { get; set; }
        public string  ProductCode { get; set; }
        public decimal Qty         { get; set; }
        public string  Notes       { get; set; }
    }

    public class CreateStockTransferDto
    {
        public int      Id              { get; set; }
        public DateTime MovementDate    { get; set; } = DateTime.Today;
        public int      FromWarehouseId { get; set; }
        public int      ToWarehouseId   { get; set; }
        public string   Notes           { get; set; }
        public List<CreateStockTransferLineDto> Lines { get; set; } = new();
    }

    public class StockTransferFilter
    {
        public string SearchText     { get; set; }
        public string SortBy         { get; set; } = "MovementDate";
        public bool   SortDescending { get; set; } = true;
    }
}
