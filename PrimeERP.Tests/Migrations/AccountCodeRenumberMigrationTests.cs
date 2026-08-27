using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Data.Core;
using PrimeERP.Data.Migrations;
using PrimeERP.Data.Repositories;
using PrimeERP.Domain.Entities;
using Xunit;
using Db = PrimeERP.Data.Core.DbHelper;

namespace PrimeERP.Tests.Migrations
{
    public class AccountCodeRenumberMigrationTests : IDisposable
    {
        private readonly TestDatabaseFixture _db = new();

        public void Dispose() => _db.Dispose();

        [Fact]
        public void Apply_RenumbersOldSeedCodes_AndCascadesToLinkedTables()
        {
            var accounts = _db.Services.GetRequiredService<IAccountRepository>();
            var customers = _db.Services.GetRequiredService<ICustomerRepository>();
            var suppliers = _db.Services.GetRequiredService<ISupplierRepository>();
            var journal = _db.Services.GetRequiredService<IJournalRepository>();

            // امسح البذور الجديدة، وازرع نسخة من الصيغة القديمة يدوياً — تحاكي PrimeERP.db الفعلية قبل الترحيل.
            Db.Execute("DELETE FROM Accounts");
            accounts.Insert(new Account { Code = "1000", ParentCode = null,   Level = 1, Name = "أصول",   Type = 1, IsLeaf = false });
            accounts.Insert(new Account { Code = "1200", ParentCode = "1000", Level = 2, Name = "متداولة", Type = 1, IsLeaf = false });
            accounts.Insert(new Account { Code = "1220", ParentCode = "1200", Level = 3, Name = "عملاء",   Type = 1, IsLeaf = false });
            // ابن مولَّد فعلياً بعد البذور (لا من الخريطة اليدوية) — يثبت أن الامتداد العام يعمل لا الخريطة فقط.
            accounts.Insert(new Account { Code = "1220001", ParentCode = "1220", Level = 4, Name = "عميل قديم حساب", Type = 1, IsLeaf = true });

            var customerId = customers.Insert(new Customer { Code = "C-1", Name = "عميل قديم", AccountCode = "1220001" });
            var supplierId = suppliers.Insert(new Supplier { Code = "S-1", Name = "مورد قديم", AccountCode = "1220" });

            var entryId = Db.RunTransaction((conn, tx) =>
            {
                var header = new JournalEntry { EntryNo = "TEST-MIG", EntryDate = DateTime.Now.ToString("yyyy-MM-dd"), Description = "test", Source = "test" };
                var id = journal.InsertHeader(conn, tx, header);
                journal.InsertLine(conn, tx, id, 1, new JournalLine { AccountCode = "1220", Debit = 10m, Credit = 0m });
                return id;
            });

            AccountCodeRenumberMigration.Apply();

            Assert.NotNull(accounts.GetByCode("1"));
            Assert.NotNull(accounts.GetByCode("12"));

            var newCustomerAccount = accounts.GetByCode("1202");
            Assert.NotNull(newCustomerAccount);
            Assert.Equal("12", newCustomerAccount.ParentCode);

            var newLeaf = accounts.GetByCode("1202001");
            Assert.NotNull(newLeaf);
            Assert.Equal("1202", newLeaf.ParentCode);

            Assert.Equal("1202001", customers.GetById(customerId).AccountCode);
            Assert.Equal("1202", suppliers.GetById(supplierId).AccountCode);
            Assert.Equal("1202", journal.GetLines(entryId).Single().AccountCode);
        }
    }
}
