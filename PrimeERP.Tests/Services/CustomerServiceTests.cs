using System;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Platform.Permissions;
using PrimeERP.Domain.Enums;
using PrimeERP.Domain.Results;
using PrimeERP.Data.Repositories;
using PrimeERP.Platform.Settings;
using PrimeERP.Domain.Entities;
using PrimeERP.Platform.Localization;
using PrimeERP.UI.Services;
using PrimeERP.Application.Services;
using PrimeERP.Application.Services.Accounting;
using PrimeERP.Application.DTOs.Accounting;
using PrimeERP.Application.Services.Parties;
using PrimeERP.Application.DTOs.Parties;
using Xunit;
using Db = PrimeERP.Data.Core.DbHelper;

namespace PrimeERP.Tests.Services
{
    /// <summary>قاعدة بيانات خاصة معزولة لكل اختبار — نفس سبب AccountServiceTests/JournalServiceTests (شجرة حسابات نظيفة، لا تسرّب حسابات/عملاء بين الاختبارات).</summary>
    public class CustomerServiceTests : IDisposable
    {
        private readonly TestDatabaseFixture _db = new();
        private readonly ICustomerService _service;
        private readonly IAccountService _accounts;
        private readonly ISettingsService _settings;
        private readonly INumberSequenceService _numbers;

        public CustomerServiceTests()
        {
            AppSession.DevMode = true;

            // AccountService.Create/Update/Delete تحلّان ICustomerService عبر IServiceProvider (حاوية DI —
            // R3) عند الإنشاء/التعديل/الحذف تحت جذر العملاء. بما أن _service وAccountService الداخلية يُحلّان
            // من نفس _db.Services، الحاوية تربطهما تلقائياً — لا تسجيل يدوي لازم بعد.
            _service = _db.Services.GetRequiredService<ICustomerService>();
            _accounts = _db.Services.GetRequiredService<IAccountService>();
            _settings = _db.Services.GetRequiredService<ISettingsService>();
            _numbers = _db.Services.GetRequiredService<INumberSequenceService>();
        }

        public void Dispose() => _db.Dispose();

        private static int CustomersRootId() => AccountRepository.GetByCode("1220").Id;

        private static CreateCustomerDto BasicDto(string name = "عميل اختباري") => new()
        {
            Name = name, Phone = "0100000000", Email = "test@example.com", CreditLimit = 1000m, PaymentTermDays = 30
        };

        private static void SeedPostedEntry(string date, params (string Code, decimal Debit, decimal Credit)[] lines)
        {
            var id = Db.RunTransaction((conn, tx) =>
            {
                var entry = new JournalEntry { EntryNo = $"TEST-{Guid.NewGuid():N}", EntryDate = date, Description = "test", Source = "test" };
                var newId = JournalRepository.InsertHeader(conn, tx, entry);
                int lineNo = 1;
                foreach (var l in lines)
                    JournalRepository.InsertLine(conn, tx, newId, lineNo++, new JournalLine { AccountCode = l.Code, Debit = l.Debit, Credit = l.Credit });
                return newId;
            });
            JournalRepository.SetPosted(id, true);
        }

        // ===================== الربط ثنائي الاتجاه (الأهم) =====================

        [Fact]
        public void Create_CreatesLinkedAccount_UnderCustomersRoot()
        {
            var result = _service.Create(BasicDto());

            Assert.True(result.IsSuccess, result.ErrorMessage);
            Assert.False(string.IsNullOrWhiteSpace(result.Value.AccountCode));

            var account = AccountRepository.GetByCode(result.Value.AccountCode);
            Assert.NotNull(account);
            Assert.Equal("1220", account.ParentCode);
            Assert.True(account.IsLeaf);
        }

        [Fact]
        public void Create_CreatesExactlyOneAccountAndOneCustomer_NoInfiniteLoop()
        {
            var before = AccountRepository.GetChildren("1220").Count;

            var result = _service.Create(BasicDto());
            Assert.True(result.IsSuccess, result.ErrorMessage);

            Assert.Equal(before + 1, AccountRepository.GetChildren("1220").Count);
            Assert.Single(CustomerRepository.GetAll(activeOnly: false));
        }

        [Fact]
        public void CreateAccount_UnderCustomersRoot_CreatesLinkedCustomer()
        {
            var accountResult = _accounts.Create(new CreateAccountDto { ParentId = CustomersRootId(), Name = "عميل عبر حساب", IsLeaf = true });
            Assert.True(accountResult.IsSuccess, accountResult.ErrorMessage);

            var customer = CustomerRepository.GetByAccountCode(accountResult.Value.Code);
            Assert.NotNull(customer);
            Assert.Equal("عميل عبر حساب", customer.Name);
        }

        [Fact]
        public void CreateAccount_UnderCustomersRoot_CreatesExactlyOneCustomer_NoInfiniteLoop()
        {
            var accountResult = _accounts.Create(new CreateAccountDto { ParentId = CustomersRootId(), Name = "عميل عبر حساب 2", IsLeaf = true });
            Assert.True(accountResult.IsSuccess, accountResult.ErrorMessage);

            Assert.Single(CustomerRepository.GetAll(activeOnly: false));
            Assert.Single(AccountRepository.GetChildren("1220")); // حساب واحد فقط — لا حسابات إضافية من حلقة
        }

        [Fact]
        public void Update_CustomerName_ChangesAccountName()
        {
            var created = _service.Create(BasicDto("اسم قديم"));
            Assert.True(created.IsSuccess, created.ErrorMessage);

            var update = new UpdateCustomerDto
            {
                Id = created.Value.Id, Name = "اسم جديد", Phone = created.Value.Phone,
                CreditLimit = created.Value.CreditLimit, PaymentTermDays = created.Value.PaymentTermDays, IsActive = true
            };
            var result = _service.Update(update);
            Assert.True(result.IsSuccess, result.ErrorMessage);

            Assert.Equal("اسم جديد", AccountRepository.GetByCode(created.Value.AccountCode).Name);
        }

        [Fact]
        public void Update_AccountName_ChangesCustomerName()
        {
            var created = _service.Create(BasicDto("اسم قديم 2"));
            Assert.True(created.IsSuccess, created.ErrorMessage);

            var account = AccountRepository.GetByCode(created.Value.AccountCode);
            var result = _accounts.Update(new UpdateAccountDto { Id = account.Id, Name = "اسم محدَّث من الحساب", IsLeaf = true, Notes = account.Notes });
            Assert.True(result.IsSuccess, result.ErrorMessage);

            Assert.Equal("اسم محدَّث من الحساب", CustomerRepository.GetById(created.Value.Id).Name);
        }

        [Fact]
        public void Delete_Customer_DeletesAccount()
        {
            var created = _service.Create(BasicDto("سيُحذف"));
            Assert.True(created.IsSuccess, created.ErrorMessage);

            var result = _service.Delete(created.Value.Id);
            Assert.True(result.IsSuccess, result.ErrorMessage);

            Assert.False(AccountRepository.GetByCode(created.Value.AccountCode).IsActive);
        }

        [Fact]
        public void Delete_Account_DeletesCustomer()
        {
            var created = _service.Create(BasicDto("سيُحذف عبر الحساب"));
            Assert.True(created.IsSuccess, created.ErrorMessage);

            var account = AccountRepository.GetByCode(created.Value.AccountCode);
            var result = _accounts.Delete(account.Id);
            Assert.True(result.IsSuccess, result.ErrorMessage);

            Assert.Null(CustomerRepository.GetById(created.Value.Id));
        }

        // ===================== الأساسيات =====================

        [Fact]
        public void Create_WithoutCustomersAccountConfigured_Fails()
        {
            _settings.Set(SettingKeys.Accounts.Customers, "");

            var result = _service.Create(BasicDto());

            Assert.False(result.IsSuccess);
        }

        [Fact]
        public void Create_GeneratesSequentialCodes()
        {
            var first = _service.Create(BasicDto("الأول"));
            var second = _service.Create(BasicDto("الثاني"));

            Assert.True(first.IsSuccess, first.ErrorMessage);
            Assert.True(second.IsSuccess, second.ErrorMessage);
            Assert.NotEqual(first.Value.Code, second.Value.Code);
            Assert.StartsWith("C-", first.Value.Code);
        }

        [Fact]
        public void Create_WithLeafParentAccount_Fails()
        {
            var leafAccount = _accounts.Create(new CreateAccountDto { ParentId = AccountRepository.GetByCode("1210").Id, Name = "حساب ورقي", IsLeaf = true });
            Assert.True(leafAccount.IsSuccess, leafAccount.ErrorMessage);

            _settings.Set(SettingKeys.Accounts.Customers, leafAccount.Value.Code);

            var result = _service.Create(BasicDto());

            Assert.False(result.IsSuccess);
        }

        [Fact]
        public void Create_AccountCreationFailure_RollsBackEverything()
        {
            // تصادم كود الحساب لا يصلح لاختبار rollback: GenerateChildCodeInternal يحسب max+1 على الأبناء
            // الحاليين فعلياً، فأي كود أُدرَج يدوياً مسبقاً يُقرأ كابن قائم ويُتجنَّب تلقائياً (لا تصادم فعلي).
            // البديل الحقيقي: نزرع تصادماً على كود العميل نفسه — نُدرج عميلاً بنفس الكود الذي سيولّده
            // NumberSequenceService.Next("Customer") تالياً (Peek لا يستهلك الرقم) — الحساب يُنشأ بنجاح داخل
            // المعاملة، ثم يفشل CustomerRepository.Insert بخرق قيد تفرّد Code، فيتراجع كل شيء (بما فيه الحساب
            // الذي أُنشئ للتو في نفس المعاملة) — rollback فعلي بفشل حقيقي، لا فشل تحقق مسبق.
            var nextCode = _numbers.Peek("Customer");
            CustomerRepository.Insert(new Customer { Code = nextCode, Name = "عميل موجود مسبقاً", IsActive = true });

            var accountsBefore = AccountRepository.GetChildren("1220").Count;

            var result = _service.Create(BasicDto("سيفشل بسبب تصادم كود العميل"));

            Assert.False(result.IsSuccess);
            Assert.Single(CustomerRepository.GetAll(activeOnly: false)); // العميل المزروع فقط، لا عميل إضافي معلَّق
            Assert.Equal(accountsBefore, AccountRepository.GetChildren("1220").Count); // الحساب الذي أُنشئ تراجع أيضاً
        }

        // ===================== الائتمان =====================

        [Fact]
        public void CheckCreditLimit_ZeroLimit_AlwaysAllows()
        {
            var created = _service.Create(new CreateCustomerDto { Name = "بلا حد" });
            Assert.True(created.IsSuccess, created.ErrorMessage);

            var result = _service.CheckCreditLimit(created.Value.Id, 1_000_000m);

            Assert.True(result.IsSuccess, result.ErrorMessage);
            Assert.True(result.Value.IsAllowed);
        }

        [Fact]
        public void CheckCreditLimit_ExceedsLimit_FailsWithAmount()
        {
            var created = _service.Create(new CreateCustomerDto { Name = "له حد", CreditLimit = 500m });
            Assert.True(created.IsSuccess, created.ErrorMessage);

            var result = _service.CheckCreditLimit(created.Value.Id, 600m);

            Assert.False(result.IsSuccess);
            Assert.Equal(ErrorCode.ValidationFailed, result.ErrorCode);
            Assert.Contains("100", result.ErrorMessage); // الفرق المتجاوز = 600 - 500 = 100
        }

        [Fact]
        public void IsOverCreditLimit_ComputedCorrectlyInDto()
        {
            var created = _service.Create(new CreateCustomerDto { Name = "متجاوز", CreditLimit = 100m });
            Assert.True(created.IsSuccess, created.ErrorMessage);

            Db.RunTransaction((conn, tx) => CustomerRepository.SetBalance(conn, tx, created.Value.Id, 500m));

            var reloaded = _service.GetById(created.Value.Id);

            Assert.True(reloaded.IsSuccess, reloaded.ErrorMessage);
            Assert.True(reloaded.Value.IsOverCreditLimit);
            Assert.Equal(StatusVariant.Danger, reloaded.Value.StatusVariant);
        }

        // ===================== الأرصدة =====================

        [Fact]
        public void RecalculateBalance_MatchesAccountBalance()
        {
            var created = _service.Create(BasicDto("رصيد مطابق"));
            Assert.True(created.IsSuccess, created.ErrorMessage);

            SeedPostedEntry("2026-01-05", (created.Value.AccountCode, 300m, 0m), ("1240", 0m, 300m));
            _accounts.RecalculateBalance(created.Value.AccountCode);

            var result = _service.RecalculateBalance(created.Value.Id);
            Assert.True(result.IsSuccess, result.ErrorMessage);

            Assert.Equal(AccountRepository.GetByCode(created.Value.AccountCode).Balance, CustomerRepository.GetById(created.Value.Id).Balance);
        }

        [Fact]
        public void PostedEntry_ChangesCustomerBalance_AfterRecalculate()
        {
            var created = _service.Create(BasicDto("قبل وبعد"));
            Assert.True(created.IsSuccess, created.ErrorMessage);
            Assert.Equal(0m, CustomerRepository.GetById(created.Value.Id).Balance);

            SeedPostedEntry("2026-01-05", (created.Value.AccountCode, 750m, 0m), ("1240", 0m, 750m));
            _service.RecalculateBalance(created.Value.Id);

            Assert.Equal(750m, CustomerRepository.GetById(created.Value.Id).Balance);
        }

        // ===================== الصلاحيات =====================

        [Fact]
        public void Create_WithoutPermission_Fails()
        {
            AppSession.DevMode = false;
            try
            {
                var result = _service.Create(BasicDto());
                Assert.False(result.IsSuccess);
                Assert.Equal(ErrorCode.Unauthorized, result.ErrorCode);
            }
            finally { AppSession.DevMode = true; }
        }

        [Fact]
        public void Update_WithoutPermission_Fails()
        {
            var created = _service.Create(BasicDto("قبل التعديل"));
            Assert.True(created.IsSuccess);

            AppSession.DevMode = false;
            try
            {
                var result = _service.Update(new UpdateCustomerDto { Id = created.Value.Id, Name = "بعد", IsActive = true });
                Assert.False(result.IsSuccess);
                Assert.Equal(ErrorCode.Unauthorized, result.ErrorCode);
            }
            finally { AppSession.DevMode = true; }
        }

        [Fact]
        public void Delete_WithoutPermission_Fails()
        {
            var created = _service.Create(BasicDto("قبل الحذف"));
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
