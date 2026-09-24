using System.Collections.Generic;
using PrimeERP.Data.Core;
using PrimeERP.Domain.Results;

namespace PrimeERP.Application.Pipeline
{
    /// <summary>سياق يمرّ بين الخطوات</summary>
    public class PipelineContext
    {
        public PrimeDbContext Db { get; set; }
        public object Input { get; set; }
        public object Output { get; set; }
        public Dictionary<string, object> Items { get; } = new();
        public List<string> Log { get; } = new();
        public string UserId { get; set; }
        public bool IsAborted { get; private set; }
        public Result AbortReason { get; private set; }

        public void Abort(Result reason)
        {
            IsAborted = true;
            AbortReason = reason;
        }

        public T InputAs<T>() => (T)Input;
        public T OutputAs<T>() => (T)Output;
    }
}
