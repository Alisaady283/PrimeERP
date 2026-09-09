using System;
using System.Collections.Generic;
using PrimeERP.UI.Components.Display;
using PrimeERP.UI.Components.Tree;

namespace PrimeERP.Composition.Definitions
{
    /// <summary>
    /// وصف تعريفي كامل لصفحة قائمة+CRUD واحدة — كل ما يحتاجه CrudPageRenderer ليبني الصفحة من القطع
    /// الجاهزة (PageHeader/FilterBar/AppDataGrid/AppPagination) بلا XAML جديد لكل كيان. الأعمدة نفس
    /// GridColumn المستخدَمة أصلاً في AppDataGrid (6.UI) — لا نوع مواز.
    /// </summary>
    public class ModuleDefinition
    {
        public required string Key { get; init; }
        public required string TitleKey { get; init; }
        public required string PermissionPrefix { get; init; }

        /// <summary>النوع المُسجَّل Transient في DI — يجب أن يرث CrudViewModelBase&lt;TDto,TFilter&gt; لكيان ما
        /// (لا يمكن التعبير عن ذلك كقيد عام هنا لاختلاف TDto/TFilter بين الوحدات؛ CrudPageRenderer يتحقق
        /// وقت التشغيل عبر dynamic — نفس مبدأ ربط WPF بالخصائص بالاسم لا بالنوع الثابت). غير مطلوبة لـ
        /// LayoutKind.Report (ReportRenderer لا يستهلكها).</summary>
        public Type ViewModelType { get; init; }

        /// <summary>
        /// مصنعٌ يبني نموذج العرض بدل حلّه بالنوع. فارغ = بالنوع كما تفعل كل وحدة مكتوبة. تحتاجه الوحدات
        /// المبنيّة: كلها تحمل النوع نفسه، فالنسخة وحدها تعرف جدولها.
        /// </summary>
        public Func<IServiceProvider, object> ViewModelFactory { get; init; }

        /// <summary>
        /// مفاتيح أزرار الكتالوج التي تظهر على هذه الشاشة (new/edit/delete/refresh/print/export…).
        /// فارغ = كلها، وهو حال كل وحدة مكتوبة. المعالج يعرضها كلها محدَّدة ويحفظ ما بقي مؤشَّراً.
        /// </summary>
        public string[] EnabledActions { get; init; }

        /// <summary>أعمدة الشبكة (LayoutKind.Grid) — أو أعمدة لوحة التفاصيل (LayoutKind.TreeSplit)، نفس القائمة
        /// بلا نوع مواز: DetailColumns في TreeRenderer تُبنى من هذه القائمة نفسها، Binding بالاسم يعمل مطابقاً
        /// على الحالتين (خاصية على TDto). غير مطلوبة لـ LayoutKind.Report.</summary>
        public List<GridColumn> Columns { get; init; }

        /// <summary>Grid افتراضياً — يحافظ على سلوك كل وحدة حالية بلا أي تغيير.</summary>
        public LayoutKind LayoutKind { get; init; } = LayoutKind.Grid;

        /// <summary>مطلوبة فقط لو LayoutKind = Tree أو TreeSplit — TreeRenderer يتحقق منها.</summary>
        public TreeLayoutOptions TreeOptions { get; init; }

        public DialogDefinition Dialog { get; init; }

        /// <summary>بديل Dialog لمستند رأس+سطور (قيود يومية، فواتير لاحقاً) — لا يجتمعان لنفس الوحدة.</summary>
        public DocumentDialogDefinition DocumentDialog { get; init; }

        /// <summary>فلاتر إعلانية إضافية بجانب مربع البحث (LayoutKind.Grid فقط) — null/فارغة = بلا تغيير.</summary>
        public List<FilterDefinition> Filters { get; init; }

        /// <summary>مطلوبة فقط لو LayoutKind = Report — ReportRenderer يستهلكها بدل Columns/ViewModelType.</summary>
        public ReportDefinition Report { get; init; }

        /// <summary>مطلوبة فقط لو LayoutKind = TreeCheckList (شاشتا الصلاحيات).</summary>
        public TreeCheckListDefinition TreeCheckList { get; init; }

        /// <summary>إجراءات إضافية على السجل المحدَّد بجانب تعديل/حذف.</summary>
        public List<RowAction> RowActions { get; init; }

        /// <summary>سجلّ واحد لا أكثر — الأرصدة الافتتاحية قيدٌ واحد للمنشأة، فزرّ الإضافة يُعطَّل بعده
        /// ويبقى التعديل والحذف بصلاحياتهما. القيد على الشاشة لا على الخدمة: النمط واحد لكل شاشة.</summary>
        public bool SingleRecord { get; init; }

        /// <summary>في أي وضع تظهر هذه الوحدة — الافتراضي: الوضعان معاً.</summary>
        public FlowScope FlowScope { get; init; } = FlowScope.Both;
    }
}
