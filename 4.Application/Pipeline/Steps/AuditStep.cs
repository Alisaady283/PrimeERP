using System;
using PrimeERP.Domain.Enums;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Audit;

namespace PrimeERP.Application.Pipeline.Steps
{
    /// <summary>خطوة تسجيل التدقيق</summary>
    public class AuditStep : IStep
    {
        private readonly IAuditLogger _audit;
        private readonly string _tableName;
        private readonly AuditAction _action;

        public string Name => $"Audit:{_tableName}:{_action}";

        public AuditStep(IAuditLogger audit, string tableName, AuditAction action)
        {
            _audit = audit;
            _tableName = tableName;
            _action = action;
        }

        public Result Execute(PipelineContext ctx)
        {
            var recordId = ctx.Items.TryGetValue("AuditRecordId", out var id) ? (int)id : 0;
            ctx.Items.TryGetValue("AuditOldValue", out var oldValue);
            ctx.Items.TryGetValue("AuditNewValue", out var newValue);
            ctx.Items.TryGetValue("AuditDetails", out var details);

            _audit.Log(_tableName, recordId, _action, oldValue, newValue, (string)details);
            return Result.Ok();
        }
    }
}
