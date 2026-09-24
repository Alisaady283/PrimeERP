using PrimeERP.Data.Core;
using System;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Application.DTOs.Common;
using PrimeERP.Application.DTOs.Inventory;
using PrimeERP.Application.DTOs.Parties;
using PrimeERP.Application.DTOs.Treasury;
using PrimeERP.Application.DTOs.Vouchers;
using PrimeERP.Application.Services.Common;
using PrimeERP.Application.Services.Inventory;
using PrimeERP.Application.Services.Parties;
using PrimeERP.Application.Services.Treasury;
using PrimeERP.Application.Services.Vouchers;
using PrimeERP.Domain.Enums;
using PrimeERP.Platform.Permissions;
using Xunit;

namespace PrimeERP.Tests.Services
{
    /// <summary>المخزن والخزينة والبنك لا يقبلون</summary>
    public class NegativeBalanceGuardTests : IDisposable
    {
        private readonly TestDatabaseFixture _db = new();
        private readonly int _productId, _warehouseId;

        public NegativeBalanceGuardTests()
        {
            AppSession.DevMode = true;

            var category = _db.Services.GetRequiredService<ICategoryService>()
                .Create(new CreateCategoryDto { Name = "فئة", ModuleKey = "Products" }).Value;

            _productId = _db.Services.GetRequiredService<IProductService>()
                .Create(new CreateProductDto { Name = "صنف", CategoryId = category.Id, CostPrice = 10, SalePrice = 20 }).Value.Id;

            _warehouseId = _db.Services.GetRequiredService<IWarehouseService>()
                .Create(new CreateWarehouseDto { Name = "مخزن" }).Value.Id;
        }

        public void Dispose() => _db.Dispose();

        private PrimeERP.Domain.Results.Result Move(MovementType type, decimal qty)
        {
            var stock = _db.Services.GetRequiredService<IStockService>();
            return DbContextFactory.RunTransaction(db => stock.RecordMovement(db, _productId, _warehouseId, type, qty, 10, "Test", null, "T"));
        }


        [Fact]
        public void TwoIssuesOfFifty_AgainstSeventy_LetTheFirstThroughAndRefuseTheSecond()
        {
            Assert.True(Move(MovementType.In, 70).IsSuccess);

            Assert.True(Move(MovementType.Out, 50).IsSuccess, "الصرف الأول كان يجب أن يمرّ");

            var second = Move(MovementType.Out, 50);
            Assert.False(second.IsSuccess, "الصرف الثاني كان يجب أن يُرفض — لم يبقَ إلا عشرون");

            Assert.Equal(20m, _db.Services.GetRequiredService<IStockService>()
                .GetBalance(_productId, _warehouseId).Value);
        }

        [Fact]
        public void WhatRemains_IsStillIssuable()
        {
            Move(MovementType.In, 70);
            Move(MovementType.Out, 50);

            Assert.True(Move(MovementType.Out, 20).IsSuccess);
            Assert.Equal(0m, _db.Services.GetRequiredService<IStockService>()
                .GetBalance(_productId, _warehouseId).Value);
        }

        [Fact]
        public void ANegativeAdjustment_CannotDriveTheBalanceBelowZero()
        {
            Move(MovementType.In, 70);

            Assert.False(Move(MovementType.Adjustment, -80).IsSuccess, "التسوية السالبة تجاوزت الرصيد ومرّت");
            Assert.True(Move(MovementType.Adjustment, -70).IsSuccess);
        }

        [Fact]
        public void ATransferOutOfAnEmptyWarehouse_IsRefused()
        {
            var other = _db.Services.GetRequiredService<IWarehouseService>()
                .Create(new CreateWarehouseDto { Name = "مخزن ثانٍ" }).Value.Id;

            Move(MovementType.In, 70);

            var stock = _db.Services.GetRequiredService<IStockService>();
            Assert.False(stock.Transfer(_productId, other, _warehouseId, 10).IsSuccess, "التحويل من مخزنٍ فارغ مرّ");
            Assert.True(stock.Transfer(_productId, _warehouseId, other, 10).IsSuccess);
        }


        private int Treasury(TreasuryKind kind)
        {
            var treasuries = _db.Services.GetRequiredService<ITreasuryService>();
            treasuries.SeedDefaults();
            return treasuries.Create(new CreateTreasuryDto
            { Name = kind == TreasuryKind.Bank ? "بنك" : "خزينة", Kind = (int)kind }).Value.Id;
        }

        private PrimeERP.Domain.Results.Result<VoucherDetailDto> Pay(int treasuryId, int supplierId, decimal amount) =>
            _db.Services.GetRequiredService<IPaymentVoucherService>().Create(new CreateVoucherDto
            {
                VoucherDate = DateTime.Today, TreasuryId = treasuryId, PartyId = supplierId,
                Amount = amount, Method = (int)PaymentMethod.Cash
            });

        [Theory]
        [InlineData(TreasuryKind.Cash)]
        [InlineData(TreasuryKind.Bank)]
        public void PayingMoreThanTheTreasuryHolds_IsRefused(TreasuryKind kind)
        {
            var treasuryId = Treasury(kind);
            var supplierId = _db.Services.GetRequiredService<ISupplierService>()
                .Create(new CreateSupplierDto { Name = "مورد" }).Value.Id;

            var result = Pay(treasuryId, supplierId, 1000);

            Assert.False(result.IsSuccess, "صُرف من خزينةٍ فارغة");
            Assert.Contains("لا يكفي", result.ErrorMessage);
        }


        private (int TreasuryId, string AccountCode) CashTreasury()
        {
            var id = Treasury(TreasuryKind.Cash);
            return (id, _db.Services.GetRequiredService<ITreasuryService>().GetById(id).Value.AccountCode);
        }

        private string OtherLeafAccount(string cashCode) =>
            _db.Services.GetRequiredService<PrimeERP.Application.Services.Accounting.IAccountService>()
                .GetLeaves().Value.First(a => a.Code != cashCode && !a.Code.StartsWith("12")).Code;

        private int OpeningBalance(string cashCode, string otherCode, decimal amount) =>
            _db.Services.GetRequiredService<PrimeERP.Application.Services.Accounting.IOpeningBalanceService>()
                .Create(new PrimeERP.Application.DTOs.Accounting.CreateJournalDto
                {
                    EntryDate = DateTime.Today, Description = "افتتاحي",
                    Lines = new()
                    {
                        new() { LineNo = 1, AccountCode = cashCode,  Debit = amount, Credit = 0 },
                        new() { LineNo = 2, AccountCode = otherCode, Debit = 0, Credit = amount }
                    }
                }).Value.Id;

        [Fact]
        public void AnEntry_ThatDrivesTheTreasuryNegative_IsRefusedOnCreation()
        {
            var treasury = CashTreasury();
            var other = OtherLeafAccount(treasury.AccountCode);

            var created = _db.Services.GetRequiredService<PrimeERP.Application.Services.Accounting.IJournalService>()
                .Create(new PrimeERP.Application.DTOs.Accounting.CreateJournalDto
                {
                    EntryDate = DateTime.Today, Description = "صرف من فارغة",
                    Lines =
                    {
                        new() { LineNo = 1, AccountCode = other, Debit = 900, Credit = 0 },
                        new() { LineNo = 2, AccountCode = treasury.AccountCode, Debit = 0, Credit = 900 }
                    }
                });

            Assert.False(created.IsSuccess, "قيدٌ أنزل الخزينة تحت الصفر ومرّ");
            Assert.Contains("لا يكفي", created.ErrorMessage);
        }

        [Fact]
        public void AnOpeningBalance_ThatDrivesTheTreasuryNegative_IsRefused()
        {
            var treasury = CashTreasury();
            var other = OtherLeafAccount(treasury.AccountCode);

            var created = _db.Services.GetRequiredService<PrimeERP.Application.Services.Accounting.IOpeningBalanceService>()
                .Create(new PrimeERP.Application.DTOs.Accounting.CreateJournalDto
                {
                    EntryDate = DateTime.Today, Description = "افتتاحي سالب",
                    Lines =
                    {
                        new() { LineNo = 1, AccountCode = other, Debit = 500, Credit = 0 },
                        new() { LineNo = 2, AccountCode = treasury.AccountCode, Debit = 0, Credit = 500 }
                    }
                });

            Assert.False(created.IsSuccess, "افتتاحيٌّ أنزل الخزينة تحت الصفر ومرّ");
        }

        [Fact]
        public void DeletingAReceiptVoucher_ThatWasPartlySpent_IsRefused()
        {
            var treasury = CashTreasury();
            var customerId = _db.Services.GetRequiredService<ICustomerService>()
                .Create(new CreateCustomerDto { Name = "عميل" }).Value.Id;
            var supplierId = _db.Services.GetRequiredService<ISupplierService>()
                .Create(new CreateSupplierDto { Name = "مورد" }).Value.Id;

            var received = _db.Services.GetRequiredService<IReceiptVoucherService>().Create(new CreateVoucherDto
            {
                VoucherDate = DateTime.Today, TreasuryId = treasury.TreasuryId, PartyId = customerId,
                Amount = 100, Method = (int)PaymentMethod.Cash
            });
            Assert.True(received.IsSuccess, received.ErrorMessage);
            Assert.True(Pay(treasury.TreasuryId, supplierId, 50).IsSuccess);

            var deleted = _db.Services.GetRequiredService<IReceiptVoucherService>().Delete(received.Value.Id);

            Assert.False(deleted.IsSuccess, "حُذف قبضٌ صُرف منه");
            Assert.Contains("لا يكفي", deleted.ErrorMessage);
        }

        [Fact]
        public void AnOpeningBalance_IsEditable_AlthoughItIsPosted()
        {
            var treasury = CashTreasury();
            var other = OtherLeafAccount(treasury.AccountCode);
            var id = OpeningBalance(treasury.AccountCode, other, 1000);

            var edited = _db.Services.GetRequiredService<PrimeERP.Application.Services.Accounting.IOpeningBalanceService>()
                .Update(new PrimeERP.Application.DTOs.Accounting.CreateJournalDto
                {
                    Id = id, EntryDate = DateTime.Today, Description = "افتتاحي",
                    Lines = new()
                    {
                        new() { LineNo = 1, AccountCode = treasury.AccountCode, Debit = 1500, Credit = 0 },
                        new() { LineNo = 2, AccountCode = other, Debit = 0, Credit = 1500 }
                    }
                });

            Assert.True(edited.IsSuccess, edited.ErrorMessage);
            Assert.Equal(1500m, _db.Services.GetRequiredService<PrimeERP.Application.Services.Accounting.IAccountService>()
                .GetByCode(treasury.AccountCode).Value.Balance);
        }

        [Fact]
        public void EditingAnOpeningBalance_BelowWhatWasSpent_IsRefused()
        {
            var treasury = CashTreasury();
            var other = OtherLeafAccount(treasury.AccountCode);
            var id = OpeningBalance(treasury.AccountCode, other, 1000);

            var supplierId = _db.Services.GetRequiredService<ISupplierService>()
                .Create(new CreateSupplierDto { Name = "مورد" }).Value.Id;
            Assert.True(Pay(treasury.TreasuryId, supplierId, 400).IsSuccess);

            var opening = _db.Services.GetRequiredService<PrimeERP.Application.Services.Accounting.IOpeningBalanceService>();

            var shrunk = opening.Update(new PrimeERP.Application.DTOs.Accounting.CreateJournalDto
            {
                Id = id, EntryDate = DateTime.Today, Description = "افتتاحي",
                Lines = new()
                {
                    new() { LineNo = 1, AccountCode = treasury.AccountCode, Debit = 300, Credit = 0 },
                    new() { LineNo = 2, AccountCode = other, Debit = 0, Credit = 300 }
                }
            });
            Assert.False(shrunk.IsSuccess, "عُدِّل الافتتاحي إلى أقل مما صُرف");

            var deleted = opening.Delete(id);
            Assert.False(deleted.IsSuccess, "حُذف افتتاحيٌّ صُرف منه");
        }
    }
}
