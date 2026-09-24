using System.Windows;
using System.Windows.Controls;

namespace PrimeERP.UI.Components.Documents
{
    /// <summary>يختار قالب الخلية</summary>
    public class LineCellTemplateSelector : DataTemplateSelector
    {
        public LineColumnType ColumnType { get; set; }
        public bool IsEditing { get; set; }
        public FrameworkElement Owner { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            var prefix = IsEditing ? "CellEdit_" : "CellView_";
            var key = prefix + ColumnType;

            if (Owner?.TryFindResource(key) is DataTemplate exact)
                return exact;

            return Owner?.TryFindResource(prefix + "Text") as DataTemplate;
        }
    }
}
