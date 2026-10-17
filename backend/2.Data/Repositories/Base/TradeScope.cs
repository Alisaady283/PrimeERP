using System.Collections.Generic;

namespace PrimeERP.Data.Repositories.Base
{
    /// <summary>
    /// نطاق تحليل البيع والشراء: طرفٌ بعينه، أو أصنافٌ بعينها (صنفٌ أو أصناف تصنيف).
    /// بالأصناف تُجمع قيمة سطورها وحدها لا الفاتورة كلها — كتصفية BI.
    /// </summary>
    public sealed record TradeScope(int? PartyId = null, IReadOnlyCollection<int> ProductIds = null)
    {
        public bool ByProducts => ProductIds != null;
    }
}
