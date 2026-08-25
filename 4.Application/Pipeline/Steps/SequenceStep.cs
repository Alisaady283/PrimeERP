using PrimeERP.Application.Services;
using PrimeERP.Domain.Results;

namespace PrimeERP.Application.Pipeline.Steps
{
    /// <summary>يولّد الرقم التالي لمفتاح تسلسل ويحفظه في ctx.Items[contextKey] — تلتقطه خطوة لاحقة (Before hook) لتعيينه على الكيان قبل الحفظ.</summary>
    public class SequenceStep : IStep
    {
        private readonly INumberSequenceService _sequences;
        private readonly string _key;
        private readonly string _contextKey;

        public string Name => $"Sequence:{_key}";

        public SequenceStep(INumberSequenceService sequences, string key, string contextKey)
        {
            _sequences = sequences;
            _key = key;
            _contextKey = contextKey;
        }

        public Result Execute(PipelineContext ctx)
        {
            var number = ctx.Conn != null
                ? _sequences.Next(ctx.Conn, ctx.Tx, _key)
                : _sequences.Next(_key);

            ctx.Items[_contextKey] = number;
            return Result.Ok();
        }
    }
}
