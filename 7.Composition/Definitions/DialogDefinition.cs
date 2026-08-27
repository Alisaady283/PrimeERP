using System;
using System.Collections.Generic;

namespace PrimeERP.Composition.Definitions
{
    public enum FieldKind { Text, Number, Check, TextArea, ReadOnly, Picker, Date }

    public class FieldDefinition
    {
        public required string Key { get; init; }
        public required string LabelKey { get; init; }
        public FieldKind Kind { get; init; } = FieldKind.Text;
        public bool IsRequired { get; init; }
        public bool IsReadOnlyOnEdit { get; init; }
        public int MaxLength { get; init; }
        public int ColumnSpan { get; init; } = 1;

        // Kind.Picker فقط — "Account" مدعوم حالياً عبر IAccountService.GetPaged (لا IPickerDataSource<T> عام،
        // بلا تسجيل DI حقيقي حتى الآن — نطاق مُبسَّط عمداً، راجع تقرير R11).
        public string PickerType { get; init; }
        public bool PickerLeafOnly { get; init; }

        // خاصية عنصر القائمة تُستخدَم كقيمة محددة (SelectedValuePath) — "Id" للاختيار برقم داخلي (حساب أب
        // مثلاً)، "Code" لسطر يحتاج كود الحساب نصاً مباشرة (سطر قيد يومية).
        public string PickerValueField { get; init; } = "Id";

        // وضع الإضافة فقط (لا Picker) — تُدفَع للعنصر لو لا editItem؛ تُتجاهَل في التعديل.
        public object DefaultValue { get; init; }

        // Kind.ReadOnly فقط — تنسيق عرض القيمة (مثال "yyyy-MM-dd HH:mm" لتاريخ)، بلا تنسيق = ToString() عادية.
        public string DisplayFormat { get; init; }
    }

    public class DialogDefinition
    {
        public required string TitleKey { get; init; }
        public required string TitleEditKey { get; init; }
        public int GridColumns { get; init; } = 2;
        public required List<FieldDefinition> Fields { get; init; }
        public required Type ServiceType { get; init; }
        public required Type CreateDtoType { get; init; }
        public required Type UpdateDtoType { get; init; }
    }
}
