using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Application.DTOs.Common;
using PrimeERP.Application.Legacy.Common;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Permissions;
using Xunit;

namespace PrimeERP.Tests.Services
{
    /// <summary>الفئات وحساباتها</summary>
    public class CategoryServiceTests : IDisposable
    {
        private readonly TestDatabaseFixture _db = new();
        private readonly ICategoryService _service;

        public CategoryServiceTests()
        {
            AppSession.DevMode = true;
            _service = _db.Services.GetRequiredService<ICategoryService>();
        }

        public void Dispose() => _db.Dispose();

        [Fact]
        public void Create_RootAndSubCategory_BuildsCorrectParentLink()
        {
            var root = _service.Create(new CreateCategoryDto { Name = "أصول ثابتة", ModuleKey = "Products" });
            Assert.True(root.IsSuccess, root.ErrorMessage);

            var sub = _service.Create(new CreateCategoryDto { Name = "سيارات", ParentId = root.Value.Id, ModuleKey = "Products" });
            Assert.True(sub.IsSuccess, sub.ErrorMessage);
            Assert.Equal(root.Value.Id, sub.Value.ParentId);
            Assert.Equal("أصول ثابتة", sub.Value.ParentName);

            var all = _service.GetAll("Products").Value;
            Assert.Equal(2, all.Count);
            Assert.True(all.Single(c => c.Id == root.Value.Id).HasChildren);
        }

        [Fact]
        public void Create_ModuleKeysAreIsolated_SameNameDifferentModules()
        {
            _service.Create(new CreateCategoryDto { Name = "عام", ModuleKey = "Products" });
            _service.Create(new CreateCategoryDto { Name = "عام", ModuleKey = "Assets" });

            Assert.Single(_service.GetAll("Products").Value);
            Assert.Single(_service.GetAll("Assets").Value);
        }

        [Fact]
        public void Create_ParentFromDifferentModule_Fails()
        {
            var productsRoot = _service.Create(new CreateCategoryDto { Name = "منتجات", ModuleKey = "Products" });
            Assert.True(productsRoot.IsSuccess);

            var result = _service.Create(new CreateCategoryDto { Name = "فرعي خاطئ", ParentId = productsRoot.Value.Id, ModuleKey = "Assets" });

            Assert.False(result.IsSuccess);
        }

        [Fact]
        public void Delete_CategoryWithActiveChildren_Fails()
        {
            var root = _service.Create(new CreateCategoryDto { Name = "أب", ModuleKey = "Products" });
            _service.Create(new CreateCategoryDto { Name = "ابن", ParentId = root.Value.Id, ModuleKey = "Products" });

            var result = _service.Delete(root.Value.Id);

            Assert.False(result.IsSuccess);
        }

        [Fact]
        public void Update_TogglesIsActive_Persists()
        {
            var created = _service.Create(new CreateCategoryDto { Name = "قابل للتعطيل", ModuleKey = "Products" });
            Assert.True(created.IsSuccess);

            var updated = _service.Update(new UpdateCategoryDto { Id = created.Value.Id, Name = created.Value.Name, IsActive = false });
            Assert.True(updated.IsSuccess, updated.ErrorMessage);

            Assert.DoesNotContain(_service.GetAll("Products").Value, c => c.Id == created.Value.Id);
            Assert.Contains(_service.GetAll("Products", includeInactive: true).Value, c => c.Id == created.Value.Id && !c.IsActive);
        }
    }
}
