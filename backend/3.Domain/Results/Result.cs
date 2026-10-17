using System;
using System.Collections.Generic;
using System.Linq;

namespace PrimeERP.Domain.Results
{
    /// <summary>نتيجة العملية وحالتها</summary>
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

    /// <summary>نتيجة عملية بلا قيمة راجعة</summary>
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

        /// <summary>يحمل الفشل نفسه إلى نوعٍ</summary>
        public Result<TOut> As<TOut>() =>
            Result<TOut>.Carry(ErrorMessage, ErrorCode, Errors);

        public Result Then(Func<Result> next) => IsSuccess ? next() : this;

        public Result<TOut> Then<TOut>(Func<Result<TOut>> next) => IsSuccess ? next() : As<TOut>();

        public static Result Combine(params Result[] results)
        {
            var failed = results.Where(r => r != null && r.IsFailure).ToList();
            if (failed.Count == 0) return Ok();

            var allErrors = failed.SelectMany(r => r.Errors).ToList();
            return Fail(allErrors, failed[0].ErrorCode);
        }
    }

    /// <summary>نتيجة بقيمة</summary>
    public class Result<T> : Result
    {
        public T Value { get; private set; }

        public static Result<T> Ok(T value) => new() { IsSuccess = true, Value = value };

        public Result<TOut> Then<TOut>(Func<T, Result<TOut>> next) => IsSuccess ? next(Value) : As<TOut>();

        public Result Then(Func<T, Result> next) => IsSuccess ? next(Value) : this;

        public new static Result<T> Fail(string message, ErrorCode code = ErrorCode.Unexpected) =>
            new() { IsSuccess = false, ErrorMessage = message, ErrorCode = code, Errors = { message } };

        internal static Result<T> Carry(string message, ErrorCode code, List<string> errors) =>
            new() { IsSuccess = false, ErrorMessage = message, ErrorCode = code, Errors = errors };
    }
}
