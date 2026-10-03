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
using PrimeERP.Platform.Localization;
using PrimeERP.Tests.Helpers;

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
        [InlineData(true, "Str.Print.ReceivedFrom")]
        [InlineData(false, "Str.Print.PaidTo")]
        public void EachLineRunsToTheEdge_WithDots(bool isReceipt, string partyKey)
        {
            var printable = Build(isReceipt);

            Assert.Contains("2026-09-13", LineText(printable, LocalizationService.Get("Str.Date")));
            Assert.Contains("محمد أحمد", LineText(printable, LocalizationService.Get(partyKey)));
            Assert.Contains("دفعة تحت الحساب", LineText(printable, LocalizationService.Get("Str.Print.For")));

        }

        [Fact]
        public void TheAmountInWords_SitsOnItsOwnLine()
        {
            Assert.Contains("جنيه", LineText(Build(isReceipt: true), LocalizationService.Get("Str.Print.AmountOf")));
            Assert.True(Localized.Says(LineText(Build(isReceipt: true), LocalizationService.Get("Str.Print.AmountOf")), "Str.Print.WordsOnly"));
        }

        [Fact]
        public void AMissingValue_LeavesTheLineToFillByHand()
        {
            var line = LineText(Build(isReceipt: true, party: null, reason: null), LocalizationService.Get("Str.Print.For"));

            Assert.Equal(LocalizationService.Get("Str.Print.For") + " : ", line);
        }

        [Fact]
        public void TheChequeAndItsBank_ShareOneLine()
        {
            var line = LineText(Build(isReceipt: true), LocalizationService.Get("Str.Print.CashOrCheque"));

            Assert.Contains(LocalizationService.Get("Str.Print.DrawnOn"), line);
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
            Assert.Equal(new[] { LocalizationService.Get("Str.Print.Accountant"), LocalizationService.Get("Str.Print.Approval") }, Build(isReceipt: true).SignatureLabels);
            Assert.Equal(new[] { LocalizationService.Get("Str.Print.Receiver"), LocalizationService.Get("Str.Print.Accountant"), LocalizationService.Get("Str.Print.Approval") }, Build(isReceipt: false).SignatureLabels);
        }

        [Theory]
        [InlineData("GoodsReceipt", new[] { "Str.Print.Storekeeper", "Str.Print.Approval" })]
        [InlineData("DeliveryNote", new[] { "Str.Print.Receiver", "Str.Print.Storekeeper", "Str.Print.Approval" })]
        public void StockVouchers_SignAtTheStore(string moduleKey, string[] expectedKeys)
        {
            var module = _db.Services.GetRequiredService<PrimeERP.Composition.Registry.IModuleRegistry>().Get(moduleKey);
            Assert.NotNull(module);

            Assert.Equal(expectedKeys.Select(k => LocalizationService.Get(k)), DocumentPrinter.StockSignatures(module.DocumentDialog));
        }

        [Fact]
        public void ADocumentThatDoesNotMoveStock_KeepsTheUsualSignatures()
        {
            var module = _db.Services.GetRequiredService<PrimeERP.Composition.Registry.IModuleRegistry>().Get("SalesInvoices");

            Assert.Null(DocumentPrinter.StockSignatures(module.DocumentDialog));
        }
    }
}
