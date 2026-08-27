using System;
using System.Data.Common;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Platform.Permissions;
using PrimeERP.Domain.Enums;
using PrimeERP.Domain.Results;
using PrimeERP.Data.Repositories;
using PrimeERP.Platform.Settings;
using PrimeERP.Domain.Entities;
using PrimeERP.Platform.Localization;
using PrimeERP.UI.Services;
using PrimeERP.Application;
using PrimeERP.Application.Services;
using PrimeERP.Application.Services.Accounting;
using PrimeERP.Application.DTOs.Accounting;
using PrimeERP.Application.Services.Parties;
using PrimeERP.Application.DTOs.Parties;
using Xunit;
using Db = PrimeERP.Data.Core.DbHelper;

namespace PrimeERP.Tests.Services
{
    /// <summary>
    /// قاعدة بيانات خاصة معزولة لكل اختبار (لا [Collection("Database")] المشتركة) عمداً — اختبارات هذه الفئة
    /// تفترض شجرة حسابات "نظيفة" (مثال: أول ابن لـ 1220 كوده 1220001 بالضبط)، وهذا يتطلب عدم تسرّب حسابات
    /// من اختبار سابق. xUnit يُنشئ نسخة جديدة من فئة الاختبار قبل كل [Fact]، فبناء TestDatabaseFixture هنا
    /// (لا عبر ICollectionFixture مشتركة) يعطي كل اختبار قاعدة بيانات مستقلة تلقائياً.
    /// </summary>
    public class AccountServiceTests : IDisposable
    {
        private readonly TestDatabaseFixture _db = new();
        private readonly IAccountService _service;

        public AccountServiceTests()
        {
            AppSession.DevMode = true;

            // AutoLinkEnabled=true افتراضياً الآن يفشل صراحةً لو ICustomerService/ISupplierService غير مسجَّلة
            // (بدل السكوت القديم) — أي اختبار ينشئ حساباً تحت جذر العملاء/الموردين يحتاج خدمة مسجَّلة، حتى لو
            // لم يكن يفحص الربط نفسه. تسجيل افتراضي بلا تأثير هنا؛ الاختبارات التي تفحص الربط الفعلي (134/187)
            // تبني حاويتها الخاصة محلياً بالـ Fake الذي تريد فحصه (آخر تسجيل لنفس النوع هو الفائز في DI).
            var services = TestDatabaseFixture.BuildServices(s =>
            {
                s.AddSingleton<ICustomerService>(new FakeCustomerService());
                s.AddSingleton<ISupplierService>(new FakeSupplierService());
            });
            _service = services.GetRequiredService<IAccountService>();
        }

        public void Dispose() => _db.Dispose();

        private int CustomersRootId() => _db.Services.GetRequiredService<IAccountRepository>().GetByCode("1220").Id;
        private int SuppliersRootId() => _db.Services.GetRequiredService<IAccountRepository>().GetByCode("2110").Id;

        private int SeedPostedEntry(string date, params (string Code, decimal Debit, decimal Credit)[] lines)
        {
            var journal = _db.Services.GetRequiredService<IJournalRepository>();
            var id = Db.RunTransaction((conn, tx) =>
            {
                var entry = new JournalEntry { EntryNo = $"TEST-{Guid.NewGuid():N}", EntryDate = date, Description = "test", Source = "test" };
                var newId = journal.InsertHeader(conn, tx, entry);
                int lineNo = 1;
                foreach (var l in lines)
                    journal.InsertLine(conn, tx, newId, lineNo++, new JournalLine { AccountCode = l.Code, Debit = l.Debit, Credit = l.Credit });
                return newId;
            });
            journal.SetPosted(id, true);
            return id;
        }

        private class FakeCustomerService : ICustomerService
        {
            public (string Code, string Name)? LastCreatedFor;
            public string? LastDeletedAccountCode;
            public (string Code, string Name)? LastNameSync;

            public Result<CustomerDto> CreateFromAccount(DbConnection conn, DbTransaction tx, string accountCode, string name)
            {
                LastCreatedFor = (accountCode, name);
                return Result.Ok(new CustomerDto { Id = 999, Code = "C-TEST", AccountCode = accountCode, Name = name });
            }

            public Result DeleteByAccountCode(DbConnection conn, DbTransaction tx, string accountCode)
            {
                LastDeletedAccountCode = accountCode;
                return Result.Ok();
            }

            public Result UpdateNameFromAccount(DbConnection conn, DbTransaction tx, string accountCode, string name)
            {
                LastNameSync = (accountCode, name);
                return Result.Ok();
            }

            // بقية ICustomerService غير مستخدَمة من AccountServiceTests — Fake مصغّر بقصد نفس الاختبارات فقط.
            public Result<PagedResult<CustomerDto>> GetPaged(int page, int pageSize, CustomerFilter filter = null) => throw new NotImplementedException();
            public Result<CustomerDto> GetById(int id) => throw new NotImplementedException();
            public Result<CustomerDto> GetByCode(string code) => throw new NotImplementedException();
            public Result<System.Collections.Generic.List<CustomerDto>> Search(string term, int maxResults = 50) => throw new NotImplementedException();
            public Result<System.Collections.Generic.List<AccountStatementLine>> GetStatement(int id, DateTime from, DateTime to) => throw new NotImplementedException();
            public Result<CustomerDto> Create(CreateCustomerDto dto) => throw new NotImplementedException();
            public Result<CustomerDto> Create(DbConnection conn, DbTransaction tx, CreateCustomerDto dto) => throw new NotImplementedException();
            public Result Update(UpdateCustomerDto dto) => throw new NotImplementedException();
            public Result Delete(int id) => throw new NotImplementedException();
            public Result RecalculateBalance(int id) => throw new NotImplementedException();
            public Result RecalculateAllBalances() => throw new NotImplementedException();
            public Result<CreditCheckResult> CheckCreditLimit(int id, decimal additional) => throw new NotImplementedException();
        }

        private class FakeSupplierService : ISupplierService
        {
            public (string Code, string Name)? LastCreatedFor;
            public string? LastDeletedAccountCode;
            public (string Code, string Name)? LastNameSync;

            public Result<SupplierDto> CreateFromAccount(DbConnection conn, DbTransaction tx, string accountCode, string name)
            {
                LastCreatedFor = (accountCode, name);
                return Result.Ok(new SupplierDto { Id = 999, Code = "S-TEST", AccountCode = accountCode, Name = name });
            }

            public Result DeleteByAccountCode(DbConnection conn, DbTransaction tx, string accountCode)
            {
                LastDeletedAccountCode = accountCode;
                return Result.Ok();
            }

            public Result UpdateNameFromAccount(DbConnection conn, DbTransaction tx, string accountCode, string name)
            {
                LastNameSync = (accountCode, name);
                return Result.Ok();
            }

            // بقية ISupplierService غير مستخدَمة من AccountServiceTests — Fake مصغّر بقصد نفس الاختبارات فقط.
            public Result<PagedResult<SupplierDto>> GetPaged(int page, int pageSize, SupplierFilter filter = null) => throw new NotImplementedException();
            public Result<SupplierDto> GetById(int id) => throw new NotImplementedException();
            public Result<SupplierDto> GetByCode(string code) => throw new NotImplementedException();
            public Result<System.Collections.Generic.List<SupplierDto>> Search(string term, int maxResults = 50) => throw new NotImplementedException();
            public Result<System.Collections.Generic.List<AccountStatementLine>> GetStatement(int id, DateTime from, DateTime to) => throw new NotImplementedException();
            public Result<SupplierDto> Create(CreateSupplierDto dto) => throw new NotImplementedException();
            public Result<SupplierDto> Create(DbConnection conn, DbTransaction tx, CreateSupplierDto dto) => throw new NotImplementedException();
            public Result Update(UpdateSupplierDto dto) => throw new NotImplementedException();
            public Result Delete(int id) => throw new NotImplementedException();
            public Result RecalculateBalance(int id) => throw new NotImplementedException();
            public Result RecalculateAllBalances() => throw new NotImplementedException();
            public Result<CreditCheckResult> CheckCreditLimit(int id, decimal additional) => throw new NotImplementedException();
        }

        [Fact]
        public void Create_GeneratesCorrectCode_AndLevelFromParent()
        {
            var result = _service.Create(new CreateAccountDto { ParentId = CustomersRootId(), Name = "عميل اختباري 1", IsLeaf = true });

            Assert.True(result.IsSuccess, result.ErrorMessage);
            Assert.Equal("1220001", result.Value.Code);
            Assert.Equal(4, result.Value.Level);
        }

        [Fact]
        public void Create_GeneratesCorrectCode_KeepsScalingPastFourLevels()
        {
            var l4 = _service.Create(new CreateAccountDto { ParentId = CustomersRootId(), Name = "مستوى 4", IsLeaf = false });
            Assert.True(l4.IsSuccess, l4.ErrorMessage);
            Assert.Equal("1220001", l4.Value.Code);
            Assert.Equal(4, l4.Value.Level);

            var l5 = _service.Create(new CreateAccountDto { ParentId = l4.Value.Id, Name = "مستوى 5", IsLeaf = true });
            Assert.True(l5.IsSuccess, l5.ErrorMessage);
            Assert.Equal("12200010001", l5.Value.Code);
            Assert.Equal(5, l5.Value.Level);
        }

        [Fact]
        public void Create_GeneratesCorrectCode_WhenMaxSuffixExceedsReservedWidth()
        {
            var repo = _db.Services.GetRequiredService<IAccountRepository>();
            repo.Insert(new Account { Code = "1220999", Name = "قرب الحد", ParentCode = "1220", Level = 4, IsLeaf = true, Type = 1 });

            var result = _service.Create(new CreateAccountDto { ParentId = CustomersRootId(), Name = "بعد الحد", IsLeaf = true });

            Assert.True(result.IsSuccess, result.ErrorMessage);
            Assert.Equal("12201000", result.Value.Code);
        }

        [Fact]
        public void Create_Fails_WhenChildSuffixExceedsMaxCap()
        {
            var repo = _db.Services.GetRequiredService<IAccountRepository>();
            repo.Insert(new Account { Code = "1220" + 9999, Name = "عند الحد", ParentCode = "1220", Level = 4, IsLeaf = true, Type = 1 });

            var result = _service.Create(new CreateAccountDto { ParentId = CustomersRootId(), Name = "بعد الحد الأقصى", IsLeaf = true });

            Assert.False(result.IsSuccess);
            Assert.Equal(ErrorCode.ValidationFailed, result.ErrorCode);
        }

        [Fact]
        public void Create_UnderLeafAccount_Fails()
        {
            var leaf = _service.Create(new CreateAccountDto { ParentId = CustomersRootId(), Name = "عميل leaf", IsLeaf = true });
            Assert.True(leaf.IsSuccess);

            var result = _service.Create(new CreateAccountDto { ParentId = leaf.Value.Id, Name = "ابن تحت leaf", IsLeaf = true });

            Assert.False(result.IsSuccess);
            Assert.Equal(ErrorCode.ValidationFailed, result.ErrorCode);
        }

        [Fact]
        public void Create_UnderCustomersRoot_CreatesLinkedCustomer()
        {
            var fake = new FakeCustomerService();
            var services = TestDatabaseFixture.BuildServices(s => s.AddSingleton<ICustomerService>(fake));
            var service = services.GetRequiredService<IAccountService>();

            var result = service.Create(new CreateAccountDto { ParentId = CustomersRootId(), Name = "عميل مرتبط", IsLeaf = true });

            Assert.True(result.IsSuccess, result.ErrorMessage);
            Assert.NotNull(fake.LastCreatedFor);
            Assert.Equal(result.Value.Code, fake.LastCreatedFor.Value.Code);
        }

        /// <summary>يتحقق أن ResolveAutoLink يعمل للمورد فعلياً عبر SupplierService الحقيقية (لا Fake) — R6.</summary>
        [Fact]
        public void Create_UnderSuppliersRoot_CreatesLinkedSupplier_ViaRealSupplierService()
        {
            var services = TestDatabaseFixture.BuildServices();
            var service = services.GetRequiredService<IAccountService>();
            var suppliers = services.GetRequiredService<ISupplierService>();

            var result = service.Create(new CreateAccountDto { ParentId = SuppliersRootId(), Name = "مورد مرتبط حقيقي", IsLeaf = true });

            Assert.True(result.IsSuccess, result.ErrorMessage);

            var bySearch = suppliers.Search("مورد مرتبط حقيقي");
            Assert.True(bySearch.IsSuccess);
            var created = Assert.Single(bySearch.Value);
            Assert.Equal(result.Value.Code, created.AccountCode);
            Assert.Equal("مورد مرتبط حقيقي", created.Name);
        }

        [Fact]
        public void Update_SetIsLeafTrue_WithChildren_Fails()
        {
            var parent = _service.Create(new CreateAccountDto { ParentId = CustomersRootId(), Name = "أب له ابن", IsLeaf = false });
            Assert.True(parent.IsSuccess, parent.ErrorMessage);

            var child = _service.Create(new CreateAccountDto { ParentId = parent.Value.Id, Name = "ابن", IsLeaf = true });
            Assert.True(child.IsSuccess, child.ErrorMessage);

            var result = _service.Update(new UpdateAccountDto { Id = parent.Value.Id, Name = parent.Value.Name, IsLeaf = true });

            Assert.False(result.IsSuccess);
        }

        [Fact]
        public void Delete_AccountWithChildren_Fails()
        {
            var parent = _service.Create(new CreateAccountDto { ParentId = CustomersRootId(), Name = "أب سيُحذف", IsLeaf = false });
            Assert.True(parent.IsSuccess);
            var child = _service.Create(new CreateAccountDto { ParentId = parent.Value.Id, Name = "ابنه", IsLeaf = true });
            Assert.True(child.IsSuccess);

            var result = _service.Delete(parent.Value.Id);

            Assert.False(result.IsSuccess);
        }

        [Fact]
        public void Delete_AccountWithTransactions_Fails()
        {
            var account = _service.Create(new CreateAccountDto { ParentId = CustomersRootId(), Name = "له قيد", IsLeaf = true });
            Assert.True(account.IsSuccess);

            SeedPostedEntry("2026-01-01", (account.Value.Code, 100m, 0m), ("1240", 0m, 100m));

            var result = _service.Delete(account.Value.Id);

            Assert.False(result.IsSuccess);
        }

        [Fact]
        public void Delete_RemovesLinkedCustomer()
        {
            var fake = new FakeCustomerService();
            var services = TestDatabaseFixture.BuildServices(s => s.AddSingleton<ICustomerService>(fake));
            var service = services.GetRequiredService<IAccountService>();

            var account = service.Create(new CreateAccountDto { ParentId = CustomersRootId(), Name = "عميل سيُحذف", IsLeaf = true });
            Assert.True(account.IsSuccess);

            var result = service.Delete(account.Value.Id);

            Assert.True(result.IsSuccess, result.ErrorMessage);
            Assert.Equal(account.Value.Code, fake.LastDeletedAccountCode);
        }

        [Fact]
        public void RecalculateBalance_ComputesFromPostedEntries()
        {
            var account = _service.Create(new CreateAccountDto { ParentId = CustomersRootId(), Name = "حساب رصيد", IsLeaf = true });
            Assert.True(account.IsSuccess);

            SeedPostedEntry("2026-01-01", (account.Value.Code, 300m, 0m), ("1240", 0m, 300m));
            SeedPostedEntry("2026-01-05", (account.Value.Code, 0m, 50m), ("1240", 50m, 0m));

            var result = _service.RecalculateBalance(account.Value.Code);
            Assert.True(result.IsSuccess, result.ErrorMessage);

            var updated = _db.Services.GetRequiredService<IAccountRepository>().GetByCode(account.Value.Code);
            Assert.Equal(250m, updated.Balance);
        }

        [Fact]
        public void GetStatement_ComputesRunningBalanceCorrectly()
        {
            var account = _service.Create(new CreateAccountDto { ParentId = CustomersRootId(), Name = "حساب كشف", IsLeaf = true });
            Assert.True(account.IsSuccess);

            SeedPostedEntry("2026-01-01", (account.Value.Code, 200m, 0m), ("1240", 0m, 200m));
            SeedPostedEntry("2026-01-10", (account.Value.Code, 0m, 80m), ("1240", 80m, 0m));

            var result = _service.GetStatement(account.Value.Code, new DateTime(2026, 1, 1), new DateTime(2026, 1, 31));

            Assert.True(result.IsSuccess, result.ErrorMessage);
            Assert.Equal(3, result.Value.Count); // افتتاحي + سطران
            Assert.Equal(0m, result.Value[0].RunningBalance);
            Assert.Equal(200m, result.Value[1].RunningBalance);
            Assert.Equal(120m, result.Value[2].RunningBalance);
        }

        [Fact]
        public void Create_WithoutPermission_ReturnsFail()
        {
            AppSession.DevMode = false;
            try
            {
                var result = _service.Create(new CreateAccountDto { ParentId = CustomersRootId(), Name = "بلا صلاحية", IsLeaf = true });

                Assert.False(result.IsSuccess);
                Assert.Equal(ErrorCode.Unauthorized, result.ErrorCode);
            }
            finally
            {
                AppSession.DevMode = true;
            }
        }
    }
}
