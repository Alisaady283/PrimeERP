using System.Collections.Generic;
using System.Linq;

namespace PrimeERP.Data.Query
{
    /// <summary>
    /// ترتيب القوائم قاعدة واحدة لا اجتهاداً في كل مستودع:
    /// السجلّ المؤرَّخ (مستند) يُرتَّب بتاريخه أولاً — الأحدث أعلى — ثم برقمه ثم بوقت إنشائه، لأن الرقم
    /// وحده لا يعكس الترتيب حين تُضاف السجلات من أكثر من شاشة بسلاسل أرقام مختلفة. والسجلّ بلا تاريخ
    /// (بيانات أساسية) يُرتَّب بكوده. وفي الحالتين Id آخر فاصل فيبقى الترتيب قطعياً لا يتبدّل بين
    /// استعلامين متطابقين. فرز المستخدم من رأس الجدول يسبق ذلك كله ولا يُلغي الفواصل بعده.
    /// </summary>
    public static class OrderBuilder
    {
        /// <summary>
        /// column: العمود المطلوب (فرز المستخدم أو افتراضي الوحدة). tieBreakers: ما يفصل تساوي قيمه،
        /// بترتيب الأهمية — يُتجاهَل منها ما طابق العمود المطلوب فلا يتكرّر في الجملة.
        /// </summary>
        public static string By(string column, bool descending, params string[] tieBreakers) =>
            By(column, descending, "Id", tieBreakers);

        /// <summary>نسخة الاستعلامات ذات الاسم المستعار (e.Id مثلاً) — الفاصل الأخير يُمرَّر مؤهَّلاً.</summary>
        public static string By(string column, bool descending, string identity, params string[] tieBreakers)
        {
            var direction = descending ? "DESC" : "ASC";

            var columns = new List<string> { column }
                .Concat(tieBreakers ?? Enumerable.Empty<string>())
                .Append(identity)
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Distinct();

            return "ORDER BY " + string.Join(", ", columns.Select(c => $"{c} {direction}"));
        }
    }
}
