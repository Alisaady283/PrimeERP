using System.Collections.Generic;

namespace PrimeERP.Domain.Contracts
{
    /// <summary>
    /// محتوى ترويسة الشركة كبيانات مجرّدة — ما يظهر وترتيبه وتسمياته يُقرَّر مرة واحدة، ثم يرسمه كل مُصيِّر
    /// بأدواته: الطباعة بـ FlowDocument والتصدير بـ PDF. بلا هذا النموذج كان لكل مسار ترويسته وتفترقان.
    /// </summary>
    public class CompanyHeader
    {
        public string Name { get; init; }
        public List<string> Details { get; init; } = new();
        public string LogoData { get; init; }

        public bool HasLogo => !string.IsNullOrWhiteSpace(LogoData);
    }
}
