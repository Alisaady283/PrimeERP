using System.Collections.Generic;
using System.Linq;

namespace PrimeERP.Core.Common
{
    public enum ErrorCode
    {
        None = 0,
        ValidationFailed,
        NotFound,
        Unauthorized,
        Conflict,
        ConcurrencyConflict,
        Unexpected
    }

    /// <summary>نتيجة عملية بلا قيمة راجعة — كل Service يرجعها بدل رمي استثناءات للتحكّم في تدفّق البرنامج.</summary>
    public class Result
    {
        public bool       IsSuccess    { get; protected set; }
        public bool       IsFailure    => !IsSuccess;
        public string     ErrorMessage { get; protected set; }
        public ErrorCode  ErrorCode    { get; protected set; } = ErrorCode.None;
        public List<string> Errors     { get; protected set; } = new();

        protected Result() { }

        public static Result Ok() => new() { IsSuccess = true };

        public static Result Fail(string message, ErrorCode code = ErrorCode.Unexpected) =>
            new() { IsSuccess = false, ErrorMessage = message, ErrorCode = code, Errors = { message } };

        public static Result Fail(IEnumerable<string> errors, ErrorCode code = ErrorCode.ValidationFailed)
        {
            var list = errors.ToList();
            return new Result
            {
                IsSuccess    = false,
                ErrorMessage = list.FirstOrDefault(),
                ErrorCode    = code,
                Errors       = list
            };
        }

        public static Result<T> Ok<T>(T value) => Result<T>.Ok(value);

        public static Result<T> Fail<T>(string message, ErrorCode code = ErrorCode.Unexpected) =>
            Result<T>.Fail(message, code);

        /// <summary>يجمع نتائج متعددة — ناجحة فقط لو كل العمليات نجحت، وإلا يجمع كل رسائل الفشل معاً.</summary>
        public static Result Combine(params Result[] results)
        {
            var failed = results.Where(r => r != null && r.IsFailure).ToList();
            if (failed.Count == 0) return Ok();

            var allErrors = failed.SelectMany(r => r.Errors).ToList();
            return Fail(allErrors, failed[0].ErrorCode);
        }
    }

    /// <summary>نتيجة عملية ترجع قيمة عند النجاح.</summary>
    public class Result<T> : Result
    {
        public T Value { get; private set; }

        public static Result<T> Ok(T value) => new() { IsSuccess = true, Value = value };

        public new static Result<T> Fail(string message, ErrorCode code = ErrorCode.Unexpected) =>
            new() { IsSuccess = false, ErrorMessage = message, ErrorCode = code, Errors = { message } };
    }
}
