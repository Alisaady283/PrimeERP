namespace PrimeERP.Api.Infrastructure.Pages
{
    /// <summary>صفحةٌ يخدمها الخادم: خدمتها، وما يُقرأ منها، وما يُكتب، وما تحجبه صلاحية — عقدها الحامي بلا صنفٍ مكرّر لكل صفحة</summary>
    public sealed record PageResource
    {
        public required string Key { get; init; }
        public required Type Service { get; init; }

        /// <summary>حقول الصفّ والتفاصيل؛ فارغةٌ = الخدمة تُرجع DTO معدّاً للعرض فيُرسل كما هو</summary>
        public string[] Read { get; init; }

        /// <summary>حقول الإدخال للكيان؛ فارغةٌ = الخدمة تأخذ DTO إدخالٍ معدّاً فيُقرأ الطلب إليه</summary>
        public string[] Write { get; init; }

        /// <summary>حقلٌ ← الصلاحية التي تُظهره وتسمح بتغييره</summary>
        public IReadOnlyDictionary<string, string> Guarded { get; init; } = new Dictionary<string, string>();

        /// <summary>حقول الاختيار في القوائم: المعرّف والنص والتلميح</summary>
        public string OptionLabel { get; init; } = "Name";
        public string OptionHint { get; init; } = "Code";
    }
}
