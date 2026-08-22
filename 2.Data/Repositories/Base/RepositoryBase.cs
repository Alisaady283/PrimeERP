using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
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

        public virtual List<T> GetAll(DbConnection conn = null, DbTransaction tx = null) =>
            Query($"SELECT * FROM {TableName}", conn, tx);

        public virtual T GetById(int id, DbConnection conn = null, DbTransaction tx = null) =>
            QueryOne($"SELECT * FROM {TableName} WHERE Id = @id", conn, tx, ("@id", id));
    }
}
