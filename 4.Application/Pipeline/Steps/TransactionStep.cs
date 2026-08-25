using System.Collections.Generic;
using PrimeERP.Domain.Results;
using Db = PrimeERP.Data.Core.DbHelper;

namespace PrimeERP.Application.Pipeline.Steps
{
    /// <summary>يفتح معاملة جديدة إن لم يكن ctx.Conn مفتوحاً بالفعل (خدمة أخرى نادت هذا الـ Pipeline ضمن معاملتها) — يعيد استخدام المعاملة القائمة بدل فتح اتصال ثانٍ (يعلّق SQLite، راجع DbHelper.Query).</summary>
    public class TransactionStep : IStep
    {
        private readonly IReadOnlyList<IStep> _inner;

        public string Name => "Transaction";

        public TransactionStep(IReadOnlyList<IStep> inner) => _inner = inner;

        public Result Execute(PipelineContext ctx)
        {
            if (ctx.Conn != null)
                return RunInner(ctx);

            using var conn = Db.GetConnection();
            using var tx = conn.BeginTransaction();
            ctx.Conn = conn;
            ctx.Tx = tx;

            try
            {
                var result = RunInner(ctx);
                if (result.IsFailure) { tx.Rollback(); return result; }
                tx.Commit();
                return result;
            }
            catch
            {
                tx.Rollback();
                throw;
            }
            finally
            {
                ctx.Conn = null;
                ctx.Tx = null;
            }
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
