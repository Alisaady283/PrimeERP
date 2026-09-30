using PrimeERP.Application.Legacy.Print;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Documents;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Composition.Print;
using PrimeERP.Composition.Registry;
using PrimeERP.Domain.Contracts;
using PrimeERP.Domain.Helpers;
using PrimeERP.Platform.Permissions;
using Xunit;

namespace PrimeERP.Tests.Composition
{
    /// <summary>خيارات الورق</summary>
    public class PrintPaperTests : IDisposable
    {
        private readonly TestDatabaseFixture _db = new();

        public PrintPaperTests() => AppSession.DevMode = true;

        public void Dispose() => _db.Dispose();

        private class Line
        {
            public string ProductCode { get; set; } = "P-1";
            public string ProductName { get; set; } = "صنف";
            public decimal Qty { get; set; } = 1;
            public decimal UnitPrice { get; set; } = 10;
        }

        private class Invoice
        {
            public string InvoiceNo { get; set; } = "INV-7";
            public string CustomerName { get; set; } = "عميل";
            public List<Line> Lines { get; set; } = new();
        }

        private Invoice InvoiceWith(int lineCount)
        {
            var invoice = new Invoice();
            for (var i = 0; i < lineCount; i++) invoice.Lines.Add(new Line());
            return invoice;
        }

        private IPrintable Build(int lineCount, PrintDocuments.PaperOptions paper)
        {
            var definition = _db.Services.GetRequiredService<IModuleRegistry>().Get("SalesInvoices").DocumentDialog;
            return PrintDocuments.Trade(definition, "فاتورة مبيعات", InvoiceWith(lineCount), paper);
        }

        [Fact]
        public void EachCopyLabelProducesItsOwnPages()
        {
            var printable = Build(1, new PrintDocuments.PaperOptions
            { CopyLabels = new() { "أصل", "صورة العميل", "صورة الحسابات" } });

            Assert.Equal(3, printable.CopyLabels.Count);

            var built = StaThreadHelper.Run(() => _db.Services.GetRequiredService<IPrintService>().Build(printable));
            Assert.True(built.IsSuccess, built.ErrorMessage);
            Assert.True(built.Value.Pages.Count >= 3, $"صفحات: {built.Value.Pages.Count}");
        }

        [Fact]
        public void ALongTableIsSplitSoItsHeaderRepeats()
        {
            var printable = Build(30, new PrintDocuments.PaperOptions { LinesPerPage = 10 });
            Assert.Equal(10, printable.LinesPerPage);

            var content = StaThreadHelper.Run(() => _db.Services.GetRequiredService<IPrintService>().BuildContent(printable));
            Assert.True(content.IsSuccess, content.ErrorMessage);

            var lineTables = content.Value.Blocks.OfType<Table>().Where(t => t.Columns.Count >= 5).ToList();
            Assert.Equal(3, lineTables.Count);
        }

        [Fact]
        public void CopiesAfterTheOriginalCarryAWatermark()
        {
            var printable = Build(1, new PrintDocuments.PaperOptions { CopyLabels = new() { "أصل", "صورة العميل" } });

            var rotatedStamps = StaThreadHelper.Run(() =>
            {
                var built = _db.Services.GetRequiredService<IPrintService>().Build(printable);
                Assert.True(built.IsSuccess, built.ErrorMessage);

                return built.Value.Pages
                    .SelectMany(p => p.Child.Children.OfType<System.Windows.Controls.TextBlock>())
                    .Count(t => t.Text == "صورة العميل" && t.RenderTransform is System.Windows.Media.RotateTransform);
            });

            Assert.True(rotatedStamps > 0, "لا ختم على النسخة");
        }

        [Fact]
        public void TheBarcodeIsEncodedFromTheDocumentNumber()
        {
            var printable = Build(1, new PrintDocuments.PaperOptions { BarcodeText = "INV-7" });

            Assert.Contains(printable.BuildSections(), s => s.Type == PrintSectionType.Barcode && s.Text == "INV-7");
            Assert.NotEmpty(Code128.Encode("INV-7"));
            Assert.Empty(Code128.Encode(""));
        }

        [Fact]
        public void TheChequePlacesEveryFieldAndDrawsACalibrationGrid()
        {
            var layout = new ChequeLayout
            {
                OffsetX = 0.2,
                Fields =
                {
                    new() { Text = "شركة النور", X = 3.0, Y = 1.5 },
                    new() { Text = "1,523.75", X = 12.0, Y = 1.5, Bold = true },
                }
            };

            var printer = _db.Services.GetRequiredService<IChequePrinter>();

            var cheque = StaThreadHelper.Run(() => printer.Build(layout));
            Assert.True(cheque.IsSuccess, cheque.ErrorMessage);
            Assert.Single(cheque.Value.Pages);

            var calibration = StaThreadHelper.Run(() => printer.BuildCalibrationSheet(layout));
            Assert.True(calibration.IsSuccess, calibration.ErrorMessage);
        }
    }
}
