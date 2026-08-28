using System.Collections.Generic;
using System.Data.Common;
using PrimeERP.Domain.Entities;

namespace PrimeERP.Data.Repositories
{
    public interface IWarehouseRepository
    {
        void CreateTable();
        Warehouse GetById(int id, DbConnection conn = null, DbTransaction tx = null);
        List<Warehouse> GetAll(bool includeInactive = false);
        int Insert(Warehouse w);
        void Update(Warehouse w);
        void Delete(int id);
    }
}
