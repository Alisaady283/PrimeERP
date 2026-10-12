using System;
using System.Collections.Generic;

namespace PrimeERP.Data.Repositories.Base
{
    /// <summary>سطر مستندٍ في سلسلة السحب</summary>
    public record FlowLine(int DocumentId, string DocNo, DateTime Date, int? PartyId, int LineId, string ProductCode, string ProductName, decimal Qty);

    /// <summary>مستودعٌ يُعطي سطور مستنداته في فترة</summary>
    public interface IFlowLines
    {
        List<FlowLine> FlowLines(DateTime from, DateTime to);
    }
}
