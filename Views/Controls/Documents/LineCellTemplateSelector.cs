using System.Windows;
using System.Windows.Controls;

namespace PrimeERP.Views.Controls.Documents
{
    /// <summary>
    /// يختار قالب الخلية (عرض أو تعديل) حسب LineColumnType من قوالب معرّفة مسبقاً في DocumentLinesGrid.xaml
    /// (مفتاح كل قالب: "CellView_{Type}" أو "CellEdit_{Type}"). كل عمود يملك نسخته الخاصة من المُحدِّد
    /// (ColumnType ثابت لكل عمود)، لكن القوالب نفسها مشتركة وساكنة — لا FrameworkElementFactory ولا XamlReader.
    /// </summary>
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
