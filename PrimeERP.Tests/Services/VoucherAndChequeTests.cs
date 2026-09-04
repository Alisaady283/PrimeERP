using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Application.DTOs.Cheques;
using PrimeERP.Application.DTOs.Parties;
using PrimeERP.Application.DTOs.Treasury;
using PrimeERP.Application.DTOs.Vouchers;
using PrimeERP.Application.Services;
using PrimeERP.Application.Services.Accounting;
using PrimeERP.Application.Services.Cheques;
using PrimeERP.Application.Services.Parties;
using PrimeERP.Application.Services.Treasury;
using PrimeERP.Application.Services.Vouchers;
using PrimeERP.Domain.Enums;
using PrimeERP.Platform.Permissions;
using PrimeERP.Platform.Settings;
using Xunit;

namespace PrimeERP.Tests.Services
{
    [Collection("Database")]
    public class VoucherAndChequeTests
    {
        private readonly TestDatabaseFixture _db;

        public VoucherAndChequeTests(TestDatabaseFixture db)
        {
            _db = db;
            AppSession.DevMode = true;
        }

        // حساب ورقي حقيقي تحت جذر النقدية — الترحيل يرفض أي حساب غير ورقي، وهذا ما تفرضه الخدمة عملياً.
        private string SeedLeafAccount()
        {
            var parentId = _db.Services.GetRequiredService<PrimeERP.Data.Repositories.IAccountRepository>().GetByCode("12").Id;
            var account = _db.Services.GetRequiredService<IAccountService>().Create(
                new PrimeERP.Application.DTOs.Accounting.CreateAccountDto { ParentId = parentId, Name = $"خزينة {Guid.NewGuid():N}", IsLeaf = true });
            Assert.True(account.IsSuccess, account.ErrorMessage);
            return account.Value.Code;
        }

        private TreasuryDto SeedTreasury()
        {
            var result = _db.Services.GetRequiredService<ITreasuryService>().Create(new CreateTreasuryDto
            {
                Name = $"خزينة {Guid.NewGuid():N}",
                AccountCode = SeedLeafAccount()
            });
            Assert.True(result.IsSuccess, result.ErrorMessage);
            return result.Value;
        }

        private CustomerDto SeedCustomer()
        {
            var result = _db.Services.GetRequiredService<ICustomerService>()
                .Create(new CreateCustomerDto { Name = $"عميل {Guid.NewGuid():N}" });
            Assert.True(result.IsSuccess, result.ErrorMessage);
            return result.Value;
        }

        [Fact]
        public void ReceiptVoucher_PostsABalancedEntryAgainstTheTreasuryAccount()
        {
            var treasury = SeedTreasury();
            var customer = SeedCustomer();

            var voucher = _db.Services.GetRequiredService<IReceiptVoucherService>().Create(new CreateVoucherDto
            {
                VoucherDate = DateTime.Today, PartyId = customer.Id, TreasuryId = treasury.Id,
                Amount = 250, Method = (int)PaymentMethod.Cash
            });
            Assert.True(voucher.IsSuccess, voucher.ErrorMessage);

            var journals = _db.Services.GetRequiredService<IJournalService>();
            var entry = journals.GetPaged(1, 50, new PrimeERP.Application.DTOs.Accounting.JournalFilter { Source = "ReceiptVoucher" });
            Assert.True(entry.IsSuccess, entry.ErrorMessage);

            var detail = journals.GetById(entry.Value.Items.First().Id).Value;
            Assert.Equal(detail.Lines.Sum(l => l.Debit), detail.Lines.Sum(l => l.Credit));
            Assert.Contains(detail.Lines, l => l.AccountCode == treasury.AccountCode && l.Debit == 250);
            Assert.Contains(detail.Lines, l => l.AccountCode == customer.AccountCode && l.Credit == 250);
        }

        [Fact]
        public void AllocationsAboveTheVoucherAmount_AreRejected()
        {
            var treasury = SeedTreasury();
            var customer = SeedCustomer();

            var voucher = _db.Services.GetRequiredService<IReceiptVoucherService>().Create(new CreateVoucherDto
            {
                VoucherDate = DateTime.Today, PartyId = customer.Id, TreasuryId = treasury.Id,
                Amount = 100, Method = (int)PaymentMethod.Cash,
                Allocations = { new CreateVoucherAllocationDto { InvoiceNo = "INV-1", Amount = 150 } }
            });

            Assert.False(voucher.IsSuccess);
            Assert.Contains("يتجاوز", voucher.ErrorMessage);
        }

        [Fact]
        public void ChequeReceipt_CreatesAnIncomingChequeInHand()
        {
            var cheque = SeedIncomingCheque(out _);

            Assert.Equal(ChequeStatus.InHand, cheque.Status);
            Assert.Equal("وارد", cheque.DirectionName);
            Assert.Single(cheque.Movements);
        }

        [Fact]
        public void ChequeMovesThroughDepositAndCollection_RecordingEveryStep()
        {
            var cheque = SeedIncomingCheque(out var treasury);
            var service = _db.Services.GetRequiredService<IChequeService>();

            Assert.True(service.Move(new MoveChequeDto { ChequeId = cheque.Id, ToStatus = (int)ChequeStatus.Deposited, TreasuryId = treasury.Id }).IsSuccess);
            Assert.True(service.Move(new MoveChequeDto { ChequeId = cheque.Id, ToStatus = (int)ChequeStatus.Collected, TreasuryId = treasury.Id }).IsSuccess);

            var after = service.GetById(cheque.Id).Value;
            Assert.Equal(ChequeStatus.Collected, after.Status);
            Assert.Equal(3, after.Movements.Count);   // الإنشاء + الإيداع + التحصيل
        }

        [Fact]
        public void IllegalTransitionIsRejected_AndFinalStatesHaveNoTransitionsLeft()
        {
            var cheque = SeedIncomingCheque(out var treasury);
            var service = _db.Services.GetRequiredService<IChequeService>();

            // شيك بالمحفظة لا يرتد — الارتداد يقع بعد الإيداع فقط.
            var illegal = service.Move(new MoveChequeDto { ChequeId = cheque.Id, ToStatus = (int)ChequeStatus.Bounced });
            Assert.False(illegal.IsSuccess);

            service.Move(new MoveChequeDto { ChequeId = cheque.Id, ToStatus = (int)ChequeStatus.Collected, TreasuryId = treasury.Id });
            Assert.Empty(service.GetAllowedTransitions(cheque.Id).Value);
        }

        private ChequeDetailDto SeedIncomingCheque(out TreasuryDto treasury)
        {
            treasury = SeedTreasury();
            var customer = SeedCustomer();

            _db.Services.GetRequiredService<ISettingsService>().SetMany(new System.Collections.Generic.Dictionary<string, object>
            {
                [SettingKeys.Accounts.ChequesInHand] = treasury.AccountCode,
                [SettingKeys.Accounts.ChequesUnderCollection] = treasury.AccountCode,
                [SettingKeys.Accounts.ChequesPayable] = treasury.AccountCode,
            });

            var voucher = _db.Services.GetRequiredService<IReceiptVoucherService>().Create(new CreateVoucherDto
            {
                VoucherDate = DateTime.Today, PartyId = customer.Id, TreasuryId = treasury.Id,
                Amount = 500, Method = (int)PaymentMethod.Cheque,
                ChequeNo = $"CHQ-{Guid.NewGuid():N}"[..12], ChequeDueDate = DateTime.Today.AddDays(30), ChequeBank = "بنك الاختبار"
            });
            Assert.True(voucher.IsSuccess, voucher.ErrorMessage);

            var cheques = _db.Services.GetRequiredService<IChequeService>().GetPaged(1, 50);
            var created = cheques.Value.Items.First();
            return _db.Services.GetRequiredService<IChequeService>().GetById(created.Id).Value;
        }
    }
}
