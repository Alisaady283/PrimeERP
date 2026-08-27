using System;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Application.Validation;
using PrimeERP.Data.Repositories;
using PrimeERP.Domain.Entities;
using PrimeERP.Platform.Permissions;
using Xunit;

namespace PrimeERP.Tests.Validation
{
    /// <summary>
    /// السبعة Validators (راجع ARCHITECTURE.md / R6 "RuleSet fluent validation") لم تكن مُختبرة مباشرة —
    /// فقط المُستهلَكة فعلياً (Account/Customer/Supplier/Journal) كانت تُمَرّ عبرها بشكل غير مباشر ضمن
    /// اختبارات الخدمة (نجاح/فشل الإنشاء). هنا كل السبعة تُختبر منعزلة، بما فيها الأربعة غير المُستهلَكة بعد
    /// (Employee/Product/User/Invoice) — التغطية الوحيدة الممكنة لمنطقها حالياً بلا خدمة تستهلكها.
    /// </summary>
    public class ValidatorsTests
    {
        [Fact]
        public void CustomerValidator_Fails_WhenNameMissing()
        {
            var result = new CustomerValidator().Validate(new Customer { Code = "C001", Name = "" });
            Assert.False(result.IsValid);
            Assert.NotNull(result["Name"]);
        }

        [Fact]
        public void CustomerValidator_Fails_OnInvalidEmail()
        {
            var result = new CustomerValidator().Validate(new Customer { Code = "C001", Name = "عميل", Email = "not-an-email" });
            Assert.False(result.IsValid);
            Assert.NotNull(result["Email"]);
        }

        [Fact]
        public void CustomerValidator_Fails_OnNegativeCreditLimit()
        {
            var result = new CustomerValidator().Validate(new Customer { Code = "C001", Name = "عميل", CreditLimit = -1 });
            Assert.False(result.IsValid);
            Assert.NotNull(result["CreditLimit"]);
        }

        [Fact]
        public void CustomerValidator_Succeeds_WithValidData()
        {
            var result = new CustomerValidator().Validate(new Customer
            {
                Code = "C001",
                Name = "عميل صحيح",
                Email = "test@example.com",
                Phone = "0501234567",
                CreditLimit = 1000
            });
            Assert.True(result.IsValid);
        }

        [Fact]
        public void SupplierValidator_Fails_WhenCodeMissing()
        {
            var result = new SupplierValidator().Validate(new Supplier { Name = "مورد" });
            Assert.False(result.IsValid);
            Assert.NotNull(result["Code"]);
        }

        [Fact]
        public void SupplierValidator_Fails_OnInvalidPhone()
        {
            var result = new SupplierValidator().Validate(new Supplier { Code = "S001", Name = "مورد", Phone = "123" });
            Assert.False(result.IsValid);
            Assert.NotNull(result["Phone"]);
        }

        [Fact]
        public void SupplierValidator_Succeeds_WithValidData()
        {
            var result = new SupplierValidator().Validate(new Supplier { Code = "S001", Name = "مورد صحيح", CreditLimit = 0 });
            Assert.True(result.IsValid);
        }

        [Fact]
        public void JournalValidator_Fails_WhenUnbalanced()
        {
            var entry = new JournalEntry
            {
                EntryDate = "2026-01-01",
                Lines = new()
                {
                    new() { AccountCode = "1204", Debit = 100m, Credit = 0m },
                    new() { AccountCode = "41", Debit = 0m,   Credit = 50m }
                }
            };

            var result = new JournalValidator().Validate(entry);
            Assert.False(result.IsValid);
        }

        [Fact]
        public void JournalValidator_Fails_WhenFewerThanTwoLines()
        {
            var entry = new JournalEntry
            {
                EntryDate = "2026-01-01",
                Lines = new() { new() { AccountCode = "1204", Debit = 100m, Credit = 0m } }
            };

            var result = new JournalValidator().Validate(entry);
            Assert.False(result.IsValid);
        }

        [Fact]
        public void JournalValidator_Fails_WhenLineHasBothDebitAndCredit()
        {
            var entry = new JournalEntry
            {
                EntryDate = "2026-01-01",
                Lines = new()
                {
                    new() { AccountCode = "1204", Debit = 100m, Credit = 100m },
                    new() { AccountCode = "41", Debit = 0m,   Credit = 100m }
                }
            };

            var result = new JournalValidator().Validate(entry);
            Assert.False(result.IsValid);
        }

        [Fact]
        public void JournalValidator_Fails_WhenInvalidDate()
        {
            var entry = new JournalEntry
            {
                EntryDate = "not-a-date",
                Lines = new()
                {
                    new() { AccountCode = "1204", Debit = 100m, Credit = 0m },
                    new() { AccountCode = "41", Debit = 0m,   Credit = 100m }
                }
            };

            var result = new JournalValidator().Validate(entry);
            Assert.False(result.IsValid);
        }

        [Fact]
        public void JournalValidator_Succeeds_WhenBalancedWithValidLines()
        {
            var entry = new JournalEntry
            {
                EntryDate = "2026-01-01",
                Lines = new()
                {
                    new() { AccountCode = "1204", Debit = 100m, Credit = 0m },
                    new() { AccountCode = "41", Debit = 0m,   Credit = 100m }
                }
            };

            var result = new JournalValidator().Validate(entry);
            Assert.True(result.IsValid);
        }

        [Fact]
        public void EmployeeValidator_Fails_WhenNameMissing()
        {
            var result = new EmployeeValidator().Validate(new Employee { Code = "E001", Name = "", HireDate = DateTime.Today });
            Assert.False(result.IsValid);
            Assert.NotNull(result["Name"]);
        }

        [Fact]
        public void EmployeeValidator_Fails_OnNegativeSalary()
        {
            var result = new EmployeeValidator().Validate(new Employee { Code = "E001", Name = "موظف", HireDate = DateTime.Today, BasicSalary = -500 });
            Assert.False(result.IsValid);
            Assert.NotNull(result["BasicSalary"]);
        }

        [Fact]
        public void EmployeeValidator_Succeeds_WithValidData()
        {
            var result = new EmployeeValidator().Validate(new Employee
            {
                Code = "E001",
                Name = "موظف صحيح",
                HireDate = DateTime.Today,
                BasicSalary = 5000,
                Phone = "0501234567",
                Email = "emp@example.com"
            });
            Assert.True(result.IsValid);
        }

        [Fact]
        public void ProductValidator_Fails_WhenSalePriceBelowMinPrice()
        {
            var result = new ProductValidator().Validate(new Product
            {
                Code = "P001",
                Name = "منتج",
                CostPrice = 10,
                SalePrice = 15,
                MinPrice = 20
            });

            Assert.False(result.IsValid);
            Assert.NotNull(result["SalePrice"]);
        }

        [Fact]
        public void ProductValidator_Fails_OnNegativeCostPrice()
        {
            var result = new ProductValidator().Validate(new Product { Code = "P001", Name = "منتج", CostPrice = -1, SalePrice = 10 });
            Assert.False(result.IsValid);
            Assert.NotNull(result["CostPrice"]);
        }

        [Fact]
        public void ProductValidator_Succeeds_WithValidData()
        {
            var result = new ProductValidator().Validate(new Product
            {
                Code = "P001",
                Name = "منتج صحيح",
                CostPrice = 10,
                SalePrice = 15,
                MinPrice = 12
            });
            Assert.True(result.IsValid);
        }

        [Fact]
        public void InvoiceValidator_ValidateSales_Fails_WhenNoLines()
        {
            var result = InvoiceValidator.ValidateSales(new SalesInvoice { InvoiceDate = DateTime.Today, NetTotal = 100 });
            Assert.False(result.IsValid);
            Assert.NotNull(result["Lines"]);
        }

        [Fact]
        public void InvoiceValidator_ValidateSales_Fails_WhenDateMissing()
        {
            var invoice = new SalesInvoice { NetTotal = 100 };
            invoice.Lines.Add(new SalesInvoiceLine());

            var result = InvoiceValidator.ValidateSales(invoice);
            Assert.False(result.IsValid);
            Assert.NotNull(result["InvoiceDate"]);
        }

        [Fact]
        public void InvoiceValidator_ValidatePurchase_Fails_OnNegativeNetTotal()
        {
            var invoice = new PurchaseInvoice { InvoiceDate = DateTime.Today, NetTotal = -50 };
            invoice.Lines.Add(new PurchaseInvoiceLine());

            var result = InvoiceValidator.ValidatePurchase(invoice);
            Assert.False(result.IsValid);
            Assert.NotNull(result["NetTotal"]);
        }

        [Fact]
        public void InvoiceValidator_ValidateSales_Succeeds_WithValidData()
        {
            var invoice = new SalesInvoice { InvoiceDate = DateTime.Today, NetTotal = 100 };
            invoice.Lines.Add(new SalesInvoiceLine());

            var result = InvoiceValidator.ValidateSales(invoice);
            Assert.True(result.IsValid);
        }

    }

    [Collection("Database")]
    public class AccountValidatorTests
    {
        private readonly IAccountRepository _repo;

        public AccountValidatorTests(TestDatabaseFixture db) => _repo = db.Services.GetRequiredService<IAccountRepository>();

        [Fact]
        public void Fails_OnInvalidCodeFormat()
        {
            var result = new AccountValidator(_repo).Validate(new Account { Code = "ABC", Name = "حساب" });
            Assert.False(result.IsValid);
            Assert.NotNull(result["Code"]);
        }

        [Fact]
        public void Fails_WhenNameMissing()
        {
            var result = new AccountValidator(_repo).Validate(new Account { Code = "9999", Name = "" });
            Assert.False(result.IsValid);
            Assert.NotNull(result["Name"]);
        }

        [Fact]
        public void Fails_WhenCodeAlreadyExists()
        {
            // 1240 (الصندوق) مزروع افتراضياً عند تهيئة قاعدة الاختبار (راجع IAccountRepository.SeedDefaults في TestDatabaseFixture).
            var result = new AccountValidator(_repo, isEdit: false, checkUniqueness: true).Validate(new Account { Code = "1204", Name = "حساب جديد" });
            Assert.False(result.IsValid);
            Assert.NotNull(result["Code"]);
        }

        [Fact]
        public void Succeeds_OnEdit_EvenWithExistingCode()
        {
            // isEdit=true يتخطّى فحص التفرّد — السجل يعدّل نفسه، لا يتصادم مع كوده الحالي.
            var result = new AccountValidator(_repo, isEdit: true).Validate(new Account { Code = "1204", Name = "الصندوق المعدَّل" });
            Assert.True(result.IsValid);
        }

        [Fact]
        public void Succeeds_WithValidNewCode()
        {
            var result = new AccountValidator(_repo).Validate(new Account { Code = "999999", Name = "حساب صحيح جديد" });
            Assert.True(result.IsValid);
        }
    }

    [Collection("Database")]
    public class UserValidatorTests
    {
        public UserValidatorTests(TestDatabaseFixture db) { }

        [Fact]
        public void Fails_WhenUsernameTooShort()
        {
            var result = new UserValidator().Validate(new User { Username = "ab", DisplayName = "مستخدم", RoleId = 1 });
            Assert.False(result.IsValid);
            Assert.NotNull(result["Username"]);
        }

        [Fact]
        public void Fails_WhenNoRoleSelected()
        {
            var result = new UserValidator().Validate(new User { Username = "validuser", DisplayName = "مستخدم", RoleId = 0 });
            Assert.False(result.IsValid);
            Assert.NotNull(result["RoleId"]);
        }

        [Fact]
        public void Fails_WhenUsernameAlreadyExists()
        {
            // "admin" مزروع دائماً عبر PermissionDb.SeedAdminUser — لكن TestDatabaseFixture لا تزرع (توقف 11).
            // نزرعه هنا صراحة لهذا الاختبار وحده (مُتخطّاة idempotently لو زرعها اختبار آخر أولاً).
            PermissionDb.SeedDefaults();

            var result = new UserValidator(isEdit: false).Validate(new User { Username = "admin", DisplayName = "مكرر", RoleId = 1 });
            Assert.False(result.IsValid);
            Assert.NotNull(result["Username"]);
        }

        [Fact]
        public void Succeeds_OnEdit_EvenWithoutUniquenessCheck()
        {
            var result = new UserValidator(isEdit: true).Validate(new User { Username = "someexistinguser", DisplayName = "مستخدم", RoleId = 1 });
            Assert.True(result.IsValid);
        }
    }
}
