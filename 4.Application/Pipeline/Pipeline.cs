using System;
using System.Collections.Generic;
using PrimeERP.Domain.Contracts;
using PrimeERP.Domain.Enums;
using PrimeERP.Domain.Results;
using PrimeERP.Application.Pipeline.Steps;
using PrimeERP.Platform.Audit;
using PrimeERP.Platform.Permissions;

namespace PrimeERP.Application.Pipeline
{
    /// <summary>مؤلِّف خطوات للعمليات غير النمطية</summary>
    public class Pipeline<TOut>
    {
        private readonly List<IStep> _steps = new();

        public Pipeline<TOut> Step(IStep step) { _steps.Add(step); return this; }

        public Pipeline<TOut> Permission(IPermissionService permissions, string key, string deniedMessage) =>
            Step(new PermissionStep(permissions, key, deniedMessage));

        public Pipeline<TOut> Validate<T>(IValidator<T> validator) =>
            Step(new ValidationStep<T>(validator));

        public Pipeline<TOut> Rule(string name, Func<PipelineContext, Result> rule) =>
            Step(new FuncStep(name, rule));

        public Pipeline<TOut> InTransaction(Action<TransactionBuilder> configure)
        {
            var builder = new TransactionBuilder();
            configure(builder);
            return Step(new TransactionStep(builder.Steps));
        }

        public Pipeline<TOut> Audit(IAuditLogger audit, string tableName, AuditAction action) =>
            Step(new AuditStep(audit, tableName, action));

        public Result<TOut> Execute(object input)
        {
            var ctx = new PipelineContext { Input = input };

            foreach (var step in _steps)
            {
                ctx.Log.Add(step.Name);
                var r = step.Execute(ctx);
                if (r.IsFailure)
                {
                    ctx.Abort(r);
                    return Result.Fail<TOut>(r.ErrorMessage, r.ErrorCode);
                }
            }

            return Result.Ok(ctx.OutputAs<TOut>());
        }
    }

    /// <summary>خطوات معاملةٍ واحدة</summary>
    public class TransactionBuilder
    {
        internal readonly List<IStep> Steps = new();

        public TransactionBuilder Before(string name, Func<PipelineContext, Result> fn) => Step(name, fn);
        public TransactionBuilder Save(string name, Func<PipelineContext, Result> fn) => Step(name, fn);
        public TransactionBuilder After(string name, Func<PipelineContext, Result> fn) => Step(name, fn);

        public TransactionBuilder Step(string name, Func<PipelineContext, Result> fn)
        {
            Steps.Add(new FuncStep(name, fn));
            return this;
        }

        public TransactionBuilder Step(IStep step)
        {
            Steps.Add(step);
            return this;
        }
    }
}
