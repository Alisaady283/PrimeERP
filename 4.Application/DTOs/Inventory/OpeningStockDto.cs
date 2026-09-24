using System;
using System.Collections.Generic;

namespace PrimeERP.Application.DTOs.Inventory
{
    /// <summary>أرصدة الأصناف الافتتاحية</summary>
    public class CreateOpeningStockDto
    {
        public int      Id          { get; set; }
        public DateTime Date        { get; set; } = DateTime.Today;
        public int      WarehouseId { get; set; }
        public string   Notes       { get; set; }

        public List<CreateOpeningStockLineDto> Lines { get; set; } = new();
    }

    public class CreateOpeningStockLineDto
    {
        public int     LineNo    { get; set; }
        public int     ProductId { get; set; }
        public decimal Qty       { get; set; }
        public decimal UnitCost  { get; set; }
        public decimal Value     { get; set; }
        public string  Notes     { get; set; }
    }
}
