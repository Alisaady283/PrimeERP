using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Application.DTOs.Accounting;
using PrimeERP.Application.DTOs.Parties;
using PrimeERP.Application.Services.Accounting;
using PrimeERP.Application.Services.Parties;
using PrimeERP.Composition.Definitions;
using PrimeERP.Composition.Registry;
using PrimeERP.Platform.Permissions;
using Xunit;

namespace PrimeERP.Tests.Services
{
    /// <summary>المعاملات كلها تصبّ في القيود</summary>
    public class JournalToStatementTests : IDisposable
    {
        private readonly TestDatabaseFixture _db = new();

        public JournalToStatementTests() => AppSession.DevMode = true;

        public void Dispose() => _db.Dispose();

        [Fact]
        public void AManualEntry_ReachesTheCustomerStatement_AsSoonAsItIsCreated()
        {
            var accounts = _db.Services.GetRequiredService<IAccountService>();
            var journals = _db.Services.GetRequiredService<IJournalService>();

            var customer = _db.Services.GetRequiredService<ICustomerService>()
                .Create(new CreateCustomerDto { Name = "عميل الكشف", IsActive = true });
            Assert.True(customer.IsSuccess, customer.ErrorMessage);

            var salesAccount = accounts.Create(new CreateAccountDto
            { ParentId = accounts.GetByCode("41").Value.Id, Name = "مبيعات", IsLeaf = true, SkipAutoLink = true }).Value.Code;

            var entry = journals.Create(new CreateJournalDto
            {
                EntryDate = DateTime.Today,
                Description = "قيد يدوي على العميل",
                Lines =
                {
                    new CreateJournalLineDto { LineNo = 1, AccountCode = customer.Value.AccountCode, Debit = 750 },
                    new CreateJournalLineDto { LineNo = 2, AccountCode = salesAccount, Credit = 750 },
                }
            });
            Assert.True(entry.IsSuccess, entry.ErrorMessage);

            var customers = _db.Services.GetRequiredService<ICustomerService>();

            var statement = customers.GetStatement(customer.Value.Id, DateTime.Today.AddDays(-1), DateTime.Today).Value;
            var line = statement.Single(l => l.Description == "قيد يدوي على العميل");
            Assert.Equal(750, line.Debit);
        }

        [Fact]
        public void TheJournalsScreen_ExposesNoPostingAction()
        {
            var definition = _db.Services.GetRequiredService<IModuleRegistry>().Get("Journals");

            Assert.True(definition.RowActions == null || definition.RowActions.Count == 0);
        }
    }
}
