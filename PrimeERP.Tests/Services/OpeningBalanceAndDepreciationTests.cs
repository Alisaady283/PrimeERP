using PrimeERP.Application.Legacy.Admin;
using PrimeERP.Application.Services.Ledger;
using PrimeERP.Tests.Helpers;
using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Application.DTOs.Accounting;
using PrimeERP.Application.DTOs.Assets;
using PrimeERP.Application.Legacy.Accounting;
using PrimeERP.Application.Legacy.Assets;
using PrimeERP.Domain.Entities;
using PrimeERP.Platform.Permissions;
using PrimeERP.Platform.Settings;
using Xunit;

namespace PrimeERP.Tests.Services
{
    /// <summary>الأرصدة الافتتاحية والإهلاك</summary>
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

        private int AssetCategory(string name = "فئة اختبار")
        {
            var created = _db.Services.GetRequiredService<PrimeERP.Application.Legacy.Common.ICategoryService>()
                .Create(new Category { Name = name, ModuleKey = "AssetCategories" });

            Assert.True(created.IsSuccess, created.ErrorMessage);
            return created.Value.Id;
        }

        private string Leaf(string parentCode, string name) =>
            _accounts.Create(new CreateAccountDto
            { ParentId = _accounts.GetByCode(parentCode).Value.Id, Name = name, SkipAutoLink = true }).Value.Code;


        private PrimeERP.Domain.Results.Result<PrimeERP.Domain.Entities.Treasury> FundedTreasury(string name, decimal amount = 500000)
        {
            var treasury = _db.Services.GetRequiredService<PrimeERP.Application.Legacy.Treasury.ITreasuryService>()
                .Create(new PrimeERP.Domain.Entities.Treasury
                { Name = name, Kind = PrimeERP.Domain.Enums.TreasuryKind.Cash, AccountCode = Leaf("1204", name), IsActive = true });

            Assert.True(treasury.IsSuccess, treasury.ErrorMessage);

            Fund(treasury.Value.AccountCode, name, amount);
            return treasury;
        }

        private void Fund(string accountCode, string name, decimal amount = 500000) =>
            _db.Services.GetRequiredService<PrimeERP.Application.Legacy.Accounting.IJournalService>()
                .Create(new CreateJournalDto
                {
                    EntryDate = DateTime.Today.AddYears(-2), Description = "تمويل " + name,
                    Lines =
                    {
                        new CreateJournalLineDto { LineNo = 1, AccountCode = accountCode, Debit = amount },
                        new CreateJournalLineDto { LineNo = 2, AccountCode = Leaf("31", "رأس مال " + name), Credit = amount }
                    }
                });

        [Fact]
        public void UnbalancedOpeningBalances_AreRefused_NotSilentlyPostedToEquity()
        {
            var equity = Leaf("31", "رأس المال المدفوع");
            _settings.Set(SettingKeys.Accounts.RetainedEarnings, equity);

            var cash = Leaf("1204", "صندوق افتتاحي");
            var openings = _db.Services.GetRequiredService<IOpeningBalanceService>();

            var unbalanced = openings.Create(new CreateJournalDto
            {
                Description = "أرصدة افتتاحية",
                Lines = { new CreateJournalLineDto { LineNo = 1, AccountCode = cash, Debit = 5000 } }
            });

            Assert.True(unbalanced.IsFailure);
            Assert.True(Localized.Says(unbalanced.ErrorMessage, "Str.Journal.Unbalanced"), unbalanced.ErrorMessage);
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

            Assert.True(journals.GetById(created.Value.Id).Value.IsPosted, "القيد الافتتاحي يُنشأ مرحَّلاً");
            var refused = journals.Delete(created.Value.Id);
            Assert.True(refused.IsFailure, "المرحَّل يجب أن يُرفض حذفه");
            Assert.NotNull(journals.GetById(created.Value.Id).Value);

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

            var statement = _accounts.GetStatement(equity, start.AddDays(-1), DateTime.Today).Value;
            Assert.Equal(5000, statement.Sum(l => l.Credit));
        }

        [Fact]
        public void Depreciation_PostsAMonthlyEntryPerAsset_AndIsReversible()
        {
            var treasury = FundedTreasury("صندوق الأصول");

            var assets = _db.Services.GetRequiredService<IAssetService>();
            var asset = assets.Create(new Asset
            {
                Name = "سيارة", CategoryId = AssetCategory("سيارات"),
                PurchaseDate = DateTime.Today.AddMonths(-12), PurchaseCost = 120000,
                UsefulLifeYears = 10, SalvageValue = 0, IsActive = true,
                AcquisitionMethod = PrimeERP.Domain.Enums.AssetAcquisition.Cash, FundingId = treasury.Value.Id
            });
            Assert.True(asset.IsSuccess, asset.ErrorMessage);

            var depreciation = _db.Services.GetRequiredService<IAssetDepreciationService>();
            Assert.Equal(1000, depreciation.MonthlyAmount(asset.Value.Id).Value);

            var run = depreciation.RunFor(DateTime.Today);
            Assert.True(run.IsSuccess, run.ErrorMessage);
            Assert.Equal(13, run.Value);

            var after = assets.GetById(asset.Value.Id).Value;
            Assert.Equal(13000, after.AccumulatedDepreciation);
            Assert.Equal(107000, after.CurrentValue);

            Assert.Equal(0, depreciation.RunFor(DateTime.Today).Value);

            var logged = depreciation.GetPaged(1, 100).Value.Items;
            Assert.Equal(13, logged.Count);
            Assert.Equal(13000, logged.Sum(c => c.Amount));

            foreach (var charge in logged) Assert.True(depreciation.Delete(charge.Id).IsSuccess);

            var restored = assets.GetById(asset.Value.Id).Value;
            Assert.Equal(0, restored.AccumulatedDepreciation);
            Assert.Equal(120000, restored.CurrentValue);
            Assert.Empty(depreciation.GetPaged(1, 100).Value.Items);
        }

        [Fact]
        public void Asset_IsAdded_Edited_AndDeleted()
        {
            var treasury = FundedTreasury("صندوق");

            var assets = _db.Services.GetRequiredService<IAssetService>();

            var created = assets.Create(new Asset
            {
                Name = "جهاز", CategoryId = AssetCategory("أجهزة"),
                PurchaseDate = DateTime.Today.AddMonths(-2), PurchaseCost = 9000,
                UsefulLifeYears = 3, SalvageValue = 0, IsActive = true,
                AcquisitionMethod = PrimeERP.Domain.Enums.AssetAcquisition.Cash, FundingId = treasury.Value.Id
            });
            Assert.True(created.IsSuccess, created.ErrorMessage);

            var loaded = assets.GetById(created.Value.Id).Value;
            Assert.Equal(PrimeERP.Domain.Enums.AssetAcquisition.Cash, loaded.AcquisitionMethod);
            Assert.Equal(treasury.Value.Id, loaded.FundingId);

            loaded.Name = "جهاز معدَّل";
            var edited = assets.Update(loaded);
            Assert.True(edited.IsSuccess, edited.ErrorMessage);
            Assert.Equal("جهاز معدَّل", assets.GetById(loaded.Id).Value.Name);

            var removed = assets.Delete(loaded.Id);
            Assert.True(removed.IsSuccess, removed.ErrorMessage);
            Assert.True(assets.GetById(loaded.Id).IsFailure);
        }

        [Fact]
        public void Asset_WithOnlyItsAcquisitionEntry_IsDeletable_ButNotAfterDepreciation()
        {
            var treasury = FundedTreasury("صندوق");

            var assets = _db.Services.GetRequiredService<IAssetService>();
            var category = AssetCategory("فئة الحذف");

            Asset New(string name) => new()
            {
                Name = name, CategoryId = category,
                PurchaseDate = DateTime.Today.AddMonths(-3), PurchaseCost = 6000,
                UsefulLifeYears = 5, SalvageValue = 0, IsActive = true,
                AcquisitionMethod = PrimeERP.Domain.Enums.AssetAcquisition.Cash, FundingId = treasury.Value.Id
            };

            var plain = assets.Create(New("أصل بقيد اقتنائه"));
            Assert.True(plain.IsSuccess, plain.ErrorMessage);
            Assert.True(assets.Delete(plain.Value.Id).IsSuccess);

            var depreciated = assets.Create(New("أصل مُهلَك"));
            Assert.True(depreciated.IsSuccess, depreciated.ErrorMessage);

            var run = _db.Services.GetRequiredService<IAssetDepreciationService>().RunFor(DateTime.Today);
            Assert.True(run.IsSuccess, run.ErrorMessage);
            Assert.True(run.Value > 0);

            Assert.True(assets.Delete(depreciated.Value.Id).IsFailure);
        }

        [Fact]
        public void SellingAnAsset_ClosesItsTwoAccounts_PostsTheGain_AndRetiresItWithoutDeleting()
        {
            var treasury = FundedTreasury("صندوق البيع");

            var assets = _db.Services.GetRequiredService<IAssetService>();
            var created = assets.Create(new Asset
            {
                Name = "رافعة", CategoryId = AssetCategory("معدات"),
                PurchaseDate = DateTime.Today.AddMonths(-12), PurchaseCost = 12000,
                UsefulLifeYears = 10, SalvageValue = 0, IsActive = true,
                AcquisitionMethod = PrimeERP.Domain.Enums.AssetAcquisition.Cash, FundingId = treasury.Value.Id
            });
            Assert.True(created.IsSuccess, created.ErrorMessage);

            var depreciation = _db.Services.GetRequiredService<IAssetDepreciationService>();
            Assert.True(depreciation.RunFor(DateTime.Today).IsSuccess);

            var beforeSale = assets.GetById(created.Value.Id).Value;
            Assert.Equal(1300, beforeSale.AccumulatedDepreciation);

            var disposals = _db.Services.GetRequiredService<IAssetDisposalService>();
            var sold = disposals.Create(new AssetDisposal
            {
                AssetId = created.Value.Id, DisposalDate = DateTime.Today,
                TreasuryId = treasury.Value.Id, SalePrice = 11500
            });
            Assert.True(sold.IsSuccess, sold.ErrorMessage);
            Assert.Equal(10700, sold.Value.BookValue);
            Assert.Equal(800, sold.Value.GainOrLoss);

            var journals = _db.Services.GetRequiredService<IJournalService>();
            var entry = journals.GetById(sold.Value.JournalEntryId.Value).Value;

            Assert.Equal(4, entry.Lines.Count);
            Assert.Equal(12800, entry.TotalDebit);
            Assert.Equal(12800, entry.TotalCredit);
            Assert.Contains(entry.Lines, l => l.AccountCode == treasury.Value.AccountCode && l.Debit == 11500);
            Assert.Contains(entry.Lines, l => l.Debit == 1300);
            Assert.Contains(entry.Lines, l => l.Credit == 12000);
            Assert.Contains(entry.Lines, l => l.AccountCode == _settings.Get<string>(SettingKeys.Accounts.CapitalGains, "") && l.Credit == 800);

            Assert.False(assets.GetById(created.Value.Id).Value.IsActive);
            Assert.Equal(0, depreciation.RunFor(DateTime.Today.AddMonths(2)).Value);
            Assert.True(disposals.Create(new AssetDisposal
            { AssetId = created.Value.Id, DisposalDate = DateTime.Today, TreasuryId = treasury.Value.Id, SalePrice = 100 }).IsFailure);

            Assert.True(disposals.Delete(sold.Value.Id).IsSuccess);
            Assert.True(assets.GetById(created.Value.Id).Value.IsActive);
            Assert.True(journals.GetById(sold.Value.JournalEntryId.Value).IsFailure);
        }

        [Fact]
        public void Revaluation_PostsToTheAssetsOwnAccount_AgainstCapitalGainsOrLosses()
        {
            var treasury = FundedTreasury("صندوق التقييم");

            var assets = _db.Services.GetRequiredService<IAssetService>();
            var created = assets.Create(new Asset
            {
                Name = "مبنى", CategoryId = AssetCategory("مبانٍ"),
                PurchaseDate = DateTime.Today, PurchaseCost = 10000,
                UsefulLifeYears = 10, SalvageValue = 0, IsActive = true,
                AcquisitionMethod = PrimeERP.Domain.Enums.AssetAcquisition.Cash, FundingId = treasury.Value.Id
            });
            Assert.True(created.IsSuccess, created.ErrorMessage);

            var revaluations = _db.Services.GetRequiredService<IAssetRevaluationService>();
            var up = revaluations.Create(new AssetRevaluation
            { AssetId = created.Value.Id, RevaluationDate = DateTime.Today, NewValue = 12000 });
            Assert.True(up.IsSuccess, up.ErrorMessage);

            var journals = _db.Services.GetRequiredService<IJournalService>();
            var entry = journals.GetById(up.Value.JournalEntryId.Value).Value;

            var gains = _settings.Get<string>(SettingKeys.Accounts.CapitalGains, "");
            var own = assets.GetById(created.Value.Id).Value;

            Assert.Equal(2000, entry.TotalDebit);
            Assert.Equal(2000, entry.TotalCredit);
            Assert.Contains(entry.Lines, l => l.Debit == 2000 && l.AccountCode != gains);
            Assert.Contains(entry.Lines, l => l.Credit == 2000 && l.AccountCode == gains);

            var down = revaluations.Create(new AssetRevaluation
            { AssetId = created.Value.Id, RevaluationDate = DateTime.Today, NewValue = 9000 });
            Assert.True(down.IsSuccess, down.ErrorMessage);

            var losses = _settings.Get<string>(SettingKeys.Accounts.CapitalLosses, "");
            var second = journals.GetById(down.Value.JournalEntryId.Value).Value;

            Assert.Equal(3000, second.TotalDebit);
            Assert.Contains(second.Lines, l => l.Debit == 3000 && l.AccountCode == losses);
            Assert.Equal(9000, assets.GetById(created.Value.Id).Value.RevaluedValue);
        }

        [Fact]
        public void AnAssetFundedByTheSeededMainCashBox_FailsWhileItsAccountIsMissing_AndPassesAfterTheRepair()
        {
            var treasuries = _db.Services.GetRequiredService<PrimeERP.Application.Legacy.Treasury.ITreasuryService>();
            var repo = _db.Services.GetRequiredService<PrimeERP.Data.Repositories.ITreasuryRepository>();
            var assets = _db.Services.GetRequiredService<IAssetService>();

            var mainCash = treasuries.GetAll().Value
                .Single(t => t.Kind == PrimeERP.Domain.Enums.TreasuryKind.Cash);

            var row = repo.GetById(mainCash.Id);
            var lostCode = row.AccountCode;
            row.AccountCode = "";
            repo.Update(row);

            Asset New() => new()
            {
                Name = "أصل من الصندوق الرئيسي", CategoryId = AssetCategory("فئة قائمة"),
                PurchaseDate = DateTime.Today, PurchaseCost = 35000,
                UsefulLifeYears = 5, SalvageValue = 0, IsActive = true,
                AcquisitionMethod = PrimeERP.Domain.Enums.AssetAcquisition.Cash, FundingId = mainCash.Id
            };

            var refused = assets.Create(New());
            Assert.True(refused.IsFailure);
            Assert.Empty(assets.GetPaged(1, 100).Value.Items);

            Assert.True(treasuries.RepairMissingAccounts().IsSuccess);
            Fund(lostCode, "صندوق البذرة");
            Assert.Equal(lostCode, treasuries.GetById(mainCash.Id).Value.AccountCode);

            var created = assets.Create(New());
            Assert.True(created.IsSuccess, created.ErrorMessage);

            var entry = _db.Services.GetRequiredService<IJournalService>()
                .GetPaged(1, 100, new PrimeERP.Application.DTOs.Accounting.JournalFilter { Source = "Assets" })
                .Value.Items.Single();

            Assert.Equal(35000, entry.TotalDebit);
            Assert.Equal(35000, entry.TotalCredit);
        }

        [Fact]
        public void Asset_IsNotSaved_WhenItsAcquisitionEntryFails()
        {
            var treasuryAccount = Leaf("1204", "صندوق بفرع");
            var treasury = _db.Services.GetRequiredService<PrimeERP.Application.Legacy.Treasury.ITreasuryService>()
                .Create(new PrimeERP.Domain.Entities.Treasury
                { Name = "صندوق بفرع", Kind = PrimeERP.Domain.Enums.TreasuryKind.Cash, AccountCode = treasuryAccount, IsActive = true });

            var assets = _db.Services.GetRequiredService<IAssetService>();
            var category = AssetCategory("فئة التراجع");

            Leaf(treasuryAccount, "فرع");

            var failed = assets.Create(new Asset
            {
                Name = "أصل لا يُحفَظ", CategoryId = category,
                PurchaseDate = DateTime.Today, PurchaseCost = 35000,
                UsefulLifeYears = 5, SalvageValue = 0, IsActive = true,
                AcquisitionMethod = PrimeERP.Domain.Enums.AssetAcquisition.Cash, FundingId = treasury.Value.Id
            });

            Assert.True(failed.IsFailure);
            Assert.Empty(assets.GetPaged(1, 100).Value.Items);
        }

        [Fact]
        public void Depreciation_CountsMonthsPerAsset_AndStopsAtTheUsefulLife()
        {
            var treasury = FundedTreasury("صندوق");

            var assets = _db.Services.GetRequiredService<IAssetService>();

            var category = AssetCategory("فئة الأصول");

            void Add(string name, int monthsAgo, int life) => Assert.True(assets.Create(new Asset
            {
                Name = name, CategoryId = category,
                PurchaseDate = DateTime.Today.AddMonths(-monthsAgo), PurchaseCost = 12000,
                UsefulLifeYears = life, SalvageValue = 0, IsActive = true,
                AcquisitionMethod = PrimeERP.Domain.Enums.AssetAcquisition.Cash, FundingId = treasury.Value.Id
            }).IsSuccess);

            Add("أصل أ", 7, 5);
            Add("أصل ب", 7, 5);
            Add("أصل ج", 7, 5);
            Add("أصل د", 1, 5);
            Add("أصل منتهٍ", 120, 5);   // مضى ضعف عمره — يُهلَك بالكامل ولا يتجاوز

            var depreciation = _db.Services.GetRequiredService<IAssetDepreciationService>();
            var run = depreciation.RunFor(DateTime.Today);
            Assert.True(run.IsSuccess, run.ErrorMessage);

            Assert.Equal((3 * 8) + 2 + 60, run.Value);

            var finished = assets.GetPaged(1, 50).Value.Items.Single(a => a.Name == "أصل منتهٍ");
            Assert.Equal(12000, finished.AccumulatedDepreciation);
            Assert.Equal(0, finished.CurrentValue);

            Assert.Equal(4 * 12, depreciation.RunFor(DateTime.Today.AddMonths(12)).Value);

            var stillFinished = assets.GetPaged(1, 50).Value.Items.Single(a => a.Name == "أصل منتهٍ");
            Assert.Equal(12000, stillFinished.AccumulatedDepreciation);
            Assert.Equal(0, stillFinished.CurrentValue);
        }
    }
}
