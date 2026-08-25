using System;
using System.Collections.Generic;
using PrimeERP.Domain.Enums;
using PrimeERP.Domain.Results;
using PrimeERP.Application.Pipeline.Steps;
using PrimeERP.Platform.Audit;

namespace PrimeERP.Application.Pipeline.Operations
{
    /// <summary>
    /// يبني عملية كتابة كاملة (Create/Update/Delete/Post/Unpost): صلاحية + تحقق (اختياري) تُضافان فوراً،
    /// ثم Rule تُلحَق مباشرة (قبل المعاملة)، ثم Before/After تُخزَّن وتُنسَج داخل معاملة واحدة عند Run().
    /// هذا الترتيب المؤجَّل هو ما يجعل الاستدعاء النهائي "CreateOp(dto).Rule(x).Before(y).After(z).Run()"
    /// ينتج التسلسل الصحيح فعلياً (صلاحية، تحقق، قاعدة، معاملة{قبل، حفظ، بعد}، تدقيق) رغم ترتيب الاستدعاء المسطّح.
    /// </summary>
    public class WriteOperation<TDto>
    {
        private readonly List<IStep> _preSteps;
        private readonly object _input;
        private readonly Func<PipelineContext, Result> _save;
        private readonly IAuditLogger _audit;
        private readonly string _tableName;
        private readonly AuditAction _auditAction;
        private readonly Func<PipelineContext, TDto> _mapOutput;

        private readonly List<Func<PipelineContext, Result>> _rules = new();
        private readonly List<Func<PipelineContext, Result>> _beforeHooks = new();
        private readonly List<Func<PipelineContext, Result>> _afterHooks = new();

        public WriteOperation(List<IStep> preSteps, object input, Func<PipelineContext, Result> save,
                               IAuditLogger audit, string tableName, AuditAction auditAction,
                               Func<PipelineContext, TDto> mapOutput)
        {
            _preSteps = preSteps;
            _input = input;
            _save = save;
            _audit = audit;
            _tableName = tableName;
            _auditAction = auditAction;
            _mapOutput = mapOutput;
        }

        public WriteOperation<TDto> Rule(Func<PipelineContext, Result> rule) { _rules.Add(rule); return this; }
        public WriteOperation<TDto> Before(Func<PipelineContext, Result> hook) { _beforeHooks.Add(hook); return this; }
        public WriteOperation<TDto> After(Func<PipelineContext, Result> hook) { _afterHooks.Add(hook); return this; }

        public Result<TDto> Run()
        {
            var pipeline = new Pipeline<TDto>();

            foreach (var step in _preSteps) pipeline.Step(step);
            foreach (var rule in _rules) pipeline.Rule("Rule", rule);

            pipeline.InTransaction(tx =>
            {
                foreach (var hook in _beforeHooks) tx.Before("Before", hook);
                tx.Save("Save", _save);
                foreach (var hook in _afterHooks) tx.After("After", hook);
                tx.Step("MapOutput", ctx => { ctx.Output = _mapOutput(ctx); return Result.Ok(); });
            });

            if (_audit != null)
                pipeline.Audit(_audit, _tableName, _auditAction);

            return pipeline.Execute(_input);
        }
    }
}
