using PrimeERP.Domain.Rules;
using PrimeERP.Domain.Contracts;
using PrimeERP.Data.Repositories;
using PrimeERP.Domain.Entities;

namespace PrimeERP.Application.Validation
{
    /// <summary>تحقّق الفئة</summary>
    public class CategoryValidator : IValidator<Category>
    {
        private readonly ICategoryRepository _repo;

        public CategoryValidator(ICategoryRepository repo) => _repo = repo;

        public ValidationResult Validate(Category category) =>
            Rules.For<Category>()
                .Required(x => x.Name, "اسم التصنيف")
                .MaxLength(x => x.Name, 200, "اسم التصنيف")
                .Custom((c, r) =>
                {
                    if (c.ParentId == null) return;
                    if (c.ParentId == c.Id) { r.AddError("ParentId", "لا يمكن أن يكون التصنيف أباً لنفسه"); return; }

                    var parent = _repo.GetById(c.ParentId.Value);
                    if (parent == null) { r.AddError("ParentId", "التصنيف الأب غير موجود"); return; }
                    if (parent.ModuleKey != c.ModuleKey) r.AddError("ParentId", "لا يمكن ربط تصنيف بأب من وحدة مختلفة");
                })
                .Validate(category);
    }
}
