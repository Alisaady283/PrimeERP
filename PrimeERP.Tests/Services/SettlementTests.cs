using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Application.DTOs.Vouchers;
using PrimeERP.Application.PageServices.Accounting;
using PrimeERP.Application.PageServices.Parties;
using PrimeERP.Application.PageServices.Treasury;
using PrimeERP.Application.PageServices.Vouchers;
using PrimeERP.Application.Reporting;
using PrimeERP.Data.Repositories.Base;
using PrimeERP.Domain.Calculations;
using PrimeERP.Domain.Entities;
using PrimeERP.Domain.Enums;
using PrimeERP.Platform.Permissions;
using Xunit;

namespace PrimeERP.Tests.Services
{
    /// <summary>توزيع السداد على الفواتير وأعمار الديون</summary>
    [Collection("Database")]
    public class SettlementTests
    {
        private readonly TestDatabaseFixture _db;

        public SettlementTests(TestDatabaseFixture db)
        {
            _db = db;
            AppSession.DevMode = true;
        }

        [Fact]
        public void Allocate_NamedFirst_ThenOldest()
        {
            var remaining = new Dictionary<int, decimal> { [1] = 100, [2] = 50, [3] = 80 };
            var applied = SettlementCalc.Allocate(140, new[] { (3, 30m) }, new[] { 1, 2, 3 }, remaining);

            Assert.Equal(new[] { (3, 30m, false), (1, 100m, true), (2, 10m, true) }, applied);
            Assert.Equal(0m, remaining[1]);
            Assert.Equal(40m, remaining[2]);
            Assert.Equal(50m, remaining[3]);
        }

        [Fact]
        public void Vouchers_PayOldestFirst_ThenComplete_AndDeleteRestores()
        {
            var (customer, treasury) = Seed();
            var invoices = _db.Services.GetRequiredService<IInvoiceRepository<SalesInvoice, SalesInvoiceLine>>();
            var older = Invoice(invoices, customer.Id, DateTime.Today.AddDays(-70), 100);
            var newer = Invoice(invoices, customer.Id, DateTime.Today.AddDays(-10), 50);
            var vouchers = _db.Services.GetRequiredService<IReceiptVoucherService>();

            var first = vouchers.Create(Voucher(customer.Id, treasury.Id, 120, DateTime.Today.AddDays(-5)));
            Assert.True(first.IsSuccess, first.ErrorMessage);
            Assert.Equal((100m, 0m), Settled(invoices, older));
            Assert.Equal((20m, 30m), Settled(invoices, newer));

            var second = vouchers.Create(Voucher(customer.Id, treasury.Id, 30, DateTime.Today));
            Assert.True(second.IsSuccess, second.ErrorMessage);
            Assert.Equal((50m, 0m), Settled(invoices, newer));

            Assert.True(vouchers.Delete(first.Value.Id).IsSuccess);
            Assert.Equal((30m, 70m), Settled(invoices, older));
            Assert.Equal((0m, 50m), Settled(invoices, newer));
        }

        [Fact]
        public void NamedInvoice_IsPaidBeforeOlderOnes_AndAgingShowsTheRest()
        {
            var (customer, treasury) = Seed();
            var invoices = _db.Services.GetRequiredService<IInvoiceRepository<SalesInvoice, SalesInvoiceLine>>();
            var old = Invoice(invoices, customer.Id, DateTime.Today.AddDays(-200), 100);
            var recent = Invoice(invoices, customer.Id, DateTime.Today.AddDays(-5), 60);
            var recentNo = invoices.GetById(recent).InvoiceNo;

            var dto = Voucher(customer.Id, treasury.Id, 60, DateTime.Today);
            dto.Allocations = new() { new() { LineNo = 1, InvoiceNo = recentNo, Amount = 60 } };
            var voucher = _db.Services.GetRequiredService<IReceiptVoucherService>().Create(dto);
            Assert.True(voucher.IsSuccess, voucher.ErrorMessage);

            Assert.Equal((0m, 100m), Settled(invoices, old));
            Assert.Equal((60m, 0m), Settled(invoices, recent));

            var aging = _db.Services.GetRequiredService<IAgingReportService>().CustomerAging(DateTime.Today);
            Assert.True(aging.IsSuccess, aging.ErrorMessage);
            var row = aging.Value.Rows.Cast<AgingRow>().Single(r => r.Name == customer.Name);
            Assert.Equal(100m, row.Over150);
            Assert.Equal(100m, row.Total);
        }

        private (Customer Customer, Treasury Treasury) Seed()
        {
            var parentId = _db.Services.GetRequiredService<PrimeERP.Data.Repositories.IAccountRepository>().GetByCode("12").Id;
            var account = _db.Services.GetRequiredService<IAccountService>().Create(
                new PrimeERP.Application.DTOs.Accounting.CreateAccountDto { ParentId = parentId, Name = $"خزينة {Guid.NewGuid():N}", IsLeaf = true });
            var treasury = _db.Services.GetRequiredService<ITreasuryService>().Create(new Treasury { Name = $"خزينة {Guid.NewGuid():N}", AccountCode = account.Value.Code });
            var customer = _db.Services.GetRequiredService<ICustomerService>().Create(new Customer { Name = $"عميل {Guid.NewGuid():N}" });
            Assert.True(treasury.IsSuccess, treasury.ErrorMessage);
            Assert.True(customer.IsSuccess, customer.ErrorMessage);
            return (customer.Value, treasury.Value);
        }

        private static int Invoice(IInvoiceRepository<SalesInvoice, SalesInvoiceLine> invoices, int customerId, DateTime date, decimal net) =>
            invoices.InsertHeader(null, new SalesInvoice
            {
                InvoiceNo = $"T{Guid.NewGuid():N}"[..12], InvoiceDate = date, DueDate = date, CustomerId = customerId,
                SubTotal = net, NetTotal = net, Status = InvoiceStatus.Confirmed
            });

        private static CreateVoucherDto Voucher(int customerId, int treasuryId, decimal amount, DateTime date) => new()
        {
            VoucherDate = date, PartyId = customerId, TreasuryId = treasuryId, Amount = amount, Method = (int)PaymentMethod.Cash
        };

        private static (decimal Paid, decimal Remaining) Settled(IInvoiceRepository<SalesInvoice, SalesInvoiceLine> invoices, int id)
        {
            var invoice = invoices.GetById(id);
            return (invoice.PaidAmount, invoice.RemainingAmount);
        }
    }
}
