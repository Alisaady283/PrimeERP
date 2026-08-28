using System;
using System.Collections.Generic;
using PrimeERP.Domain.Entities.Common;

namespace PrimeERP.Domain.Entities
{
    public class StockTransferDocument : BaseModel
    {
        public string   DocNo           { get; set; }
        public DateTime MovementDate    { get; set; }
        public int      FromWarehouseId { get; set; }
        public int      ToWarehouseId   { get; set; }
        public string   Notes           { get; set; }

        public List<StockTransferLine> Lines { get; set; } = new();
    }

    public class StockTransferLine
    {
        public int     Id          { get; set; }
        public int     DocumentId  { get; set; }
        public int     LineNo      { get; set; }
        public int     ProductId   { get; set; }
        public string  ProductCode { get; set; }
        public string  ProductName { get; set; }
        public decimal Qty         { get; set; }
        public string  Notes       { get; set; }
    }
}
