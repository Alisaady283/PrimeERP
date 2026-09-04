namespace PrimeERP.UI.Components.Tree
{
    public enum LayoutKind
    {
        /// <summary>قائمة+CRUD مسطّحة عبر CrudPageRenderer — الافتراضي، سلوك كل الوحدات الحالية بلا تغيير.</summary>
        Grid,

        /// <summary>شجرة كاملة تملأ الصفحة بلا لوحة تفاصيل.</summary>
        Tree,

        /// <summary>شجرة (يمين، RTL) + لوحة تفاصيل للعقدة المختارة (يسار).</summary>
        TreeSplit,

        /// <summary>معايير + تشغيل + نتيجة عبر ReportRenderer — بلا شبكة CRUD، بلا حوار إضافة/تعديل.</summary>
        Report,

        /// <summary>صفحة إعدادات بتبويبات عبر SettingsPageRenderer — وحدة واحدة فقط في كل النظام.</summary>
        Settings,

        /// <summary>شجرة قابلة للتأشير (صلاحيات الدور/المستخدم) عبر TreeCheckListRenderer.</summary>
        TreeCheckList,

        /// <summary>مستند كصفحة كاملة (رأس+سطور+فوتر) عبر DocumentPageRenderer بدل حوار.</summary>
        DocumentPage,

        /// <summary>شبكة الشيكات + زر تحريك الحالة عبر ChequeBoardRenderer — لا إضافة ولا حذف (الشيك يُنشأ من السند).</summary>
        ChequeBoard
    }

    public enum SelectableRule { All, LeafOnly }

    /// <summary>يصف كيف يُحوَّل List&lt;TDto&gt; المسطَّحة إلى شجرة TreeNodeViewModel — TreeRenderer (7.Composition)
    /// يقرأ كل حقل هنا بالاسم عبر Reflection (نفس فلسفة الربط بالاسم لا بالنوع الثابت المُتَّبعة في
    /// CrudPageRenderer). موجودة في 6.UI لا 7.Composition عمداً: TreeViewModelBase (6.UI أيضاً) لا يمكنها
    /// الاعتماد على نوع من طبقة أعلى (7.Composition) — نفس قيد "6.UI لا يعتمد على طبقة أعلى" في check.sh،
    /// بينما العكس (Composition يعتمد على UI) هو الاتجاه المسموح والمستخدَم أصلاً (ModuleDefinition.Columns
    /// من PrimeERP.UI.Components.Display بالفعل).</summary>
    public class TreeLayoutOptions
    {
        public required string IdField       { get; init; }
        public required string ParentIdField { get; init; }
        public string CodeField { get; init; }
        public string NameField { get; init; }

        /// <summary>مثل "{Code} - {Name}" — {Field} أو {Field:Format} (نفس صياغة string.Format الجزئية).</summary>
        public string DisplayTemplate { get; init; } = "{Name}";

        /// <summary>يُلحَق بعد DisplayTemplate بفاصل مسافة — مثل "({Balance:N2})".</summary>
        public string ExtraInfoTemplate { get; init; }

        public bool ExpandRootsByDefault { get; init; } = true;
        public int  ExpandToLevel        { get; init; }

        /// <summary>اسم خاصية bool على TDto — تُقرأ في TreeNodeViewModel.IsLeaf.</summary>
        public string LeafFlagField { get; init; }

        public SelectableRule SelectableRule { get; init; } = SelectableRule.All;
    }
}
