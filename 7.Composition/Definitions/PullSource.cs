using System.Collections.Generic;

namespace PrimeERP.Composition.Definitions
{
    public class PullSource
    {
        public required string SourceKind { get; init; }
        public required string Label { get; init; }
        public string PermissionKey { get; init; }
        public List<string> MatchFields { get; init; } = new();
        public Dictionary<string, string> LineMapping { get; init; } = new();
        public bool AllowPartial { get; init; } = true;
        public bool AllowMultiSource { get; init; } = true;
    }

    public enum StockEffect
    {
        None,
        In,
        Out
    }
}
