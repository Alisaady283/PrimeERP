using PrimeERP.Application.Validation;
using System;
using System.Collections.Generic;
using System.Linq;
using PrimeERP.Domain.Enums;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Localization;

namespace PrimeERP.Application.Services.Entities
{
    /// <summary>سطورٌ تُحلّ بكود كيانها</summary>
    public static class ByCode
    {
        public static Result<List<TOut>> Resolve<TIn, TEntity, TOut>(Func<IEnumerable<string>, IReadOnlyDictionary<string, TEntity>> find,
            IEnumerable<TIn> input, Func<TIn, string> code, string missingKey, Func<TIn, TEntity, int, Result<TOut>> build)
            where TEntity : class
        {
            var list = input.ToList();
            var found = find(list.Select(code));
            var lines = new List<TOut>();

            foreach (var item in list)
            {
                var entity = found.GetValueOrDefault(code(item) ?? "");
                var built = entity == null
                    ? Result.Fail<TOut>(LocalizationService.Get(missingKey, code(item)), ErrorCode.ValidationFailed)
                    : build(item, entity, lines.Count + 1);
                if (built.IsFailure) return built.As<List<TOut>>();
                lines.Add(built.Value);
            }

            return Result.Ok(lines);
        }
    }
}
