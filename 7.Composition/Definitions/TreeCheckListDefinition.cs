using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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

        /// <summary>الحفظ غير متزامن: منه ما يطول (نسخ برنامج كامل)، وخيط الواجهة لا يُحجَز.</summary>
        public Func<IServiceProvider, int, List<TreeNodeViewModel>, Task<Result>> Save { get; init; }

        /// <summary>أزرار إضافية بجانب الحفظ (نسخ من دور، إعادة للموروث...).</summary>
        public List<TreeCheckListAction> Actions { get; init; } = new();

        /// <summary>قواعد بين العقد (مثل: لا فعل بلا عرض، أو القسم يورّث صفحاته) — تُطبَّق بعد البناء
        /// وبعد كل نقرة وبعد كل إجراء جماعي. موضع واحد لا ثلاثة، وإلا سرت القاعدة في مسار وسقطت في آخر.
        /// المعامل الثاني هو العقدة التي تغيّرت — null بعد البناء والإجراءات الجماعية.</summary>
        public Action<List<TreeNodeViewModel>, TreeNodeViewModel> ApplyRules { get; init; }

        /// <summary>نصّ زرّ الحفظ — بلا إعلان يبقى «حفظ».</summary>
        public string SaveTextKey { get; init; } = "Str.Save";

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
