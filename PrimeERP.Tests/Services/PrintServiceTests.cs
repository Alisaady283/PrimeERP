using PrimeERP.UI.Services;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Domain.Entities;
using Xunit;

namespace PrimeERP.Tests.Services
{
    /// <summary>الطباعة</summary>
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
            StaThreadHelper.Run(() =>
            {
                var printable = PrimeERP.Composition.Print.PrintDocuments.Report(new PrimeERP.Composition.Definitions.ReportResult
                {
                    Title = "قيد اختباري",
                    Columns = new() { new() { Header = "الحساب", Binding = "AccountCode", Width = 40 },
                                      new() { Header = "مدين", Binding = "Debit", Width = 30, Format = "N2" },
                                      new() { Header = "دائن", Binding = "Credit", Width = 30, Format = "N2" } },
                    Rows = new List<object>
                    {
                        new JournalLine { AccountCode = "1204", AccountName = "الصندوق", Debit = 100m, Credit = 0m },
                        new JournalLine { AccountCode = "41", AccountName = "إيرادات المبيعات", Debit = 0m, Credit = 100m }
                    }
                });

                var result = _service.Build(printable);

                Assert.True(result.IsSuccess, result.ErrorMessage);
                Assert.NotNull(result.Value);
                Assert.True(result.Value.Pages.Count > 0);
            });
        }
    }
}
