using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Application.DTOs.Accounting;
using PrimeERP.Application.DTOs.Assets;
using PrimeERP.Application.Services;
using PrimeERP.Application.Services.Accounting;
using PrimeERP.Application.Services.Assets;
using PrimeERP.Platform.Permissions;
using PrimeERP.Platform.Settings;
using Xunit;

namespace PrimeERP.Tests.Services
{
    public class OpeningBalanceAndDepreciationTests : IDisposable
    {
        private readonly TestDatabaseFixture _db = new();
        private readonly IAccountService _accounts;
        private readonly ISettingsService _settings;

        public OpeningBalanceAndDepreciationTests()
        {
            AppSession.DevMode = true;
            _accounts = _db.Services.GetRequiredService<IAccountService>();
            _settings = _db.Services.GetRequiredService<ISettingsService>();
        }

        public void Dispose() => _db.Dispose();

        private string Leaf(string parentCode, string name) =>
            _accounts.Create(new CreateAccountDto
            { ParentId = _accounts.GetByCode(parentCode).Value.Id, Name = name, SkipAutoLink = true }).Value.Code;

        [Fact]
        public void UnbalancedOpeningBalances_AreRefused_NotSilentlyPostedToEquity()
        {
            var equity = Leaf("31", "رأس المال المدفوع");
            _settings.Set(SettingKeys.Accounts.RetainedEarnings, equity);

            var cash = Leaf("1204", "صندوق افتتاحي");
            var openings = _db.Services.GetRequiredService<IOpeningBalanceService>();

            // التوازن شرط لا تسوية: الفرق كان يُرحَّل لحقوق الملكية بلا علم المستخدم.
            var unbalanced = openings.Create(new CreateJournalDto
            {
                Description = "أرصدة افتتاحية",
                Lines = { new CreateJournalLineDto { LineNo = 1, AccountCode = cash, Debit = 5000 } }
            });

            Assert.True(unbalanced.IsFailure);
            Assert.Contains("غير متزن", unbalanced.ErrorMessage);
            Assert.Contains("5,000.00", unbalanced.ErrorMessage);
        }


        [Fact]
        public void PostedOpeningBalances_AreEditableAndDeletable_AfterUnpostingWithThePermission()
        {
            var equity = Leaf("31", "رأس مال للتعديل");
            _settings.Set(SettingKeys.Accounts.RetainedEarnings, equity);
            var cash = Leaf("1204", "صندوق للتعديل");

            var openings = _db.Services.GetRequiredService<IOpeningBalanceService>();
            var journals = _db.Services.GetRequiredService<IJournalService>();

            var created = openings.Create(new CreateJournalDto
            {
                Description = "أرصدة افتتاحية",
                Lines =
                {
                    new CreateJournalLineDto { LineNo = 1, AccountCode = cash,   Debit  = 5000 },
                    new CreateJournalLineDto { LineNo = 2, AccountCode = equity, Credit = 5000 }
                }
            });
            Assert.True(created.IsSuccess, created.ErrorMessage);

            // يُنشأ مرحَّلاً، فهو مغلق على التعديل والحذف — هذا هو المقصود، لا عطل.
            Assert.True(journals.GetById(created.Value.Id).Value.IsPosted, "القيد الافتتاحي يُنشأ مرحَّلاً");
            // السلوك لا النص: قواميس النصوص قد تكون محمَّلة أو لا حسب ما سبقه من اختبارات في المجموعة.
            var refused = journals.Delete(created.Value.Id);
            Assert.True(refused.IsFailure, "المرحَّل يجب أن يُرفض حذفه");
            Assert.NotNull(journals.GetById(created.Value.Id).Value);

            // إلغاء الترحيل يفتحه لمن يملك صلاحيته، والتعديل يمرّ من مستنده هو لا من شاشة القيود.
            var unposted = journals.Unpost(created.Value.Id);
            Assert.True(unposted.IsSuccess, "إلغاء الترحيل: " + unposted.ErrorMessage);

            var edited = openings.Update(new CreateJournalDto
            {
                Id = created.Value.Id, Description = "أرصدة افتتاحية معدَّلة",
                Lines =
                {
                    new CreateJournalLineDto { LineNo = 1, AccountCode = cash,   Debit  = 7000 },
                    new CreateJournalLineDto { LineNo = 2, AccountCode = equity, Credit = 7000 }
                }
            });
            Assert.True(edited.IsSuccess, edited.ErrorMessage);
            Assert.Equal(7000m, journals.GetById(created.Value.Id).Value.TotalDebit);

            var deleted = openings.Delete(created.Value.Id);
            Assert.True(deleted.IsSuccess, "الحذف: " + deleted.ErrorMessage);
        }
        [Fact]
        public void BalancedOpeningBalances_TakeTheirDateFromTheStartSetting_AndReachTheTrialBalance()
        {
            var start = new DateTime(2026, 1, 1);
            _settings.Set(SettingKeys.Company.StartDate, start);

            var equity = Leaf("31", "رأس المال المدفوع");
            var cash = Leaf("1204", "صندوق افتتاحي");

            var created = _db.Services.GetRequiredService<IOpeningBalanceService>().Create(new CreateJournalDto
            {
                EntryDate = DateTime.Today,          // يُتجاهَل — التاريخ من الإعداد
                Description = "أرصدة افتتاحية",
                Lines =
                {
                    new CreateJournalLineDto { LineNo = 1, AccountCode = cash,   Debit = 5000 },
                    new CreateJournalLineDto { LineNo = 2, AccountCode = equity, Credit = 5000 },
                }
            });
            Assert.True(created.IsSuccess, created.ErrorMessage);
            Assert.Equal(start, created.Value.EntryDate.Date);

            // تُرحَّل فور إنشائها — المسودّة لا تصل التقارير، فترحيل يدوي لاحق ليس شرطاً.
            var statement = _accounts.GetStatement(equity, start.AddDays(-1), DateTime.Today).Value;
            Assert.Equal(5000, statement.Sum(l => l.Credit));
        }

        [Fact]
        public void Depreciation_PostsOneEntry_AndReducesTheAssetValue()
        {
            _settings.Set(SettingKeys.Accounts.DepreciationExpense, Leaf("51", "مصروف إهلاك"));
            _settings.Set(SettingKeys.Accounts.AccumulatedDepreciation, Leaf("1101", "مجمع الإهلاك"));

            var assets = _db.Services.GetRequiredService<IAssetService>();
            var asset = assets.Create(new CreateAssetDto
            {
                Name = "سيارة", PurchaseDate = DateTime.Today.AddMonths(-12), PurchaseCost = 120000,
                CurrentValue = 120000, UsefulLifeYears = 10, SalvageValue = 0, IsActive = true
            });
            Assert.True(asset.IsSuccess, asset.ErrorMessage);

            var depreciation = _db.Services.GetRequiredService<IAssetDepreciationService>();
            Assert.Equal(1000, depreciation.MonthlyAmount(asset.Value.Id).Value);

            var run = depreciation.RunFor(DateTime.Today);
            Assert.True(run.IsSuccess, run.ErrorMessage);
            Assert.Equal(1, run.Value);

            var after = assets.GetById(asset.Value.Id).Value;
            Assert.Equal(12000, after.AccumulatedDepreciation);
            Assert.Equal(108000, after.CurrentValue);

            // تشغيلة ثانية بلا شهور جديدة لا تنتج قيداً.
            Assert.Equal(0, depreciation.RunFor(DateTime.Today).Value);
        }
    }
}
