using System.Collections.Generic;

namespace PrimeERP.Composition.Definitions
{
    /// <summary>مصدر السحب وأثره المخزني</summary>
    public record PullSource
    {
        public required string SourceKind { get; init; }
        public required string Label { get; init; }
        public string PermissionKey { get; init; }
        public List<string> MatchFields { get; init; } = new();
        public bool AllowPartial { get; init; } = true;
        public bool AllowMultiSource { get; init; } = true;
        public FlowScope FlowScope { get; init; } = FlowScope.Both;

        /// <summary>نطاقٌ يضيّق المرشّحين في الخادم (IPullScope) — مثل مرتجع نقطة البيع</summary>
        public string Scope { get; init; }
    }

    public enum StockEffect
    {
        None,
        In,
        Out
    }
}
