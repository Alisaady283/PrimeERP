namespace PrimeERP.Composition.Definitions
{
    /// <summary>نطاق الدورة</summary>
    public enum FlowScope
    {
        Both,
        FullCycleOnly,
        SimplifiedOnly
    }

    public static class FlowScopes
    {
        public static bool Allows(this FlowScope scope, bool simplified) =>
            scope == FlowScope.Both || scope == (simplified ? FlowScope.SimplifiedOnly : FlowScope.FullCycleOnly);
    }
}
