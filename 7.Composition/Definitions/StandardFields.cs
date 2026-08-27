using System.Collections.Generic;
using PrimeERP.Platform.Localization;
using PrimeERP.UI.Components.Display;

namespace PrimeERP.Composition.Definitions
{
    // قطع قابلة للاستدعاء من أي تسجيل وحدة — حالة نشط/غير نشط + تاريخ إنشاء/تعديل. تفترض أن الـ DTO المقروء
    // (لا Create/Update) يملك IsActive/StatusText/CreatedAt/UpdatedAt بهذه الأسماء بالضبط؛ Create/Update
    // بلا CreatedAt/UpdatedAt عمداً — ApplyFields تتجاهل أي حقل بلا خاصية مطابقة على الـ DTO المستهدف، فحقلا
    // التاريخ يُعرَضان فقط في الفورم ولا يُرسَلان أبداً.
    public static class StandardFields
    {
        public static List<FieldDefinition> DialogFields() => new()
        {
            new() { Key = "IsActive", LabelKey = "Str.Active", Kind = FieldKind.Check, DefaultValue = true },
            new() { Key = "CreatedAt", LabelKey = "Str.CreatedAt", Kind = FieldKind.ReadOnly, DisplayFormat = "yyyy-MM-dd HH:mm" },
            new() { Key = "UpdatedAt", LabelKey = "Str.UpdatedAt", Kind = FieldKind.ReadOnly, DisplayFormat = "yyyy-MM-dd HH:mm" },
        };

        public static List<GridColumn> AuditColumns() => new()
        {
            new() { Header = LocalizationService.Get("Str.Status"), Binding = "StatusText" },
            new() { Header = LocalizationService.Get("Str.CreatedAt"), Binding = "CreatedAt", Format = "yyyy-MM-dd HH:mm" },
            new() { Header = LocalizationService.Get("Str.UpdatedAt"), Binding = "UpdatedAt", Format = "yyyy-MM-dd HH:mm" },
        };
    }
}
