using PrimeERP.Domain.Enums;
using System;
using System.Linq.Expressions;

namespace PrimeERP.Application.Validation
{
    /// <summary>شرطُ حقلٍ بكل معاملاته</summary>
    public sealed record Field<T>(
        Expression<Func<T, object>> Of,
        string Label,
        bool Required = false,
        int Min = 0,
        int Max = 0,
        decimal? From = null,
        decimal? To = null,
        FieldFormat Format = FieldFormat.None,
        Func<T, bool> Must = null,
        string Message = null,
        Func<T, object[]> Args = null,
        string Name = null);
}
