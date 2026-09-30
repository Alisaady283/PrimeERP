using PrimeERP.Application.DTOs.Common;
using PrimeERP.Domain.Results;

namespace PrimeERP.Application.Legacy.Common
{
    /// <summary>عقد خدمة الفئات</summary>
    public interface ICategoryService
    {
        Result<System.Collections.Generic.List<CategoryDto>> GetAll(string moduleKey, bool includeInactive = false);
        Result<CategoryDto> GetById(int id);
        Result<CategoryDto> Create(CreateCategoryDto dto);
        Result Update(UpdateCategoryDto dto);
        Result Delete(int id);
    }
}
