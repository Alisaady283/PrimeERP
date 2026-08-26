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
        /// وقت التشغيل عبر dynamic — نفس مبدأ ربط WPF بالخصائص بالاسم لا بالنوع الثابت).</summary>
        public required Type ViewModelType { get; init; }

        /// <summary>أعمدة الشبكة (LayoutKind.Grid) — أو أعمدة لوحة التفاصيل (LayoutKind.TreeSplit)، نفس القائمة
        /// بلا نوع مواز: DetailColumns في TreeRenderer تُبنى من هذه القائمة نفسها، Binding بالاسم يعمل مطابقاً
        /// على الحالتين (خاصية على TDto).</summary>
        public required List<GridColumn> Columns { get; init; }

        /// <summary>Grid افتراضياً — يحافظ على سلوك كل وحدة حالية بلا أي تغيير.</summary>
        public LayoutKind LayoutKind { get; init; } = LayoutKind.Grid;

        /// <summary>مطلوبة فقط لو LayoutKind = Tree أو TreeSplit — TreeRenderer يتحقق منها.</summary>
        public TreeLayoutOptions TreeOptions { get; init; }
    }
}
