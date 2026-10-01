using PrimeERP.Domain.Calculations;
using PrimeERP.Application.Legacy.Documents;
using PrimeERP.Platform.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using PrimeERP.Application.DTOs.Documents;
using PrimeERP.Data.Repositories;
using PrimeERP.Domain.Entities.Common;
using PrimeERP.Domain.Enums;
using PrimeERP.Domain.Helpers;
using PrimeERP.Domain.Results;

namespace PrimeERP.Application.Services.Documents
{
    /// <summary>سطور الفواتير والمرتجعات ومجاميعها</summary>
    public static class TradeLines
    {
        public static Result<(List<TLine> Lines, LineAmounts Totals)> Resolve<TLine>(IProductRepository products,
            IReadOnlyList<CreateTradeLineDto> input) where TLine : DocumentLineBase, new()
        {
            var amounts = new List<LineAmounts>();
            var lines = ProductLines.Resolve(products, input, l => l.ProductCode, (l, product, _) =>
            {
                var a = LineCalc.ForLine(l.Qty, l.UnitPrice, l.DiscountPercent, l.VatPercent, l.WithholdingPercent);
                amounts.Add(a);
                return Result.Ok(new TLine
                {
                    LineNo = l.LineNo, Qty = l.Qty, UnitPrice = l.UnitPrice,
                    DiscountPercent = l.DiscountPercent, DiscountAmount = a.Discount,
                    VatPercent = l.VatPercent, VatAmount = a.Vat,
                    WithholdingPercent = l.WithholdingPercent, WithholdingAmount = a.Withholding,
                    LineTotal = a.Gross, NetAmount = a.Net, Notes = l.Notes
                });
            });
            if (lines.IsFailure) return lines.As<(List<TLine>, LineAmounts)>();

            return Result.Ok((lines.Value, LineCalc.Sum(amounts)));
        }

        public static List<TDto> ToDtos<TDto>(IEnumerable<DocumentLineBase> lines) where TDto : TradeLineDto, new() =>
            lines.Select(l => new TDto
            {
                LineNo = l.LineNo, ProductCode = l.ProductCode, ProductName = l.ProductName, Qty = l.Qty,
                UnitPrice = l.UnitPrice,
                DiscountPercent = l.DiscountPercent, DiscountAmount = l.DiscountAmount,
                VatPercent = l.VatPercent, VatAmount = l.VatAmount,
                WithholdingPercent = l.WithholdingPercent, WithholdingAmount = l.WithholdingAmount,
                LineTotal = l.LineTotal, NetAmount = l.NetAmount, Notes = l.Notes
            }).ToList();

        /// <summary>السطور ثم السحب ثم الحسابات</summary>
        public static Result<(List<TLine> Lines, LineAmounts Totals, TradeAccounts Accounts)> Prepare<TLine>(
            IProductRepository products, IDocumentPull links, IReadOnlyList<CreateTradeLineDto> input,
            Func<LineAmounts, Result<TradeAccounts>> accountsOf) where TLine : DocumentLineBase, new() =>
            Resolve<TLine>(products, input).Then(resolved =>
                links.ValidatePulls(input.Select(l => ((IPullableLine)l, l.Qty)))
                    .Then(() => accountsOf(resolved.Totals))
                    .Then(accounts => Result.Ok((resolved.Lines, resolved.Totals, accounts))));
    }
}
