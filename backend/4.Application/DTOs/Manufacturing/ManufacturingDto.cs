using System;
using System.Collections.Generic;

namespace PrimeERP.Application.DTOs.Manufacturing
{
    public class BomDto
    {
        public int      Id          { get; set; }
        public string   Code        { get; set; }
        public string   Name        { get; set; }
        public string   ProductCode { get; set; }
        public string   ProductName { get; set; }
        public decimal  Qty         { get; set; }
        public bool     IsActive    { get; set; }
        public DateTime CreatedAt   { get; set; }
    }

    public class BomDetailDto : BomDto
    {
        public string Notes { get; set; }
        public List<BomLineDto> Lines { get; set; } = new();
    }

    public class BomLineDto
    {
        public int     LineNo       { get; set; }
        public int     Kind         { get; set; }
        public string  ProductCode  { get; set; }
        public string  ProductName  { get; set; }
        public int?    WorkCenterId { get; set; }
        public decimal Qty          { get; set; }
        public decimal WastePercent { get; set; }
        public string  Notes        { get; set; }
    }

    public class CreateBomDto
    {
        public int     Id          { get; set; }
        public string  Name        { get; set; }
        public string  ProductCode { get; set; }
        public decimal Qty         { get; set; } = 1;
        public bool    IsActive    { get; set; } = true;
        public string  Notes       { get; set; }
        public List<CreateBomLineDto> Lines { get; set; } = new();
    }

    public class CreateBomLineDto
    {
        public int     LineNo       { get; set; }
        public int     Kind         { get; set; } = 1;
        public string  ProductCode  { get; set; }
        public int?    WorkCenterId { get; set; }
        public decimal Qty          { get; set; }
        public decimal WastePercent { get; set; }
        public string  Notes        { get; set; }
    }

    public class BomFilter
    {
        public string SearchText     { get; set; }
        public string SortBy         { get; set; }
        public bool   SortDescending { get; set; }
    }

    public class ProductionOrderDto
    {
        public int      Id             { get; set; }
        public string   OrderNo        { get; set; }
        public DateTime OrderDate      { get; set; }
        public string   ProductName    { get; set; }
        public decimal  Qty            { get; set; }
        public decimal  MaterialsCost  { get; set; }
        public decimal  OperationsCost { get; set; }
        public decimal  TotalCost      { get; set; }
        public DateTime CreatedAt      { get; set; }
    }

    public class ProductionOrderDetailDto : ProductionOrderDto
    {
        public int       BomId               { get; set; }
        public int       RawWarehouseId      { get; set; }
        public int       FinishedWarehouseId { get; set; }
        public string    BatchNo             { get; set; }
        public DateTime? ExpiryDate          { get; set; }
        public string    Notes               { get; set; }
        public List<ProductionOrderLineDto> Lines { get; set; } = new();
    }

    public class ProductionOrderLineDto
    {
        public int       LineNo       { get; set; }
        public int       Kind         { get; set; }
        public string    ProductCode  { get; set; }
        public string    ProductName  { get; set; }
        public int?      WorkCenterId { get; set; }
        public decimal   Qty          { get; set; }
        public decimal   UnitCost     { get; set; }
        public string    BatchNo      { get; set; }
        public DateTime? ExpiryDate   { get; set; }
        public string    Notes        { get; set; }
    }

    public class CreateProductionOrderDto
    {
        public int       Id                  { get; set; }
        public DateTime  OrderDate           { get; set; } = DateTime.Today;
        public int       BomId               { get; set; }
        public decimal   Qty                 { get; set; }
        public int       RawWarehouseId      { get; set; }
        public int       FinishedWarehouseId { get; set; }
        public string    BatchNo             { get; set; }
        public DateTime? ExpiryDate          { get; set; }
        public string    Notes               { get; set; }
        public List<CreateProductionOrderLineDto> Lines { get; set; } = new();
    }

    public class CreateProductionOrderLineDto
    {
        public int       LineNo       { get; set; }
        public int       Kind         { get; set; } = 1;
        public string    ProductCode  { get; set; }
        public int?      WorkCenterId { get; set; }
        public decimal   Qty          { get; set; }
        public string    BatchNo      { get; set; }
        public DateTime? ExpiryDate   { get; set; }
        public string    Notes        { get; set; }
    }

    public class ProductionOrderFilter
    {
        public string SearchText     { get; set; }
        public string SortBy         { get; set; }
        public bool   SortDescending { get; set; }
    }
}
