using System;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using PrimeERP.Data.Repositories.Base;
using PrimeERP.Data.Core;
using PrimeERP.Domain.Entities;

namespace PrimeERP.Data.Repositories
{
    /// <summary>مستودع NumberSequenceRow</summary>
    public record NumberSequenceRow(string Prefix, int NextNumber, int Padding, bool ResetYearly, int? LastYear);

    public interface INumberSequenceRepository
    {
        void EnsureRow(string key);
        void EnsureRow(string key, string prefix);
        void EnsureRow(string key, string prefix, int padding, bool resetYearly);
        void EnsureRow(PrimeDbContext db, string key);
        NumberSequenceRow GetRow(string key);
        NumberSequenceRow GetRow(PrimeDbContext db, string key);
        void UpdateNext(PrimeDbContext db, string key, int nextNumber, int year);
    }

    /// <summary>طبقة وصول بيانات تسلسل الأرقام</summary>
    public class NumberSequenceRepository : RepositoryBase<NumberSequence>, INumberSequenceRepository
    {
        protected override string TableName => "NumberSequences";


        public void EnsureRow(string key) => EnsureRow(key, key);

        public void EnsureRow(string key, string prefix) => EnsureRowCore(null, key, prefix);

        public void EnsureRow(string key, string prefix, int padding, bool resetYearly) =>
            EnsureRowCore(null, key, prefix, padding, resetYearly);

        public void EnsureRow(PrimeDbContext db, string key) => EnsureRowCore(db, key, key);

        private void EnsureRowCore(PrimeDbContext db, string key, string prefix,
            int padding = 5, bool resetYearly = true) =>
            Write(db =>
            {
                var existing = SetOf(db).FirstOrDefault(s => s.Key == key);
                if (existing != null)
                {
                    if (existing.Prefix == existing.Key && prefix != key) existing.Prefix = prefix;
                    return 0;
                }
                SetOf(db).Add(new NumberSequence
                {
                    Key = key, Prefix = prefix, NextNumber = 1, Padding = padding, ResetYearly = resetYearly
                });
                return 0;
            }, db);

        public NumberSequenceRow GetRow(string key) => GetRow(null, key);

        public NumberSequenceRow GetRow(PrimeDbContext db, string key) =>
            Fetch(q => q.Where(s => s.Key == key), db)
                .Select(s => new NumberSequenceRow(s.Prefix ?? s.Key, s.NextNumber, s.Padding, s.ResetYearly, s.LastYear))
                .FirstOrDefault();

        public void UpdateNext(PrimeDbContext db, string key, int nextNumber, int year) =>
            Edit(s => s.Key == key, row =>
            {
                row.NextNumber = nextNumber;
                row.LastYear = year;
            }, db);
    }
}
