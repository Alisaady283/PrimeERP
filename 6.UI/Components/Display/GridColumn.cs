using System.Windows;

namespace PrimeERP.UI.Components.Display
{
    /// <summary>محاذاة منطقية لا فيزيائية</summary>
    public enum ColumnAlign { Auto, Start, Center, End }

    public enum FooterAggregate { None, Sum, Count, Average }

    public enum GridSelectionMode { Single, Multiple }

    /// <summary>تعريف عمود AppDataGrid</summary>
    public record GridColumn
    {
        public string Header { get; set; }

        public string Group { get; set; }
        public string Binding { get; set; }
        public double Width { get; set; } = 120;
        public bool IsStarWidth { get; set; }
        public ColumnAlign Align { get; set; } = ColumnAlign.Auto;
        public string Format { get; set; }
        public string PermissionKey { get; set; }
        public bool IsVisible { get; set; } = true;
        public bool IsSortable { get; set; } = true;
        public FooterAggregate Footer { get; set; } = FooterAggregate.None;
        public DataTemplate CellTemplate { get; set; }
    }
}
