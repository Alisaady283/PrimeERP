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
    }
}
