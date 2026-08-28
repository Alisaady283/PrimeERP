using System.Collections.Generic;
using System.Data.Common;
using PrimeERP.Domain.Entities;

namespace PrimeERP.Data.Repositories
{
    public interface IJobTitleRepository
    {
        void CreateTable();
        JobTitle GetById(int id, DbConnection conn = null, DbTransaction tx = null);
        List<JobTitle> GetAll(bool includeInactive = false);
        int Insert(JobTitle j);
        void Update(JobTitle j);
        void Delete(int id);
    }
}
