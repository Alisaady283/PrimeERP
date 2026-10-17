using PrimeERP.Domain.Results;

namespace PrimeERP.Api.Infrastructure
{
    /// <summary>Result إلى استجابة HTTP: النجاح بقيمته، والفشل ProblemDetails بحالته ورسالته المترجمة</summary>
    public static class ResultHttp
    {
        public static IResult ToHttp(this Result result) =>
            result.IsSuccess ? Results.NoContent() : Problem(result);

        public static IResult ToHttp<T, TResponse>(this Result<T> result, Func<T, TResponse> map) =>
            result.IsSuccess ? Results.Ok(map(result.Value)) : Problem(result);

        public static IResult Problem(Result result) => Results.Problem(
            detail: result.ErrorMessage,
            statusCode: StatusOf(result.ErrorCode),
            extensions: new Dictionary<string, object> { ["errors"] = result.Errors });

        private static int StatusOf(ErrorCode code) => code switch
        {
            ErrorCode.ValidationFailed => StatusCodes.Status422UnprocessableEntity,
            ErrorCode.NotFound => StatusCodes.Status404NotFound,
            ErrorCode.Unauthorized => StatusCodes.Status403Forbidden,
            ErrorCode.Conflict or ErrorCode.ConcurrencyConflict => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError,
        };
    }
}
