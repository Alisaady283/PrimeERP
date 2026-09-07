using System;
using System.Collections;
using System.Collections.Generic;
using PrimeERP.UI.Components.Display;

namespace PrimeERP.Composition.Definitions
{
    // معيار واحد في لوحة تشغيل التقرير — يُبنى بنفس آلية FieldDefinition (DialogRenderer.BuildField/
    // LoadPickerItems/GetControlValue)، لا نظام مستقل.
    public class ParameterDefinition
    {
        public required string Key { get; init; }
        public required string LabelKey { get; init; }
        public FieldKind Kind { get; init; } = FieldKind.Date;
        public string PickerType { get; init; }
        public string PickerCategoryModuleKey { get; init; }

        /// <summary>الحسابات التي تقبل قيوداً فقط — نفس قيد القيود والأرصدة الافتتاحية.</summary>
        public bool PickerLeafOnly { get; init; }
        public object DefaultValue { get; init; }
    }

    // نتيجة موحّدة لكل التقارير — GridColumn نفسها المستخدَمة في AppDataGrid (لا نوع مواز)، Rows أي
    // IEnumerable (WPF يربط بالاسم على النوع الفعلي وقت التشغيل بصرف النظر عن نوع القائمة المُعلَن).
    public class ReportResult
    {
        /// <summary>نوع الصفّ (heading/total) لتمييزه — فارغ يعني صفّ بيانات عادياً.</summary>
        public Func<object, string> RowKind { get; init; }

        /// <summary>false يوقف تبادل ألوان الصفوف — للقوائم المالية.</summary>
        public bool AlternatingRows { get; init; } = true;

        public string Title { get; init; }
        public string SubTitle { get; init; }
        public required List<GridColumn> Columns { get; init; }
        public required IEnumerable Rows { get; init; }
        public Dictionary<string, string> Totals { get; init; }
        public DateTime GeneratedAt { get; init; } = DateTime.Now;
    }

    public class ReportDefinition
    {
        public required string Key { get; init; }
        public required string TitleKey { get; init; }
        public required string PermissionKey { get; init; }
        public List<ParameterDefinition> Parameters { get; init; } = new();

        // المولِّد الفعلي — تنفيذ التقرير نفسه (استعلام/تجميع)، لا حاجة لآلية Reflection على اسم دالة خدمة؛
        // كل تقرير سطور قليلة تستدعي خدمة موجودة فعلاً (JournalService.GetTrialBalance، إلخ).
        public required Func<IServiceProvider, Dictionary<string, object>, PrimeERP.Domain.Results.Result<ReportResult>> Generate { get; init; }
    }
}
