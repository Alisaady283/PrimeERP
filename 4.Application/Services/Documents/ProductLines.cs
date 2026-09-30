using System;
using System.Collections.Generic;
using PrimeERP.Application.Services.Entities;
using PrimeERP.Data.Repositories;
using PrimeERP.Domain.Entities;
using PrimeERP.Domain.Results;

namespace PrimeERP.Application.Services.Documents
{
    /// <summary>سطور المستند بأصنافها</summary>
    public static class ProductLines
    {
        public static Result<List<TLine>> Resolve<TIn, TLine>(IProductRepository products, IEnumerable<TIn> input,
            Func<TIn, string> code, Func<TIn, Product, int, Result<TLine>> line) =>
            ByCode.Resolve(codes => products.ByCodes(codes), input, code, "Str.Document.ProductCodeNotFound", line);
    }
}
