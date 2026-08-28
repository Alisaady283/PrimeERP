using System.Collections.Generic;
using System.Data.Common;
using PrimeERP.Domain.Entities;

namespace PrimeERP.Data.Repositories
{
    public interface IDepartmentRepository
    {
        void CreateTable();
        Department GetById(int id, DbConnection conn = null, DbTransaction tx = null);
        List<Department> GetAll(bool includeInactive = false);
        int Insert(Department d);
        void Update(Department d);
        void Delete(int id);
    }
}
