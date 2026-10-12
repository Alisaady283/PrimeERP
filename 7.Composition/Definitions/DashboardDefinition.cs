using System;
using System.Collections.Generic;
using PrimeERP.Platform.Settings;
using PrimeERP.UI.Components.Charts;

namespace PrimeERP.Composition.Definitions
{
    public enum ValueKind { Money, Count, Percent, Days }

    /// <summary>لوحة تحليل: سؤالها وبطاقاتها ورسومها، والخدمة التي تحسبها</summary>
    public record DashboardDefinition
    {
        public required Type ServiceType { get; init; }
        public required string Method { get; init; }
        public string QuestionKey { get; init; }
        public List<ParameterDefinition> Parameters { get; init; } = new();
        public string[] Arguments { get; init; } = { "From", "To" };
        public List<DashboardCard> Cards { get; init; } = new();
        public List<DashboardChart> Charts { get; init; } = new();
    }

    public record DashboardCard
    {
        public required string Key { get; init; }
        public required string TitleKey { get; init; }
        public ValueKind Kind { get; init; } = ValueKind.Money;
        public string IconKey { get; init; }
        public string DrillKey { get; init; }
        public bool LowerIsBetter { get; init; }
        public FlowScope FlowScope { get; init; } = FlowScope.Both;
        public Func<ISettingsProvider, bool> ShownWhen { get; init; }
    }

    public record DashboardChart
    {
        public required string Key { get; init; }
        public required string TitleKey { get; init; }
        public string QuestionKey { get; init; }
        public ChartKind Kind { get; init; } = ChartKind.Column;
        public ChartTone Tone { get; init; } = ChartTone.Auto;
        public bool Smooth { get; init; }
        public ValueKind Value { get; init; } = ValueKind.Money;
        /// <summary>عرض الرسم من ثلاثة أعمدة: ١ أو ٢ أو ٣</summary>
        public int Span { get; init; } = 1;
        public string DrillKey { get; init; }
        public FlowScope FlowScope { get; init; } = FlowScope.Both;
        public Func<ISettingsProvider, bool> ShownWhen { get; init; }
    }
}
