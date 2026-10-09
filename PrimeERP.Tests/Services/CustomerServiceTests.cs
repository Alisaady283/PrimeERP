using PrimeERP.Application.PageServices.Admin;
using PrimeERP.Application.Services.Ledger;
using PrimeERP.Application.Services.Core;
using PrimeERP.Data.Core;
using System;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Platform.Permissions;
using PrimeERP.Domain.Calculations;
using PrimeERP.Domain.Enums;
using PrimeERP.Domain.Results;
using PrimeERP.Data.Repositories;
using PrimeERP.Platform.Settings;
using PrimeERP.Domain.Entities;
using PrimeERP.Platform.Localization;
using PrimeERP.UI.Services;
using PrimeERP.Application.PageServices.Accounting;
using PrimeERP.Application.DTOs.Accounting;
using PrimeERP.Application.PageServices.Parties;
using PrimeERP.Application.DTOs.Parties;
using Xunit;

namespace PrimeERP.Tests.Services
{
    /// <summary>قاعدة بيانات خاصة معزولة لكل</summary>
    public class CustomerServiceTests : IDisposable
    {
        private readonly TestDatabaseFixture _db = new();
        private readonly ICustomerService _service;
        private readonly IAccountService _accounts;
        private readonly ISettingsService _settings;
        private readonly INumberSequenceService _numbers;
        private readonly IAccountRepository _accountRepo;
        private readonly IPartyRepository<Customer> _customerRepo;

        public CustomerServiceTests()
        {
            AppSession.DevMode = true;

            _service = _db.Services.GetRequiredService<ICustomerService>();
            _accounts = _db.Services.GetRequiredService<IAccountService>();
            _settings = _db.Services.GetRequiredService<ISettingsService>();
            _numbers = _db.Services.GetRequiredService<INumberSequenceService>();
            _accountRepo = _db.Services.GetRequiredService<IAccountRepository>();
            _customerRepo = _db.Services.GetRequiredService<IPartyRepository<Customer>>();
        }

        public void Dispose() => _db.Dispose();

        private int CustomersRootId() => _db.Services.GetRequiredService<IAccountRepository>().GetByCode("1202").Id;

        private static Customer Basic(string name = "عميل اختباري") => new()
        {
            Name = name, Phone = "0100000000", Email = "test@example.com", CreditLimit = 1000m, PaymentTermDays = 30
        };

        private void SeedPostedEntry(string date, params (string Code, decimal Debit, decimal Credit)[] lines)
        {
            var journal = _db.Services.GetRequiredService<IJournalRepository>();
            var id = DbContextFactory.RunTransaction(db =>
            {
                var entry = new JournalEntry { EntryNo = $"TEST-{Guid.NewGuid():N}", EntryDate = date, Description = "test", Source = "test" };
                var newId = journal.InsertHeader(db, entry);
                int lineNo = 1;
                foreach (var l in lines)
                    journal.InsertLine(db, newId, lineNo++, new JournalLine { AccountCode = l.Code, Debit = l.Debit, Credit = l.Credit });
                return newId;
            });
            journal.SetPosted(id, true);
        }


        [Fact]
        public void Create_CreatesLinkedAccount_UnderCustomersRoot()
        {
            var result = _service.Create(Basic());

            Assert.True(result.IsSuccess, result.ErrorMessage);
            Assert.False(string.IsNullOrWhiteSpace(result.Value.AccountCode));

            var account = _accountRepo.GetByCode(result.Value.AccountCode);
            Assert.NotNull(account);
            Assert.Equal("1202", account.ParentCode);
            Assert.True(account.IsLeaf);
        }

        [Fact]
        public void Create_CreatesExactlyOneAccountAndOneCustomer_NoInfiniteLoop()
        {
            var before = _accountRepo.GetChildren("1202").Count;

            var result = _service.Create(Basic());
            Assert.True(result.IsSuccess, result.ErrorMessage);

            Assert.Equal(before + 1, _accountRepo.GetChildren("1202").Count);
            Assert.Single(_customerRepo.GetAll(activeOnly: false));
        }

        [Fact]
        public void CreateAccount_UnderCustomersRoot_CreatesLinkedCustomer()
        {
            var accountResult = _accounts.Create(new CreateAccountDto { ParentId = CustomersRootId(), Name = "عميل عبر حساب", IsLeaf = true });
            Assert.True(accountResult.IsSuccess, accountResult.ErrorMessage);

            var customer = _customerRepo.GetByAccountCode(accountResult.Value.Code);
            Assert.NotNull(customer);
            Assert.Equal("عميل عبر حساب", customer.Name);
        }

        [Fact]
        public void CreateAccount_UnderCustomersRoot_CreatesExactlyOneCustomer_NoInfiniteLoop()
        {
            var accountResult = _accounts.Create(new CreateAccountDto { ParentId = CustomersRootId(), Name = "عميل عبر حساب 2", IsLeaf = true });
            Assert.True(accountResult.IsSuccess, accountResult.ErrorMessage);

            Assert.Single(_customerRepo.GetAll(activeOnly: false));
            Assert.Single(_accountRepo.GetChildren("1202")); // حساب واحد فقط — لا حسابات إضافية من حلقة
        }

        [Fact]
        public void Update_CustomerName_ChangesAccountName()
        {
            var created = _service.Create(Basic("اسم قديم"));
            Assert.True(created.IsSuccess, created.ErrorMessage);

            var update = _service.GetById(created.Value.Id).Value;
            update.Name = "اسم جديد";
            var result = _service.Update(update);
            Assert.True(result.IsSuccess, result.ErrorMessage);

            Assert.Equal("اسم جديد", _accountRepo.GetByCode(created.Value.AccountCode).Name);
        }

        [Fact]
        public void Update_AccountName_ChangesCustomerName()
        {
            var created = _service.Create(Basic("اسم قديم 2"));
            Assert.True(created.IsSuccess, created.ErrorMessage);

            var account = _accountRepo.GetByCode(created.Value.AccountCode);
            var result = _accounts.Update(new UpdateAccountDto { Id = account.Id, Name = "اسم محدَّث من الحساب", IsLeaf = true, Notes = account.Notes });
            Assert.True(result.IsSuccess, result.ErrorMessage);

            Assert.Equal("اسم محدَّث من الحساب", _customerRepo.GetById(created.Value.Id).Name);
        }

        [Fact]
        public void Delete_Customer_DeletesAccount()
        {
            var created = _service.Create(Basic("سيُحذف"));
            Assert.True(created.IsSuccess, created.ErrorMessage);

            var result = _service.Delete(created.Value.Id);
            Assert.True(result.IsSuccess, result.ErrorMessage);

            Assert.False(_accountRepo.GetByCode(created.Value.AccountCode).IsActive);
        }

        [Fact]
        public void Delete_Account_DeletesCustomer()
        {
            var created = _service.Create(Basic("سيُحذف عبر الحساب"));
            Assert.True(created.IsSuccess, created.ErrorMessage);

            var account = _accountRepo.GetByCode(created.Value.AccountCode);
            var result = _accounts.Delete(account.Id);
            Assert.True(result.IsSuccess, result.ErrorMessage);

            Assert.Null(_customerRepo.GetById(created.Value.Id));
        }


        [Fact]
        public void Create_WithoutCustomersAccountConfigured_Fails()
        {
            _settings.Set(SettingKeys.Accounts.Customers, "");

            var result = _service.Create(Basic());

            Assert.False(result.IsSuccess);
        }

        [Fact]
        public void Create_GeneratesSequentialCodes()
        {
            var first = _service.Create(Basic("الأول"));
            var second = _service.Create(Basic("الثاني"));

            Assert.True(first.IsSuccess, first.ErrorMessage);
            Assert.True(second.IsSuccess, second.ErrorMessage);
            Assert.NotEqual(first.Value.Code, second.Value.Code);
            Assert.Matches("^C[0-9]+$", first.Value.Code);
        }

        [Fact]
        public void Create_WithLeafParentAccount_Fails()
        {
            var leafAccount = _accounts.Create(new CreateAccountDto { ParentId = _accountRepo.GetByCode("52").Id, Name = "حساب ورقي", IsLeaf = true });
            Assert.True(leafAccount.IsSuccess, leafAccount.ErrorMessage);

            _settings.Set(SettingKeys.Accounts.Customers, leafAccount.Value.Code);

            var result = _service.Create(Basic());

            Assert.False(result.IsSuccess);
        }

        [Fact]
        public void Create_AccountCreationFailure_RollsBackEverything()
        {
            var nextCode = _numbers.Peek("Customer");
            _customerRepo.Insert(new Customer { Code = nextCode, Name = "عميل موجود مسبقاً", IsActive = true });

            var accountsBefore = _accountRepo.GetChildren("1202").Count;

            var result = _service.Create(Basic("سيفشل بسبب تصادم كود العميل"));

            Assert.False(result.IsSuccess);
            Assert.Single(_customerRepo.GetAll(activeOnly: false)); // العميل المزروع فقط، لا عميل إضافي معلَّق
            Assert.Equal(accountsBefore, _accountRepo.GetChildren("1202").Count); // الحساب الذي أُنشئ تراجع أيضاً
        }


        [Fact]
        public void CheckCreditLimit_ZeroLimit_AlwaysAllows()
        {
            var created = _service.Create(new Customer { Name = "بلا حد" });
            Assert.True(created.IsSuccess, created.ErrorMessage);

            var result = _service.CheckCreditLimit(created.Value.Id, 1_000_000m);

            Assert.True(result.IsSuccess, result.ErrorMessage);
            Assert.True(result.Value.IsAllowed);
        }

        [Fact]
        public void CheckCreditLimit_ExceedsLimit_FailsWithAmount()
        {
            var created = _service.Create(new Customer { Name = "له حد", CreditLimit = 500m });
            Assert.True(created.IsSuccess, created.ErrorMessage);

            var result = _service.CheckCreditLimit(created.Value.Id, 600m);

            Assert.False(result.IsSuccess);
            Assert.Equal(ErrorCode.ValidationFailed, result.ErrorCode);
            Assert.Contains("100", result.ErrorMessage); // الفرق المتجاوز = 600 - 500 = 100
        }

        [Fact]
        public void IsOverCreditLimit_FromStoredBalance()
        {
            var created = _service.Create(new Customer { Name = "متجاوز", CreditLimit = 100m });
            Assert.True(created.IsSuccess, created.ErrorMessage);

            DbContextFactory.RunTransaction(db => _customerRepo.SetBalance(created.Value.Id, 500m, db));

            var reloaded = _service.GetById(created.Value.Id);

            Assert.True(reloaded.IsSuccess, reloaded.ErrorMessage);
            Assert.True(PartyCalc.IsOverCreditLimit(reloaded.Value.Balance, reloaded.Value.CreditLimit));
        }

        [Fact]
        public void Update_KeepsStoredBalance()
        {
            var created = _service.Create(Basic("رصيد محفوظ"));
            Assert.True(created.IsSuccess, created.ErrorMessage);

            var edited = _service.GetById(created.Value.Id).Value;
            DbContextFactory.RunTransaction(db => _customerRepo.SetBalance(created.Value.Id, 250m, db));

            edited.Name = "اسم بعد الرصيد";
            var result = _service.Update(edited);

            Assert.True(result.IsSuccess, result.ErrorMessage);
            Assert.Equal(250m, _customerRepo.GetById(created.Value.Id).Balance);
        }


        [Fact]
        public void RecalculateBalance_MatchesAccountBalance()
        {
            var created = _service.Create(Basic("رصيد مطابق"));
            Assert.True(created.IsSuccess, created.ErrorMessage);

            SeedPostedEntry("2026-01-05", (created.Value.AccountCode, 300m, 0m), ("12030070001", 0m, 300m));
            _accounts.RecalculateBalance(created.Value.AccountCode);

            var result = _service.RecalculateBalance(created.Value.Id);
            Assert.True(result.IsSuccess, result.ErrorMessage);

            Assert.Equal(_accountRepo.GetByCode(created.Value.AccountCode).Balance, _customerRepo.GetById(created.Value.Id).Balance);
        }

        [Fact]
        public void PostedEntry_ChangesCustomerBalance_AfterRecalculate()
        {
            var created = _service.Create(Basic("قبل وبعد"));
            Assert.True(created.IsSuccess, created.ErrorMessage);
            Assert.Equal(0m, _customerRepo.GetById(created.Value.Id).Balance);

            SeedPostedEntry("2026-01-05", (created.Value.AccountCode, 750m, 0m), ("12030070001", 0m, 750m));
            _service.RecalculateBalance(created.Value.Id);

            Assert.Equal(750m, _customerRepo.GetById(created.Value.Id).Balance);
        }


        [Fact]
        public void Create_WithoutPermission_Fails()
        {
            AppSession.DevMode = false;
            try
            {
                var result = _service.Create(Basic());
                Assert.False(result.IsSuccess);
                Assert.Equal(ErrorCode.Unauthorized, result.ErrorCode);
            }
            finally { AppSession.DevMode = true; }
        }

        [Fact]
        public void Update_WithoutPermission_Fails()
        {
            var created = _service.Create(Basic("قبل التعديل"));
            Assert.True(created.IsSuccess);

            AppSession.DevMode = false;
            try
            {
                var result = _service.Update(new Customer { Id = created.Value.Id, Name = "بعد" });
                Assert.False(result.IsSuccess);
                Assert.Equal(ErrorCode.Unauthorized, result.ErrorCode);
            }
            finally { AppSession.DevMode = true; }
        }

        [Fact]
        public void Delete_WithoutPermission_Fails()
        {
            var created = _service.Create(Basic("قبل الحذف"));
            Assert.True(created.IsSuccess);

            AppSession.DevMode = false;
            try
            {
                var result = _service.Delete(created.Value.Id);
                Assert.False(result.IsSuccess);
                Assert.Equal(ErrorCode.Unauthorized, result.ErrorCode);
            }
            finally { AppSession.DevMode = true; }
        }
    }
}
