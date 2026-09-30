using PrimeERP.Application.Services.Ledger.Accounts;
using PrimeERP.Application.Services.Ledger;
using PrimeERP.Application.Legacy.Security;
using PrimeERP.Application.Legacy.Inventory;
using PrimeERP.Application.Legacy.HR;
using PrimeERP.Application.Legacy.Parties;
using System;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Application.Validation;
using PrimeERP.Data.Repositories;
using PrimeERP.Domain.Entities;
using PrimeERP.Data.Seeders;
using PrimeERP.Platform.Permissions;
using Xunit;

namespace PrimeERP.Tests.Validation
{
    /// <summary>المتحقّقون السبعة</summary>
    public class ValidatorsTests
    {
        [Fact]
        public void CustomerValidator_Fails_WhenNameMissing()
        {
            var result = Check.Fields(new Customer { Code = "C001", Name = "" }, CustomerService.Rules);
            Assert.False(result.IsValid);
            Assert.NotNull(result["Name"]);
        }

        [Fact]
        public void CustomerValidator_Fails_OnInvalidEmail()
        {
            var result = Check.Fields(new Customer { Code = "C001", Name = "عميل", Email = "not-an-email" }, CustomerService.Rules);
            Assert.False(result.IsValid);
            Assert.NotNull(result["Email"]);
        }

        [Fact]
        public void CustomerValidator_Fails_OnNegativeCreditLimit()
        {
            var result = Check.Fields(new Customer { Code = "C001", Name = "عميل", CreditLimit = -1 }, CustomerService.Rules);
            Assert.False(result.IsValid);
            Assert.NotNull(result["CreditLimit"]);
        }

        [Fact]
        public void CustomerValidator_Succeeds_WithValidData()
        {
            var result = Check.Fields(new Customer
            {
                Code = "C001",
                Name = "عميل صحيح",
                Email = "test@example.com",
                Phone = "0501234567",
                CreditLimit = 1000
            }, CustomerService.Rules);
            Assert.True(result.IsValid);
        }

        [Fact]
        public void SupplierValidator_Fails_WhenCodeMissing()
        {
            var result = Check.Fields(new Supplier { Name = "مورد" }, SupplierService.Rules);
            Assert.False(result.IsValid);
            Assert.NotNull(result["Code"]);
        }

        [Fact]
        public void SupplierValidator_Fails_OnInvalidPhone()
        {
            var result = Check.Fields(new Supplier { Code = "S001", Name = "مورد", Phone = "123" }, SupplierService.Rules);
            Assert.False(result.IsValid);
            Assert.NotNull(result["Phone"]);
        }

        [Fact]
        public void SupplierValidator_Succeeds_WithValidData()
        {
            var result = Check.Fields(new Supplier { Code = "S001", Name = "مورد صحيح", CreditLimit = 0 }, SupplierService.Rules);
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

            var result = Check.Fields(entry, Entries.Shape);
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

            var result = Check.Fields(entry, Entries.Shape);
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

            var result = Check.Fields(entry, Entries.Shape);
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

            var result = Check.Fields(entry, Entries.Shape);
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

            var result = Check.Fields(entry, Entries.Shape);
            Assert.True(result.IsValid);
        }

        [Fact]
        public void EmployeeValidator_Fails_WhenNameMissing()
        {
            var result = Check.Fields(new Employee { Code = "E001", Name = "", HireDate = DateTime.Today }, EmployeeService.Rules);
            Assert.False(result.IsValid);
            Assert.NotNull(result["Name"]);
        }

        [Fact]
        public void EmployeeValidator_Fails_OnNegativeSalary()
        {
            var result = Check.Fields(new Employee { Code = "E001", Name = "موظف", HireDate = DateTime.Today, BasicSalary = -500 }, EmployeeService.Rules);
            Assert.False(result.IsValid);
            Assert.NotNull(result["BasicSalary"]);
        }

        [Fact]
        public void EmployeeValidator_Succeeds_WithValidData()
        {
            var result = Check.Fields(new Employee
            {
                Code = "E001",
                Name = "موظف صحيح",
                HireDate = DateTime.Today,
                BasicSalary = 5000,
                Phone = "0501234567",
                Email = "emp@example.com"
            }, EmployeeService.Rules);
            Assert.True(result.IsValid);
        }

        [Fact]
        public void ProductValidator_Fails_WhenSalePriceBelowMinPrice()
        {
            var result = Check.Fields(new Product
            {
                Code = "P001",
                Name = "منتج",
                CostPrice = 10,
                SalePrice = 15,
                MinPrice = 20
            }, ProductService.Rules);

            Assert.False(result.IsValid);
            Assert.NotNull(result["SalePrice"]);
        }

        [Fact]
        public void ProductValidator_Fails_OnNegativeCostPrice()
        {
            var result = Check.Fields(new Product { Code = "P001", Name = "منتج", CostPrice = -1, SalePrice = 10 }, ProductService.Rules);
            Assert.False(result.IsValid);
            Assert.NotNull(result["CostPrice"]);
        }

        [Fact]
        public void ProductValidator_Succeeds_WithValidData()
        {
            var result = Check.Fields(new Product
            {
                Code = "P001",
                Name = "منتج صحيح",
                CostPrice = 10,
                SalePrice = 15,
                MinPrice = 12
            }, ProductService.Rules);
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
            var result = Check.Fields(new Account { Code = "ABC", Name = "حساب" }, AddTreeAccount.AccountFields);
            Assert.False(result.IsValid);
            Assert.NotNull(result["Code"]);
        }

        [Fact]
        public void Fails_WhenNameMissing()
        {
            var result = Check.Fields(new Account { Code = "9999", Name = "" }, AddTreeAccount.AccountFields);
            Assert.False(result.IsValid);
            Assert.NotNull(result["Name"]);
        }

        [Fact]
        public void Succeeds_OnEdit_EvenWithExistingCode()
        {
            var result = Check.Fields(new Account { Code = "1204", Name = "الصندوق المعدَّل" }, AddTreeAccount.AccountFields);
            Assert.True(result.IsValid);
        }

        [Fact]
        public void Succeeds_WithValidNewCode()
        {
            var result = Check.Fields(new Account { Code = "999999", Name = "حساب صحيح جديد" }, AddTreeAccount.AccountFields);
            Assert.True(result.IsValid);
        }
    }

    [Collection("Database")]
    public class UserValidatorTests
    {
        private readonly IPermissionStore _store;

        public UserValidatorTests(TestDatabaseFixture db) => _store = db.Permissions;

        [Fact]
        public void Fails_WhenUsernameTooShort()
        {
            var result = Check.Fields(new User { Username = "ab", DisplayName = "مستخدم", RoleId = 1 }, UserService.UserFields(_store, isEdit: false));
            Assert.False(result.IsValid);
            Assert.NotNull(result["Username"]);
        }

        [Fact]
        public void Fails_WhenNoRoleSelected()
        {
            var result = Check.Fields(new User { Username = "validuser", DisplayName = "مستخدم", RoleId = 0 }, UserService.UserFields(_store, isEdit: false));
            Assert.False(result.IsValid);
            Assert.NotNull(result["RoleId"]);
        }

        [Fact]
        public void Fails_WhenUsernameAlreadyExists()
        {
            PermissionSeeder.Seed(_store);

            var result = Check.Fields(new User { Username = "admin", DisplayName = "مكرر", RoleId = 1 }, UserService.UserFields(_store, isEdit: false));
            Assert.False(result.IsValid);
            Assert.NotNull(result["Username"]);
        }

        [Fact]
        public void Succeeds_OnEdit_EvenWithoutUniquenessCheck()
        {
            var result = Check.Fields(new User { Username = "someexistinguser", DisplayName = "مستخدم", RoleId = 1 }, UserService.UserFields(_store, isEdit: true));
            Assert.True(result.IsValid);
        }
    }
}
