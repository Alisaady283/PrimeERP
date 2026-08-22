using System.Data.Common;

namespace PrimeERP.Data.Repositories
{
    public record NumberSequenceRow(string Prefix, int NextNumber, int Padding, bool ResetYearly, int? LastYear);

    public interface INumberSequenceRepository
    {
        void CreateTable();
        void EnsureRow(string key);
        void EnsureRow(string key, string prefix);
        void EnsureRow(DbConnection conn, DbTransaction tx, string key);
        NumberSequenceRow GetRow(string key);
        NumberSequenceRow GetRow(DbConnection conn, DbTransaction tx, string key);
        void UpdateNext(DbConnection conn, DbTransaction tx, string key, int nextNumber, int year);
    }
}
