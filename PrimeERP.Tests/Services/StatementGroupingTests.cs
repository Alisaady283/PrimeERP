using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Application.DTOs.Accounting;
using PrimeERP.Application.Services.Accounting;
using PrimeERP.Domain.Enums;
using PrimeERP.Modules;
using PrimeERP.Platform.Permissions;
using Xunit;
using F = PrimeERP.Application.Reporting.FinancialStatementFactory;

namespace PrimeERP.Tests.Services
{
    /// <summary>
    /// القائمة تعرض مستوى التجميع لا دفتر الأستاذ: «ذمم مدينة» سطر واحد مهما بلغ عدد العملاء.
    /// </summary>
    public class StatementGroupingTests : IDisposable
    {
        private readonly TestDatabaseFixture _db = new();
        private readonly IAccountService _accounts;

        public StatementGroupingTests()
        {
            AppSession.DevMode = true;
            _accounts = _db.Services.GetRequiredService<IAccountService>();
        }

        public void Dispose() => _db.Dispose();

        private string Leaf(string parentCode, string name) =>
            _accounts.Create(new CreateAccountDto
            { ParentId = _accounts.GetByCode(parentCode).Value.Id, Name = name, SkipAutoLink = true }).Value.Code;

        [Fact]
        public void TwoCustomerAccounts_AppearAsOneReceivablesLine()
        {
            var first  = Leaf("1202", "عميل أ");
            var second = Leaf("1202", "عميل ب");
            var cash   = Leaf("1204", "صندوق");

            var journal = _db.Services.GetRequiredService<IJournalService>();
            var created = journal.Create(new CreateJournalDto
            {
                EntryDate = DateTime.Today,
                Description = "بيع",
                Lines =
                {
                    new CreateJournalLineDto { LineNo = 1, AccountCode = first,  Debit = 300 },
                    new CreateJournalLineDto { LineNo = 2, AccountCode = second, Debit = 200 },
                    new CreateJournalLineDto { LineNo = 3, AccountCode = cash,   Credit = 500 },
                }
            });
            Assert.True(created.IsSuccess, created.ErrorMessage);
            Assert.True(journal.Post(created.Value.Id).IsSuccess);

            var balance = journal.GetTrialBalance(DateTime.Today.AddDays(-1), DateTime.Today, includeZero: true).Value;
            var currentAssets = F.Closing(balance, AccountType.Asset, false, F.StartsWith("12"));

            var receivables = currentAssets.Where(l => l.Statement.Contains("ذمم")).ToList();

            Assert.Single(receivables);                       // سطر واحد لا عميلان
            Assert.Equal(500m, receivables[0].Partial);       // مجموعهما
            Assert.DoesNotContain(currentAssets, l => l.Statement is "عميل أ" or "عميل ب");
        }
    }
}
