using System;
using PrimeERP.Domain.Results;

namespace PrimeERP.Application.DTOs.Inventory
{
    // نطاق مُبسَّط عمداً — الحقول المرتبطة بوحدات لم تُبنَ بعد (وحدة قياس/مجموعة ضريبة/حسابات محاسبية) غير
    // مكشوفة هنا رغم وجودها في المخطط (راجع ProductRepository)؛ تُضاف حين تُبنى تلك الوحدات.
    public class ProductDto
    {
        public int      Id           { get; set; }
        public string   Code         { get; set; }
        public string   Barcode      { get; set; }
        public string   Name         { get; set; }
        public string   NameEn       { get; set; }
        public int?     CategoryId   { get; set; }
        public string   CategoryName { get; set; }
        public decimal  CostPrice    { get; set; }
        public decimal  SalePrice    { get; set; }
        public decimal  MinPrice     { get; set; }
        public string   Notes        { get; set; }
        public bool     IsActive     { get; set; }
        public string   StatusText   { get; set; }
        public DateTime CreatedAt    { get; set; }
        public DateTime UpdatedAt    { get; set; }
        public StatusVariant StatusVariant { get; set; }

        public bool CanEdit   { get; set; }
        public bool CanDelete { get; set; }
    }

    public class CreateProductDto
    {
        public string  Barcode    { get; set; }
        public string  Name       { get; set; }
        public string  NameEn     { get; set; }
        public int?    CategoryId { get; set; }
        public decimal CostPrice  { get; set; }
        public decimal SalePrice  { get; set; }
        public decimal MinPrice   { get; set; }
        public string  Notes      { get; set; }
        public bool    IsActive   { get; set; } = true;
    }

    public class UpdateProductDto
    {
        public int     Id         { get; set; }
        public string  Name       { get; set; }
        public string  NameEn     { get; set; }
        public int?    CategoryId { get; set; }
        public decimal CostPrice  { get; set; }
        public decimal SalePrice  { get; set; }
        public decimal MinPrice   { get; set; }
        public string  Notes      { get; set; }
        public bool    IsActive   { get; set; }
    }

    public class ProductFilter
    {
        public string SearchText     { get; set; }
        public bool?  IsActive       { get; set; }
        public int?   CategoryId     { get; set; }
        public string SortBy         { get; set; } = "Name";
        public bool   SortDescending { get; set; }
    }
}
