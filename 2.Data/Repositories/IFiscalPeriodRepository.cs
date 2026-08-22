using System;
using System.Collections.Generic;
using System.Data.Common;
using PrimeERP.Domain.Entities;

namespace PrimeERP.Data.Repositories
{
    public interface IFiscalPeriodRepository
    {
        void CreateTable();

        List<FiscalYear> GetAllYears();
        FiscalYear GetYearById(int id);
        FiscalYear GetCurrentYear();
        FiscalYear GetYearContaining(string date);
        bool AnyYearOverlapping(string start, string end, int? excludeId);
        int InsertYear(DbConnection conn, DbTransaction tx, FiscalYear year);
        void UpdateYear(DbConnection conn, DbTransaction tx, FiscalYear year);
        void SetYearClosed(DbConnection conn, DbTransaction tx, int id, DateTime closedAt, string closedBy, int? closingEntryId);
        void SetYearReopened(DbConnection conn, DbTransaction tx, int id);
        void SetCurrentYear(DbConnection conn, DbTransaction tx, int id);

        List<FiscalPeriod> GetPeriods(int yearId);
        FiscalPeriod GetPeriodById(int id);
        FiscalPeriod GetPeriodContaining(string date);
        void InsertPeriod(DbConnection conn, DbTransaction tx, FiscalPeriod period);
        void UpdatePeriod(DbConnection conn, DbTransaction tx, FiscalPeriod period);
        void SetPeriodClosed(DbConnection conn, DbTransaction tx, int id, DateTime closedAt, string closedBy);
        void SetPeriodReopened(DbConnection conn, DbTransaction tx, int id);
    }
}
