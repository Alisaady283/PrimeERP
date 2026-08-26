using System;
using System.Collections.Generic;
using PrimeERP.UI.Components.Display;

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

        public required List<GridColumn> Columns { get; init; }
    }
}
