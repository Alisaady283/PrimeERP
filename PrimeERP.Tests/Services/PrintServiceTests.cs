using System.Collections.Generic;
using PrimeERP.Models;
using PrimeERP.Services.Print;
using Xunit;

namespace PrimeERP.Tests.Services
{
    [Collection("Database")]
    public class PrintServiceTests
    {
        public PrintServiceTests(TestDatabaseFixture _) { }

        private static JournalEntry SampleEntry() => new()
        {
            EntryNo     = "JE-2026-00001",
            EntryDate   = "2026-08-17",
            Description = "قيد اختباري",
            Source      = "يدوي",
            CreatedBy   = "tester",
            TotalDebit  = 100m,
            TotalCredit = 100m,
            Lines = new List<JournalLine>
            {
                new() { AccountCode = "1240", AccountName = "الصندوق", Debit = 100m, Credit = 0m },
                new() { AccountCode = "4100", AccountName = "إيرادات المبيعات", Debit = 0m, Credit = 100m }
            }
        };

        [Fact]
        public void Build_JournalEntryPrint_ProducesNonEmptyFixedDocument()
        {
            // أنواع WPF (FlowDocument/Table) تفرض خيط STA — انظر StaThreadHelper.
            StaThreadHelper.Run(() =>
            {
                var printable = PrintTemplates.JournalEntryPrint(SampleEntry());
                var service = new PrintService();

                var result = service.Build(printable);

                Assert.True(result.IsSuccess, result.ErrorMessage);
                Assert.NotNull(result.Value);
                Assert.True(result.Value.Pages.Count > 0);
            });
        }
    }
}
