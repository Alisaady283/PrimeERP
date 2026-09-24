using System;
using System.Collections;
using System.Collections.Generic;
using PrimeERP.UI.Components.Display;

namespace PrimeERP.Composition.Definitions
{
    /// <summary>وصف التقرير ومعاملاته</summary>
    public record ParameterDefinition
    {
        public required string Key { get; init; }
        public required string LabelKey { get; init; }
        public FieldKind Kind { get; init; } = FieldKind.Date;
        public string PickerType { get; init; }
        public string PickerCategoryModuleKey { get; init; }

        public bool PickerLeafOnly { get; init; }
        public object DefaultValue { get; init; }
    }

    public class ReportResult
    {
        public Func<object, string> RowKind { get; init; }

        public bool AlternatingRows { get; init; } = true;

        public string Title { get; init; }
        public string SubTitle { get; init; }
        public required List<GridColumn> Columns { get; init; }
        public required IEnumerable Rows { get; init; }
        public Dictionary<string, string> Totals { get; init; }
        public DateTime GeneratedAt { get; init; } = DateTime.Now;
    }

    public record ReportDefinition
    {
        public required string Key { get; init; }
        public required string TitleKey { get; init; }
        public required string PermissionKey { get; init; }
        public List<ParameterDefinition> Parameters { get; init; } = new();

        public required Type ServiceType { get; init; }
        public required string Method { get; init; }
        public string[] Arguments { get; init; } = System.Array.Empty<string>();

        public Dictionary<string, object> FixedArguments { get; init; }

        public required List<GridColumn> Columns { get; init; }
        public string TitleOverrideTotalKey { get; init; }
        public Func<object, string> RowKind { get; init; }
        public bool AlternatingRows { get; init; } = true;
    }
}
