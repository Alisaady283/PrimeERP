using PrimeERP.Domain.Calculations;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PrimeERP.Application.Services.Documents
{
    /// <summary>الحالة كترحيل</summary>
    public sealed class StatusChange<TStatus> where TStatus : struct, Enum
    {
        private readonly IReadOnlyDictionary<TStatus, TStatus[]> _allowed;
        private readonly Func<TStatus, bool> _posts;

        public StatusChange(IReadOnlyDictionary<TStatus, TStatus[]> allowed, Func<TStatus, bool> posts)
        {
            _allowed = allowed;
            _posts = posts;
        }

        public IReadOnlyList<TStatus> Next(TStatus from) =>
            _allowed.TryGetValue(from, out var next) ? next : Array.Empty<TStatus>();

        public bool Allows(TStatus from, TStatus to) => Next(from).Contains(to);

        /// <summary>الدخول إليها يُرحِّل قيداً</summary>
        public bool Posts(TStatus status) => _posts(status);

        /// <summary>الخروج منها يعكس قيدها</summary>
        public bool Reverses(TStatus from) => _posts(from);
    }
}
