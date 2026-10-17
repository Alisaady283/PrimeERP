using PrimeERP.Api.Infrastructure;
using PrimeERP.Application.PageServices.Common;
using PrimeERP.Domain.Entities;

namespace PrimeERP.Api.Features.Categories
{
    public sealed record CategoryOption(int Id, string Name, int? ParentId, string ParentName, bool IsActive, string Notes);

    public sealed record CategoryInput(string Name, int? ParentId, bool IsActive, string Notes);

    /// <summary>فئات كل صفحة (الفئات · العلامات · أنواع الأصول) بخدمة الفئات وصلاحياتها — الحساب والصفحة يملكهما النظام</summary>
    public static class CategoryEndpoints
    {
        public static RouteGroupBuilder MapCategories(this RouteGroupBuilder api)
        {
            var categories = api.MapGroup("/categories/{module}").WithTags("Categories").RequireAuthorization();

            categories.MapGet("/", (string module, ICategoryService service) =>
                service.GetAll(module, includeInactive: true).ToHttp(list => list.Select(Option).ToList()));

            categories.MapPost("/", (string module, CategoryInput input, ICategoryService service) =>
                service.Create(new Category { ModuleKey = module, Name = input.Name, ParentId = input.ParentId, IsActive = input.IsActive, Notes = input.Notes })
                    .ToHttp(Option));

            categories.MapPut("/{id:int}", (string module, int id, CategoryInput input, ICategoryService service) =>
            {
                var current = service.GetById(id);
                if (!current.IsSuccess) return ResultHttp.Problem(current);
                var category = current.Value;
                if (!string.Equals(category.ModuleKey, module, StringComparison.OrdinalIgnoreCase)) return Results.NotFound();
                category.Name = input.Name; category.IsActive = input.IsActive; category.Notes = input.Notes;
                return service.Update(category).ToHttp();
            });

            categories.MapDelete("/{id:int}", (string module, int id, ICategoryService service) =>
            {
                var current = service.GetById(id);
                if (!current.IsSuccess) return ResultHttp.Problem(current);
                return !string.Equals(current.Value.ModuleKey, module, StringComparison.OrdinalIgnoreCase) ? Results.NotFound() : service.Delete(id).ToHttp();
            });

            return api;
        }

        private static CategoryOption Option(Category c) => new(c.Id, c.Name, c.ParentId, c.ParentName, c.IsActive, c.Notes);
    }
}
