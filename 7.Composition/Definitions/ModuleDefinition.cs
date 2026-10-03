using System;
using System.Collections.Generic;
using PrimeERP.UI.Components.Display;
using PrimeERP.UI.Components.Tree;

namespace PrimeERP.Composition.Definitions
{
    /// <summary>وصف تعريفي كامل لصفحة قائمة+CRUD</summary>
    public record ModuleDefinition
    {
        public required string Key { get; init; }
        public required string TitleKey { get; init; }
        public required string PermissionPrefix { get; init; }

        public Type ViewModelType { get; init; }

        public Func<IServiceProvider, object> ViewModelFactory { get; init; }

        public string[] EnabledActions { get; init; }

        public List<GridColumn> Columns { get; init; }

        public LayoutKind LayoutKind { get; init; } = LayoutKind.Grid;

        public TreeLayoutOptions TreeOptions { get; init; }

        public DialogDefinition Dialog { get; init; }

        public DocumentDialogDefinition DocumentDialog { get; init; }

        /// <summary>صفٌّ يحرّره محرّر نوعه</summary>
        public string RowKindProperty { get; init; }

        public List<DocumentDialogDefinition> KindDialogs { get; init; }

        public List<FilterDefinition> Filters { get; init; }

        public ReportDefinition Report { get; init; }

        public TreeCheckListDefinition TreeCheckList { get; init; }

        public List<RowAction> RowActions { get; init; }

        public bool SingleRecord { get; init; }

        public bool Reorderable { get; init; }

        public FlowScope FlowScope { get; init; } = FlowScope.Both;
    }
}
