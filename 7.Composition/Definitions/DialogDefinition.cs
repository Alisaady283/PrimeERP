using System;
using System.Collections.Generic;

namespace PrimeERP.Composition.Definitions
{
    public enum FieldKind { Text, Number, Check, TextArea, ReadOnly, Picker, Date, Password, Image }

    public class FieldDefinition
    {
        public required string Key { get; init; }
        public required string LabelKey { get; init; }
        public FieldKind Kind { get; init; } = FieldKind.Text;
        public bool IsRequired { get; init; }
        public bool IsReadOnlyOnEdit { get; init; }
        public int MaxLength { get; init; }

        // ===== قواعد التحقّق المُعلَنة — يطبّقها FieldValidation وحده لكل الشاشات =====
        public int      MinLength      { get; init; }
        public decimal? Min            { get; init; }
        public decimal? Max            { get; init; }
        public DateTime? MinDate       { get; init; }
        public DateTime? MaxDate       { get; init; }

        /// <summary>تعبير نمطي على النص — فارغ يعني بلا قيد صيغة.</summary>
        public string   Pattern        { get; init; }
        public string   PatternMessage { get; init; }
        public int ColumnSpan { get; init; } = 1;

        // Kind.Picker فقط — "Account" عبر IAccountService.GetPaged، "Category" عبر ICategoryService.GetAll
        // (بلا IPickerDataSource<T> عام، بلا تسجيل DI حقيقي حتى الآن — نطاق مُبسَّط عمداً، راجع تقرير R11).
        public string PickerType { get; init; }
        public bool PickerLeafOnly { get; init; }

        /// <summary>عكس PickerLeafOnly — لا يعرض إلا الحسابات التجميعية (الآباء). لأصول الشجرة المرتبطة.</summary>
        public bool PickerGroupsOnly { get; init; }

        // Kind.Picker مع PickerType="Category" فقط — ModuleKey تُقرَأ منها فئات هذه الوحدة تحديداً.
        public string PickerCategoryModuleKey { get; init; }

        // خاصية عنصر القائمة تُستخدَم كقيمة محددة (SelectedValuePath) — "Id" للاختيار برقم داخلي (حساب أب
        // مثلاً)، "Code" لسطر يحتاج كود الحساب نصاً مباشرة (سطر قيد يومية).
        public string PickerValueField { get; init; } = "Id";

        // وضع الإضافة فقط (لا Picker) — تُدفَع للعنصر لو لا editItem؛ تُتجاهَل في التعديل.
        public object DefaultValue { get; init; }

        // Kind.ReadOnly فقط — تنسيق عرض القيمة (مثال "yyyy-MM-dd HH:mm" لتاريخ)، بلا تنسيق = ToString() عادية.
        public string DisplayFormat { get; init; }

        // يظهر الحقل فقط عندما تساوي قيمة الحقل المذكور VisibleWhenValue — حقول الشيك تحت طريقة الدفع مثلاً.
        public string VisibleWhenField { get; init; }
        public object VisibleWhenValue { get; init; }

        /// <summary>بديل VisibleWhenValue لشرط غير مساواة بسيطة — يأخذ قيمة الحقل الحاكم والحاوية.</summary>
        public Func<object, IServiceProvider, bool> VisibleWhen { get; init; }

        // Kind.Picker فقط — تُعاد تعبئة القائمة كلما تغيّرت قيمة هذا الحقل، وتُمرَّر قيمته كمرشِّح
        // (اختيار "بنك" يحصر قائمة الخزائن في الحسابات البنكية مثلاً).
        public string PickerFilterField { get; init; }
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

        // قيم تُطبَّق على الـ DTO مباشرة بعد ApplyFields، بلا أي عنصر مرئي للمستخدم (مثال: ModuleKey ثابتة
        // لحوار فئة وحدة بعينها عبر CategoryDialogFactory).
        public Dictionary<string, object> FixedValues { get; init; }
    }
}
