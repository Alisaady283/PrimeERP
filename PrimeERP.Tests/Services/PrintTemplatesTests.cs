using PrimeERP.Domain.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using PrimeERP.Platform.Permissions;
using PrimeERP.Domain.Enums;
using PrimeERP.Domain.Results;
using PrimeERP.Data.Repositories;
using PrimeERP.Platform.Settings;
using PrimeERP.Platform.Localization;
using PrimeERP.UI.Services;
using PrimeERP.Application;
using PrimeERP.Application.Services;
using PrimeERP.Application.Services.Accounting;
using PrimeERP.Application.DTOs.Accounting;
using PrimeERP.Application.Services.Print;
using PrimeERP.Application.Services.Print.Templates;
using Xunit;

namespace PrimeERP.Tests.Services
{
    /// <summary>قاعدة بيانات خاصة معزولة لكل اختبار — تُنشئ حسابات وقيوداً حقيقية (نفس سبب JournalServiceTests).</summary>
    public class PrintTemplatesTests : IDisposable
    {
        private readonly TestDatabaseFixture _db = new();
        private readonly AccountService _accounts = new();
        private readonly JournalService _journal = new();

        public PrintTemplatesTests()
        {
            AppSession.DevMode = true;
            ServiceLocator.Register<IJournalService>(_journal);
        }

        public void Dispose() => _db.Dispose();

        private string CreateLeaf(string parentCode, string name) =>
            _accounts.Create(new CreateAccountDto { ParentId = AccountRepository.GetByCode(parentCode).Id, Name = name, IsLeaf = true }).Value.Code;

        private void Post(string debitCode, string creditCode, string date, decimal amount)
        {
            var dto = new CreateJournalDto
            {
                EntryDate   = DateTime.Parse(date),
                Description = "قيد اختبار طباعة",
                Source      = "Manual",
                Lines = new List<CreateJournalLineDto>
                {
                    new() { LineNo = 1, AccountCode = debitCode,  Debit = amount, Credit = 0m },
                    new() { LineNo = 2, AccountCode = creditCode, Debit = 0m,     Credit = amount }
                }
            };
            var created = _journal.Create(dto);
            Assert.True(created.IsSuccess, created.ErrorMessage);
            var posted = _journal.Post(created.Value.Id);
            Assert.True(posted.IsSuccess, posted.ErrorMessage);
            _accounts.RecalculateAllBalances();
        }

        // ===================== كشف الحساب =====================

        [Fact]
        public void AccountStatementPrint_BuildsNonEmptyFixedDocument()
        {
            var cash = CreateLeaf("1240", "نقدية اختبار الطباعة");
            var revenue = CreateLeaf("4100", "إيراد اختبار الطباعة");
            Post(cash, revenue, "2026-01-05", 500m);
            Post(cash, revenue, "2026-01-10", 300m);

            var account = _accounts.GetByCode(cash).Value;
            var statement = _accounts.GetStatement(cash, new DateTime(2026, 1, 1), new DateTime(2026, 1, 31)).Value;

            StaThreadHelper.Run(() =>
            {
                var printable = AccountStatementPrintTemplate.Build(account, statement, new DateTime(2026, 1, 1), new DateTime(2026, 1, 31));
                var service = new PrintService();

                var result = service.Build(printable);

                Assert.True(result.IsSuccess, result.ErrorMessage);
                Assert.True(result.Value.Pages.Count > 0);
            });
        }

        [Fact]
        public void AccountStatementPrint_RunningBalance_IsSequential()
        {
            var cash = CreateLeaf("1240", "نقدية تسلسل");
            var revenue = CreateLeaf("4100", "إيراد تسلسل");
            Post(cash, revenue, "2026-01-05", 500m);
            Post(cash, revenue, "2026-01-10", 300m);

            var statement = _accounts.GetStatement(cash, new DateTime(2026, 1, 1), new DateTime(2026, 1, 31)).Value;

            Assert.Equal("Opening", statement[0].SourceType);
            Assert.Equal(0m, statement[0].RunningBalance);
            Assert.Equal(500m, statement[1].RunningBalance);
            Assert.Equal(800m, statement[2].RunningBalance);
        }

        // ===================== ميزان المراجعة =====================

        [Fact]
        public void TrialBalancePrint_IsLandscape_AndBuildsNonEmptyFixedDocument()
        {
            var cash = CreateLeaf("1240", "نقدية ميزان");
            var revenue = CreateLeaf("4100", "إيراد ميزان");
            Post(cash, revenue, "2026-01-05", 1000m);

            var trialBalance = _journal.GetTrialBalance(new DateTime(2026, 1, 1), new DateTime(2026, 1, 31)).Value;

            StaThreadHelper.Run(() =>
            {
                var printable = TrialBalancePrintTemplate.Build(trialBalance, new DateTime(2026, 1, 1), new DateTime(2026, 1, 31), includesDrafts: false);

                Assert.Equal(PrintOrientation.Landscape, printable.Orientation);

                var service = new PrintService();
                var result = service.Build(printable);

                Assert.True(result.IsSuccess, result.ErrorMessage);
                Assert.True(result.Value.Pages.Count > 0);
            });
        }

        [Fact]
        public void TrialBalancePrint_Totals_AreCorrect()
        {
            var cash = CreateLeaf("1240", "نقدية إجماليات");
            var revenue = CreateLeaf("4100", "إيراد إجماليات");
            Post(cash, revenue, "2026-01-05", 700m);

            var trialBalance = _journal.GetTrialBalance(new DateTime(2026, 1, 1), new DateTime(2026, 1, 31)).Value;

            Assert.Equal(trialBalance.Sum(l => l.ClosingDebit), trialBalance.Sum(l => l.ClosingCredit));

            var cashLine = trialBalance.Single(l => l.Code == cash);
            Assert.Equal(700m, cashLine.ClosingDebit);
        }

        [Fact]
        public void TrialBalancePrint_Unbalanced_ShowsWarningCallout()
        {
            // ميزان غير متوازن مبني يدوياً (بدل GetTrialBalance الذي ينتج ميزاناً متوازناً دائماً من قيود
            // حقيقية) — يختبر مسار التحذير في القالب نفسه بمعزل عن صحة الخدمة.
            var lines = new List<TrialBalanceLine>
            {
                new() { Code = "1240", Name = "نقدية", Level = 3, Type = AccountType.Asset,   IsLeaf = true, ClosingDebit = 500m, ClosingCredit = 0m },
                new() { Code = "4100", Name = "إيراد",  Level = 3, Type = AccountType.Revenue, IsLeaf = true, ClosingDebit = 0m,   ClosingCredit = 300m }
            };

            var printable = TrialBalancePrintTemplate.Build(lines, new DateTime(2026, 1, 1), new DateTime(2026, 1, 31), includesDrafts: false);
            var sections = printable.BuildSections();

            Assert.Contains(sections, s => s.Type == PrintSectionType.Callout && s.Variant == StatusVariant.Danger);
        }
    }
}
