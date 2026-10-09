using PrimeERP.Application.PageServices.Admin;
using PrimeERP.Application.Services.Ledger;
using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Application.DTOs.Treasury;
using PrimeERP.Application.PageServices.Accounting;
using PrimeERP.Application.PageServices.Treasury;
using PrimeERP.Domain.Entities;
using PrimeERP.Domain.Enums;
using PrimeERP.Platform.Permissions;
using Xunit;

namespace PrimeERP.Tests.Services
{
    /// <summary>قوائم السندات كانت تبدو "لا</summary>
    public class TreasurySeedTests : IDisposable
    {
        private readonly TestDatabaseFixture _db = new();

        public TreasurySeedTests() => AppSession.DevMode = true;

        public void Dispose() => _db.Dispose();

        [Fact]
        public void SeedDefaults_CreatesOnlyTheMainCash_UnderItsParentAccount()
        {
            var treasuries = _db.Services.GetRequiredService<ITreasuryService>();
            var accounts = _db.Services.GetRequiredService<IAccountService>();

            Assert.True(treasuries.SeedDefaults().IsSuccess);
            treasuries.SeedDefaults(); // مرتين — لا يكرّر

            var all = treasuries.GetAll().Value;
            Assert.Single(all);

            var cash = all.Single(t => t.Kind == TreasuryKind.Cash);

            Assert.StartsWith("1203007", cash.AccountCode);
            Assert.True(accounts.GetByCode(cash.AccountCode).IsSuccess);
            Assert.False(accounts.GetByCode("1203007").Value.IsLeaf);
            Assert.False(accounts.GetByCode("1203003").Value.IsLeaf);
        }

        [Fact]
        public void CreatingALeafAccountUnderTheCashOrBankRoot_CreatesItsTreasury()
        {
            var accounts = _db.Services.GetRequiredService<IAccountService>();
            var treasuries = _db.Services.GetRequiredService<ITreasuryService>();

            var cashRoot = accounts.GetByCode("1203007").Value;
            var bankRoot = accounts.GetByCode("1203003").Value;

            var cashLeaf = accounts.Create(new PrimeERP.Application.DTOs.Accounting.CreateAccountDto
            { ParentId = cashRoot.Id, Name = "صندوق الفرع", IsLeaf = true });
            var bankLeaf = accounts.Create(new PrimeERP.Application.DTOs.Accounting.CreateAccountDto
            { ParentId = bankRoot.Id, Name = "بنك القاهرة", IsLeaf = true });
            Assert.True(cashLeaf.IsSuccess, cashLeaf.ErrorMessage);
            Assert.True(bankLeaf.IsSuccess, bankLeaf.ErrorMessage);

            var all = treasuries.GetAll().Value;
            var cash = all.Single(t => t.AccountCode == cashLeaf.Value.Code);
            var bank = all.Single(t => t.AccountCode == bankLeaf.Value.Code);

            Assert.Equal(TreasuryKind.Cash, cash.Kind);
            Assert.Equal(TreasuryKind.Bank, bank.Kind);
            Assert.Equal("صندوق الفرع", cash.Name);

            Assert.True(accounts.Update(new PrimeERP.Application.DTOs.Accounting.UpdateAccountDto
            { Id = cashLeaf.Value.Id, Name = "صندوق الفرع الرئيسي", IsLeaf = true }).IsSuccess);
            Assert.Equal("صندوق الفرع الرئيسي", treasuries.GetAll().Value.Single(t => t.AccountCode == cashLeaf.Value.Code).Name);

            Assert.True(accounts.Delete(cashLeaf.Value.Id).IsSuccess);
            Assert.DoesNotContain(treasuries.GetAll().Value, t => t.AccountCode == cashLeaf.Value.Code);
            Assert.True(accounts.Delete(bankLeaf.Value.Id).IsSuccess);
            Assert.DoesNotContain(treasuries.GetAll().Value, t => t.AccountCode == bankLeaf.Value.Code);
            Assert.False(accounts.GetByCode("1203007").Value.IsLeaf);
            Assert.False(accounts.GetByCode("1203003").Value.IsLeaf);
            Assert.False(accounts.Delete(cashRoot.Id).IsSuccess);
            Assert.False(accounts.Delete(bankRoot.Id).IsSuccess);
        }

        [Fact]
        public void CreatingATreasuryWithoutAnAccount_BuildsTheLeafAccountItself()
        {
            var treasuries = _db.Services.GetRequiredService<ITreasuryService>();

            var created = treasuries.Create(new Treasury { Name = "خزينة الفرع", Kind = TreasuryKind.Cash, IsActive = true });
            Assert.True(created.IsSuccess, created.ErrorMessage);
            Assert.False(string.IsNullOrWhiteSpace(created.Value.AccountCode));

            var account = _db.Services.GetRequiredService<IAccountService>().GetByCode(created.Value.AccountCode);
            Assert.True(account.IsSuccess);
            Assert.Equal("خزينة الفرع", account.Value.Name);
            Assert.True(account.Value.IsLeaf);
        }

        [Fact]
        public void WhenTheRootAccountCannotHoldChildren_CreationFailsWithAReason()
        {
            var settings = _db.Services.GetRequiredService<PrimeERP.Application.PageServices.Admin.ISettingsService>();
            var treasuries = _db.Services.GetRequiredService<ITreasuryService>();

            settings.Set(PrimeERP.Platform.Settings.SettingKeys.Accounts.Bank, "9999");

            var created = treasuries.Create(new Treasury { Name = "بنك بلا أصل", Kind = TreasuryKind.Bank, IsActive = true });

            Assert.True(created.IsFailure, "أُنشئت خزينة بلا حساب بدل رفض واضح");
            Assert.Contains("9999", created.ErrorMessage);
            Assert.DoesNotContain(treasuries.GetAll(includeInactive: true).Value, t => t.Name == "بنك بلا أصل");
        }

        [Fact]
        public void ATreasuryLeftWithoutItsAccount_IsRelinkedToTheExistingOne_NotGivenASecond()
        {
            var treasuries = _db.Services.GetRequiredService<ITreasuryService>();
            var accounts = _db.Services.GetRequiredService<IAccountService>();
            var repo = _db.Services.GetRequiredService<PrimeERP.Data.Repositories.ITreasuryRepository>();

            var created = treasuries.Create(new Treasury { Name = "الصندوق الرئيسي", Kind = TreasuryKind.Cash, IsActive = true });
            Assert.True(created.IsSuccess, created.ErrorMessage);
            var accountCode = created.Value.AccountCode;

            var entity = repo.GetById(created.Value.Id);
            entity.AccountCode = "";
            repo.Update(entity);
            Assert.True(string.IsNullOrWhiteSpace(treasuries.GetById(created.Value.Id).Value.AccountCode));

            var leavesBefore = accounts.GetLeaves().Value.Count;

            Assert.True(treasuries.RepairMissingAccounts().IsSuccess);

            Assert.Equal(accountCode, treasuries.GetById(created.Value.Id).Value.AccountCode);
            Assert.Equal(leavesBefore, accounts.GetLeaves().Value.Count);

            Assert.True(treasuries.RepairMissingAccounts().IsSuccess);
            Assert.Equal(accountCode, treasuries.GetById(created.Value.Id).Value.AccountCode);
            Assert.Equal(leavesBefore, accounts.GetLeaves().Value.Count);
        }

        [Fact]
        public void RenamingATreasuryFromItsPage_RenamesItsAccountToo()
        {
            var treasuries = _db.Services.GetRequiredService<ITreasuryService>();
            var accounts = _db.Services.GetRequiredService<IAccountService>();

            var created = treasuries.Create(new Treasury { Name = "خزينة الفرع", Kind = TreasuryKind.Cash, IsActive = true });
            Assert.True(created.IsSuccess, created.ErrorMessage);

            var updated = treasuries.Update(new Treasury
            { Id = created.Value.Id, Name = "خزينة الفرع الرئيسي", Kind = TreasuryKind.Cash, IsActive = true });
            Assert.True(updated.IsSuccess, updated.ErrorMessage);

            Assert.Equal("خزينة الفرع الرئيسي", accounts.GetByCode(created.Value.AccountCode).Value.Name);
        }

        [Fact]
        public void DeletingATreasuryFromItsPage_RemovesItsAccountFromTheTree()
        {
            var treasuries = _db.Services.GetRequiredService<ITreasuryService>();
            var accounts = _db.Services.GetRequiredService<IAccountService>();

            var created = treasuries.Create(new Treasury { Name = "بنك الفرع", Kind = TreasuryKind.Bank, IsActive = true });
            Assert.True(created.IsSuccess, created.ErrorMessage);
            Assert.Contains(accounts.GetLeaves().Value, a => a.Code == created.Value.AccountCode);

            Assert.True(treasuries.Delete(created.Value.Id).IsSuccess);

            Assert.DoesNotContain(accounts.GetLeaves().Value, a => a.Code == created.Value.AccountCode);
        }

        [Fact]
        public void DeletingATreasuryThenAddingAnother_DoesNotReuseTheFreedAccountCode()
        {
            var treasuries = _db.Services.GetRequiredService<ITreasuryService>();

            var first = treasuries.Create(new Treasury { Name = "بنك أول", Kind = TreasuryKind.Bank, IsActive = true });
            Assert.True(first.IsSuccess, first.ErrorMessage);

            Assert.True(treasuries.Delete(first.Value.Id).IsSuccess);

            var second = treasuries.Create(new Treasury { Name = "بنك ثانٍ", Kind = TreasuryKind.Bank, IsActive = true });
            Assert.True(second.IsSuccess, second.ErrorMessage);
            Assert.NotEqual(first.Value.AccountCode, second.Value.AccountCode);
        }
    }
}
