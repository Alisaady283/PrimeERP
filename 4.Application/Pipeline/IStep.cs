using PrimeERP.Domain.Results;

namespace PrimeERP.Application.Pipeline
{
    public interface IStep
    {
        string Name { get; }
        Result Execute(PipelineContext ctx);
    }
}
