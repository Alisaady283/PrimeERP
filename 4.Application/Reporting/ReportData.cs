using System.Collections;
using System.Collections.Generic;

namespace PrimeERP.Application.Reporting
{
    /// <summary>
    /// ما يُرجعه أي تقرير في النظام: صفوفه وإجمالياته. لا أعمدة ولا عنوان ولا ألوان — تلك تخطيطُ شاشة
    /// تبقى في إعلان التقرير. شكلٌ واحد تستورده التقارير كلها، فلا يجد المنطق باباً يسكن به طبقة التسجيل.
    /// </summary>
    public class ReportData
    {
        public required IEnumerable Rows { get; init; }
        public Dictionary<string, string> Totals { get; init; } = new();
    }
}
