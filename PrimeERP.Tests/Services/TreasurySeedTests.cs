using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Application.DTOs.Treasury;
using PrimeERP.Application.Services.Accounting;
using PrimeERP.Application.Services.Treasury;
using PrimeERP.Domain.Enums;
using PrimeERP.Platform.Permissions;
using Xunit;

namespace PrimeERP.Tests.Services
{
    /// <summary>قوائم السندات كانت تبدو "لا تتفاعل" لأنها فارغة أصلاً — لا خزائن ولا بنوك. الخزينة الآن
    /// تُنشئ حسابها الورقي تحت "الصناديق"/"البنوك" تلقائياً، وتُبذَر خزينة وبنك عند أول تشغيل.</summary>
    public class TreasurySeedTests : IDisposable
    {
        private readonly TestDatabaseFixture _db = new();

        public TreasurySeedTests() => AppSession.DevMode = true;

        public void Dispose() => _db.Dispose();

        [Fact]
        public void SeedDefaults_CreatesOneCashAndOneBank_EachLinkedUnderItsParentAccount()
        {
            var treasuries = _db.Services.GetRequiredService<ITreasuryService>();
            var accounts = _db.Services.GetRequiredService<IAccountService>();

            Assert.True(treasuries.SeedDefaults().IsSuccess);
            treasuries.SeedDefaults(); // مرتين — لا يكرّر

            var all = treasuries.GetAll().Value;
            Assert.Equal(2, all.Count);

            var cash = all.Single(t => t.Kind == TreasuryKind.Cash);
            var bank = all.Single(t => t.Kind == TreasuryKind.Bank);

            Assert.StartsWith("1204", cash.AccountCode);
            Assert.StartsWith("1203", bank.AccountCode);
            Assert.True(accounts.GetByCode(cash.AccountCode).IsSuccess);
            Assert.True(accounts.GetByCode(bank.AccountCode).IsSuccess);
        }

        [Fact]
        public void CreatingALeafAccountUnderTheCashOrBankRoot_CreatesItsTreasury()
        {
            var accounts = _db.Services.GetRequiredService<IAccountService>();
            var treasuries = _db.Services.GetRequiredService<ITreasuryService>();

            var cashRoot = accounts.GetByCode("1204").Value;
            var bankRoot = accounts.GetByCode("1203").Value;

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

            // إعادة تسمية الحساب تعيد تسمية الخزينة، وحذفه يعطّلها — نفس سلوك العملاء.
            Assert.True(accounts.Update(new PrimeERP.Application.DTOs.Accounting.UpdateAccountDto
            { Id = cashLeaf.Value.Id, Name = "صندوق الفرع الرئيسي", IsLeaf = true }).IsSuccess);
            Assert.Equal("صندوق الفرع الرئيسي", treasuries.GetAll().Value.Single(t => t.AccountCode == cashLeaf.Value.Code).Name);

            Assert.True(accounts.Delete(cashLeaf.Value.Id).IsSuccess);
            Assert.DoesNotContain(treasuries.GetAll().Value, t => t.AccountCode == cashLeaf.Value.Code);
        }

        [Fact]
        public void CreatingATreasuryWithoutAnAccount_BuildsTheLeafAccountItself()
        {
            var treasuries = _db.Services.GetRequiredService<ITreasuryService>();

            var created = treasuries.Create(new CreateTreasuryDto { Name = "خزينة الفرع", IsBank = false, IsActive = true });
            Assert.True(created.IsSuccess, created.ErrorMessage);
            Assert.False(string.IsNullOrWhiteSpace(created.Value.AccountCode));

            var account = _db.Services.GetRequiredService<IAccountService>().GetByCode(created.Value.AccountCode);
            Assert.True(account.IsSuccess);
            Assert.Equal("خزينة الفرع", account.Value.Name);
            Assert.True(account.Value.IsLeaf);
        }
    }
}
