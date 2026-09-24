using PrimeERP.Domain.Results;

namespace PrimeERP.Application.Pipeline
{
    /// <summary>عقد الخطوة الواحدة</summary>
    public interface IStep
    {
        string Name { get; }
        Result Execute(PipelineContext ctx);
    }
}
