using PrimeERP.Data.Core;
using System;
using PrimeERP.Data.Repositories;
using PrimeERP.Platform.Settings;
using PrimeERP.Platform.Permissions;

namespace PrimeERP.Application.Services.Core
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
            return $"{row.Prefix}{row.NextNumber}";
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
            _repo.UpdateNext(db, key, row.NextNumber + 1, DateTime.Now.Year);
            return $"{row.Prefix}{row.NextNumber}";
        }
    }
}
