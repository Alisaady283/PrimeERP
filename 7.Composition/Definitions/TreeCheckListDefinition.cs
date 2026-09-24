using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PrimeERP.Domain.Results;
using PrimeERP.UI.Components.Tree;

namespace PrimeERP.Composition.Definitions
{
    /// <summary>شاشة شجرة قابلة للتأشير مدفوعة</summary>
    public class TreeCheckListDefinition
    {
        public string TitleKey { get; init; }
        public string SourceLabelKey { get; init; }
        public TreeCheckMode Mode { get; init; } = TreeCheckMode.TwoState;

        public Func<IServiceProvider, List<SourceOption>> SourceItems { get; init; }

        public Func<IServiceProvider, int, string> SourceNote { get; init; }

        public Func<IServiceProvider, int, List<TreeNodeViewModel>> BuildTree { get; init; }

        public Func<IServiceProvider, int, List<TreeNodeViewModel>, Task<Result>> Save { get; init; }

        public List<TreeCheckListAction> Actions { get; init; } = new();

        public Action<List<TreeNodeViewModel>, TreeNodeViewModel> ApplyRules { get; init; }

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

        public bool RequiresSource { get; init; } = true;

        public Func<IServiceProvider, int, List<TreeNodeViewModel>, Task<Result>> RunAsync { get; init; }

        public Action<IServiceProvider, int, List<TreeNodeViewModel>> Run { get; init; }
    }
}
