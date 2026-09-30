using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Application.Legacy.Common;
using PrimeERP.Domain.Entities;
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
            var root = _service.Create(new Category { Name = "أصول ثابتة", ModuleKey = "Products" });
            Assert.True(root.IsSuccess, root.ErrorMessage);

            var sub = _service.Create(new Category { Name = "سيارات", ParentId = root.Value.Id, ModuleKey = "Products" });
            Assert.True(sub.IsSuccess, sub.ErrorMessage);
            Assert.Equal(root.Value.Id, sub.Value.ParentId);

            var all = _service.GetAll("Products").Value;
            Assert.Equal(2, all.Count);
            Assert.Equal("أصول ثابتة", all.Single(c => c.Id == sub.Value.Id).ParentName);
        }

        [Fact]
        public void Create_ModuleKeysAreIsolated_SameNameDifferentModules()
        {
            _service.Create(new Category { Name = "عام", ModuleKey = "Products" });
            _service.Create(new Category { Name = "عام", ModuleKey = "Assets" });

            Assert.Single(_service.GetAll("Products").Value);
            Assert.Single(_service.GetAll("Assets").Value);
        }

        [Fact]
        public void Create_ParentFromDifferentModule_Fails()
        {
            var productsRoot = _service.Create(new Category { Name = "منتجات", ModuleKey = "Products" });
            Assert.True(productsRoot.IsSuccess);

            var result = _service.Create(new Category { Name = "فرعي خاطئ", ParentId = productsRoot.Value.Id, ModuleKey = "Assets" });

            Assert.False(result.IsSuccess);
        }

        [Fact]
        public void Delete_CategoryWithActiveChildren_Fails()
        {
            var root = _service.Create(new Category { Name = "أب", ModuleKey = "Products" });
            _service.Create(new Category { Name = "ابن", ParentId = root.Value.Id, ModuleKey = "Products" });

            var result = _service.Delete(root.Value.Id);

            Assert.False(result.IsSuccess);
        }

        [Fact]
        public void Update_TogglesIsActive_Persists()
        {
            var created = _service.Create(new Category { Name = "قابل للتعطيل", ModuleKey = "Products" });
            Assert.True(created.IsSuccess);

            var loaded = _service.GetById(created.Value.Id).Value;
            loaded.IsActive = false;
            var updated = _service.Update(loaded);
            Assert.True(updated.IsSuccess, updated.ErrorMessage);

            Assert.DoesNotContain(_service.GetAll("Products").Value, c => c.Id == created.Value.Id);
            Assert.Contains(_service.GetAll("Products", includeInactive: true).Value, c => c.Id == created.Value.Id && !c.IsActive);
        }
    }
}
