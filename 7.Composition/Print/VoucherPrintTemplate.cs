using System;
using System.Collections.Generic;
using PrimeERP.Application.DTOs.Vouchers;
using PrimeERP.Application.Services.Print;
using PrimeERP.Domain.Contracts;
using PrimeERP.Domain.Enums;
using PrimeERP.Domain.Results;

namespace PrimeERP.Composition.Print
{
    /// <summary>سند القبض/الصرف بصيغته المتعارف عليها: "استلمنا من السيد" أو "ادفعوا بموجب هذا الأمر إلى
    /// السيد"، والمبلغ رقماً وكتابةً، ثم سبب الصرف وطريقة الدفع والتوقيعات. القالب العام لا يصلح له لأنه
    /// جدول سطور، والسند إقرار مكتوب لا كشف بنود.</summary>
    public static class VoucherPrintTemplate
    {
        public static IPrintable From(VoucherDetailDto voucher, bool isReceipt, string currency, string subUnit) =>
            new VoucherPrintable(voucher, isReceipt, currency, subUnit);

        private class VoucherPrintable : IPrintable
        {
            private readonly VoucherDetailDto _voucher;
            private readonly bool _isReceipt;
            private readonly string _currency, _subUnit;

            public VoucherPrintable(VoucherDetailDto voucher, bool isReceipt, string currency, string subUnit)
            {
                _voucher = voucher;
                _isReceipt = isReceipt;
                _currency = currency;
                _subUnit = subUnit;
            }

            public string DocumentTitle    => _isReceipt ? "سند قبض" : "سند صرف";
            public string DocumentSubtitle => _voucher.VoucherNo;
            public PrintOrientation Orientation => PrintOrientation.Portrait;

            public Dictionary<string, string> HeaderFields => new()
            {
                ["رقم السند"] = _voucher.VoucherNo,
                ["التاريخ"]   = _voucher.VoucherDate.ToString("yyyy-MM-dd"),
            };

            public Dictionary<string, string> FooterFields => null;

            public bool ShowCompanyHeader => true;
            public bool ShowPageNumbers   => false;
            public bool ShowSignatures    => true;

            public List<string> SignatureLabels => _isReceipt
                ? new() { "المستلِم", "المحاسب", "المدير" }
                : new() { "المستلِم", "أمين الخزينة", "المحاسب", "المدير" };

            public List<PrintSection> BuildSections()
            {
                var party = string.IsNullOrWhiteSpace(_voucher.PartyName) ? "………………………………" : _voucher.PartyName;
                var reason = string.IsNullOrWhiteSpace(_voucher.Notes) ? "………………………………" : _voucher.Notes;

                var sections = new List<PrintSection>
                {
                    new()
                    {
                        Type = PrintSectionType.Text,
                        Text = _isReceipt
                            ? $"استلمنا من السيد / السادة: {party}"
                            : $"ادفعوا بموجب هذا الأمر إلى السيد / السادة: {party}"
                    },
                    new()
                    {
                        Type = PrintSectionType.Callout,
                        Variant = StatusVariant.Info,
                        Text = $"مبلغاً وقدره: {_voucher.Amount:N2}   —   {PrimeERP.Domain.Helpers.ArabicNumberToWords.Convert(_voucher.Amount, _currency, _subUnit)}"
                    },
                    new() { Type = PrintSectionType.Text, Text = $"وذلك عن: {reason}" },
                    new()
                    {
                        Type = PrintSectionType.KeyValues,
                        KeyValues = PaymentDetails()
                    }
                };

                if (_voucher.Allocations is { Count: > 0 })
                    sections.Add(new PrintSection
                    {
                        Type = PrintSectionType.Table,
                        Title = "الفواتير المسدَّدة",
                        Columns = new()
                        {
                            new() { Key = "InvoiceNo", Header = "الفاتورة" },
                            new() { Key = "Amount", Header = "المبلغ", Format = "N2" },
                        },
                        Rows = _voucher.Allocations.ConvertAll(a => new Dictionary<string, object>
                        { ["InvoiceNo"] = a.InvoiceNo, ["Amount"] = a.Amount })
                    });

                return sections;
            }

            private Dictionary<string, string> PaymentDetails()
            {
                var details = new Dictionary<string, string>
                {
                    ["طريقة الدفع"] = _voucher.MethodName,
                    ["الخزينة / البنك"] = _voucher.TreasuryName,
                };

                if (!string.IsNullOrWhiteSpace(_voucher.Reference))
                    details[_voucher.Method == PaymentMethod.Cheque ? "رقم الشيك" : "المرجع"] = _voucher.Reference;

                return details;
            }
        }
    }
}
