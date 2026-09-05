using System;
using System.Linq;
using System.Windows.Documents;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Application.Services;
using PrimeERP.Application.Services.Print;
using PrimeERP.Domain.Contracts;
using PrimeERP.Platform.Permissions;
using PrimeERP.Platform.Settings;
using Xunit;

namespace PrimeERP.Tests.Services
{
    /// <summary>الشعار ومحاذاة الجدول في الورق — كلاهما كان يفشل بصمت: شعار لا يظهر، ورأس جدول يتبع محاذاة
    /// بياناته بدل التوسيط.</summary>
    public class PrintFormattingTests : IDisposable
    {
        private readonly TestDatabaseFixture _db = new();

        public PrintFormattingTests() => AppSession.DevMode = true;

        public void Dispose() => _db.Dispose();

        private class SamplePrintable : IPrintable
        {
            public string DocumentTitle => "فاتورة";
            public string DocumentSubtitle => "INV-1";
            public PrintOrientation Orientation => PrintOrientation.Portrait;
            public System.Collections.Generic.Dictionary<string, string> HeaderFields => null;
            public System.Collections.Generic.Dictionary<string, string> FooterFields => null;
            public bool ShowCompanyHeader => true;
            public bool ShowPageNumbers => false;
            public bool ShowSignatures => false;
            public System.Collections.Generic.List<string> SignatureLabels => null;

            public System.Collections.Generic.List<PrintSection> BuildSections() => new()
            {
                new PrintSection
                {
                    Type = PrintSectionType.Table,
                    Columns = new() { new() { Key = "Name", Header = "الصنف" }, new() { Key = "Qty", Header = "الكمية" } },
                    Rows = new() { new() { ["Name"] = "صنف تجريبي", ["Qty"] = 5m } }
                }
            };
        }

        [Fact]
        public void TheCompanyLogo_ReachesThePrintedDocument()
        {
            StaThreadHelper.Run(() =>
            {
                // صورة 1×1 صالحة بترميز Base64 — يكفي لإثبات وصولها من الإعدادات للورق.
                const string pixel = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==";
                _db.Services.GetRequiredService<ISettingsService>().Set(SettingKeys.Company.LogoData, pixel);

                var built = _db.Services.GetRequiredService<IPrintService>().BuildContent(new SamplePrintable());
                Assert.True(built.IsSuccess, built.ErrorMessage);

                var images = Paragraphs(built.Value.Blocks)
                    .SelectMany(p => p.Inlines.OfType<InlineUIContainer>())
                    .Select(c => c.Child).OfType<System.Windows.Controls.Image>().ToList();
                Assert.True(images.Any(i => i.Source != null), "الشعار لم يصل لمستند الطباعة");
            });
        }

        [Fact]
        public void TableHeaders_AreCentred_AndTextCellsAreRightAligned()
        {
            StaThreadHelper.Run(() =>
            {
                var built = _db.Services.GetRequiredService<IPrintService>().BuildContent(new SamplePrintable());
                Assert.True(built.IsSuccess, built.ErrorMessage);

                var paragraphs = Paragraphs(built.Value.Blocks).Where(p => p.Inlines.FirstInline is Run).ToList();

                string TextOf(Paragraph p) => ((Run)p.Inlines.FirstInline).Text;

                Assert.Equal(System.Windows.TextAlignment.Center, paragraphs.First(p => TextOf(p) == "الصنف").TextAlignment);
                Assert.Equal(System.Windows.TextAlignment.Right, paragraphs.First(p => TextOf(p) == "صنف تجريبي").TextAlignment);
                Assert.Equal(System.Windows.TextAlignment.Center, paragraphs.First(p => TextOf(p) == "5").TextAlignment);
            });
        }

        // FlowDocument شجرة كتل لا شجرة منطقية — LogicalTreeHelper لا يصل لخلايا الجداول.
        private static System.Collections.Generic.IEnumerable<Paragraph> Paragraphs(BlockCollection blocks)
        {
            foreach (var block in blocks)
            {
                if (block is Paragraph paragraph) yield return paragraph;

                if (block is Table table)
                    foreach (var cell in table.RowGroups.SelectMany(g => g.Rows).SelectMany(r => r.Cells))
                        foreach (var nested in Paragraphs(cell.Blocks)) yield return nested;

                if (block is Section section)
                    foreach (var nested in Paragraphs(section.Blocks)) yield return nested;
            }
        }
    }
}
