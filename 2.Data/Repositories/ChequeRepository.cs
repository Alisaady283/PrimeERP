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

namespace PrimeERP.Data.Repositories
{
    public interface IChequeRepository
    {
        void CreateTable();
        Cheque GetById(int id, DbConnection conn = null, DbTransaction tx = null);
        (List<Cheque> Items, int Total) GetPaged(ChequeDirection? direction, ChequeStatus? status, int page, int pageSize, string searchText);
        List<ChequeMovement> GetMovements(int chequeId);
        List<Cheque> GetOpenForParty(int partyId, DateTime from, DateTime to);
        int Insert(DbConnection conn, DbTransaction tx, Cheque c);
        void SetStatus(DbConnection conn, DbTransaction tx, int id, ChequeStatus status, int? treasuryId);
        void Update(DbConnection conn, DbTransaction tx, Cheque c);
        void Delete(DbConnection conn, DbTransaction tx, int id);
        void DeleteMovements(DbConnection conn, DbTransaction tx, int chequeId);
        void InsertMovement(DbConnection conn, DbTransaction tx, ChequeMovement m);
    }

    public class ChequeRepository : RepositoryBase<Cheque>, IChequeRepository
    {
        protected override string TableName => "Cheques";

        public void CreateTable()
        {
            SchemaBuilder.Table("Cheques")
                .Id()
                .Text("ChequeNo", 40, required: true)
                .Int("Direction", nullable: false, defaultValue: 1)
                .Int("PartyKind", nullable: false, defaultValue: 1)
                .Int("PartyId")
                .Decimal("Amount")
                .DateCol("IssueDate", nullable: false)
                .DateCol("DueDate", nullable: false)
                .Text("BankName", 200)
                .Int("Status", nullable: false, defaultValue: 1)
                .Int("TreasuryId")
                .Int("VoucherId")
                .Text("Notes")
                .Audit()
                .Index("DueDate")
                .Index("Status")
                .Create();

            SchemaBuilder.Table("ChequeMovements")
                .Id()
                .Int("ChequeId", nullable: false)
                .DateCol("MovementDate", nullable: false)
                .Int("FromStatus", nullable: false, defaultValue: 1)
                .Int("ToStatus", nullable: false, defaultValue: 1)
                .Int("TreasuryId")
                .Int("JournalEntryId")
                .Text("Notes")
                .Audit()
                .ForeignKey("ChequeId", "Cheques", "Id")
                .Index("ChequeId")
                .Create();
        }

        protected override Cheque Map(DataRow row) => new()
        {
            Id = Convert.ToInt32(row["Id"]),
            ChequeNo = row["ChequeNo"].ToString(),
            Direction = (ChequeDirection)Convert.ToInt32(row["Direction"]),
            PartyKind = (PartyKind)Convert.ToInt32(row["PartyKind"]),
            PartyId = row["PartyId"] == DBNull.Value ? null : Convert.ToInt32(row["PartyId"]),
            Amount = Convert.ToDecimal(row["Amount"]),
            IssueDate = Convert.ToDateTime(row["IssueDate"]),
            DueDate = Convert.ToDateTime(row["DueDate"]),
            BankName = row["BankName"] == DBNull.Value ? null : row["BankName"].ToString(),
            Status = (ChequeStatus)Convert.ToInt32(row["Status"]),
            TreasuryId = row["TreasuryId"] == DBNull.Value ? null : Convert.ToInt32(row["TreasuryId"]),
            VoucherId = row["VoucherId"] == DBNull.Value ? null : Convert.ToInt32(row["VoucherId"]),
            Notes = row["Notes"] == DBNull.Value ? null : row["Notes"].ToString(),
            CreatedAt = row["CreatedAt"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(row["CreatedAt"]),
            CreatedBy = row["CreatedBy"] == DBNull.Value ? null : row["CreatedBy"].ToString(),
        };

        private static ChequeMovement MapMovement(DataRow row) => new()
        {
            Id = Convert.ToInt32(row["Id"]), ChequeId = Convert.ToInt32(row["ChequeId"]),
            MovementDate = Convert.ToDateTime(row["MovementDate"]),
            FromStatus = (ChequeStatus)Convert.ToInt32(row["FromStatus"]),
            ToStatus = (ChequeStatus)Convert.ToInt32(row["ToStatus"]),
            TreasuryId = row["TreasuryId"] == DBNull.Value ? null : Convert.ToInt32(row["TreasuryId"]),
            JournalEntryId = row["JournalEntryId"] == DBNull.Value ? null : Convert.ToInt32(row["JournalEntryId"]),
            Notes = row["Notes"] == DBNull.Value ? null : row["Notes"].ToString(),
            CreatedAt = row["CreatedAt"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(row["CreatedAt"]),
            CreatedBy = row["CreatedBy"] == DBNull.Value ? null : row["CreatedBy"].ToString(),
        };

        public override Cheque GetById(int id, DbConnection conn = null, DbTransaction tx = null) =>
            QueryOne("SELECT * FROM Cheques WHERE Id = @id", conn, tx, ("@id", id));

        public (List<Cheque> Items, int Total) GetPaged(ChequeDirection? direction, ChequeStatus? status, int page, int pageSize, string searchText)
        {
            var where = new WhereBuilder().LikeAny(searchText, "ChequeNo", "BankName");
            if (direction != null) where.Eq("Direction", (int)direction.Value);
            if (status != null) where.Eq("Status", (int)status.Value);

            return Page(where, page, pageSize, OrderBuilder.By("DueDate", true, "ChequeNo", "CreatedAt"));
        }

        // المعلّق = لم يُحصَّل/يُصرَف من البنك بعد — يظهر بالكشف كقيمة استعلامية فقط.
        public List<Cheque> GetOpenForParty(int partyId, DateTime from, DateTime to) =>
            Query(@"SELECT * FROM Cheques WHERE PartyId = @p AND Status NOT IN (@collected, @paid)
                    AND IssueDate >= @from AND IssueDate <= @to ORDER BY DueDate",
                null, null, ("@p", partyId), ("@collected", (int)ChequeStatus.Collected),
                ("@paid", (int)ChequeStatus.Paid), ("@from", from), ("@to", to));

        public List<ChequeMovement> GetMovements(int chequeId) =>
            QueryAs(MapMovement, "SELECT * FROM ChequeMovements WHERE ChequeId = @id ORDER BY Id", null, null, ("@id", chequeId));

        public int Insert(DbConnection conn, DbTransaction tx, Cheque c) =>
            InsertGetId(@"INSERT INTO Cheques (ChequeNo, Direction, PartyKind, PartyId, Amount, IssueDate, DueDate, BankName, Status, TreasuryId, VoucherId, Notes, CreatedBy)
                          VALUES (@no, @dir, @pkind, @party, @amount, @issue, @due, @bank, @status, @treasury, @voucher, @notes, @by)",
                conn, tx, ("@no", c.ChequeNo), ("@dir", (int)c.Direction), ("@pkind", (int)c.PartyKind),
                ("@party", (object)c.PartyId ?? DBNull.Value), ("@amount", c.Amount), ("@issue", c.IssueDate), ("@due", c.DueDate),
                ("@bank", c.BankName ?? ""), ("@status", (int)c.Status), ("@treasury", (object)c.TreasuryId ?? DBNull.Value),
                ("@voucher", (object)c.VoucherId ?? DBNull.Value), ("@notes", c.Notes ?? ""), ("@by", c.CreatedBy));

        public void SetStatus(DbConnection conn, DbTransaction tx, int id, ChequeStatus status, int? treasuryId) =>
            Exec("UPDATE Cheques SET Status = @status, TreasuryId = @treasury, UpdatedAt = @now WHERE Id = @id", conn, tx,
                ("@status", (int)status), ("@treasury", (object)treasuryId ?? DBNull.Value), ("@now", DateTime.Now), ("@id", id));

        public void Update(DbConnection conn, DbTransaction tx, Cheque c) =>
            Exec(@"UPDATE Cheques SET ChequeNo = @no, PartyId = @party, Amount = @amount, IssueDate = @issue,
                          DueDate = @due, BankName = @bank, Notes = @notes, UpdatedAt = @now
                   WHERE Id = @id", conn, tx,
                ("@no", c.ChequeNo), ("@party", (object)c.PartyId ?? DBNull.Value), ("@amount", c.Amount),
                ("@issue", c.IssueDate), ("@due", c.DueDate), ("@bank", c.BankName ?? ""), ("@notes", c.Notes ?? ""),
                ("@now", DateTime.Now), ("@id", c.Id));

        public void Delete(DbConnection conn, DbTransaction tx, int id) =>
            Exec("DELETE FROM Cheques WHERE Id = @id", conn, tx, ("@id", id));

        public void DeleteMovements(DbConnection conn, DbTransaction tx, int chequeId) =>
            Exec("DELETE FROM ChequeMovements WHERE ChequeId = @id", conn, tx, ("@id", chequeId));

        public void InsertMovement(DbConnection conn, DbTransaction tx, ChequeMovement m) =>
            Exec(@"INSERT INTO ChequeMovements (ChequeId, MovementDate, FromStatus, ToStatus, TreasuryId, JournalEntryId, Notes, CreatedBy)
                   VALUES (@cid, @date, @from, @to, @treasury, @je, @notes, @by)",
                conn, tx, ("@cid", m.ChequeId), ("@date", m.MovementDate), ("@from", (int)m.FromStatus), ("@to", (int)m.ToStatus),
                ("@treasury", (object)m.TreasuryId ?? DBNull.Value), ("@je", (object)m.JournalEntryId ?? DBNull.Value),
                ("@notes", m.Notes ?? ""), ("@by", m.CreatedBy));
    }
}
