using System.Collections.Generic;
using PrimeERP.Views.Controls.Documents;
using Xunit;

namespace PrimeERP.Tests.Documents
{
    /// <summary>محوَّلة من Views/Controls/Documents/Tests/LineEngineTests.cs (مُشغِّل اختبارات يدوي بلا إطار) إلى xUnit حقيقية.</summary>
    public class LineComputeEngineTests
    {
        private static DocumentLine InvoiceLine() => new() { Qty = 10, Price = 100, DiscountPercent = 10, TaxPercent = 14 };

        [Fact]
        public void LineSubTotal_EqualsQtyTimesPrice() =>
            Assert.Equal(1000m, LineComputeEngine.Compute("LineSubTotal", InvoiceLine()));

        [Fact]
        public void DiscountAmount_EqualsQtyTimesPriceTimesDiscountPercent() =>
            Assert.Equal(100m, LineComputeEngine.Compute("DiscountAmount", InvoiceLine()));

        [Fact]
        public void TaxAmount_EqualsSubTotalMinusDiscountTimesTaxPercent() =>
            Assert.Equal(126m, LineComputeEngine.Compute("TaxAmount", InvoiceLine())); // (1000-100)*14% = 126

        [Fact]
        public void LineTotal_EqualsSubTotalMinusDiscountPlusTax() =>
            Assert.Equal(1026m, LineComputeEngine.Compute("LineTotal", InvoiceLine())); // 1000-100+126

        [Fact]
        public void StockTotal_EqualsQtyTimesPrice()
        {
            var stockLine = new DocumentLine { Qty = 5, Price = 40 };
            Assert.Equal(200m, LineComputeEngine.Compute("StockTotal", stockLine));
        }

        [Fact]
        public void Recalculate_WritesLineTotalBackIntoLine()
        {
            var line = InvoiceLine();
            LineComputeEngine.Recalculate(line, LineColumnPresets.SalesInvoice());
            Assert.Equal(1026m, line.LineTotal);
        }

        [Fact]
        public void Compute_UnregisteredFormulaKey_ReturnsZeroSafely() =>
            Assert.Equal(0m, LineComputeEngine.Compute("NotRegistered", InvoiceLine()));
    }

    public class LineValidationEngineTests
    {
        private static readonly List<LineColumn> JournalColumns = LineColumnPresets.Journal();
        private static readonly List<LineColumn> InvoiceColumns = LineColumnPresets.SalesInvoice();

        [Fact]
        public void EmptyLine_IsNotConsideredAnError()
        {
            var errors = LineValidationEngine.Validate(new DocumentLine(), JournalColumns,
                new DocumentLinesContext { Mode = DocumentLinesMode.Journal });
            Assert.Empty(errors);
        }

        [Fact]
        public void JournalLine_WithBothDebitAndCredit_IsAnError()
        {
            var line = new DocumentLine { ItemCode = "1110", Debit = 100, Credit = 50 };
            var errors = LineValidationEngine.Validate(line, JournalColumns,
                new DocumentLinesContext { Mode = DocumentLinesMode.Journal });
            Assert.True(errors.ContainsKey(nameof(DocumentLine.Debit)));
        }

        [Fact]
        public void JournalLine_WithNeitherDebitNorCredit_IsAnError()
        {
            var line = new DocumentLine { ItemCode = "1110" };
            var errors = LineValidationEngine.Validate(line, JournalColumns,
                new DocumentLinesContext { Mode = DocumentLinesMode.Journal });
            Assert.True(errors.ContainsKey(nameof(DocumentLine.Debit)));
        }

        [Fact]
        public void JournalLine_WithDebitOnly_IsValid()
        {
            var line = new DocumentLine { ItemCode = "1110", Debit = 100 };
            var errors = LineValidationEngine.Validate(line, JournalColumns,
                new DocumentLinesContext { Mode = DocumentLinesMode.Journal });
            Assert.Empty(errors);
        }

        [Fact]
        public void InvoiceLine_WithNegativeQty_IsAnError()
        {
            var line = new DocumentLine { ItemCode = "P001", Qty = -5, Price = 10 };
            var errors = LineValidationEngine.Validate(line, InvoiceColumns,
                new DocumentLinesContext { Mode = DocumentLinesMode.SalesInvoice });
            Assert.True(errors.ContainsKey(nameof(DocumentLine.Qty)));
        }

        [Fact]
        public void InvoiceLine_WithZeroQtyButNonEmptyRow_IsAnError()
        {
            var line = new DocumentLine { ItemCode = "P001", Qty = 0, Price = 10 };
            var errors = LineValidationEngine.Validate(line, InvoiceColumns,
                new DocumentLinesContext { Mode = DocumentLinesMode.SalesInvoice });
            Assert.True(errors.ContainsKey(nameof(DocumentLine.Qty)));
        }

        [Fact]
        public void InvoiceLine_WithNegativePrice_IsAnError()
        {
            var line = new DocumentLine { ItemCode = "P001", Qty = 1, Price = -10 };
            var errors = LineValidationEngine.Validate(line, InvoiceColumns,
                new DocumentLinesContext { Mode = DocumentLinesMode.SalesInvoice });
            Assert.True(errors.ContainsKey(nameof(DocumentLine.Price)));
        }

        [Fact]
        public void InvoiceLine_WithDiscountOver100Percent_IsAnError()
        {
            var line = new DocumentLine { ItemCode = "P001", Qty = 1, Price = 10, DiscountPercent = 150 };
            var errors = LineValidationEngine.Validate(line, InvoiceColumns,
                new DocumentLinesContext { Mode = DocumentLinesMode.SalesInvoice });
            Assert.True(errors.ContainsKey(nameof(DocumentLine.DiscountPercent)));
        }

        [Fact]
        public void InvoiceLine_FullyValid_HasNoErrors()
        {
            var line = new DocumentLine { ItemCode = "P002", Qty = 2, Price = 50, DiscountPercent = 5 };
            var errors = LineValidationEngine.Validate(line, InvoiceColumns,
                new DocumentLinesContext { Mode = DocumentLinesMode.SalesInvoice });
            Assert.Empty(errors);
        }

        [Fact]
        public void DuplicateItemCodeAcrossLines_IsAnError()
        {
            var line1 = new DocumentLine { ItemCode = "P001", Qty = 1, Price = 10 };
            var line2 = new DocumentLine { ItemCode = "P001", Qty = 2, Price = 20 };
            var context = new DocumentLinesContext
            {
                AllLines = new List<DocumentLine> { line1, line2 },
                DuplicateCheckKey = "ItemCode",
                Mode = DocumentLinesMode.SalesInvoice
            };

            var errors = LineValidationEngine.Validate(line1, InvoiceColumns, context);
            Assert.True(errors.ContainsKey("ItemCode"));
        }

        [Fact]
        public void DistinctItemCodesAcrossLines_HasNoDuplicateError()
        {
            var line1 = new DocumentLine { ItemCode = "P001", Qty = 1, Price = 10 };
            var line2 = new DocumentLine { ItemCode = "P002", Qty = 2, Price = 20 };
            var context = new DocumentLinesContext
            {
                AllLines = new List<DocumentLine> { line1, line2 },
                DuplicateCheckKey = "ItemCode",
                Mode = DocumentLinesMode.SalesInvoice
            };

            var errors = LineValidationEngine.Validate(line1, InvoiceColumns, context);
            Assert.False(errors.ContainsKey("ItemCode"));
        }
    }

    public class DocumentLineTests
    {
        [Fact]
        public void Clone_CopiesFieldValues()
        {
            var original = new DocumentLine { ItemCode = "P001", Qty = 3, Price = 15, Notes = "ملاحظة" };
            var clone = original.Clone();

            Assert.Equal(original.ItemCode, clone.ItemCode);
            Assert.Equal(original.Qty, clone.Qty);
            Assert.Equal(original.Notes, clone.Notes);
        }

        [Fact]
        public void Clone_IsIndependentInstance()
        {
            var original = new DocumentLine { ItemCode = "P001", Qty = 3 };
            var clone = original.Clone();

            Assert.False(ReferenceEquals(original, clone));

            clone.Qty = 99;
            Assert.Equal(3, original.Qty);
        }

        [Fact]
        public void Indexer_Write_ReflectsInDirectProperty()
        {
            var line = new DocumentLine { ["Qty"] = 7m };
            Assert.Equal(7m, line.Qty);
        }

        [Fact]
        public void DirectProperty_Write_ReflectsInIndexer()
        {
            var line = new DocumentLine { Price = 25m };
            Assert.Equal(25m, (decimal)line["Price"]);
        }
    }
}
