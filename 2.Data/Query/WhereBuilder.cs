using System;
using System.Collections.Generic;
using System.Linq;

namespace PrimeERP.Data.Query
{
    /// <summary>
    /// يبني جملة WHERE + قائمة بارامترات بشكل شرطي (بند يُضاف فقط لو قيمته موجودة) — بديل تكرار
    /// "var where = new List&lt;string&gt;(); var parameters = ...; if (x.HasValue) { where.Add(...); }"
    /// اليدوي المكرَّر في كل Repository له استعلام صفحات/بحث. بناء استعلام بحت — صفر منطق أعمال،
    /// صفر اتصال بقاعدة بيانات (النتيجة تُمرَّر لـ DbHelper.Query/Scalar كالمعتاد).
    /// </summary>
    public class WhereBuilder
    {
        private readonly List<string> _clauses = new();
        private readonly List<(string, object)> _parameters = new();
        private int _paramIndex;

        /// <summary>بند مساواة بسيط — يُضاف فقط لو value ليست null.</summary>
        public WhereBuilder Eq(string column, object value)
        {
            if (value is null) return this;
            var p = NextParam();
            _clauses.Add($"{column} = {p}");
            _parameters.Add((p, value));
            return this;
        }

        /// <summary>بند LIKE بحث جزئي (%term%) — يُضاف فقط لو term غير فارغ.</summary>
        public WhereBuilder Like(string column, string term)
        {
            if (string.IsNullOrWhiteSpace(term)) return this;
            var p = NextParam();
            _clauses.Add($"{column} LIKE {p}");
            _parameters.Add((p, $"%{term}%"));
            return this;
        }

        /// <summary>بند LIKE عبر أكثر من عمود بعلاقة OR (بحث حر عبر عدة حقول بنفس المصطلح) — بارامتر واحد مشترك.</summary>
        public WhereBuilder LikeAny(string term, params string[] columns)
        {
            if (string.IsNullOrWhiteSpace(term) || columns.Length == 0) return this;
            var p = NextParam();
            _clauses.Add("(" + string.Join(" OR ", columns.Select(c => $"{c} LIKE {p}")) + ")");
            _parameters.Add((p, $"%{term}%"));
            return this;
        }

        /// <summary>بند خام مُجهَّز مسبقاً بلا بارامتر شرطي (مقارنة عمودين ببعض، أو ثابت) — يُضاف دائماً لو condition (افتراضياً true).</summary>
        public WhereBuilder Raw(string sqlFragment, bool condition = true)
        {
            if (condition) _clauses.Add(sqlFragment);
            return this;
        }

        /// <summary>بند خام مع بارامتر واحد — لمقارنات غير المساواة البسيطة (>, <, !=...). يُضاف فقط لو value ليست null.</summary>
        public WhereBuilder RawWithParam(Func<string, string> sqlFragment, object value)
        {
            if (value is null) return this;
            var p = NextParam();
            _clauses.Add(sqlFragment(p));
            _parameters.Add((p, value));
            return this;
        }

        private string NextParam() => $"@w{_paramIndex++}";

        /// <summary>"WHERE ... AND ..." كاملة، أو "" لو بلا أي بند (بلا كلمة WHERE أصلاً — يُلحَق مباشرة بعد اسم الجدول).</summary>
        public string Sql => _clauses.Count == 0 ? "" : "WHERE " + string.Join(" AND ", _clauses);

        public (string, object)[] Parameters => _parameters.ToArray();
    }
}
