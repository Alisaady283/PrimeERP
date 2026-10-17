using System;
using System.Collections.Generic;
using System.Linq;
using PrimeERP.Application.Services.Entities;
using PrimeERP.Data.Repositories;
using PrimeERP.Domain.Entities;
using PrimeERP.Domain.Entities.Common;
using PrimeERP.Domain.Results;

namespace PrimeERP.Application.Services.Documents
{
    /// <summary>سطور المستند بأصنافها</summary>
    public static class ProductLines
    {
        public static Result<List<TLine>> Resolve<TIn, TLine>(IProductRepository products, IEnumerable<TIn> input,
            Func<TIn, string> code, Func<TIn, Product, int, Result<TLine>> line) where TLine : class, IProductLine =>
            ByCode.Resolve(codes => Find(products, codes.ToList()), input, code, "Str.Document.ProductCodeNotFound",
                (item, product, no) => line(item, product, no).Then(built =>
                {
                    (built.ProductId, built.ProductCode, built.ProductName) = (product.Id, product.Code, product.Name);
                    return Result.Ok(built);
                }));

        private static IReadOnlyDictionary<string, Product> Find(IProductRepository products, List<string> codes)
        {
            var found = products.ByCodes(codes);
            foreach (var (barcode, product) in products.ByBarcodes(codes.Where(c => c != null && !found.ContainsKey(c))))
                found[barcode] = product;
            return found;
        }
    }
}
