using System.Collections.Generic;
using System.Data.Common;
using PrimeERP.Domain.Entities;

namespace PrimeERP.Data.Repositories
{
    public interface IPayrollRepository
    {
        void CreateTable();
        void DeleteDocument(DbConnection conn, DbTransaction tx, int id);
        Payroll GetById(int id, DbConnection conn = null, DbTransaction tx = null);
        List<PayrollLine> GetLines(int payrollId, DbConnection conn = null, DbTransaction tx = null);
        (List<Payroll> Items, int Total) GetPaged(int page, int pageSize, string searchText, string sortColumn, bool sortDescending);
        int InsertHeader(DbConnection conn, DbTransaction tx, Payroll payroll);
        void InsertLine(DbConnection conn, DbTransaction tx, int payrollId, PayrollLine line);
        void SetJournalEntryId(DbConnection conn, DbTransaction tx, int payrollId, int? journalEntryId);
        void SetPosted(DbConnection conn, DbTransaction tx, int payrollId, bool posted);
    }
}
