using PrimeERP.Domain.Contracts;
using PrimeERP.Domain.Results;

namespace PrimeERP.Application.Pipeline.Steps
{
    public class ValidationStep<T> : IStep
    {
        private readonly IValidator<T> _validator;

        public string Name => $"Validate:{typeof(T).Name}";

        public ValidationStep(IValidator<T> validator) => _validator = validator;

        public Result Execute(PipelineContext ctx)
        {
            var result = _validator.Validate(ctx.InputAs<T>());
            return result.IsValid ? Result.Ok() : Result.Fail(result.Errors.Values, ErrorCode.ValidationFailed);
        }
    }
}
