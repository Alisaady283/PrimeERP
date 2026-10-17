using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PrimeERP.Application.DTOs.Vouchers;
using PrimeERP.Application.PageServices.Vouchers;
using PrimeERP.Data.Repositories;
using PrimeERP.Data.Repositories.Base;
using PrimeERP.Domain.Entities;
using PrimeERP.Domain.Enums;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Localization;
using PrimeERP.Platform.Permissions;
using PrimeERP.Platform.Settings;

namespace PrimeERP.Application.Services.Integration
{
    public sealed record PaymentLink(int Id, string Url, decimal Amount, string Provider);

    public interface IPaymentLinkService
    {
        /// <summary>رابط دفعٍ لباقي الفاتورة أو جزءٍ منه</summary>
        Task<Result<PaymentLink>> RequestAsync(int invoiceId, decimal? amount = null);
        /// <summary>إشعار البوابة: يُتحقَّق من توقيعه ومبلغه، ويُنشئ سند القبض مرةً واحدة</summary>
        Result Confirm(string provider, string body, IReadOnlyDictionary<string, string> headers, IReadOnlyDictionary<string, string> query);
    }

    /// <summary>
    /// الدفع الإلكتروني للفاتورة: الطالب يحتاج صلاحية سند القبض لأن التأكيد يُنشئه باسمه — فلا حساب «نظام» بلا صاحب،
    /// والتدقيق يذكر من طلب. والتأكيد بلا جلسة (من البوابة) يدخل بصفته في نطاقٍ مستقل ثم يخرج.
    /// </summary>
    public sealed class PaymentLinkService : IPaymentLinkService
    {
        private readonly IPaymentRepository _payments;
        private readonly IInvoiceRepository<SalesInvoice, SalesInvoiceLine> _invoices;
        private readonly IPartyRepository<Customer> _customers;
        private readonly IReceiptVoucherService _receipts;
        private readonly IPermissionService _permissions;
        private readonly IPermissionStore _users;
        private readonly ISettingsProvider _settings;
        private readonly Alerts _alerts;
        private readonly Dictionary<string, IPaymentGateway> _gateways;

        public PaymentLinkService(IPaymentRepository payments, IInvoiceRepository<SalesInvoice, SalesInvoiceLine> invoices, IPartyRepository<Customer> customers,
            IReceiptVoucherService receipts, IPermissionService permissions, IPermissionStore users, ISettingsProvider settings, Alerts alerts,
            IEnumerable<IPaymentGateway> gateways)
        {
            _payments = payments;
            _invoices = invoices;
            _customers = customers;
            _receipts = receipts;
            _permissions = permissions;
            _users = users;
            _settings = settings;
            _alerts = alerts;
            _gateways = gateways.ToDictionary(g => g.Name, StringComparer.OrdinalIgnoreCase);
        }

        private static string ReceiptsCreate => $"{PermissionKeys.GateOf("Receipts", "Receipts")}.Create";

        public async Task<Result<PaymentLink>> RequestAsync(int invoiceId, decimal? amount = null)
        {
            if (!_permissions.Can(ReceiptsCreate)) return Result.Fail<PaymentLink>(LocalizationService.Get("Str.PermissionDenied"), ErrorCode.Unauthorized);

            var provider = _settings.Get(SettingKeys.Payment.Provider, "None");
            if (!_gateways.TryGetValue(provider, out var gateway)) return Fail("NoProvider");
            if (!int.TryParse(_settings.Get(SettingKeys.Payment.TreasuryId, ""), out _)) return Fail("NoTreasury");
            if (string.IsNullOrWhiteSpace(_settings.Get(SettingKeys.Payment.PublicUrl, ""))) return Fail("NoPublicUrl");

            var invoice = _invoices.GetById(invoiceId);
            if (invoice == null) return Fail("NoInvoice");
            var due = amount ?? invoice.RemainingAmount;
            if (due <= 0 || due > invoice.RemainingAmount) return Fail("BadAmount", invoice.RemainingAmount);

            var payment = new PaymentRequest
            {
                Provider = gateway.Name, InvoiceId = invoice.Id, InvoiceNo = invoice.InvoiceNo, CustomerId = invoice.CustomerId,
                Amount = due, Currency = _settings.Get(SettingKeys.Payment.Currency, "EGP"), RequestedBy = AppSession.UserId
            };
            payment.Id = _payments.Insert(payment);

            var customer = _customers.GetById(invoice.CustomerId);
            var link = await gateway.CreateAsync(payment, customer?.Name, customer?.Phone).ConfigureAwait(false);
            if (link.IsFailure)
            {
                _payments.Fail(payment.Id, link.ErrorMessage);
                return link.As<PaymentLink>();
            }

            _payments.SetLink(payment.Id, link.Value.Url, link.Value.Reference);
            return Result.Ok(new PaymentLink(payment.Id, link.Value.Url, due, gateway.Name));
        }

        public Result Confirm(string provider, string body, IReadOnlyDictionary<string, string> headers, IReadOnlyDictionary<string, string> query)
        {
            if (!_gateways.TryGetValue(provider ?? "", out var gateway)) return Result.Fail("Unknown provider", ErrorCode.NotFound);

            var callback = gateway.Read(body, headers, query);
            if (!callback.Valid) return Result.Fail(callback.Error, ErrorCode.Unauthorized);
            if (!callback.Paid || callback.PaymentId == null) return Result.Ok();   // حدثٌ آخر أو دفعٌ لم يكتمل

            var payment = _payments.GetById(callback.PaymentId.Value);
            if (payment == null || !string.Equals(payment.Provider, gateway.Name, StringComparison.OrdinalIgnoreCase))
                return Result.Fail("Unknown payment", ErrorCode.NotFound);
            if (callback.Amount < payment.Amount)
            {
                _payments.Fail(payment.Id, $"Paid {callback.Amount} < {payment.Amount}");
                return Result.Fail("Amount mismatch", ErrorCode.ValidationFailed);
            }
            if (!_payments.MarkPaid(payment.Id, DateTime.Now, callback.Reference)) return Result.Ok();   // سبق تأكيده

            var receipt = AsRequester(payment.RequestedBy, () => _receipts.Create(new CreateVoucherDto
            {
                VoucherDate = DateTime.Today, PartyId = payment.CustomerId, Amount = payment.Amount, Method = (int)PaymentMethod.Bank,
                TreasuryId = int.Parse(_settings.Get(SettingKeys.Payment.TreasuryId, "0")),
                Reference = $"{payment.Provider}:{callback.Reference}",
                Notes = LocalizationService.Get("Str.Payment.ReceiptNote", payment.Provider, payment.InvoiceNo),
                Allocations = new() { new() { LineNo = 1, InvoiceNo = payment.InvoiceNo, Amount = payment.Amount } }
            }));
            if (receipt.IsFailure)
            {
                _payments.Fail(payment.Id, receipt.ErrorMessage);
                return receipt;
            }

            _payments.SetVoucher(payment.Id, receipt.Value.Id);
            _alerts.Raise(new AlertMessage(AlertEvents.PaymentReceived, LocalizationService.Get("Str.Alert.Event.Payment.Received"),
                LocalizationService.Get("Str.Payment.ReceivedBody", payment.Amount, payment.InvoiceNo, payment.Provider),
                PageKey: "Receipts", RecordId: receipt.Value.Id, PermissionKey: $"{PermissionKeys.GateOf("Receipts", "Receipts")}.View",
                DedupKey: $"{AlertEvents.PaymentReceived}:{payment.Id}", Severity: "info"));
            return Result.Ok();
        }

        /// <summary>العمل بصفة الطالب وصلاحياته في نطاقٍ يُغلق بانتهائه</summary>
        private T AsRequester<T>(int userId, Func<T> work)
        {
            var user = _users.GetAllUsers().FirstOrDefault(u => u.Id == userId);
            using var scope = AppSession.Enter();
            if (user != null)
                AppSession.SignIn(user.Id, user.Username, user.DisplayName, user.RoleId, user.RoleName, _permissions.GetUserPermissions(user.Id), user.BranchId);
            return work();
        }

        private static Result<PaymentLink> Fail(string key, params object[] args) =>
            Result.Fail<PaymentLink>(LocalizationService.Get($"Str.Payment.{key}", args), ErrorCode.ValidationFailed);

        public static string ReturnUrl(ISettingsProvider settings, PaymentRequest payment) =>
            $"{settings.Get(SettingKeys.Payment.PublicUrl, "").TrimEnd('/')}/payments/done?id={payment.Id}";
    }
}
