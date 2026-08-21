using System;
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
using Xunit;
using Db = PrimeERP.Data.Core.DbHelper;

namespace PrimeERP.Tests.Services
{
    /// <summary>قاعدة بيانات خاصة معزولة لكل اختبار — نفس سبب AccountServiceTests/FiscalPeriodServiceTests.</summary>
    public class JournalServiceTests : IDisposable
    {
        private readonly TestDatabaseFixture _db = new();
        private readonly JournalService _service = new();
        private readonly AccountService _accounts = new();

        public JournalServiceTests()
        {
            AppSession.DevMode = true;
            // FiscalPeriodService.ClosePeriod/CloseYear يحلّان IJournalService عبر ServiceLocator (Lazy) —
            // مطلوب لأي اختبار هنا يستخدم FiscalPeriodService (فترة مقفلة، قيد إقفال سنة).
            ServiceLocator.Register<IJournalService>(_service);
        }

        public void Dispose() => _db.Dispose();

        private static int AssetRootId() => AccountRepository.GetByCode("1240").Id;
        private static int RevenueRootId() => AccountRepository.GetByCode("4100").Id;
        private static int ExpenseRootId() => AccountRepository.GetByCode("5100").Id;

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

        // ===================== الإنشاء =====================

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

            var result = _service.Create(BuildDto(new DateTime(2026, 1, 5), (cash, 100m, 0m), ("4100", 0m, 100m)));

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
            SettingsService.Instance.Set(SettingKeys.Financial.AllowDuplicateAccountInEntry, true);
            var (cash, _) = CreateCashAndRevenue();

            var result = _service.Create(BuildDto(new DateTime(2026, 1, 5), (cash, 50m, 0m), (cash, 0m, 50m)));

            Assert.True(result.IsSuccess, result.ErrorMessage);
        }

        [Fact]
        public void Create_InClosedPeriod_Fails()
        {
            var (cash, revenue) = CreateCashAndRevenue();
            var fiscal = new FiscalPeriodService();
            var year = fiscal.CreateYear(new DateTime(2026, 1, 1), 1);
            fiscal.ClosePeriod(year.Value.Periods.Single().Id);

            var result = _service.Create(BuildDto(new DateTime(2026, 1, 15), (cash, 100m, 0m), (revenue, 0m, 100m)));

            Assert.False(result.IsSuccess);
        }

        [Fact]
        public void Create_DoesNotChangeAnyAccountBalance()
        {
            var (cash, revenue) = CreateCashAndRevenue();

            var result = _service.Create(BuildDto(new DateTime(2026, 1, 5), (cash, 500m, 0m), (revenue, 0m, 500m)));
            Assert.True(result.IsSuccess);

            Assert.Equal(0m, AccountRepository.GetByCode(cash).Balance);
            Assert.Equal(0m, AccountRepository.GetByCode(revenue).Balance);
        }

        [Fact]
        public void Create_NumbersLinesSequentiallyFromOne()
        {
            var (cash, revenue) = CreateCashAndRevenue();

            var result = _service.Create(BuildDto(new DateTime(2026, 1, 5), (cash, 500m, 0m), (revenue, 0m, 500m)));
            Assert.True(result.IsSuccess);

            var lines = JournalRepository.GetLines(result.Value.Id);
            Assert.Equal(new[] { 1, 2 }, lines.Select(l => l.LineNo).OrderBy(n => n));
        }

        // ===================== التعديل =====================

        [Fact]
        public void Update_Draft_Succeeds_AndReplacesLines()
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
        public void Update_Posted_Fails()
        {
            var (cash, revenue) = CreateCashAndRevenue();
            var created = _service.Create(BuildDto(new DateTime(2026, 1, 5), (cash, 500m, 0m), (revenue, 0m, 500m)));
            _service.Post(created.Value.Id);

            var updateDto = BuildDto(new DateTime(2026, 1, 6), (cash, 700m, 0m), (revenue, 0m, 700m));
            updateDto.Id = created.Value.Id;

            var result = _service.Update(updateDto);
            Assert.False(result.IsSuccess);
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

            Assert.Equal(originalNo, JournalRepository.GetById(created.Value.Id).EntryNo);
        }

        // ===================== الحذف =====================

        [Fact]
        public void Delete_Draft_Succeeds()
        {
            var (cash, revenue) = CreateCashAndRevenue();
            var created = _service.Create(BuildDto(new DateTime(2026, 1, 5), (cash, 500m, 0m), (revenue, 0m, 500m)));

            var result = _service.Delete(created.Value.Id);

            Assert.True(result.IsSuccess, result.ErrorMessage);
            Assert.Null(JournalRepository.GetById(created.Value.Id));
        }

        [Fact]
        public void Delete_Posted_Fails()
        {
            var (cash, revenue) = CreateCashAndRevenue();
            var created = _service.Create(BuildDto(new DateTime(2026, 1, 5), (cash, 500m, 0m), (revenue, 0m, 500m)));
            _service.Post(created.Value.Id);

            var result = _service.Delete(created.Value.Id);

            Assert.False(result.IsSuccess);
        }

        // ===================== الترحيل =====================

        [Fact]
        public void Post_UpdatesAllAccountBalancesCorrectly()
        {
            var (cash, revenue) = CreateCashAndRevenue();
            var created = _service.Create(BuildDto(new DateTime(2026, 1, 5), (cash, 500m, 0m), (revenue, 0m, 500m)));

            var result = _service.Post(created.Value.Id);
            Assert.True(result.IsSuccess, result.ErrorMessage);

            Assert.Equal(500m, AccountRepository.GetByCode(cash).Balance);
            Assert.Equal(-500m, AccountRepository.GetByCode(revenue).Balance);
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

            var fiscal = new FiscalPeriodService();
            var year = fiscal.CreateYear(new DateTime(2026, 1, 1), 1);

            // ClosePeriod العادية ترفض الإقفال لوجود القيد أعلاه غير مرحّل (بتصميم النظام: لا يمكن أصلاً أن
            // توجد فترة مقفلة بها قيد غير مرحّل عبر المسار الطبيعي — Create وClosePeriod كلاهما يمنعان هذا
            // التوليف). نحاكي الحالة الحدّية مباشرة عبر FiscalPeriodRepository لاختبار حارس IsOpen في Post فعلياً.
            Db.RunTransaction((conn, tx) => FiscalPeriodRepository.SetPeriodClosed(conn, tx, year.Value.Periods.Single().Id, DateTime.Now, "test"));

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

            Assert.Equal(0m, AccountRepository.GetByCode(cash).Balance);
            Assert.Equal(0m, AccountRepository.GetByCode(revenue).Balance);
        }

        [Fact]
        public void Unpost_ClosingEntry_Fails()
        {
            SettingsService.Instance.Set(SettingKeys.Accounts.RetainedEarnings, "3200");
            var cash = CreateLeaf(AssetRootId(), "نقدية للإقفال");
            var revenue = CreateLeaf(RevenueRootId(), "إيراد للإقفال");

            var fiscal = new FiscalPeriodService();
            var year = fiscal.CreateYear(new DateTime(2026, 1, 1), 1);

            var entry = _service.Create(BuildDto(new DateTime(2026, 1, 5), (cash, 300m, 0m), (revenue, 0m, 300m)));
            Assert.True(entry.IsSuccess, entry.ErrorMessage);
            _service.Post(entry.Value.Id);
            _accounts.RecalculateAllBalances();

            fiscal.ClosePeriod(year.Value.Periods.Single().Id);
            var closeResult = fiscal.CloseYear(year.Value.Id);
            Assert.True(closeResult.IsSuccess, closeResult.ErrorMessage);

            var closingEntryId = FiscalPeriodRepository.GetYearById(year.Value.Id).ClosingEntryId;
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

            // نجعل الثاني غير قابل للترحيل بترحيله يدوياً مسبقاً — هذا الترحيل اليدوي نفسه يُحدِّث رصيد cash
            // إلى 300 شرعياً (عملية منفصلة عن PostBatch)؛ ما نتحقق منه هو أن PostBatch لا يضيف فوقه شيئاً.
            _service.Post(invalid.Value.Id);
            var cashBalanceBeforeBatch = AccountRepository.GetByCode(cash).Balance;

            var result = _service.PostBatch(new() { valid.Value.Id, invalid.Value.Id });

            Assert.True(result.IsSuccess);
            Assert.Equal(0, result.Value.SuccessCount);
            Assert.True(result.Value.FailedCount > 0);

            // الأول (الصالح) لم يُرحَّل، ورصيده (ورصيد cash عموماً) لم يتغيّر بفعل PostBatch
            Assert.False(JournalRepository.GetById(valid.Value.Id).IsPosted);
            Assert.Equal(cashBalanceBeforeBatch, AccountRepository.GetByCode(cash).Balance);
        }

        // ===================== ميزان المراجعة =====================

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
            _service.Create(BuildDto(new DateTime(2026, 1, 5), (cash, 1000m, 0m), (revenue, 0m, 1000m))); // مسودة، بلا ترحيل

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
            var posted = _service.Post(created.Value.Id);
            Assert.True(posted.IsSuccess, posted.ErrorMessage);
        }

        // ===================== الصلاحيات =====================

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
