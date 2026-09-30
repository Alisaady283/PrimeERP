using PrimeERP.Application.Services.Ledger;
using PrimeERP.Platform.Localization;
using PrimeERP.Tests.Helpers;
using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Application.DTOs.Cheques;
using PrimeERP.Application.DTOs.Parties;
using PrimeERP.Application.DTOs.Treasury;
using PrimeERP.Application.DTOs.Vouchers;
using PrimeERP.Application.Legacy.Accounting;
using PrimeERP.Application.Legacy.Cheques;
using PrimeERP.Application.Legacy.Parties;
using PrimeERP.Application.Legacy.Treasury;
using PrimeERP.Application.Legacy.Vouchers;
using PrimeERP.Domain.Entities;
using PrimeERP.Domain.Enums;
using PrimeERP.Platform.Permissions;
using PrimeERP.Platform.Settings;
using Xunit;

namespace PrimeERP.Tests.Services
{
    /// <summary>السندات والشيكات</summary>
    [Collection("Database")]
    public class VoucherAndChequeTests
    {
        private readonly TestDatabaseFixture _db;

        public VoucherAndChequeTests(TestDatabaseFixture db)
        {
            _db = db;
            AppSession.DevMode = true;
        }

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

        private Customer SeedCustomer()
        {
            var result = _db.Services.GetRequiredService<ICustomerService>()
                .Create(new Customer { Name = $"عميل {Guid.NewGuid():N}" });
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
            Assert.True(Localized.Says(voucher.ErrorMessage, "Str.Voucher.AllocationsExceed"), voucher.ErrorMessage);
        }

        [Fact]
        public void ChequeReceipt_CreatesAnIncomingChequeInHand()
        {
            var cheque = SeedIncomingCheque(out _);

            Assert.Equal(ChequeStatus.InHand, cheque.Status);
            Assert.Equal(LocalizationService.Get("Str.Cheque.Incoming"), cheque.DirectionName);
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
        public void IllegalTransitionIsRejected_AndAFinalStateStillHasAWayBack()
        {
            var cheque = SeedIncomingCheque(out var treasury);
            var service = _db.Services.GetRequiredService<IChequeService>();

            var illegal = service.Move(new MoveChequeDto { ChequeId = cheque.Id, ToStatus = (int)ChequeStatus.Bounced });
            Assert.False(illegal.IsSuccess);

            service.Move(new MoveChequeDto { ChequeId = cheque.Id, ToStatus = (int)ChequeStatus.Collected, TreasuryId = treasury.Id });
            Assert.Contains(ChequeStatus.InHand, service.GetAllowedTransitions(cheque.Id).Value);
        }


        [Fact]
        public void ChequeIsEditableWhileUnmoved_AndClosedOnceItMoves()
        {
            var cheque = SeedIncomingCheque(out var treasury);
            var service = _db.Services.GetRequiredService<IChequeService>();

            var edited = service.UpdateUnmoved(cheque.Id,
                new CreateChequeLineDto { ChequeNo = "CHQ-EDITED", Amount = 750m, BankName = "بنك آخر", PartyId = cheque.PartyId },
                DateTime.Today);
            Assert.True(edited.IsSuccess, edited.ErrorMessage);

            var after = service.GetById(cheque.Id).Value;
            Assert.Equal("CHQ-EDITED", after.ChequeNo);
            Assert.Equal(750m, after.Amount);

            Assert.True(service.Move(new MoveChequeDto
            { ChequeId = cheque.Id, ToStatus = (int)ChequeStatus.Deposited, TreasuryId = treasury.Id }).IsSuccess);

            var refused = service.UpdateUnmoved(cheque.Id,
                new CreateChequeLineDto { ChequeNo = "CHQ-AGAIN", Amount = 900m, BankName = "بنك آخر", PartyId = cheque.PartyId },
                DateTime.Today);
            Assert.True(refused.IsFailure);
            Assert.True(Localized.Says(refused.ErrorMessage, "Str.Cheque.MovedLocked"), refused.ErrorMessage);
            Assert.True(service.DeleteUnmoved(cheque.Id).IsFailure);
        }
        private ChequeDetailDto SeedIncomingCheque(out TreasuryDto treasury)
        {
            treasury = SeedTreasury();
            var customer = SeedCustomer();
            var chequeNo = $"CHQ-{Guid.NewGuid():N}"[..12];

            var document = _db.Services.GetRequiredService<IChequeService>().CreateBatch(new CreateChequeDocumentDto
            {
                DocDate = DateTime.Today,
                Lines =
                {
                    new CreateChequeLineDto
                    {
                        LineNo = 1, ChequeNo = chequeNo, BankName = "بنك الاختبار",
                        Amount = 500, PartyId = customer.Id, DueDate = DateTime.Today.AddDays(30)
                    }
                }
            }, ChequeDirection.Incoming);
            Assert.True(document.IsSuccess, document.ErrorMessage);

            var cheques = _db.Services.GetRequiredService<IChequeService>().GetPaged(1, 200);
            var created = cheques.Value.Items.Single(c => c.ChequeNo == chequeNo);
            return _db.Services.GetRequiredService<IChequeService>().GetById(created.Id).Value;
        }
    }
}
