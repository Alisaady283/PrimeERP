using System.Collections.Generic;
using PrimeERP.Domain.Entities;
using PrimeERP.Domain.Results;

namespace PrimeERP.Application.PageServices.Common
{
    /// <summary>عقد خدمة الفئات</summary>
    public interface ICategoryService
    {
        Result<List<Category>> GetAll(string moduleKey, bool includeInactive = false);
        Result<Category> GetById(int id);
        Result<Category> Create(Category category);
        Result Update(Category category);
        Result Delete(int id);
    }
}
