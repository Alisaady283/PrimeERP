using System;
using System.Collections.Generic;

namespace PrimeERP.Application.DTOs.Documents
{
    /// <summary>مستندٌ يُسحَب منه</summary>
    public interface ISourceDocument
    {
        string   DocNo     { get; }
        DateTime DocDate   { get; }
        string   PartyName { get; }
        IEnumerable<ISourceLine> Lines { get; }
    }

    /// <summary>سطرٌ يُسحَب منه</summary>
    public interface ISourceLine
    {
        int     Id          { get; }
        string  ProductCode { get; }
        string  ProductName { get; }
        decimal Qty         { get; }
        decimal UnitPrice   { get; }
    }
}
