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
        public void OpeningBalances_PostTheDifferenceToEquity_AndReachTheTrialBalance()
        {
            var equity = Leaf("31", "رأس المال المدفوع");
            _settings.Set(SettingKeys.Accounts.RetainedEarnings, equity);

            var cash = Leaf("1204", "صندوق افتتاحي");
            var openings = _db.Services.GetRequiredService<IOpeningBalanceService>();

            var created = openings.Create(new CreateJournalDto
            {
                EntryDate = DateTime.Today,
                Description = "أرصدة افتتاحية",
                Lines = { new CreateJournalLineDto { LineNo = 1, AccountCode = cash, Debit = 5000 } }
            });
            Assert.True(created.IsSuccess, created.ErrorMessage);

            // سطر الفرق أُلحق تلقائياً فصار القيد متوازناً وقابلاً للترحيل.
            Assert.True(_db.Services.GetRequiredService<IJournalService>().Post(created.Value.Id).IsSuccess);

            var statement = _accounts.GetStatement(equity, DateTime.Today.AddDays(-1), DateTime.Today).Value;
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
