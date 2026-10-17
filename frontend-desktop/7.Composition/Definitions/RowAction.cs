using System;
using PrimeERP.Domain.Results;

namespace PrimeERP.Composition.Definitions
{
    /// <summary>إجراء إضافي على السجل المحدَّد</summary>
    public class RowAction
    {
        public required string Label { get; init; }
        public string Variant { get; init; } = "secondary";
        public string PermissionKey { get; init; }

        public Func<object, bool> AppliesTo { get; init; }

        public bool RequiresSelection { get; init; } = true;

        public required Func<IServiceProvider, object, Result> Execute { get; init; }
    }
}
