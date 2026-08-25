using System;
using PrimeERP.Domain.Results;

namespace PrimeERP.Application.Pipeline.Steps
{
    /// <summary>غلاف عام لخطوة سطر واحد — يُستخدم لـ Rule/Before/After/Save/Map بدل تكرار نفس الشكل في أربع كلاسات منفصلة.</summary>
    public class FuncStep : IStep
    {
        private readonly Func<PipelineContext, Result> _action;

        public string Name { get; }

        public FuncStep(string name, Func<PipelineContext, Result> action)
        {
            Name = name;
            _action = action;
        }

        public Result Execute(PipelineContext ctx) => _action(ctx);
    }
}
