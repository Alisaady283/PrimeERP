namespace PrimeERP.UI.Components.Tree
{
    /// <summary>خيارات تخطيط الشجرة</summary>
    public enum LayoutKind
    {
        Grid,

        Tree,

        TreeSplit,

        Report,

        Settings,

        TreeCheckList,

        DocumentPage,

        ChequeBoard
    }

    public enum SelectableRule { All, LeafOnly }

    public class TreeLayoutOptions
    {
        public required string IdField       { get; init; }
        public required string ParentIdField { get; init; }
        public string CodeField { get; init; }
        public string NameField { get; init; }

        public string DisplayTemplate { get; init; } = "{Name}";

        public string ExtraInfoTemplate { get; init; }

        public bool ExpandRootsByDefault { get; init; } = true;
        public int  ExpandToLevel        { get; init; }

        public string LeafFlagField { get; init; }

        public SelectableRule SelectableRule { get; init; } = SelectableRule.All;
    }
}
