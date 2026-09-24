using System.Collections.Generic;
using PrimeERP.Platform.Localization;
using PrimeERP.UI.Components.Display;

namespace PrimeERP.Composition.Definitions
{
    /// <summary>حقول تتكرر في كل حوار</summary>
    public static class StandardFields
    {
        public static List<FieldDefinition> DialogFields() => new()
        {
            new() { Key = "IsActive", LabelKey = "Str.Active", Kind = FieldKind.Check, DefaultValue = true },
            new() { Key = "CreatedAt", LabelKey = "Str.CreatedAt", Kind = FieldKind.ReadOnly, DisplayFormat = "yyyy-MM-dd HH:mm" },
            new() { Key = "UpdatedAt", LabelKey = "Str.UpdatedAt", Kind = FieldKind.ReadOnly, DisplayFormat = "yyyy-MM-dd HH:mm" },
        };

        public static List<ParameterDefinition> DateRange(int monthsBack = 1) => new()
        {
            new() { Key = "From", LabelKey = "Str.DateFrom", Kind = FieldKind.Date, DefaultValue = System.DateTime.Today.AddMonths(-monthsBack) },
            new() { Key = "To",   LabelKey = "Str.DateTo",   Kind = FieldKind.Date, DefaultValue = System.DateTime.Today },
        };

        public static List<GridColumn> AuditColumns() => new()
        {
            new() { Header = LocalizationService.Get("Str.Status"), Binding = "StatusText" },
            new() { Header = LocalizationService.Get("Str.CreatedAt"), Binding = "CreatedAt", Format = "yyyy-MM-dd HH:mm" },
            new() { Header = LocalizationService.Get("Str.UpdatedAt"), Binding = "UpdatedAt", Format = "yyyy-MM-dd HH:mm" },
        };
    }
}
