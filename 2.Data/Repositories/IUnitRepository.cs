using System.Collections.Generic;
using System.Data.Common;
using PrimeERP.Domain.Entities;

namespace PrimeERP.Data.Repositories
{
    public interface IUnitRepository
    {
        void CreateTable();
        Unit GetById(int id, DbConnection conn = null, DbTransaction tx = null);
        List<Unit> GetAll(bool includeInactive = false);
        int Insert(Unit u);
        void Update(Unit u);
        void Delete(int id);
    }
}
