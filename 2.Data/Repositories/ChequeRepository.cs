using System;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Collections.Generic;
using PrimeERP.Data.Core;
using PrimeERP.Data.Repositories.Base;
using PrimeERP.Domain.Entities;
using PrimeERP.Domain.Enums;

namespace PrimeERP.Data.Repositories
{
    /// <summary>مستودع Cheque</summary>
    public interface IChequeRepository
    {
        Cheque GetById(int id, PrimeDbContext db = null);
        (List<Cheque> Items, int Total) GetPaged(ChequeDirection? direction, ChequeStatus? status, int page, int pageSize, string searchText);
        List<ChequeMovement> GetMovements(int chequeId);
        List<Cheque> GetOpenForParty(int partyId, DateTime from, DateTime to);
        int Insert(PrimeDbContext db, Cheque c);
        void SetStatus(PrimeDbContext db, int id, ChequeStatus status, int? treasuryId);
        void Update(PrimeDbContext db, Cheque c);
        void Delete(PrimeDbContext db, int id);
        void DeleteMovements(PrimeDbContext db, int chequeId);
        void InsertMovement(PrimeDbContext db, ChequeMovement m);
    }

    public class ChequeRepository : RepositoryBase<Cheque>, IChequeRepository
    {
        protected override string TableName => "Cheques";


        private const string Movements = "ChequeMovements";


        public (List<Cheque> Items, int Total) GetPaged(ChequeDirection? direction, ChequeStatus? status,
                                                        int page, int pageSize, string searchText)
        {
            IQueryable<Cheque> Shape(IQueryable<Cheque> rows)
            {
                var q = rows;
                if (!string.IsNullOrWhiteSpace(searchText))
                    q = q.Where(c => EF.Functions.Like(c.ChequeNo, $"%{searchText}%")
                                  || EF.Functions.Like(c.BankName, $"%{searchText}%"));
                if (direction != null) q = q.Where(c => c.Direction == direction);
                if (status != null) q = q.Where(c => c.Status == status);
                return q;
            }

            return Page(page, pageSize, Shape, DocumentOrder(c => c.DueDate, true, c => c.ChequeNo));
        }

        public List<Cheque> GetOpenForParty(int partyId, DateTime from, DateTime to) =>
            Fetch(q => q.Where(c => c.PartyId == partyId
                                 && c.Status != ChequeStatus.Collected && c.Status != ChequeStatus.Paid
                                 && c.IssueDate >= from && c.IssueDate <= to)
                        .OrderBy(c => c.DueDate));

        public List<ChequeMovement> GetMovements(int chequeId) =>
            FetchOf<ChequeMovement>(Movements, q => q.Where(m => m.ChequeId == chequeId).OrderBy(m => m.Id));

        public int Insert(PrimeDbContext db, Cheque c) => Add(c, db);

        public void SetStatus(PrimeDbContext db, int id, ChequeStatus status, int? treasuryId) =>
            Edit(c => c.Id == id, row =>
            {
                row.Status = status;
                row.TreasuryId = treasuryId;
            }, db);

        public void Update(PrimeDbContext db, Cheque c) =>
            Edit(x => x.Id == c.Id, row =>
            {
                row.ChequeNo = c.ChequeNo;
                row.PartyId = c.PartyId;
                row.Amount = c.Amount;
                row.IssueDate = c.IssueDate;
                row.DueDate = c.DueDate;
                row.BankName = c.BankName ?? "";
                row.Notes = c.Notes ?? "";
            }, db);

        public void Delete(PrimeDbContext db, int id) =>
            Write(db =>
            {
                var row = Rows(db).AsTracking().FirstOrDefault(c => c.Id == id);
                if (row != null) SetOf(db).Remove(row);
                return 0;
            }, db);

        public void DeleteMovements(PrimeDbContext db, int chequeId) =>
            Write(db =>
            {
                SetOf<ChequeMovement>(db, Movements)
                    .RemoveRange(RowsOf<ChequeMovement>(db, Movements).Where(m => m.ChequeId == chequeId));
                return 0;
            }, db);

        public void InsertMovement(PrimeDbContext db, ChequeMovement m) =>
            Write(db =>
            {
                SetOf<ChequeMovement>(db, Movements).Add(m);
                return 0;
            }, db);
    }
}
