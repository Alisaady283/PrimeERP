using PrimeERP.Core;
using PrimeERP.Models.Common;

namespace PrimeERP.Models
{
    public class Product : BaseModel
    {
        public string      Code               { get; set; }
        public string      Barcode            { get; set; }
        public string      Name               { get; set; }
        public string      NameEn             { get; set; }
        public int?        CategoryId         { get; set; }
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

        /// <summary>
        /// رصيد وقت الاستعلام في مخزن محدد — ليس عموداً في جدول Products (يُحسب من StockMovements)،
        /// بل حقل يملؤه مصدر البيانات وقت الجلب (مثل IPickerDataSource&lt;Product&gt;) عند الحاجة لعرضه.
        /// </summary>
        public decimal CurrentStock { get; set; }

        /// <summary>مثل CurrentStock — يملؤه مصدر البيانات وقت الجلب (اسم الوحدة الافتراضية للصنف)، ليس عموداً في Products.</summary>
        public string UnitName { get; set; }

        /// <summary>مثل UnitName — نسبة الضريبة الافتراضية للصنف (تُشتق فعلياً من TaxGroupId، هنا حقل مسطّح للعرض/التعبئة التلقائية).</summary>
        public decimal TaxRate { get; set; }
    }
}
