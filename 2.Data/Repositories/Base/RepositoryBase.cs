using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using PrimeERP.Data.Query;
using Db = PrimeERP.Data.Core.DbHelper;

namespace PrimeERP.Data.Repositories.Base
{
    /// <summary>
    /// أساس حقيقي بالتوريث لكل Repository — يمتص التكرار الحقيقي المكتشَف عبر الستة الحالية: كل دالة
    /// قراءة/كتابة كانت تُكتب مرتين حرفياً (نسخة عادية + نسخة (DbConnection conn, DbTransaction tx)
    /// للاستدعاء من داخل معاملة مستدعٍ آخر مفتوحة بالفعل — راجع DbHelper.Query(conn,tx,...) لسبب الحاجة
    /// الحقيقية: اتصال جديد من داخل معاملة أخرى يُعلِّق على SQLite). هنا بدالة واحدة ببارامترين اختياريين
    /// (conn=null, tx=null) — النسخة العادية تفتح اتصالاً ضمنياً عبر DbHelper، النسخة (conn,tx) تُعيد
    /// استخدام اتصال المستدعي. Map(DataRow) وTableName فقط ما يبقى خاصاً بكل Repository مشتق.
    /// </summary>
    public abstract class RepositoryBase<T>
    {
        protected abstract string TableName { get; }
        protected abstract T Map(DataRow row);

        protected List<T> Query(string sql, DbConnection conn = null, DbTransaction tx = null, params (string, object)[] p) =>
            (conn != null ? Db.Query(conn, tx, sql, Db.Params(p)) : Db.Query(sql, Db.Params(p)))
                .AsEnumerable().Select(Map).ToList();

        protected T QueryOne(string sql, DbConnection conn = null, DbTransaction tx = null, params (string, object)[] p) =>
            Query(sql, conn, tx, p).FirstOrDefault();

        protected static void Exec(string sql, DbConnection conn = null, DbTransaction tx = null, params (string, object)[] p)
        {
            if (conn != null) { using var cmd = Db.CreateCommand(conn, tx, sql, Db.Params(p)); cmd.ExecuteNonQuery(); }
            else Db.Execute(sql, Db.Params(p));
        }

        protected static int InsertGetId(string sql, DbConnection conn = null, DbTransaction tx = null, params (string, object)[] p) =>
            conn != null ? Db.InsertAndGetId(conn, tx, sql, Db.Params(p)) : Db.InsertAndGetId(sql, Db.Params(p));

        protected static object Scalar(string sql, params (string, object)[] p) => Db.Scalar(sql, Db.Params(p));

        // ===================== الحذف =====================

        /// <summary>
        /// جدول سطور المستند ومفتاحه فيه. يُعلنهما مستودع المستند، وفارغ = سجلٌّ بلا سطور. المنطق
        /// والحواجز تبقى في الخدمة — هذا ينفّذ فقط.
        /// </summary>
        protected virtual string LineTable => null;
        protected virtual string LineForeignKey => "DocumentId";

        /// <summary>حذف ناعم: السجل يبقى ويُخفى — كل استعلامات القراءة ترشّح IsDeleted.</summary>
        protected void SoftDelete(int id, string deletedBy = null, DbConnection conn = null, DbTransaction tx = null) =>
            SoftDelete(TableName, id, deletedBy, conn, tx);

        /// <summary>جدولٌ ثانٍ يديره نفس المستودع — نفس الجملة بلا نسخةٍ منها.</summary>
        protected void SoftDelete(string table, int id, string deletedBy = null, DbConnection conn = null, DbTransaction tx = null) =>
            Exec($"UPDATE {table} SET IsDeleted = @deleted, DeletedAt = @at, DeletedBy = @by WHERE Id = @id",
                conn, tx, ("@deleted", true), ("@at", DateTime.Now), ("@by", deletedBy ?? ""), ("@id", id));

        /// <summary>حذف صلب: السجل وسطوره إن كان له سطور — في معاملة المستدعي.</summary>
        protected void HardDelete(int id, DbConnection conn = null, DbTransaction tx = null)
        {
            if (LineTable != null)
                Exec($"DELETE FROM {LineTable} WHERE {LineForeignKey} = @id", conn, tx, ("@id", id));

            Exec($"DELETE FROM {TableName} WHERE Id = @id", conn, tx, ("@id", id));
        }


        /// <summary>
        /// نفس Query أعلاه لكن لنوع سطر آخر غير T — لِـ Repository يدير أكثر من كيان مرتبط (مثال: JournalRepository
        /// يدير JournalEntry+JournalLine معاً، FiscalPeriodRepository يدير FiscalYear+FiscalPeriod معاً).
        /// T نفسها تبقى الكيان الأساسي (GetAll/GetById المُوروثتان)؛ هذه لقراءة الكيان الثانوي بنفس آلية الاتصال.
        /// </summary>
        protected static List<TOther> QueryAs<TOther>(Func<DataRow, TOther> map, string sql, DbConnection conn = null, DbTransaction tx = null, params (string, object)[] p) =>
            (conn != null ? Db.Query(conn, tx, sql, Db.Params(p)) : Db.Query(sql, Db.Params(p)))
                .AsEnumerable().Select(map).ToList();

        protected static TOther QueryOneAs<TOther>(Func<DataRow, TOther> map, string sql, DbConnection conn = null, DbTransaction tx = null, params (string, object)[] p) =>
            QueryAs(map, sql, conn, tx, p).FirstOrDefault();

        /// <summary>صفحةٌ واحدة وعددٌ كامل — العدّ والقطع هنا وحدهما، والمستودع يصف شرطه وترتيبه فقط.</summary>
        protected (List<T> Items, int Total) Page(WhereBuilder where, int page, int pageSize, string order,
            string from = null, string select = null, DbConnection conn = null, DbTransaction tx = null)
        {
            var table = from ?? TableName;
            var total = Convert.ToInt32(Scalar($"SELECT COUNT(*) FROM {table} {where.Sql}", where.Parameters));

            var sql = $@"{select ?? $"SELECT * FROM {table}"} {where.Sql} {order}
                         {Core.DbFactory.Current.LimitClause(Math.Max(0, page - 1) * pageSize, pageSize)}";

            return (Query(sql, conn, tx, where.Parameters), total);
        }

        /// <summary>أوائل الصفوف لبحثٍ سريع — نفس القطع بلا عدّ.</summary>
        protected List<T> Top(WhereBuilder where, string order, int max,
            string from = null, DbConnection conn = null, DbTransaction tx = null) =>
            Query($"SELECT * FROM {from ?? TableName} {where.Sql} {order} {Core.DbFactory.Current.LimitClause(0, max)}",
                conn, tx, where.Parameters);

        /// <summary>عمود الفرز المطلوب إن كان مسموحاً، وإلا الافتراضي — فلا يصل نصٌّ غير مُعلَن إلى SQL.</summary>
        protected static string SortOf(string requested, string fallback, params string[] allowed) =>
            allowed.Contains(requested) ? requested : fallback;

        public virtual List<T> GetAll(DbConnection conn = null, DbTransaction tx = null) =>
            Query($"SELECT * FROM {TableName}", conn, tx);

        public virtual T GetById(int id, DbConnection conn = null, DbTransaction tx = null) =>
            QueryOne($"SELECT * FROM {TableName} WHERE Id = @id", conn, tx, ("@id", id));
    }
}
