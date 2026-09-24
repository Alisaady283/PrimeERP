using PrimeERP.Domain.Enums;
using PrimeERP.Domain.Entities.Common;

namespace PrimeERP.Domain.Entities
{
    /// <summary>كيان Product</summary>
    public class Product : BaseModel
    {
        public string      Code               { get; set; }
        public string      Barcode            { get; set; }
        public string      Name               { get; set; }
        public string      NameEn             { get; set; }
        public int?        CategoryId         { get; set; }
        public int?        BrandId            { get; set; }
        public string      CategoryName       { get; set; }
        public string      BrandName          { get; set; }
        public int?        UnitId             { get; set; }
        public decimal     CostPrice          { get; set; }
        public decimal     SalePrice          { get; set; }
        public decimal     MinPrice           { get; set; }
        public decimal     MinQty             { get; set; }
        public decimal     MaxQty             { get; set; }
        public decimal     ReorderPoint       { get; set; }
        public CostMethod  CostMethod         { get; set; } = CostMethod.WeightedAverage;
        public int?        TaxGroupId         { get; set; }
        public int?        InventoryAccountId { get; set; }
        public int?        SalesAccountId     { get; set; }
        public int?        CostAccountId      { get; set; }
        public bool        IsStockTracked     { get; set; } = true;
        public bool        IsService          { get; set; } = false;
        public string      Notes              { get; set; }
        public bool        IsActive           { get; set; } = true;

        public decimal CurrentStock { get; set; }

        public string UnitName { get; set; }

        public decimal TaxRate { get; set; }
    }
}
