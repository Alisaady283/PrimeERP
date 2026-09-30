using PrimeERP.Application.Services.Ledger;
using PrimeERP.Tests.Helpers;
using System;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Application.DTOs.Accounting;
using PrimeERP.Application.Legacy.Accounting;
using PrimeERP.Platform.Permissions;
using Xunit;

namespace PrimeERP.Tests.Services
{
    /// <summary>الحساب إمّا أب وإمّا يقبل</summary>
    public class AccountLeafStateTests : IDisposable
    {
        private readonly TestDatabaseFixture _db = new();
        private readonly IAccountService _accounts;

        public AccountLeafStateTests()
        {
            AppSession.DevMode = true;
            _accounts = _db.Services.GetRequiredService<IAccountService>();
        }

        public void Dispose() => _db.Dispose();

        private AccountDto Child(int parentId, string name) =>
            _accounts.Create(new CreateAccountDto { ParentId = parentId, Name = name, SkipAutoLink = true }).Value;

        [Fact]
        public void ANewAccount_AcceptsEntries_AndBecomesAParentOnItsFirstChild()
        {
            var branch = Child(_accounts.GetByCode("11").Value.Id, "فرع");
            Assert.True(branch.IsLeaf);

            var leaf = Child(branch.Id, "ابن");
            Assert.True(leaf.IsLeaf);
            Assert.False(_accounts.GetByCode(branch.Code).Value.IsLeaf);
        }

        [Fact]
        public void DeletingTheLastChild_MakesTheParentAcceptEntriesAgain()
        {
            var branch = Child(_accounts.GetByCode("11").Value.Id, "فرع");
            var leaf = Child(branch.Id, "ابن");

            Assert.False(_accounts.GetByCode(branch.Code).Value.IsLeaf);
            Assert.True(_accounts.Delete(leaf.Id).IsSuccess);
            Assert.True(_accounts.GetByCode(branch.Code).Value.IsLeaf);
        }

        [Fact]
        public void AnAccountWithEntries_RefusesChildren()
        {
            var branch = Child(_accounts.GetByCode("11").Value.Id, "فرع به قيود");
            var other = Child(_accounts.GetByCode("31").Value.Id, "طرف مقابل");

            var entry = _db.Services.GetRequiredService<IJournalService>().Create(new CreateJournalDto
            {
                EntryDate = DateTime.Today,
                Description = "قيد",
                Lines =
                {
                    new CreateJournalLineDto { LineNo = 1, AccountCode = branch.Code, Debit = 100 },
                    new CreateJournalLineDto { LineNo = 2, AccountCode = other.Code, Credit = 100 },
                }
            });
            Assert.True(entry.IsSuccess, entry.ErrorMessage);

            var refused = _accounts.Create(new CreateAccountDto { ParentId = branch.Id, Name = "ابن مرفوض", SkipAutoLink = true });

            Assert.True(refused.IsFailure);
            Assert.True(Localized.Says(refused.ErrorMessage, "Str.Accounts.ParentHasEntries"), refused.ErrorMessage);
        }
    }
}
