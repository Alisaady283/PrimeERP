using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using PrimeERP.Data.Core;
using PrimeERP.Data.Query;
using PrimeERP.Data.Repositories.Base;
using PrimeERP.Data.Schema;
using PrimeERP.Domain.Entities;
using PrimeERP.Domain.Enums;
using Db = PrimeERP.Data.Core.DbHelper;

namespace PrimeERP.Data.Repositories
{
    public interface IVoucherRepository
    {
        void CreateTable();
        Voucher GetById(int id, DbConnection conn = null, DbTransaction tx = null);
        List<VoucherAllocation> GetAllocations(int voucherId, DbConnection conn = null, DbTransaction tx = null);
        (List<Voucher> Items, int Total) GetPaged(VoucherKind kind, int page, int pageSize, string searchText, bool sortDescending);
        int InsertHeader(DbConnection conn, DbTransaction tx, Voucher v);
        void InsertAllocation(DbConnection conn, DbTransaction tx, int voucherId, VoucherAllocation a);
        void SetLinks(DbConnection conn, DbTransaction tx, int voucherId, int? journalEntryId, int? chequeId);
        void Delete(DbConnection conn, DbTransaction tx, int id);
    }

    public class VoucherRepository : RepositoryBase<Voucher>, IVoucherRepository
    {
        protected override string TableName => "Vouchers";

        public void CreateTable()
        {
            SchemaBuilder.Table("Vouchers")
                .Id()
                .Text("VoucherNo", 30, required: true, unique: true)
                .DateCol("VoucherDate", nullable: false)
                .Int("Kind", nullable: false, defaultValue: 1)
                .Int("PartyKind", nullable: false, defaultValue: 1)
                .Int("PartyId")
                .Int("TreasuryId", nullable: false)
                .Decimal("Amount")
                .Int("Method", nullable: false, defaultValue: 1)
                .Text("Reference", 100)
                .Text("Notes")
                .Int("JournalEntryId")
                .Int("ChequeId")
                .Audit()
                .Index("VoucherDate")
                .Create();

            SchemaBuilder.Table("VoucherAllocations")
                .Id()
                .Int("VoucherId", nullable: false)
                .Int("LineNo", nullable: false, defaultValue: 1)
                .Text("InvoiceType", 40)
                .Text("InvoiceNo", 40)
                .Decimal("Amount")
                .Text("Notes")
                .ForeignKey("VoucherId", "Vouchers", "Id")
                .Index("VoucherId")
                .Create();
        }

        protected override Voucher Map(DataRow row) => new()
        {
            Id = Convert.ToInt32(row["Id"]),
            VoucherNo = row["VoucherNo"].ToString(),
            VoucherDate = Convert.ToDateTime(row["VoucherDate"]),
            Kind = (VoucherKind)Convert.ToInt32(row["Kind"]),
            PartyKind = (PartyKind)Convert.ToInt32(row["PartyKind"]),
            PartyId = row["PartyId"] == DBNull.Value ? null : Convert.ToInt32(row["PartyId"]),
            TreasuryId = Convert.ToInt32(row["TreasuryId"]),
            Amount = Convert.ToDecimal(row["Amount"]),
            Method = (PaymentMethod)Convert.ToInt32(row["Method"]),
            Reference = row["Reference"] == DBNull.Value ? null : row["Reference"].ToString(),
            Notes = row["Notes"] == DBNull.Value ? null : row["Notes"].ToString(),
            JournalEntryId = row["JournalEntryId"] == DBNull.Value ? null : Convert.ToInt32(row["JournalEntryId"]),
            ChequeId = row["ChequeId"] == DBNull.Value ? null : Convert.ToInt32(row["ChequeId"]),
            CreatedAt = row["CreatedAt"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(row["CreatedAt"]),
            CreatedBy = row["CreatedBy"] == DBNull.Value ? null : row["CreatedBy"].ToString(),
        };

        private static VoucherAllocation MapAllocation(DataRow row) => new()
        {
            Id = Convert.ToInt32(row["Id"]), VoucherId = Convert.ToInt32(row["VoucherId"]), LineNo = Convert.ToInt32(row["LineNo"]),
            InvoiceType = row["InvoiceType"] == DBNull.Value ? null : row["InvoiceType"].ToString(),
            InvoiceNo = row["InvoiceNo"] == DBNull.Value ? null : row["InvoiceNo"].ToString(),
            Amount = Convert.ToDecimal(row["Amount"]),
            Notes = row["Notes"] == DBNull.Value ? null : row["Notes"].ToString(),
        };

        public override Voucher GetById(int id, DbConnection conn = null, DbTransaction tx = null) =>
            QueryOne("SELECT * FROM Vouchers WHERE Id = @id", conn, tx, ("@id", id));

        public List<VoucherAllocation> GetAllocations(int voucherId, DbConnection conn = null, DbTransaction tx = null) =>
            QueryAs(MapAllocation, "SELECT * FROM VoucherAllocations WHERE VoucherId = @id ORDER BY LineNo", conn, tx, ("@id", voucherId));

        public (List<Voucher> Items, int Total) GetPaged(VoucherKind kind, int page, int pageSize, string searchText, bool sortDescending)
        {
            var where = new WhereBuilder().Eq("Kind", (int)kind).LikeAny(searchText, "VoucherNo", "Reference");
            var direction = sortDescending ? "DESC" : "ASC";

            var total = Convert.ToInt32(Scalar($"SELECT COUNT(*) FROM Vouchers {where.Sql}", where.Parameters));
            var sql = $@"SELECT * FROM Vouchers {where.Sql} ORDER BY VoucherDate {direction}, Id {direction}
                         {DbFactory.Current.LimitClause(Math.Max(0, page - 1) * pageSize, pageSize)}";
            return (Query(sql, null, null, where.Parameters), total);
        }

        public int InsertHeader(DbConnection conn, DbTransaction tx, Voucher v) =>
            InsertGetId(@"INSERT INTO Vouchers (VoucherNo, VoucherDate, Kind, PartyKind, PartyId, TreasuryId, Amount, Method, Reference, Notes, CreatedBy)
                          VALUES (@no, @date, @kind, @pkind, @party, @treasury, @amount, @method, @ref, @notes, @by)",
                conn, tx, ("@no", v.VoucherNo), ("@date", v.VoucherDate), ("@kind", (int)v.Kind), ("@pkind", (int)v.PartyKind),
                ("@party", (object)v.PartyId ?? DBNull.Value), ("@treasury", v.TreasuryId), ("@amount", v.Amount),
                ("@method", (int)v.Method), ("@ref", v.Reference ?? ""), ("@notes", v.Notes ?? ""), ("@by", v.CreatedBy));

        public void InsertAllocation(DbConnection conn, DbTransaction tx, int voucherId, VoucherAllocation a) =>
            Exec(@"INSERT INTO VoucherAllocations (VoucherId, LineNo, InvoiceType, InvoiceNo, Amount, Notes)
                   VALUES (@vid, @lno, @type, @no, @amount, @notes)",
                conn, tx, ("@vid", voucherId), ("@lno", a.LineNo), ("@type", a.InvoiceType ?? ""),
                ("@no", a.InvoiceNo ?? ""), ("@amount", a.Amount), ("@notes", a.Notes ?? ""));

        public void SetLinks(DbConnection conn, DbTransaction tx, int voucherId, int? journalEntryId, int? chequeId) =>
            Exec("UPDATE Vouchers SET JournalEntryId = @je, ChequeId = @cq WHERE Id = @id", conn, tx,
                ("@je", (object)journalEntryId ?? DBNull.Value), ("@cq", (object)chequeId ?? DBNull.Value), ("@id", voucherId));

        public void Delete(DbConnection conn, DbTransaction tx, int id)
        {
            Exec("DELETE FROM VoucherAllocations WHERE VoucherId = @id", conn, tx, ("@id", id));
            Exec("DELETE FROM Vouchers WHERE Id = @id", conn, tx, ("@id", id));
        }
    }
}
