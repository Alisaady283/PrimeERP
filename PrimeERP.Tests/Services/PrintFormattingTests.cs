using PrimeERP.UI.Services;
using PrimeERP.Application.PageServices.Admin;
using System;
using System.Linq;
using System.Windows.Documents;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Domain.Contracts;
using PrimeERP.Platform.Permissions;
using PrimeERP.Platform.Settings;
using Xunit;

namespace PrimeERP.Tests.Services
{
    /// <summary>الشعار ومحاذاة الجدول في الورق</summary>
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
                const string pixel = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==";
                _db.Services.GetRequiredService<ISettingsService>().Set(SettingKeys.Company.LogoData, pixel);

                var built = _db.Services.GetRequiredService<IPrintService>().BuildContent(new SamplePrintable());
                Assert.True(built.IsSuccess, built.ErrorMessage);

                var images = Elements<System.Windows.Controls.Image>(built.Value.Blocks).ToList();
                Assert.True(images.Any(i => i.Source != null), "الشعار لم يصل لمستند الطباعة");
            });
        }

        [Fact]
        public void TheHeader_KeepsTheLogoLeft_AndCompanyDataAtTheRightEdge()
        {
            StaThreadHelper.Run(() =>
            {
                const string pixel = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==";
                var settings = _db.Services.GetRequiredService<ISettingsService>();
                settings.Set(SettingKeys.Company.LogoData, pixel);
                settings.Set(SettingKeys.Company.Name, "شركة برايم للتجارة والتوزيع");

                var built = _db.Services.GetRequiredService<IPrintService>().BuildContent(new SamplePrintable());
                Assert.True(built.IsSuccess, built.ErrorMessage);

                const double width = 714;
                var header = (System.Windows.FrameworkElement)built.Value.Blocks.OfType<BlockUIContainer>().First().Child;
                header.Measure(new System.Windows.Size(width, double.PositiveInfinity));
                header.Arrange(new System.Windows.Rect(0, 0, width, header.DesiredSize.Height));
                header.UpdateLayout();

                (double Left, double Right) Visual(System.Windows.FrameworkElement element)
                {
                    var box = element.TransformToAncestor(header).TransformBounds(new System.Windows.Rect(element.RenderSize));
                    return (box.X, box.X + box.Width);
                }

                var logo = Visual(Descendants<System.Windows.Controls.Image>(header).First());
                var company = Visual(Descendants<System.Windows.Controls.StackPanel>(header)
                    .First(panel => panel.Children.OfType<System.Windows.Controls.TextBlock>().Any()));

                Assert.True(logo.Left < 2, $"الشعار ليس على حافة اليسار: {logo.Left:F0}");
                Assert.True(company.Right > width - 2, $"بيانات الشركة لا تبلغ حافة اليمين: {company.Right:F0}");
                Assert.True(company.Left - logo.Right > width / 3, "الطرفان ملتصقان ككتلة واحدة بدل طرفَي الورقة");
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

                Assert.Equal(System.Windows.TextAlignment.Left, paragraphs.First(p => TextOf(p) == "صنف تجريبي").TextAlignment);
                Assert.Equal(System.Windows.TextAlignment.Center, paragraphs.First(p => TextOf(p) == "5").TextAlignment);
            });
        }

        private static System.Collections.Generic.IEnumerable<T> Elements<T>(BlockCollection blocks)
            where T : System.Windows.DependencyObject
        {
            foreach (var block in blocks)
            {
                if (block is BlockUIContainer container && container.Child != null)
                    foreach (var found in Descendants<T>(container.Child)) yield return found;

                if (block is Table table)
                    foreach (var cell in table.RowGroups.SelectMany(g => g.Rows).SelectMany(r => r.Cells))
                        foreach (var found in Elements<T>(cell.Blocks)) yield return found;

                if (block is Section section)
                    foreach (var found in Elements<T>(section.Blocks)) yield return found;

                if (block is Paragraph paragraph)
                    foreach (var inline in paragraph.Inlines.OfType<InlineUIContainer>())
                        foreach (var found in Descendants<T>(inline.Child)) yield return found;
            }
        }

        private static System.Collections.Generic.IEnumerable<T> Descendants<T>(System.Windows.DependencyObject node)
            where T : System.Windows.DependencyObject
        {
            if (node == null) yield break;
            if (node is T typed) yield return typed;

            foreach (var child in System.Windows.LogicalTreeHelper.GetChildren(node).OfType<System.Windows.DependencyObject>())
                foreach (var found in Descendants<T>(child)) yield return found;
        }

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
