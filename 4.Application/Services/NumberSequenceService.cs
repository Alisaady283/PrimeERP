using PrimeERP.Data.Core;
using System;
using PrimeERP.Data.Repositories;
using PrimeERP.Platform.Settings;
using PrimeERP.Platform.Permissions;

namespace PrimeERP.Application.Services
{
    /// <summary>أرقام متسلسلة لكل مفتاح</summary>
    public class NumberSequenceService : INumberSequenceService
    {
        private readonly INumberSequenceRepository _repo;

        public NumberSequenceService(INumberSequenceRepository repo) => _repo = repo;

        public string Peek(string key)
        {
            _repo.EnsureRow(key);
            var row = _repo.GetRow(key);
            return Format(row.Prefix, DateTime.Now.Year, row.NextNumber, row.Padding, row.ResetYearly);
        }

        public string Next(string key)
        {
            _repo.EnsureRow(key);
            return DbContextFactory.RunTransaction(db => NextCore(db, key));   // لا يرث ServiceBase: بلا صلاحية ولا تدقيق
        }

        public string Next(PrimeDbContext db, string key)
        {
            _repo.EnsureRow(db, key);
            return NextCore(db, key);
        }

        private string NextCore(PrimeDbContext db, string key)
        {
            var row = _repo.GetRow(db, key);
            var year = DateTime.Now.Year;
            var number = row.ResetYearly && row.LastYear != year ? 1 : row.NextNumber;

            _repo.UpdateNext(db, key, number + 1, year);

            return Format(row.Prefix, year, number, row.Padding, row.ResetYearly);
        }

        private static string Format(string prefix, int year, int number, int padding, bool yearly) =>
            yearly ? $"{prefix}-{year}-{number.ToString("D" + padding)}"
                   : $"{prefix}{number.ToString("D" + padding)}";
    }
}
