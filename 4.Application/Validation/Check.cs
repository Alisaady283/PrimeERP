using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using PrimeERP.Domain.Enums;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Localization;

namespace PrimeERP.Application.Validation
{
    /// <summary>الدالة الوحيدة للتحقق</summary>
    public static class Check
    {
        public static ValidationResult Fields<T>(T item, params Field<T>[] fields)
        {
            var result = new ValidationResult();
            foreach (var field in fields)
            {
                var error = Error(item, field);
                if (error != null) result.AddError(field.Name ?? NameOf(field.Of), error);
            }
            return result;
        }

        /// <summary>النتيجة بصيغة العملية</summary>
        public static Result Valid<T>(T item, params Field<T>[] fields)
        {
            var result = Fields(item, fields);
            return result.IsValid ? Result.Ok() : Result.Fail(result.Errors.Values, ErrorCode.ValidationFailed);
        }

        private static string Error<T>(T item, Field<T> f)
        {
            var value = f.Of.Compile()(item);
            var text = value as string;
            var label = string.IsNullOrEmpty(f.Label) ? "" : LocalizationService.Get(f.Label);

            string rule = null;
            object[] args = { label };

            if (f.Required && Missing(value)) rule = "Required";
            else if (f.Min > 0 && !string.IsNullOrEmpty(text) && text.Length < f.Min) (rule, args) = ("MinLength", new object[] { label, f.Min });
            else if (f.Max > 0 && text?.Length > f.Max) (rule, args) = ("MaxLength", new object[] { label, f.Max });
            else if ((f.From != null || f.To != null) && Number(value) is { } n && (n < f.From || n > f.To))
                (rule, args) = f.To != null ? ("Range", new object[] { label, f.From, f.To }) : ("NonNegative", new object[] { label });
            else if (f.Positive && !(Number(value) > 0)) rule = "Positive";
            else if (f.Format != FieldFormat.None && !Missing(value) && !Matches(value, f.Format)) rule = "Invalid";
            else if (f.Must != null && !f.Must(item)) rule = "Invalid";

            if (rule == null) return null;
            if (f.Message != null) return LocalizationService.Get(f.Message, f.Args?.Invoke(item) ?? Array.Empty<object>());
            return LocalizationService.Get("Str.Rule." + rule, args);
        }

        private static bool Missing(object value) => value switch
        {
            null => true,
            string s => string.IsNullOrWhiteSpace(s),
            int i => i <= 0,
            DateTime d => d == default,
            System.Collections.ICollection c => c.Count == 0,
            _ => false
        };

        private static decimal? Number(object value) =>
            value is IConvertible c && value is not string ? Convert.ToDecimal(c, CultureInfo.InvariantCulture) : null;

        private static bool Matches(object value, FieldFormat format) => format switch
        {
            FieldFormat.Phone => Regex.Replace(value.ToString(), @"[^\d]", "").Length is >= 7 and <= 15,
            FieldFormat.Email => Regex.IsMatch(value.ToString(), @"^[^@\s]+@[^@\s]+\.[^@\s]+$"),
            FieldFormat.Date => value is DateTime d ? d != default : DateTime.TryParse(value.ToString(), out _),
            FieldFormat.Digits => Regex.IsMatch(value.ToString(), @"^\d{1,30}$"),
            _ => true
        };

        private static string NameOf<T>(Expression<Func<T, object>> of)
        {
            var body = of.Body is UnaryExpression unary ? unary.Operand : of.Body;
            return body is MemberExpression member ? member.Member.Name : "";
        }
    }
}
