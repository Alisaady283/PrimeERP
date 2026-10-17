using System;
using System.Collections.Generic;
using System.Linq;
using PrimeERP.Domain.Results;

namespace PrimeERP.Application.Validation
{
    /// <summary>سطور المستند بشروط Check</summary>
    public static class DocumentLines
    {
        public static Result Check<T>(IReadOnlyCollection<T> lines, Func<T, decimal> qty, string noLinesKey = "Str.Document.NoLines",
            Func<T, decimal> price = null, string priceLabel = "Str.Line.Price")
        {
            var values = new List<Field<T>> { new(l => qty(l), "Str.Qty", Positive: true, Name: "Qty") };
            if (price != null) values.Add(new(l => price(l), priceLabel, Positive: true, Name: "Price"));

            return Validation.Check.Valid(lines ?? Array.Empty<T>(),
                    new Field<IReadOnlyCollection<T>>(x => x, "", Name: "Lines", Required: true, Message: noLinesKey))
                .Then(() => lines.Select(l => Validation.Check.Valid(l, values.ToArray())).FirstOrDefault(r => r.IsFailure) ?? Result.Ok());
        }

        /// <summary>مجموع السطور لا يتجاوز حدّه</summary>
        public static Result Within<T>(IReadOnlyCollection<T> lines, Func<T, decimal> amount, decimal limit, string exceedKey)
        {
            var total = (lines ?? Array.Empty<T>()).Sum(amount);
            return Validation.Check.Valid(lines ?? Array.Empty<T>(),
                new Field<IReadOnlyCollection<T>>(x => x, "", Name: "Lines", Must: _ => total <= limit, Message: exceedKey,
                    Args: _ => new object[] { total, limit }));
        }
    }
}
