using PrimeERP.Data.Core;
using System;
using PrimeERP.Application.Services;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Platform.Permissions;
using PrimeERP.Domain.Enums;
using PrimeERP.Domain.Results;
using PrimeERP.Data.Repositories;
using PrimeERP.Platform.Settings;
using PrimeERP.Platform.Localization;
using PrimeERP.UI.Services;
using PrimeERP.Application.Services.Accounting;
using PrimeERP.Application.DTOs.Accounting;
using Xunit;
using PrimeERP.Application.Services.Admin;

namespace PrimeERP.Tests.Services
{
    /// <summary>قاعدة بيانات خاصة معزولة لكل</summary>
    public class JournalServiceTests : IDisposable
    {
        private readonly TestDatabaseFixture _db = new();
        private readonly IJournalService _service;
        private readonly IAccountService _accounts;
        private readonly ISettingsService _settings;
        private readonly IAccountRepository _accountRepo;
        private readonly IJournalRepository _journalRepo;
        private readonly IFiscalPeriodRepository _fiscalRepo;

        public JournalServiceTests()
        {
            AppSession.DevMode = true;
            _service = _db.Services.GetRequiredService<IJournalService>();
            _accounts = _db.Services.GetRequiredService<IAccountService>();
            _settings = _db.Services.GetRequiredService<ISettingsService>();
            _accountRepo = _db.Services.GetRequiredService<IAccountRepository>();
            _journalRepo = _db.Services.GetRequiredService<IJournalRepository>();
            _fiscalRepo = _db.Services.GetRequiredService<IFiscalPeriodRepository>();
        }

        public void Dispose() => _db.Dispose();

        private int AssetRootId() => _accountRepo.GetByCode("1204").Id;
        private int RevenueRootId() => _accountRepo.GetByCode("41").Id;
        private int ExpenseRootId() => _accountRepo.GetByCode("51").Id;

        private string CreateLeaf(int parentId, string name) =>
            _accounts.Create(new CreateAccountDto { ParentId = parentId, Name = name, IsLeaf = true }).Value.Code;

        private (string Cash, string Revenue) CreateCashAndRevenue() =>
            (CreateLeaf(AssetRootId(), "نقدية اختبار"), CreateLeaf(RevenueRootId(), "إيراد اختباري"));

        private static CreateJournalDto BuildDto(DateTime date, params (string Code, decimal Debit, decimal Credit)[] lines) => new()
        {
            EntryDate = date,
            Description = "قيد اختباري",
            Source = "Manual",
            Lines = lines.Select((l, i) => new CreateJournalLineDto { LineNo = i + 1, AccountCode = l.Code, Debit = l.Debit, Credit = l.Credit }).ToList()
        };


        [Fact]
        public void Create_Balanced_Succeeds_AndGeneratesEntryNo()
        {
            var (cash, revenue) = CreateCashAndRevenue();

            var result = _service.Create(BuildDto(new DateTime(2026, 1, 5), (cash, 500m, 0m), (revenue, 0m, 500m)));

            Assert.True(result.IsSuccess, result.ErrorMessage);
            Assert.False(string.IsNullOrWhiteSpace(result.Value.EntryNo));
            Assert.Equal(500m, result.Value.TotalDebit);
            Assert.Equal(500m, result.Value.TotalCredit);
        }

        [Fact]
        public void Create_Unbalanced_Fails_WithDifferenceInMessage()
        {
            var (cash, revenue) = CreateCashAndRevenue();

            var result = _service.Create(BuildDto(new DateTime(2026, 1, 5), (cash, 500m, 0m), (revenue, 0m, 300m)));

            Assert.False(result.IsSuccess);
            Assert.Equal(ErrorCode.ValidationFailed, result.ErrorCode);
        }

        [Fact]
        public void Create_SingleLine_Fails()
        {
            var (cash, _) = CreateCashAndRevenue();

            var result = _service.Create(BuildDto(new DateTime(2026, 1, 5), (cash, 500m, 0m)));

            Assert.False(result.IsSuccess);
        }

        [Fact]
        public void Create_ZeroAmounts_Fails()
        {
            var (cash, revenue) = CreateCashAndRevenue();

            var result = _service.Create(BuildDto(new DateTime(2026, 1, 5), (cash, 0m, 0m), (revenue, 0m, 0m)));

            Assert.False(result.IsSuccess);
        }

        [Fact]
        public void Create_LineWithBothDebitAndCredit_Fails()
        {
            var (cash, revenue) = CreateCashAndRevenue();

            var result = _service.Create(BuildDto(new DateTime(2026, 1, 5), (cash, 100m, 100m), (revenue, 0m, 200m)));

            Assert.False(result.IsSuccess);
        }

        [Fact]
        public void Create_UnknownAccount_Fails()
        {
            var (cash, _) = CreateCashAndRevenue();

            var result = _service.Create(BuildDto(new DateTime(2026, 1, 5), (cash, 100m, 0m), ("9999999", 0m, 100m)));

            Assert.False(result.IsSuccess);
        }

        [Fact]
        public void Create_NonLeafAccount_Fails()
        {
            var (cash, _) = CreateCashAndRevenue();

            var result = _service.Create(BuildDto(new DateTime(2026, 1, 5), (cash, 100m, 0m), ("41", 0m, 100m)));

            Assert.False(result.IsSuccess);
        }

        [Fact]
        public void Create_DuplicateAccount_Fails_ByDefault()
        {
            var (cash, _) = CreateCashAndRevenue();

            var result = _service.Create(BuildDto(new DateTime(2026, 1, 5), (cash, 50m, 0m), (cash, 0m, 50m)));

            Assert.False(result.IsSuccess);
        }

        [Fact]
        public void Create_DuplicateAccount_Succeeds_WhenSettingEnabled()
        {
            _settings.Set(SettingKeys.Financial.AllowDuplicateAccountInEntry, true);
            var (cash, _) = CreateCashAndRevenue();

            var result = _service.Create(BuildDto(new DateTime(2026, 1, 5), (cash, 50m, 0m), (cash, 0m, 50m)));

            Assert.True(result.IsSuccess, result.ErrorMessage);
        }

        [Fact]
        public void Create_InClosedPeriod_Fails()
        {
            var (cash, revenue) = CreateCashAndRevenue();
            var fiscal = _db.Services.GetRequiredService<IFiscalPeriodService>();
            var year = fiscal.CreateYear(new DateTime(2026, 1, 1), 1);
            fiscal.ClosePeriod(year.Value.Periods.Single().Id);

            var result = _service.Create(BuildDto(new DateTime(2026, 1, 15), (cash, 100m, 0m), (revenue, 0m, 100m)));

            Assert.False(result.IsSuccess);
        }

        [Fact]
        public void Create_PostsTheEntry_AndMovesBalances()
        {
            var (cash, revenue) = CreateCashAndRevenue();

            var result = _service.Create(BuildDto(new DateTime(2026, 1, 5), (cash, 500m, 0m), (revenue, 0m, 500m)));
            Assert.True(result.IsSuccess);

            Assert.Equal(500m, _accountRepo.GetByCode(cash).Balance);
            Assert.Equal(-500m, _accountRepo.GetByCode(revenue).Balance);
        }

        [Fact]
        public void Create_NumbersLinesSequentiallyFromOne()
        {
            var (cash, revenue) = CreateCashAndRevenue();

            var result = _service.Create(BuildDto(new DateTime(2026, 1, 5), (cash, 500m, 0m), (revenue, 0m, 500m)));
            Assert.True(result.IsSuccess);

            var lines = _journalRepo.GetLines(result.Value.Id);
            Assert.Equal(new[] { 1, 2 }, lines.Select(l => l.LineNo).OrderBy(n => n));
        }


        [Fact]
        public void Update_ReplacesLines_AndFollowsBalances()
        {
            var (cash, revenue) = CreateCashAndRevenue();
            var created = _service.Create(BuildDto(new DateTime(2026, 1, 5), (cash, 500m, 0m), (revenue, 0m, 500m)));

            var updateDto = BuildDto(new DateTime(2026, 1, 6), (cash, 700m, 0m), (revenue, 0m, 700m));
            updateDto.Id = created.Value.Id;

            var result = _service.Update(updateDto);
            Assert.True(result.IsSuccess, result.ErrorMessage);

            var reloaded = _service.GetById(created.Value.Id);
            Assert.Equal(700m, reloaded.Value.TotalDebit);
            Assert.Equal(2, reloaded.Value.Lines.Count);
        }


        [Fact]
        public void Update_NeverChangesEntryNo()
        {
            var (cash, revenue) = CreateCashAndRevenue();
            var created = _service.Create(BuildDto(new DateTime(2026, 1, 5), (cash, 500m, 0m), (revenue, 0m, 500m)));
            var originalNo = created.Value.EntryNo;

            var updateDto = BuildDto(new DateTime(2026, 1, 6), (cash, 700m, 0m), (revenue, 0m, 700m));
            updateDto.Id = created.Value.Id;
            _service.Update(updateDto);

            Assert.Equal(originalNo, _journalRepo.GetById(created.Value.Id).EntryNo);
        }


        [Fact]
        public void Delete_RemovesTheEntry_AndRestoresBalances()
        {
            var (cash, revenue) = CreateCashAndRevenue();
            var created = _service.Create(BuildDto(new DateTime(2026, 1, 5), (cash, 500m, 0m), (revenue, 0m, 500m)));

            var result = _service.Delete(created.Value.Id);

            Assert.True(result.IsSuccess, result.ErrorMessage);
            Assert.Null(_journalRepo.GetById(created.Value.Id));
        }



        [Fact]
        public void Creating_UpdatesAllAccountBalancesCorrectly()
        {
            var (cash, revenue) = CreateCashAndRevenue();
            var created = _service.Create(BuildDto(new DateTime(2026, 1, 5), (cash, 500m, 0m), (revenue, 0m, 500m)));

            Assert.Equal(500m, _accountRepo.GetByCode(cash).Balance);
            Assert.Equal(-500m, _accountRepo.GetByCode(revenue).Balance);
        }

        [Fact]
        public void Post_AlreadyPosted_Fails()
        {
            var (cash, revenue) = CreateCashAndRevenue();
            var created = _service.Create(BuildDto(new DateTime(2026, 1, 5), (cash, 500m, 0m), (revenue, 0m, 500m)));
            _service.Post(created.Value.Id);

            var result = _service.Post(created.Value.Id);

            Assert.False(result.IsSuccess);
        }

        [Fact]
        public void Post_InClosedPeriod_Fails()
        {
            var (cash, revenue) = CreateCashAndRevenue();
            var created = _service.Create(BuildDto(new DateTime(2026, 1, 5), (cash, 500m, 0m), (revenue, 0m, 500m)));

            var fiscal = _db.Services.GetRequiredService<IFiscalPeriodService>();
            var year = fiscal.CreateYear(new DateTime(2026, 1, 1), 1);

            DbContextFactory.RunTransaction(db => _fiscalRepo.SetPeriodClosed(db, year.Value.Periods.Single().Id, DateTime.Now, "test"));

            var result = _service.Post(created.Value.Id);

            Assert.False(result.IsSuccess);
        }

        [Fact]
        public void Unpost_ReversesBalancesCorrectly()
        {
            var (cash, revenue) = CreateCashAndRevenue();
            var created = _service.Create(BuildDto(new DateTime(2026, 1, 5), (cash, 500m, 0m), (revenue, 0m, 500m)));
            _service.Post(created.Value.Id);

            var result = _service.Unpost(created.Value.Id);
            Assert.True(result.IsSuccess, result.ErrorMessage);

            Assert.Equal(0m, _accountRepo.GetByCode(cash).Balance);
            Assert.Equal(0m, _accountRepo.GetByCode(revenue).Balance);
        }

        [Fact]
        public void Unpost_ClosingEntry_Fails()
        {
            _settings.Set(SettingKeys.Accounts.RetainedEarnings, "32");
            var cash = CreateLeaf(AssetRootId(), "نقدية للإقفال");
            var revenue = CreateLeaf(RevenueRootId(), "إيراد للإقفال");

            var fiscal = _db.Services.GetRequiredService<IFiscalPeriodService>();
            var year = fiscal.CreateYear(new DateTime(2026, 1, 1), 1);

            var entry = _service.Create(BuildDto(new DateTime(2026, 1, 5), (cash, 300m, 0m), (revenue, 0m, 300m)));
            Assert.True(entry.IsSuccess, entry.ErrorMessage);
            _service.Post(entry.Value.Id);
            _accounts.RecalculateAllBalances();

            fiscal.ClosePeriod(year.Value.Periods.Single().Id);
            var closeResult = fiscal.CloseYear(year.Value.Id);
            Assert.True(closeResult.IsSuccess, closeResult.ErrorMessage);

            var closingEntryId = _fiscalRepo.GetYearById(year.Value.Id).ClosingEntryId;
            Assert.NotNull(closingEntryId);

            var result = _service.Unpost(closingEntryId.Value);

            Assert.False(result.IsSuccess);
        }

        [Fact]
        public void PostBatch_OneFailure_RollsBackAll_BalancesUnchanged()
        {
            var (cash, revenue) = CreateCashAndRevenue();
            var valid = _service.Create(BuildDto(new DateTime(2026, 1, 5), (cash, 200m, 0m), (revenue, 0m, 200m)));
            var invalid = _service.Create(BuildDto(new DateTime(2026, 1, 6), (cash, 300m, 0m), (revenue, 0m, 300m)));
            Assert.True(valid.IsSuccess);
            Assert.True(invalid.IsSuccess);

            _service.Unpost(valid.Value.Id);
            var cashBalanceBeforeBatch = _accountRepo.GetByCode(cash).Balance;

            var result = _service.PostBatch(new() { valid.Value.Id, invalid.Value.Id });

            Assert.True(result.IsSuccess);
            Assert.Equal(0, result.Value.SuccessCount);
            Assert.True(result.Value.FailedCount > 0);

            Assert.False(_journalRepo.GetById(valid.Value.Id).IsPosted);
            Assert.Equal(cashBalanceBeforeBatch, _accountRepo.GetByCode(cash).Balance);
        }


        [Fact]
        public void TrialBalance_TotalDebitEqualsTotalCredit()
        {
            var (cash, revenue) = CreateCashAndRevenue();
            var expense = CreateLeaf(ExpenseRootId(), "مصروف اختباري");

            Post(BuildDto(new DateTime(2026, 1, 5), (cash, 1000m, 0m), (revenue, 0m, 1000m)));
            Post(BuildDto(new DateTime(2026, 1, 10), (expense, 300m, 0m), (cash, 0m, 300m)));

            var result = _service.GetTrialBalance(new DateTime(2026, 1, 1), new DateTime(2026, 1, 31));

            Assert.True(result.IsSuccess, result.ErrorMessage);
            Assert.Equal(result.Value.Sum(l => l.ClosingDebit), result.Value.Sum(l => l.ClosingCredit));
        }

        [Fact]
        public void TrialBalance_OpeningBalance_ComputedFromBeforeFrom()
        {
            var (cash, revenue) = CreateCashAndRevenue();

            Post(BuildDto(new DateTime(2026, 1, 5), (cash, 1000m, 0m), (revenue, 0m, 1000m)));

            var result = _service.GetTrialBalance(new DateTime(2026, 2, 1), new DateTime(2026, 2, 28));
            Assert.True(result.IsSuccess, result.ErrorMessage);

            var cashLine = result.Value.Single(l => l.Code == cash);
            Assert.Equal(1000m, cashLine.OpeningDebit);
            Assert.Equal(0m, cashLine.PeriodDebit);
            Assert.Equal(0m, cashLine.PeriodCredit);
        }

        [Fact]
        public void TrialBalance_PostedOnlyTrue_ExcludesDrafts()
        {
            var (cash, revenue) = CreateCashAndRevenue();
            var entry = _service.Create(BuildDto(new DateTime(2026, 1, 5), (cash, 1000m, 0m), (revenue, 0m, 1000m)));
            _service.Unpost(entry.Value.Id);

            var result = _service.GetTrialBalance(new DateTime(2026, 1, 1), new DateTime(2026, 1, 31), includeZero: true, postedOnly: true);

            Assert.True(result.IsSuccess, result.ErrorMessage);
            var cashLine = result.Value.SingleOrDefault(l => l.Code == cash);
            Assert.True(cashLine == null || cashLine.PeriodDebit == 0m);
        }

        [Fact]
        public void TrialBalance_NormalBalanceSide_CorrectPerAccountType()
        {
            var (cash, revenue) = CreateCashAndRevenue();
            Post(BuildDto(new DateTime(2026, 1, 5), (cash, 1000m, 0m), (revenue, 0m, 1000m)));

            var result = _service.GetTrialBalance(new DateTime(2026, 1, 1), new DateTime(2026, 1, 31));
            Assert.True(result.IsSuccess, result.ErrorMessage);

            var cashLine = result.Value.Single(l => l.Code == cash);
            Assert.True(cashLine.ClosingDebit > 0 && cashLine.ClosingCredit == 0); // أصل: طبيعته مدينة

            var revenueLine = result.Value.Single(l => l.Code == revenue);
            Assert.True(revenueLine.ClosingCredit > 0 && revenueLine.ClosingDebit == 0); // إيراد: طبيعته دائنة
        }

        private void Post(CreateJournalDto dto)
        {
            var created = _service.Create(dto);
            Assert.True(created.IsSuccess, created.ErrorMessage);
        }


        [Fact]
        public void Create_WithoutPermission_Fails()
        {
            var (cash, revenue) = CreateCashAndRevenue();
            AppSession.DevMode = false;
            try
            {
                var result = _service.Create(BuildDto(new DateTime(2026, 1, 5), (cash, 500m, 0m), (revenue, 0m, 500m)));
                Assert.False(result.IsSuccess);
                Assert.Equal(ErrorCode.Unauthorized, result.ErrorCode);
            }
            finally { AppSession.DevMode = true; }
        }

        [Fact]
        public void Update_WithoutPermission_Fails()
        {
            var (cash, revenue) = CreateCashAndRevenue();
            var created = _service.Create(BuildDto(new DateTime(2026, 1, 5), (cash, 500m, 0m), (revenue, 0m, 500m)));

            AppSession.DevMode = false;
            try
            {
                var updateDto = BuildDto(new DateTime(2026, 1, 6), (cash, 700m, 0m), (revenue, 0m, 700m));
                updateDto.Id = created.Value.Id;
                var result = _service.Update(updateDto);
                Assert.False(result.IsSuccess);
                Assert.Equal(ErrorCode.Unauthorized, result.ErrorCode);
            }
            finally { AppSession.DevMode = true; }
        }

        [Fact]
        public void Delete_WithoutPermission_Fails()
        {
            var (cash, revenue) = CreateCashAndRevenue();
            var created = _service.Create(BuildDto(new DateTime(2026, 1, 5), (cash, 500m, 0m), (revenue, 0m, 500m)));

            AppSession.DevMode = false;
            try
            {
                var result = _service.Delete(created.Value.Id);
                Assert.False(result.IsSuccess);
                Assert.Equal(ErrorCode.Unauthorized, result.ErrorCode);
            }
            finally { AppSession.DevMode = true; }
        }

        [Fact]
        public void Post_WithoutPermission_Fails()
        {
            var (cash, revenue) = CreateCashAndRevenue();
            var created = _service.Create(BuildDto(new DateTime(2026, 1, 5), (cash, 500m, 0m), (revenue, 0m, 500m)));

            AppSession.DevMode = false;
            try
            {
                var result = _service.Post(created.Value.Id);
                Assert.False(result.IsSuccess);
                Assert.Equal(ErrorCode.Unauthorized, result.ErrorCode);
            }
            finally { AppSession.DevMode = true; }
        }

        [Fact]
        public void Unpost_WithoutPermission_Fails()
        {
            var (cash, revenue) = CreateCashAndRevenue();
            var created = _service.Create(BuildDto(new DateTime(2026, 1, 5), (cash, 500m, 0m), (revenue, 0m, 500m)));
            _service.Post(created.Value.Id);

            AppSession.DevMode = false;
            try
            {
                var result = _service.Unpost(created.Value.Id);
                Assert.False(result.IsSuccess);
                Assert.Equal(ErrorCode.Unauthorized, result.ErrorCode);
            }
            finally { AppSession.DevMode = true; }
        }
    }
}
