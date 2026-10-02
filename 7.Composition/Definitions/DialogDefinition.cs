using System;
using System.Collections.Generic;

namespace PrimeERP.Composition.Definitions
{
    /// <summary>وصف الحوار وحقوله</summary>
    public enum FieldKind { Text, Number, Check, TextArea, ReadOnly, Picker, Date, Password, Image }

    public class FieldDefinition
    {
        public required string Key { get; init; }
        public required string LabelKey { get; init; }
        public FieldKind Kind { get; init; } = FieldKind.Text;
        public bool IsRequired { get; init; }
        public bool IsReadOnlyOnEdit { get; init; }

        public bool IsReadOnly { get; init; }
        public int MaxLength { get; init; }

        public int      MinLength      { get; init; }
        public decimal? Min            { get; init; }
        public decimal? Max            { get; init; }
        public DateTime? MinDate       { get; init; }
        public DateTime? MaxDate       { get; init; }

        public string   Pattern        { get; init; }
        public string   PatternMessage { get; init; }

        public FlowScope FlowScope { get; init; } = FlowScope.Both;
        public int ColumnSpan { get; init; } = 1;

        public string PickerType { get; init; }
        public bool PickerLeafOnly { get; init; }

        public string PickerCategoryModuleKey { get; init; }

        public string PickerValueField { get; init; } = "Id";

        public object DefaultValue { get; init; }

        public string DisplayFormat { get; init; }

        public string VisibleWhenField { get; init; }
        public object VisibleWhenValue { get; init; }

        public Func<object, IServiceProvider, bool> VisibleWhen { get; init; }

        public string PickerFilterField { get; init; }
    }

    public class DialogDefinition
    {
        public required string TitleKey { get; init; }
        public required string TitleEditKey { get; init; }
        public int GridColumns { get; init; } = 2;
        public required List<FieldDefinition> Fields { get; init; }
        public required Type ServiceType { get; init; }

        public Func<IServiceProvider, object> ServiceFactory { get; init; }
        public required Type CreateDtoType { get; init; }
        public required Type UpdateDtoType { get; init; }

        public Dictionary<string, object> FixedValues { get; init; }
    }
}
