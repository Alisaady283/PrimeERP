using System;
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
        /// <summary>حقولٌ مطلوبة غير الاسم: الخاصية ونصّ عنوانها</summary>
        public IReadOnlyDictionary<string, string> Required { get; init; }
        /// <summary>أسماء المعرّفات في الصفّ (ProjectId ← ProjectName) لتُعرض في القائمة</summary>
        public IReadOnlyList<NameJoin> Names { get; init; }
    }

    /// <summary>اسم معرّفٍ يُقرأ بدفعةٍ من مصدره — Name خاصية الصفّ الناتجة</summary>
    public sealed record NameJoin(string Id, string Name, Func<IServiceProvider, IEnumerable<int>, Dictionary<int, string>> Of);
}
