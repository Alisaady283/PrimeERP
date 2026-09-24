using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Composition.Print;
using PrimeERP.Composition.Registry;
using PrimeERP.Domain.Contracts;
using PrimeERP.Platform.Permissions;
using Xunit;

namespace PrimeERP.Tests.Composition
{
    /// <summary>عائلتا المستندات تبنيان من مصدر</summary>
    public class PrintDocumentsTests : System.IDisposable
    {
        private readonly TestDatabaseFixture _db = new();

        public PrintDocumentsTests() => AppSession.DevMode = true;

        public void Dispose() => _db.Dispose();

        private class Line
        {
            public string ProductCode { get; set; }
            public string ProductName { get; set; }
            public decimal Qty { get; set; }
            public decimal UnitPrice { get; set; }
        }

        private class Invoice
        {
            public string InvoiceNo { get; set; } = "INV-1";
            public string CustomerName { get; set; } = "عميل";
            public List<Line> Lines { get; set; } = new();
        }

        [Fact]
        public void Trade_SplitsCodeAndName_AndSumsOnlyAdditiveColumns()
        {
            var definition = _db.Services.GetRequiredService<IModuleRegistry>().Get("SalesInvoices").DocumentDialog;

            var invoice = new Invoice
            {
                Lines =
                {
                    new Line { ProductCode = "P-1", ProductName = "صنف أول", Qty = 2, UnitPrice = 100 },
                    new Line { ProductCode = "P-2", ProductName = "صنف ثانٍ", Qty = 3, UnitPrice = 50 },
                }
            };

            var table = PrintDocuments.Trade(definition, "فاتورة مبيعات", invoice)
                .BuildSections().Single(s => s.Type == PrintSectionType.Table);

            Assert.Contains(table.Columns, c => c.Key == "ProductCode" && c.Header == "الكود");
            Assert.Contains(table.Columns, c => c.Key == "ProductName");

            Assert.Equal("صنف أول", table.Rows[0]["ProductName"]);
            Assert.Equal(5m, table.TotalsRow["Qty"]);

            Assert.Equal("", table.TotalsRow["UnitPrice"]);
        }

        [Fact]
        public void Narrative_FillsPlaceholders_AndCarriesTheAmountInWords()
        {
            var printable = PrintDocuments.Narrative(new NarrativeDocument
            {
                Title = "سند قبض",
                Number = "RV-1",
                Template = "استلمنا من السيد / السادة: {Party}",
                Values = new() { ["Party"] = "شركة النور" },
                Amount = 1523.75m,
                Signatures = new() { "المستلِم" }
            });

            var sections = printable.BuildSections();

            Assert.Contains("شركة النور", sections.First(s => s.Type == PrintSectionType.Text).Text);

            var amount = sections.Single(s => s.Type == PrintSectionType.AmountInWords);
            Assert.Equal(1523.75m, amount.Amount);
            Assert.True(printable.ShowSignatures);
        }
    }
}
