using System;
using System.Collections.Generic;
using PrimeERP.Domain.Results;
using PrimeERP.UI.Components.Tree;

namespace PrimeERP.Composition.Definitions
{
    /// <summary>شاشة شجرة قابلة للتأشير مدفوعة بمصدر (دور/مستخدم) — تُستهلك عبر TreeCheckListRenderer.</summary>
    public class TreeCheckListDefinition
    {
        public string TitleKey { get; init; }
        public string SourceLabelKey { get; init; }
        public TreeCheckMode Mode { get; init; } = TreeCheckMode.TwoState;

        /// <summary>عناصر المصدر أعلى الشاشة (الأدوار أو المستخدمون).</summary>
        public Func<IServiceProvider, List<SourceOption>> SourceItems { get; init; }

        /// <summary>يبني الشجرة بحالتها الحالية للمصدر المختار.</summary>
        public Func<IServiceProvider, int, List<TreeNodeViewModel>> BuildTree { get; init; }

        public Func<IServiceProvider, int, List<TreeNodeViewModel>, Result> Save { get; init; }

        /// <summary>أزرار إضافية بجانب الحفظ (نسخ من دور، إعادة للموروث...).</summary>
        public List<TreeCheckListAction> Actions { get; init; } = new();

        public string PermissionKey { get; init; }
    }

    public class SourceOption
    {
        public int Id { get; init; }
        public string Display { get; init; }
    }

    public class TreeCheckListAction
    {
        public string TextKey { get; init; }
        public string Variant { get; init; } = "secondary";

        /// <summary>يستقبل (services, sourceId, nodes) ويعدّل حالات العقد مباشرة.</summary>
        public Action<IServiceProvider, int, List<TreeNodeViewModel>> Run { get; init; }
    }
}
