using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using PrimeERP.Data.Core;
using PrimeERP.Data.Repositories.Base;
using PrimeERP.Data.Schema;
using PrimeERP.Domain.Entities;
using Db = PrimeERP.Data.Core.DbHelper;

namespace PrimeERP.Data.Repositories
{
    public interface IDocumentLinkRepository
    {
        void CreateTable();
        int Insert(DocumentLink link, DbConnection conn = null, DbTransaction tx = null);
        List<DocumentLink> GetBySource(string sourceType, int sourceId, DbConnection conn = null, DbTransaction tx = null);
        List<DocumentLink> GetByTarget(string targetType, int targetId, DbConnection conn = null, DbTransaction tx = null);
        decimal GetPulledQty(string sourceType, int sourceLineId, DbConnection conn = null, DbTransaction tx = null);
        Dictionary<int, decimal> GetPulledBySource(string sourceType, int sourceId, DbConnection conn = null, DbTransaction tx = null);
        void DeleteByTarget(string targetType, int targetId, DbConnection conn = null, DbTransaction tx = null);
    }

    // سجل روابط السحب — إضافة/حذف فقط (لا تعديل): تغيير كمية مسحوبة = حذف روابط المستند الهدف وإعادة تسجيلها.
    public class DocumentLinkRepository : RepositoryBase<DocumentLink>, IDocumentLinkRepository
    {
        protected override string TableName => "DocumentLinks";

        public void CreateTable() =>
            SchemaBuilder.Table("DocumentLinks")
                .Id()
                .Text("SourceType", 40, required: true)
                .Int("SourceId", nullable: false)
                .Text("SourceNo", 40)
                .Int("SourceLineId", nullable: false)
                .Text("TargetType", 40, required: true)
                .Int("TargetId", nullable: false)
                .Int("TargetLineId", nullable: false)
                .Decimal("PulledQty")
                .Audit()
                .Index("SourceLineId")
                .Index("TargetId")
                .Create();

        protected override DocumentLink Map(DataRow row) => new()
        {
            Id           = Convert.ToInt32(row["Id"]),
            SourceType   = row["SourceType"].ToString(),
            SourceId     = Convert.ToInt32(row["SourceId"]),
            SourceNo     = row["SourceNo"] == DBNull.Value ? null : row["SourceNo"].ToString(),
            SourceLineId = Convert.ToInt32(row["SourceLineId"]),
            TargetType   = row["TargetType"].ToString(),
            TargetId     = Convert.ToInt32(row["TargetId"]),
            TargetLineId = Convert.ToInt32(row["TargetLineId"]),
            PulledQty    = Convert.ToDecimal(row["PulledQty"]),
            CreatedAt    = row["CreatedAt"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(row["CreatedAt"]),
            CreatedBy    = row["CreatedBy"] == DBNull.Value ? null : row["CreatedBy"].ToString()
        };

        private const string InsertSql = @"
            INSERT INTO DocumentLinks
                (SourceType, SourceId, SourceNo, SourceLineId, TargetType, TargetId, TargetLineId, PulledQty, CreatedBy)
            VALUES
                (@sType, @sId, @sNo, @sLine, @tType, @tId, @tLine, @qty, @createdBy)";

        public int Insert(DocumentLink l, DbConnection conn = null, DbTransaction tx = null) =>
            InsertGetId(InsertSql, conn, tx,
                ("@sType", l.SourceType), ("@sId", l.SourceId), ("@sNo", l.SourceNo ?? ""), ("@sLine", l.SourceLineId),
                ("@tType", l.TargetType), ("@tId", l.TargetId), ("@tLine", l.TargetLineId),
                ("@qty", l.PulledQty), ("@createdBy", l.CreatedBy));

        public List<DocumentLink> GetBySource(string sourceType, int sourceId, DbConnection conn = null, DbTransaction tx = null) =>
            Query("SELECT * FROM DocumentLinks WHERE SourceType = @t AND SourceId = @i", conn, tx,
                ("@t", sourceType), ("@i", sourceId));

        public List<DocumentLink> GetByTarget(string targetType, int targetId, DbConnection conn = null, DbTransaction tx = null) =>
            Query("SELECT * FROM DocumentLinks WHERE TargetType = @t AND TargetId = @i", conn, tx,
                ("@t", targetType), ("@i", targetId));

        public decimal GetPulledQty(string sourceType, int sourceLineId, DbConnection conn = null, DbTransaction tx = null)
        {
            const string sql = "SELECT COALESCE(SUM(PulledQty), 0) FROM DocumentLinks WHERE SourceType = @t AND SourceLineId = @l";
            var result = conn != null
                ? Db.Query(conn, tx, sql, Db.Params(("@t", sourceType), ("@l", sourceLineId))).Rows[0][0]
                : Scalar(sql, ("@t", sourceType), ("@l", sourceLineId));
            return result == DBNull.Value ? 0m : Convert.ToDecimal(result);
        }

        // استعلام واحد لكل مستند مصدر بدل استعلام لكل سطر — هذا ما يجعل حساب المتبقي وقت السحب رخيصاً
        // بلا الحاجة لتخزين الكمية المسحوبة على السطر نفسه (قيمة مشتقّة لا تُخزَّن).
        public Dictionary<int, decimal> GetPulledBySource(string sourceType, int sourceId, DbConnection conn = null, DbTransaction tx = null)
        {
            const string sql = @"SELECT SourceLineId, COALESCE(SUM(PulledQty), 0) AS Pulled FROM DocumentLinks
                                 WHERE SourceType = @t AND SourceId = @i GROUP BY SourceLineId";
            var table = conn != null
                ? Db.Query(conn, tx, sql, Db.Params(("@t", sourceType), ("@i", sourceId)))
                : Db.Query(sql, Db.Params(("@t", sourceType), ("@i", sourceId)));

            var map = new Dictionary<int, decimal>();
            foreach (DataRow row in table.Rows)
                map[Convert.ToInt32(row["SourceLineId"])] = Convert.ToDecimal(row["Pulled"]);
            return map;
        }

        public void DeleteByTarget(string targetType, int targetId, DbConnection conn = null, DbTransaction tx = null) =>
            Exec("DELETE FROM DocumentLinks WHERE TargetType = @t AND TargetId = @i", conn, tx,
                ("@t", targetType), ("@i", targetId));
    }
}
