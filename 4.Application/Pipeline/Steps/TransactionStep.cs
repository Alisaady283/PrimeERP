using System.Collections.Generic;
using PrimeERP.Domain.Results;
using PrimeERP.Data.Core;

namespace PrimeERP.Application.Pipeline.Steps
{
    /// <summary>خطوة فتح المعاملة أو إعادة</summary>
    public class TransactionStep : IStep
    {
        private readonly IReadOnlyList<IStep> _inner;

        public string Name => "Transaction";

        public TransactionStep(IReadOnlyList<IStep> inner) => _inner = inner;

        public Result Execute(PipelineContext ctx)
        {
            if (ctx.Db != null)
                return RunInner(ctx);

            try
            {
                return DbContextFactory.RunTransaction(db =>
                {
                    ctx.Db = db;

                    var result = RunInner(ctx);
                    if (result.IsFailure) throw new StepFailure(result);   // الفشل يُرجع المعاملة
                    return result;
                });
            }
            catch (StepFailure failure)
            {
                return failure.Result;
            }
            finally
            {
                ctx.Db = null;
            }
        }

        private sealed class StepFailure : System.Exception
        {
            public StepFailure(Result result) => Result = result;
            public Result Result { get; }
        }

        private Result RunInner(PipelineContext ctx)
        {
            foreach (var step in _inner)
            {
                ctx.Log.Add(step.Name);
                var r = step.Execute(ctx);
                if (r.IsFailure) return r;
            }
            return Result.Ok();
        }
    }
}
