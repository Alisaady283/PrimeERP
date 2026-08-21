using System;
using System.Data.Common;
using System.Linq;
using PrimeERP.Platform.Permissions;
using PrimeERP.Domain.Enums;
using PrimeERP.Domain.Results;
using PrimeERP.Data.Repositories;
using PrimeERP.Platform.Settings;
using PrimeERP.Platform.Permissions;
using PrimeERP.Domain.Entities;
using PrimeERP.Platform.Localization;
using PrimeERP.UI.Services;
using PrimeERP.Application;
using PrimeERP.Application.Services;
using PrimeERP.Application.Services.Accounting;
using PrimeERP.Application.DTOs.Accounting;
using PrimeERP.Platform.Settings;
using Xunit;
using Db = PrimeERP.Data.Core.DbHelper;

namespace PrimeERP.Tests.Services
{
    /// <summary>
    /// قاعدة بيانات خاصة معزولة لكل اختبار (لا [Collection("Database")] المشتركة) — نفس سبب AccountServiceTests:
    /// اختبارات تراكب السنوات المالية وتوليد الفترات تفترض "لا توجد سنوات أخرى" مسبقاً.
    /// </summary>
    public class FiscalPeriodServiceTests : IDisposable
    {
        private readonly TestDatabaseFixture _db = new();
        private readonly FiscalPeriodService _service = new();
        private readonly AccountService _accounts = new();

        public FiscalPeriodServiceTests()
        {
            AppSession.DevMode = true;
            ServiceLocator.Register<IJournalService>(new JournalService());
        }

        public void Dispose() => _db.Dispose();

        private static int RevenueRootId() => AccountRepository.GetByCode("4100").Id;
        private static int ExpenseRootId() => AccountRepository.GetByCode("5100").Id;

        private static void SeedPostedEntry(string date, params (string Code, decimal Debit, decimal Credit)[] lines) =>
            SeedEntry(date, posted: true, lines);

        private static void SeedEntry(string date, bool posted, params (string Code, decimal Debit, decimal Credit)[] lines)
        {
            var id = Db.RunTransaction((conn, tx) =>
            {
                var entry = new JournalEntry { EntryNo = $"TEST-{Guid.NewGuid():N}", EntryDate = date, Description = "test", Source = "test" };
                var newId = JournalRepository.InsertHeader(conn, tx, entry);
                int lineNo = 1;
                foreach (var l in lines)
                    JournalRepository.InsertLine(conn, tx, newId, lineNo++, new JournalLine { AccountCode = l.Code, Debit = l.Debit, Credit = l.Credit });
                return newId;
            });
            if (posted) JournalRepository.SetPosted(id, true);
        }

        // ===================== CreateYear =====================

        [Theory]
        [InlineData(12, 12)]
        [InlineData(4, 4)]
        [InlineData(6, 6)]
        [InlineData(1, 1)]
        public void CreateYear_ValidPeriodsCount_SplitsEvenly(int periodsCount, int expectedPeriods)
        {
            var result = _service.CreateYear(new DateTime(2026, 1, 1), periodsCount);

            Assert.True(result.IsSuccess, result.ErrorMessage);
            Assert.Equal(expectedPeriods, result.Value.Periods.Count);
            Assert.Equal(new DateTime(2026, 1, 1), result.Value.Periods.First().StartDate);
            Assert.Equal(new DateTime(2026, 12, 31), result.Value.Periods.Last().EndDate);

            for (int i = 0; i < result.Value.Periods.Count - 1; i++)
                Assert.Equal(result.Value.Periods[i].EndDate.AddDays(1), result.Value.Periods[i + 1].StartDate);
        }

        [Fact]
        public void CreateYear_InvalidPeriodsCount_Fails()
        {
            var result = _service.CreateYear(new DateTime(2026, 1, 1), 5);

            Assert.False(result.IsSuccess);
            Assert.Equal(ErrorCode.ValidationFailed, result.ErrorCode);
        }

        [Fact]
        public void CreateYear_Overlapping_Fails()
        {
            var first = _service.CreateYear(new DateTime(2026, 1, 1), 12);
            Assert.True(first.IsSuccess, first.ErrorMessage);

            var overlapping = _service.CreateYear(new DateTime(2026, 6, 1), 12);

            Assert.False(overlapping.IsSuccess);
            Assert.Equal(ErrorCode.Conflict, overlapping.ErrorCode);
        }

        [Fact]
        public void CreateYear_NonCalendarStartMonth_PeriodsStartMidYear()
        {
            var result = _service.CreateYear(new DateTime(2026, 7, 1), 4);

            Assert.True(result.IsSuccess, result.ErrorMessage);
            Assert.Equal(new DateTime(2026, 7, 1), result.Value.StartDate);
            Assert.Equal(new DateTime(2027, 6, 30), result.Value.EndDate);
            Assert.Equal(new DateTime(2026, 10, 1), result.Value.Periods[1].StartDate);
        }

        [Fact]
        public void CreateYear_FirstYear_AutoSetsCurrent()
        {
            var result = _service.CreateYear(new DateTime(2026, 1, 1), 12);
            Assert.True(result.IsSuccess);
            Assert.True(result.Value.IsCurrent);

            var current = _service.GetCurrentYear();
            Assert.True(current.IsSuccess, current.ErrorMessage);
            Assert.Equal(result.Value.Id, current.Value.Id);
        }

        [Fact]
        public void SetCurrent_ClosedYear_Fails()
        {
            var year = _service.CreateYear(new DateTime(2026, 1, 1), 12);
            Assert.True(year.IsSuccess);
            Db.RunTransaction((conn, tx) => FiscalPeriodRepository.SetYearClosed(conn, tx, year.Value.Id, DateTime.Now, "test", null));

            var result = _service.SetCurrent(year.Value.Id);

            Assert.False(result.IsSuccess);
        }

        // ===================== IsOpen =====================

        [Fact]
        public void IsOpen_NoDefinedPeriod_DefaultsOpen()
        {
            Assert.True(_service.IsOpen(DateTime.Today));
        }

        [Fact]
        public void IsOpen_NoDefinedPeriod_RequireFiscalPeriodTrue_ReturnsClosed()
        {
            SettingsService.Instance.Set(SettingKeys.Financial.RequireFiscalPeriod, true);
            try
            {
                Assert.False(_service.IsOpen(DateTime.Today));
            }
            finally
            {
                SettingsService.Instance.Set(SettingKeys.Financial.RequireFiscalPeriod, false);
            }
        }

        [Fact]
        public void IsOpen_ClosedPeriod_ReturnsFalse()
        {
            var year = _service.CreateYear(new DateTime(2026, 1, 1), 12);
            var closeResult = _service.ClosePeriod(year.Value.Periods.First().Id);
            Assert.True(closeResult.IsSuccess, closeResult.ErrorMessage);

            Assert.False(_service.IsOpen(new DateTime(2026, 1, 15)));
        }

        [Fact]
        public void IsOpen_ClosedYear_ReturnsFalse()
        {
            var year = _service.CreateYear(new DateTime(2026, 1, 1), 12);
            Db.RunTransaction((conn, tx) => FiscalPeriodRepository.SetYearClosed(conn, tx, year.Value.Id, DateTime.Now, "test", null));

            Assert.False(_service.IsOpen(new DateTime(2026, 3, 1)));
        }

        // ===================== ClosePeriod / ReopenPeriod =====================

        [Fact]
        public void ClosePeriod_WithUnpostedEntries_Fails()
        {
            var year = _service.CreateYear(new DateTime(2026, 1, 1), 12);
            SeedEntry("2026-01-10", posted: false, ("1240", 100m, 0m), ("1230", 0m, 100m));

            var result = _service.ClosePeriod(year.Value.Periods.First().Id);

            Assert.False(result.IsSuccess);
        }

        [Fact]
        public void ClosePeriod_PreviousPeriodStillOpen_Fails()
        {
            var year = _service.CreateYear(new DateTime(2026, 1, 1), 12);

            var result = _service.ClosePeriod(year.Value.Periods[1].Id);

            Assert.False(result.IsSuccess);
        }

        [Fact]
        public void ClosePeriod_Sequential_Succeeds()
        {
            var year = _service.CreateYear(new DateTime(2026, 1, 1), 4);

            var p1 = _service.ClosePeriod(year.Value.Periods[0].Id);
            Assert.True(p1.IsSuccess, p1.ErrorMessage);

            var p2 = _service.ClosePeriod(year.Value.Periods[1].Id);
            Assert.True(p2.IsSuccess, p2.ErrorMessage);
        }

        [Fact]
        public void ReopenPeriod_LaterPeriodStillClosed_Fails()
        {
            var year = _service.CreateYear(new DateTime(2026, 1, 1), 4);
            _service.ClosePeriod(year.Value.Periods[0].Id);
            _service.ClosePeriod(year.Value.Periods[1].Id);

            var result = _service.ReopenPeriod(year.Value.Periods[0].Id);

            Assert.False(result.IsSuccess);
        }

        [Fact]
        public void ReopenPeriod_LastClosedPeriod_Succeeds()
        {
            var year = _service.CreateYear(new DateTime(2026, 1, 1), 4);
            _service.ClosePeriod(year.Value.Periods[0].Id);
            _service.ClosePeriod(year.Value.Periods[1].Id);

            var result = _service.ReopenPeriod(year.Value.Periods[1].Id);

            Assert.True(result.IsSuccess, result.ErrorMessage);
        }

        // ===================== CloseYear / ReopenYear =====================

        [Fact]
        public void CloseYear_BuildsBalancedClosingEntry_NetProfitEqualsRevenueMinusExpense()
        {
            SettingsService.Instance.Set(SettingKeys.Accounts.RetainedEarnings, "3200");

            var year = _service.CreateYear(new DateTime(2026, 1, 1), 1);
            Assert.True(year.IsSuccess);

            var revenueAccount = _accounts.Create(new CreateAccountDto { ParentId = RevenueRootId(), Name = "إيراد اختباري", IsLeaf = true });
            var expenseAccount = _accounts.Create(new CreateAccountDto { ParentId = ExpenseRootId(), Name = "مصروف اختباري", IsLeaf = true });
            Assert.True(revenueAccount.IsSuccess);
            Assert.True(expenseAccount.IsSuccess);

            SeedPostedEntry("2026-01-05", (revenueAccount.Value.Code, 0m, 1000m), ("1240", 1000m, 0m));
            SeedPostedEntry("2026-01-10", (expenseAccount.Value.Code, 400m, 0m), ("1240", 0m, 400m));
            _accounts.RecalculateAllBalances();

            var closePeriod = _service.ClosePeriod(year.Value.Periods.Single().Id);
            Assert.True(closePeriod.IsSuccess, closePeriod.ErrorMessage);

            var closeYear = _service.CloseYear(year.Value.Id);
            Assert.True(closeYear.IsSuccess, closeYear.ErrorMessage);

            var closedYear = FiscalPeriodRepository.GetYearById(year.Value.Id);
            Assert.True(closedYear.IsClosed);
            Assert.NotNull(closedYear.ClosingEntryId);

            var lines = JournalRepository.GetLines(closedYear.ClosingEntryId.Value);
            Assert.Equal(lines.Sum(l => l.Debit), lines.Sum(l => l.Credit));

            var revenueLine = lines.Single(l => l.AccountCode == revenueAccount.Value.Code);
            Assert.Equal(1000m, revenueLine.Debit);
            Assert.Equal(0m, revenueLine.Credit);

            var expenseLine = lines.Single(l => l.AccountCode == expenseAccount.Value.Code);
            Assert.Equal(400m, expenseLine.Credit);
            Assert.Equal(0m, expenseLine.Debit);

            var retainedLine = lines.Single(l => l.AccountCode == "3200");
            Assert.Equal(600m, retainedLine.Credit);
            Assert.Equal(0m, retainedLine.Debit);
        }

        [Fact]
        public void ReopenYear_UnpostsAndDeletesClosingEntry_ReopensYear()
        {
            SettingsService.Instance.Set(SettingKeys.Accounts.RetainedEarnings, "3200");

            var year = _service.CreateYear(new DateTime(2026, 1, 1), 1);
            var revenueAccount = _accounts.Create(new CreateAccountDto { ParentId = RevenueRootId(), Name = "إيراد 2", IsLeaf = true });
            Assert.True(revenueAccount.IsSuccess);

            SeedPostedEntry("2026-01-05", (revenueAccount.Value.Code, 0m, 500m), ("1240", 500m, 0m));
            _accounts.RecalculateAllBalances();

            _service.ClosePeriod(year.Value.Periods.Single().Id);
            var closeResult = _service.CloseYear(year.Value.Id);
            Assert.True(closeResult.IsSuccess, closeResult.ErrorMessage);

            var closingEntryId = FiscalPeriodRepository.GetYearById(year.Value.Id).ClosingEntryId;
            Assert.NotNull(closingEntryId);

            var reopenResult = _service.ReopenYear(year.Value.Id);
            Assert.True(reopenResult.IsSuccess, reopenResult.ErrorMessage);

            var reopenedYear = FiscalPeriodRepository.GetYearById(year.Value.Id);
            Assert.False(reopenedYear.IsClosed);
            Assert.Null(reopenedYear.ClosingEntryId);
            Assert.Null(JournalRepository.GetById(closingEntryId.Value));
        }

        // ===================== الصلاحيات =====================

        [Fact]
        public void CreateYear_WithoutPermission_Fails()
        {
            AppSession.DevMode = false;
            try
            {
                var result = _service.CreateYear(new DateTime(2026, 1, 1), 12);
                Assert.False(result.IsSuccess);
                Assert.Equal(ErrorCode.Unauthorized, result.ErrorCode);
            }
            finally { AppSession.DevMode = true; }
        }

        [Fact]
        public void SetCurrent_WithoutPermission_Fails()
        {
            var year = _service.CreateYear(new DateTime(2026, 1, 1), 12);
            AppSession.DevMode = false;
            try
            {
                var result = _service.SetCurrent(year.Value.Id);
                Assert.False(result.IsSuccess);
                Assert.Equal(ErrorCode.Unauthorized, result.ErrorCode);
            }
            finally { AppSession.DevMode = true; }
        }

        [Fact]
        public void ClosePeriod_WithoutPermission_Fails()
        {
            var year = _service.CreateYear(new DateTime(2026, 1, 1), 12);
            AppSession.DevMode = false;
            try
            {
                var result = _service.ClosePeriod(year.Value.Periods.First().Id);
                Assert.False(result.IsSuccess);
                Assert.Equal(ErrorCode.Unauthorized, result.ErrorCode);
            }
            finally { AppSession.DevMode = true; }
        }

        [Fact]
        public void ReopenPeriod_WithoutPermission_Fails()
        {
            var year = _service.CreateYear(new DateTime(2026, 1, 1), 12);
            _service.ClosePeriod(year.Value.Periods.First().Id);
            AppSession.DevMode = false;
            try
            {
                var result = _service.ReopenPeriod(year.Value.Periods.First().Id);
                Assert.False(result.IsSuccess);
                Assert.Equal(ErrorCode.Unauthorized, result.ErrorCode);
            }
            finally { AppSession.DevMode = true; }
        }

        [Fact]
        public void CloseYear_WithoutPermission_Fails()
        {
            var year = _service.CreateYear(new DateTime(2026, 1, 1), 1);
            _service.ClosePeriod(year.Value.Periods.Single().Id);
            AppSession.DevMode = false;
            try
            {
                var result = _service.CloseYear(year.Value.Id);
                Assert.False(result.IsSuccess);
                Assert.Equal(ErrorCode.Unauthorized, result.ErrorCode);
            }
            finally { AppSession.DevMode = true; }
        }

        [Fact]
        public void ReopenYear_WithoutPermission_Fails()
        {
            SettingsService.Instance.Set(SettingKeys.Accounts.RetainedEarnings, "3200");
            var year = _service.CreateYear(new DateTime(2026, 1, 1), 1);
            _service.ClosePeriod(year.Value.Periods.Single().Id);
            _service.CloseYear(year.Value.Id);

            AppSession.DevMode = false;
            try
            {
                var result = _service.ReopenYear(year.Value.Id);
                Assert.False(result.IsSuccess);
                Assert.Equal(ErrorCode.Unauthorized, result.ErrorCode);
            }
            finally { AppSession.DevMode = true; }
        }
    }
}
