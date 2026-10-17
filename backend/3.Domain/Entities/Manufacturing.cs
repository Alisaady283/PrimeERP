using System;
using System.Collections.Generic;
using PrimeERP.Domain.Entities.Common;
using PrimeERP.Domain.Enums;

namespace PrimeERP.Domain.Entities
{
    public class WorkCenter : BaseModel
    {
        public string  Code        { get; set; }
        public string  Name        { get; set; }
        public decimal CostPerHour { get; set; }
        public bool    IsActive    { get; set; } = true;
    }

    public class BillOfMaterials : BaseModel
    {
        public string  Code        { get; set; }
        public string  Name        { get; set; }
        public int     ProductId   { get; set; }
        public string  ProductCode { get; set; }
        public string  ProductName { get; set; }
        public decimal Qty         { get; set; } = 1;
        public bool    IsActive    { get; set; } = true;
        public string  Notes       { get; set; }

        public List<BomLine> Lines { get; set; } = new();
    }

    public class BomLine : IProductLine
    {
        public int         Id           { get; set; }
        public int         BomId        { get; set; }
        public int         LineNo       { get; set; }
        public BomLineKind Kind         { get; set; } = BomLineKind.Component;
        public int         ProductId    { get; set; }
        public string      ProductCode  { get; set; }
        public string      ProductName  { get; set; }
        public int?        WorkCenterId { get; set; }
        public decimal     Qty          { get; set; }
        public decimal     WastePercent { get; set; }
        public string      BatchNo      { get; set; }
        public DateTime?   ExpiryDate   { get; set; }
        public string      Notes        { get; set; }
    }

    public class ProductionOrder : BaseModel
    {
        public string    OrderNo             { get; set; }
        public DateTime  OrderDate           { get; set; }
        public bool      Unbuild             { get; set; }
        public int       BomId               { get; set; }
        public int       ProductId           { get; set; }
        public string    ProductCode         { get; set; }
        public string    ProductName         { get; set; }
        public decimal   Qty                 { get; set; }
        public int       RawWarehouseId      { get; set; }
        public int       FinishedWarehouseId { get; set; }
        public string    BatchNo             { get; set; }
        public DateTime? ExpiryDate          { get; set; }
        public decimal   MaterialsCost       { get; set; }
        public decimal   OperationsCost      { get; set; }
        public decimal   TotalCost           { get; set; }
        public int?      JournalEntryId      { get; set; }
        public string    Notes               { get; set; }

        public List<ProductionOrderLine> Lines { get; set; } = new();
    }

    public class ProductionOrderLine : IProductLine
    {
        public int         Id           { get; set; }
        public int         OrderId      { get; set; }
        public int         LineNo       { get; set; }
        public BomLineKind Kind         { get; set; } = BomLineKind.Component;
        public int         ProductId    { get; set; }
        public string      ProductCode  { get; set; }
        public string      ProductName  { get; set; }
        public int?        WorkCenterId { get; set; }
        public decimal     Qty          { get; set; }
        public decimal     UnitCost     { get; set; }
        public string      BatchNo      { get; set; }
        public DateTime?   ExpiryDate   { get; set; }
        public string      Notes        { get; set; }
    }
}
