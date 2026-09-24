using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Application.DTOs.Vouchers;
using PrimeERP.Composition.Renderers;
using PrimeERP.Domain.Contracts;
using PrimeERP.Domain.Enums;
using PrimeERP.Platform.Permissions;
using PrimeERP.Platform.Settings;
using Xunit;

namespace PrimeERP.Tests.Services
{
    /// <summary>سند القبض والصرف</summary>
    public class VoucherPrintTests : IDisposable
    {
        private readonly TestDatabaseFixture _db = new();

        public VoucherPrintTests() => AppSession.DevMode = true;

        public void Dispose() => _db.Dispose();

        private IPrintable Build(bool isReceipt, string party = "محمد أحمد", string reason = "دفعة تحت الحساب") =>
            DocumentPrinter.VoucherDocument(
                new VoucherDetailDto
                {
                    VoucherNo = "V-1", VoucherDate = new DateTime(2026, 9, 13),
                    PartyName = party, TreasuryName = "الخزينة الرئيسية",
                    Amount = 1500m, MethodName = "نقدي", Method = PaymentMethod.Cash, Notes = reason
                },
                isReceipt,
                _db.Services.GetRequiredService<ISettingsProvider>());

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void TheVoucher_IsAFramedHalfPage(bool isReceipt)
        {
            var printable = Build(isReceipt);

            Assert.True(printable.Framed, "السند بلا إطار");
            Assert.True(printable.HalfPage, "السند يُطبع على صفحةٍ كاملة");
            Assert.Equal(PrintOrientation.Portrait, printable.Orientation);
        }

        [Fact]
        public void TheNumberIsNotRepeated_InTheHeader()
        {
            var printable = Build(isReceipt: true);

            Assert.Null(printable.HeaderFields);
            Assert.Equal("V-1", printable.DocumentSubtitle);
        }

        [Fact]
        public void TheAmountStandsAlone_InItsOwnBox()
        {
            var box = Build(isReceipt: true).BuildSections().First();

            Assert.Equal(PrintSectionType.Callout, box.Type);
            Assert.Equal("1,500.00", box.Text);
        }

        private static string LineText(IPrintable printable, string label) =>
            printable.BuildSections()
                .Where(s => s.Type == PrintSectionType.Text && s.FillParts is { Count: > 0 })
                .Select(s => string.Join("  ", s.FillParts))
                .First(text => text.StartsWith(label + " :"));

        [Theory]
        [InlineData(true, "استلمنا من السيد")]
        [InlineData(false, "صرفنا إلى السيد")]
        public void EachLineRunsToTheEdge_WithDots(bool isReceipt, string partyLabel)
        {
            var printable = Build(isReceipt);

            Assert.Contains("2026-09-13", LineText(printable, "التاريخ"));
            Assert.Contains("محمد أحمد", LineText(printable, partyLabel));
            Assert.Contains("دفعة تحت الحساب", LineText(printable, "وذلك عن"));

        }

        [Fact]
        public void TheAmountInWords_SitsOnItsOwnLine()
        {
            Assert.Contains("جنيه", LineText(Build(isReceipt: true), "مبلغاً وقدره"));
            Assert.Contains("لا غير", LineText(Build(isReceipt: true), "مبلغاً وقدره"));
        }

        [Fact]
        public void AMissingValue_LeavesTheLineToFillByHand()
        {
            var line = LineText(Build(isReceipt: true, party: null, reason: null), "وذلك عن");

            Assert.Equal("وذلك عن : ", line);
        }

        [Fact]
        public void TheChequeAndItsBank_ShareOneLine()
        {
            var line = LineText(Build(isReceipt: true), "نقداً / شيك رقم");

            Assert.Contains("مسحوب على بنك", line);
            Assert.Contains("الخزينة الرئيسية", line);
        }

        [Fact]
        public void EveryLine_AsksToBeFilledToTheEdge()
        {
            var lines = Build(isReceipt: true).BuildSections()
                .Where(s => s.Type == PrintSectionType.Text)
                .ToList();

            Assert.NotEmpty(lines);
            Assert.All(lines, line =>
            {
                Assert.NotEmpty(line.FillParts);
                Assert.Equal(line.FillParts.Count, line.FillShares.Count);
                Assert.Equal(1.0, line.FillShares.Sum(), 3);
            });
        }

        [Fact]
        public void ReceiptSignsTwice_AndPaymentThrice()
        {
            Assert.Equal(new[] { "المحاسب", "الاعتماد" }, Build(isReceipt: true).SignatureLabels);
            Assert.Equal(new[] { "المستلِم", "المحاسب", "الاعتماد" }, Build(isReceipt: false).SignatureLabels);
        }

        [Theory]
        [InlineData("GoodsReceipt", new[] { "أمين المخزن", "الاعتماد" })]
        [InlineData("DeliveryNote", new[] { "المستلِم", "أمين المخزن", "الاعتماد" })]
        public void StockVouchers_SignAtTheStore(string moduleKey, string[] expected)
        {
            var module = _db.Services.GetRequiredService<PrimeERP.Composition.Registry.IModuleRegistry>().Get(moduleKey);
            Assert.NotNull(module);

            Assert.Equal(expected, DocumentPrinter.StockSignatures(module.DocumentDialog));
        }

        [Fact]
        public void ADocumentThatDoesNotMoveStock_KeepsTheUsualSignatures()
        {
            var module = _db.Services.GetRequiredService<PrimeERP.Composition.Registry.IModuleRegistry>().Get("SalesInvoices");

            Assert.Null(DocumentPrinter.StockSignatures(module.DocumentDialog));
        }
    }
}
