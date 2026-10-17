using System;
using System.Collections.Generic;
using System.Formats.Asn1;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using PrimeERP.Data.Repositories;
using PrimeERP.Data.Repositories.Base;
using PrimeERP.Domain.Calculations;
using PrimeERP.Domain.Entities;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Localization;
using PrimeERP.Platform.Net;
using PrimeERP.Platform.Permissions;
using PrimeERP.Platform.Settings;

namespace PrimeERP.Application.Services.Integration
{
    /// <summary>الفاتورة وسطورها وعميلها — ما يبني منه كل مزوّدٍ مستنده</summary>
    public sealed record EInvoiceSource(SalesInvoice Invoice, IReadOnlyList<SalesInvoiceLine> Lines, Customer Customer, IReadOnlyDictionary<int, Product> Products);

    public sealed record EInvoiceOutcome(bool Accepted, string Uuid, string SubmissionId, string LongId, string Qr, string Error = null);

    /// <summary>جهةٌ ضريبية: تبني المستند وتوقّعه وتقدّمه، وتُرجع ما يُحفظ ويُطبع (المعرّف والرمز)</summary>
    public interface IEInvoiceProvider
    {
        string Name { get; }
        Task<EInvoiceOutcome> SubmitAsync(EInvoiceSource source);
    }

    /// <summary>
    /// هيئة الزكاة (ZATCA) — المرحلة الأولى: رمز QR بصيغة TLV على كل فاتورة (EInvoiceCalc.ZatcaQr).
    /// المرحلة الثانية (UBL موقّع وربطٌ بالهيئة) تحتاج شهادة CSID من الهيئة وتُضاف مزوّداً بالعقد نفسه.
    /// </summary>
    public sealed class ZatcaProvider : IEInvoiceProvider
    {
        private readonly ISettingsProvider _settings;
        public ZatcaProvider(ISettingsProvider settings) => _settings = settings;

        public string Name => "ZATCA";

        public Task<EInvoiceOutcome> SubmitAsync(EInvoiceSource source)
        {
            var vatNumber = _settings.Get(SettingKeys.Company.TaxNumber, "");
            if (string.IsNullOrWhiteSpace(vatNumber)) return Task.FromResult(new EInvoiceOutcome(false, null, null, null, null, LocalizationService.Get("Str.EInvoice.NoTaxNumber")));

            var invoice = source.Invoice;
            var qr = EInvoiceCalc.ZatcaQr(_settings.Get(SettingKeys.Company.Name, ""), vatNumber,
                invoice.InvoiceDate.Date.Add(invoice.CreatedAt.TimeOfDay), invoice.NetTotal, invoice.VatAmount);
            return Task.FromResult(new EInvoiceOutcome(true, Guid.NewGuid().ToString(), null, null, qr));
        }
    }

    /// <summary>
    /// مصلحة الضرائب المصرية (ETA): المستند JSON بمخطّطها v1.0، والشكل القياسي (EInvoiceCalc.EtaCanonical)
    /// يُوقَّع CAdES-BES بشهادة الشركة من مخزن ويندوز (رمز USB يظهر فيه ويطلب رقمه السري)، ثم يُقدَّم برمز OAuth.
    /// </summary>
    public sealed class EtaProvider : IEInvoiceProvider
    {
        private readonly ISettingsProvider _settings;
        private readonly IHttpGateway _http;

        public EtaProvider(ISettingsProvider settings, IHttpGateway http) { _settings = settings; _http = http; }

        public string Name => "ETA";

        private bool Production => _settings.Get(SettingKeys.EInvoice.EtaProduction, false);
        private string IdHost => Production ? "https://id.eta.gov.eg" : "https://id.preprod.eta.gov.eg";
        private string ApiHost => Production ? "https://api.invoicing.eta.gov.eg" : "https://api.preprod.invoicing.eta.gov.eg";
        private string PortalHost => Production ? "https://invoicing.eta.gov.eg" : "https://preprod.invoicing.eta.gov.eg";

        public async Task<EInvoiceOutcome> SubmitAsync(EInvoiceSource source)
        {
            var document = Document(source);
            var canonical = EInvoiceCalc.EtaCanonical(JsonSerializer.SerializeToElement(document));

            string signature;
            try { signature = Sign(canonical); }
            catch (Exception ex) { return Failed(LocalizationService.Get("Str.EInvoice.SignFailed", ex.Message)); }
            document["signatures"] = new[] { new Dictionary<string, object> { ["signatureType"] = "I", ["value"] = signature } };

            var token = await TokenAsync().ConfigureAwait(false);
            if (token.IsFailure) return Failed(token.ErrorMessage);

            var (ok, error, body) = await _http.SendAsync(HttpMethod.Post, $"{ApiHost}/api/v1/documentsubmissions",
                JsonSerializer.Serialize(new { documents = new[] { document } }),
                new Dictionary<string, string> { ["Authorization"] = $"Bearer {token.Value}" }).ConfigureAwait(false);
            if (!ok) return Failed(error);

            var submission = body.TryGetProperty("submissionId", out var sid) ? sid.GetString() : null;
            if (body.TryGetProperty("acceptedDocuments", out var accepted) && accepted.GetArrayLength() > 0)
            {
                var doc = accepted[0];
                var uuid = doc.GetProperty("uuid").GetString();
                var longId = doc.GetProperty("longId").GetString();
                return new EInvoiceOutcome(true, uuid, submission, longId, $"{PortalHost}/print/documents/{uuid}/share/{longId}");
            }

            var rejected = body.TryGetProperty("rejectedDocuments", out var r) && r.GetArrayLength() > 0 ? r[0].GetProperty("error").GetRawText() : body.GetRawText();
            return new EInvoiceOutcome(false, null, submission, null, null, rejected);
        }

        private static EInvoiceOutcome Failed(string error) => new(false, null, null, null, null, error);

        private async Task<Result<string>> TokenAsync()
        {
            var form = $"grant_type=client_credentials&client_id={Uri.EscapeDataString(_settings.Get(SettingKeys.EInvoice.EtaClientId, ""))}" +
                       $"&client_secret={Uri.EscapeDataString(_settings.Secret(SettingKeys.EInvoice.EtaClientSecret))}&scope=InvoicingAPI";
            var (ok, error, body) = await _http.SendAsync(HttpMethod.Post, $"{IdHost}/connect/token", form, null, "application/x-www-form-urlencoded").ConfigureAwait(false);
            return ok ? Result.Ok(body.GetProperty("access_token").GetString()) : Result.Fail<string>(error, ErrorCode.Unauthorized);
        }

        /// <summary>CAdES-BES منفصل على الشكل القياسي، بوسم شهادة التوقيع v2 كما تشترط المصلحة</summary>
        private string Sign(string canonical)
        {
            var thumbprint = _settings.Get(SettingKeys.EInvoice.EtaCertThumbprint, "").Replace(" ", "");
            using var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadOnly);
            var certificate = store.Certificates.Find(X509FindType.FindByThumbprint, thumbprint, validOnly: false).OfType<X509Certificate2>().FirstOrDefault()
                              ?? throw new InvalidOperationException(LocalizationService.Get("Str.EInvoice.NoCertificate"));

            var content = new ContentInfo(Encoding.UTF8.GetBytes(canonical));
            var signed = new SignedCms(content, detached: true);
            var signer = new CmsSigner(certificate) { DigestAlgorithm = new Oid("2.16.840.1.101.3.4.2.1"), IncludeOption = X509IncludeOption.EndCertOnly };
            signer.SignedAttributes.Add(new Pkcs9SigningTime(DateTime.UtcNow));
            signer.SignedAttributes.Add(new AsnEncodedData("1.2.840.113549.1.9.16.2.47", SigningCertificateV2(certificate)));
            signed.ComputeSignature(signer);
            return Convert.ToBase64String(signed.Encode());
        }

        /// <summary>SigningCertificateV2 ::= SEQUENCE { certs SEQUENCE OF ESSCertIDv2 } — ESSCertIDv2 بخلاصة SHA-256 (الافتراضية فلا تُكتب)</summary>
        private static byte[] SigningCertificateV2(X509Certificate2 certificate)
        {
            var writer = new AsnWriter(AsnEncodingRules.DER);
            using (writer.PushSequence())
            using (writer.PushSequence())
            using (writer.PushSequence())
                writer.WriteOctetString(SHA256.HashData(certificate.RawData));
            return writer.Encode();
        }

        private Dictionary<string, object> Document(EInvoiceSource source)
        {
            var invoice = source.Invoice;
            var taxNumber = _settings.Get(SettingKeys.Company.TaxNumber, "");
            static decimal R(decimal value) => Math.Round(value, 5, MidpointRounding.AwayFromZero);

            var lines = source.Lines.Select(l =>
            {
                var product = source.Products.GetValueOrDefault(l.ProductId);
                var gs1 = !string.IsNullOrWhiteSpace(product?.Barcode);
                var sales = R(l.Qty * l.UnitPrice);
                return new Dictionary<string, object>
                {
                    ["description"] = l.ProductName ?? l.ProductCode, ["itemType"] = gs1 ? "GS1" : "EGS",
                    ["itemCode"] = gs1 ? product.Barcode : $"EG-{taxNumber}-{l.ProductCode}", ["unitType"] = "EA", ["quantity"] = R(l.Qty),
                    ["internalCode"] = l.ProductCode, ["salesTotal"] = sales, ["total"] = R(l.NetAmount), ["valueDifference"] = 0m,
                    ["totalTaxableFees"] = 0m, ["netTotal"] = R(sales - l.DiscountAmount), ["itemsDiscount"] = 0m,
                    ["unitValue"] = new Dictionary<string, object> { ["currencySold"] = "EGP", ["amountEGP"] = R(l.UnitPrice) },
                    ["discount"] = new Dictionary<string, object> { ["rate"] = R(l.DiscountPercent), ["amount"] = R(l.DiscountAmount) },
                    ["taxableItems"] = new[] { new Dictionary<string, object> { ["taxType"] = "T1", ["amount"] = R(l.VatAmount), ["subType"] = "V009", ["rate"] = R(l.VatPercent) } }
                };
            }).ToList();

            return new Dictionary<string, object>
            {
                ["issuer"] = new Dictionary<string, object>
                {
                    ["type"] = "B", ["id"] = taxNumber, ["name"] = _settings.Get(SettingKeys.Company.Name, ""),
                    ["address"] = new Dictionary<string, object>
                    {
                        ["branchID"] = _settings.Get(SettingKeys.EInvoice.EtaBranchId, "0"), ["country"] = "EG",
                        ["governate"] = _settings.Get(SettingKeys.EInvoice.EtaGovernate, ""), ["regionCity"] = _settings.Get(SettingKeys.EInvoice.EtaCity, ""),
                        ["street"] = _settings.Get(SettingKeys.EInvoice.EtaStreet, ""), ["buildingNumber"] = _settings.Get(SettingKeys.EInvoice.EtaBuilding, "")
                    }
                },
                ["receiver"] = new Dictionary<string, object>
                {
                    ["type"] = string.IsNullOrWhiteSpace(source.Customer?.TaxNumber) ? "P" : "B", ["id"] = source.Customer?.TaxNumber ?? "",
                    ["name"] = source.Customer?.Name ?? "", ["address"] = new Dictionary<string, object> { ["country"] = "EG" }
                },
                ["documentType"] = "I", ["documentTypeVersion"] = "1.0",
                ["dateTimeIssued"] = invoice.InvoiceDate.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture),
                ["taxpayerActivityCode"] = _settings.Get(SettingKeys.EInvoice.EtaActivityCode, ""), ["internalID"] = invoice.InvoiceNo,
                ["invoiceLines"] = lines,
                ["totalSalesAmount"] = R(source.Lines.Sum(l => l.Qty * l.UnitPrice)), ["totalDiscountAmount"] = R(source.Lines.Sum(l => l.DiscountAmount)),
                ["netAmount"] = R(source.Lines.Sum(l => l.Qty * l.UnitPrice - l.DiscountAmount)),
                ["taxTotals"] = new[] { new Dictionary<string, object> { ["taxType"] = "T1", ["amount"] = R(source.Lines.Sum(l => l.VatAmount)) } },
                ["totalAmount"] = R(invoice.NetTotal), ["extraDiscountAmount"] = 0m, ["totalItemsDiscountAmount"] = 0m
            };
        }
    }

    public interface IEInvoiceService
    {
        /// <summary>يقدّم الفاتورة للجهة المفعّلة ويحفظ ردّها — ما قُبل لا يُقدَّم ثانية</summary>
        Task<Result<EInvoiceSubmission>> SubmitAsync(int invoiceId);
        EInvoiceSubmission Latest(int invoiceId);
    }

    public sealed class EInvoiceService : IEInvoiceService
    {
        private readonly IEInvoiceRepository _submissions;
        private readonly IInvoiceRepository<SalesInvoice, SalesInvoiceLine> _invoices;
        private readonly IPartyRepository<Customer> _customers;
        private readonly IProductRepository _products;
        private readonly ISettingsProvider _settings;
        private readonly IPermissionService _permissions;
        private readonly Dictionary<string, IEInvoiceProvider> _providers;

        public EInvoiceService(IEInvoiceRepository submissions, IInvoiceRepository<SalesInvoice, SalesInvoiceLine> invoices,
            IPartyRepository<Customer> customers, IProductRepository products, ISettingsProvider settings,
            IPermissionService permissions, IEnumerable<IEInvoiceProvider> providers)
        {
            _submissions = submissions;
            _invoices = invoices;
            _customers = customers;
            _products = products;
            _settings = settings;
            _permissions = permissions;
            _providers = providers.ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);
        }

        public EInvoiceSubmission Latest(int invoiceId) => _submissions.Latest(invoiceId);

        public async Task<Result<EInvoiceSubmission>> SubmitAsync(int invoiceId)
        {
            if (!_permissions.Can($"{PermissionKeys.GateOf("SalesInvoices", "Sales")}.Edit"))
                return Result.Fail<EInvoiceSubmission>(LocalizationService.Get("Str.PermissionDenied"), ErrorCode.Unauthorized);
            if (!_providers.TryGetValue(_settings.Get(SettingKeys.EInvoice.Provider, "None"), out var provider))
                return Result.Fail<EInvoiceSubmission>(LocalizationService.Get("Str.EInvoice.NoProvider"), ErrorCode.ValidationFailed);

            var previous = _submissions.Latest(invoiceId);
            if (previous?.Status == EInvoiceStatus.Accepted) return Result.Ok(previous);

            var invoice = _invoices.GetById(invoiceId);
            if (invoice == null) return Result.Fail<EInvoiceSubmission>(LocalizationService.Get("Str.Payment.NoInvoice"), ErrorCode.NotFound);
            var lines = _invoices.GetLines(invoiceId);
            var products = _products.GetByIds(lines.Select(l => l.ProductId).Distinct()).ToDictionary(p => p.Id);

            var outcome = await provider.SubmitAsync(new EInvoiceSource(invoice, lines, _customers.GetById(invoice.CustomerId), products)).ConfigureAwait(false);
            var submission = new EInvoiceSubmission
            {
                InvoiceId = invoice.Id, InvoiceNo = invoice.InvoiceNo, Provider = provider.Name,
                Status = outcome.Accepted ? EInvoiceStatus.Accepted : EInvoiceStatus.Rejected,
                Uuid = outcome.Uuid, SubmissionId = outcome.SubmissionId, LongId = outcome.LongId, Qr = outcome.Qr,
                LastError = outcome.Error, SubmittedAt = DateTime.Now
            };
            submission.Id = _submissions.Insert(submission);
            return outcome.Accepted ? Result.Ok(submission) : Result.Fail<EInvoiceSubmission>(outcome.Error, ErrorCode.ValidationFailed);
        }
    }
}
