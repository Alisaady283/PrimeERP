using System.Collections.Generic;

namespace PrimeERP.Application.Services.Entities
{
    /// <summary>إعلان صفحة كيان</summary>
    public sealed record EntitySpec
    {
        public required string Key { get; init; }
        public required string Strings { get; init; }
        public string NameLabel { get; init; }
        public string Sequence { get; init; }
        public IReadOnlyDictionary<string, string> Defaults { get; init; }
        public bool BranchScoped { get; init; }
    }
}
