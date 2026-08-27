using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Domain.Entities;
using PrimeERP.Application.Services.Print;
using Xunit;

namespace PrimeERP.Tests.Services
{
    [Collection("Database")]
    public class PrintServiceTests
    {
        private readonly IPrintService _service;

        public PrintServiceTests(TestDatabaseFixture db) => _service = db.Services.GetRequiredService<IPrintService>();

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
                new() { AccountCode = "1204", AccountName = "الصندوق", Debit = 100m, Credit = 0m },
                new() { AccountCode = "41", AccountName = "إيرادات المبيعات", Debit = 0m, Credit = 100m }
            }
        };

        [Fact]
        public void Build_JournalEntryPrint_ProducesNonEmptyFixedDocument()
        {
            // أنواع WPF (FlowDocument/Table) تفرض خيط STA — انظر StaThreadHelper.
            StaThreadHelper.Run(() =>
            {
                var printable = PrintTemplates.JournalEntryPrint(SampleEntry());

                var result = _service.Build(printable);

                Assert.True(result.IsSuccess, result.ErrorMessage);
                Assert.NotNull(result.Value);
                Assert.True(result.Value.Pages.Count > 0);
            });
        }
    }
}
