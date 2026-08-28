using System.Collections.Generic;
using System.Data.Common;
using PrimeERP.Domain.Entities;

namespace PrimeERP.Data.Repositories
{
    public interface IEmployeeRepository
    {
        void CreateTable();
        Employee GetById(int id, DbConnection conn = null, DbTransaction tx = null);
        List<Employee> Search(string term, int maxResults);
        (List<Employee> Items, int Total) GetPaged(
            int page, int pageSize, string searchText = null, int? departmentId = null,
            string sortColumn = "Name", bool sortDescending = false);

        int Insert(Employee e, DbConnection conn = null, DbTransaction tx = null);
        void Update(Employee e, DbConnection conn = null, DbTransaction tx = null);
        void Delete(int id, string deletedBy, DbConnection conn = null, DbTransaction tx = null);
    }
}
