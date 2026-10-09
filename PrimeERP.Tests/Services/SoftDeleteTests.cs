using PrimeERP.Application.PageServices.Builder;
using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Application.DTOs.Parties;
using PrimeERP.Application.PageServices.Parties;
using PrimeERP.Composition.Registry;
using PrimeERP.Data.Repositories;
using PrimeERP.Domain.Entities;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Permissions;
using Xunit;

namespace PrimeERP.Tests.Services
{
    /// <summary>المحذوف منطقياً خارج القراءة</summary>
    public class SoftDeleteTests : IDisposable
    {
        private readonly TestDatabaseFixture _db = new();

        public SoftDeleteTests() => AppSession.DevMode = true;

        public void Dispose() => _db.Dispose();

        private T Service<T>() => _db.Services.GetRequiredService<T>();

        [Fact]
        public void ADeletedCustomer_LeavesEveryRead()
        {
            var customers = Service<ICustomerService>();
            var created = customers.Create(new Customer { Name = "عميل يُحذف" });
            Assert.True(created.IsSuccess, created.ErrorMessage);

            Assert.True(customers.Delete(created.Value.Id).IsSuccess);

            Assert.Equal(ErrorCode.NotFound, customers.GetById(created.Value.Id).ErrorCode);
            Assert.DoesNotContain(customers.GetPaged(1, 100, new CustomerFilter()).Value.Items, c => c.Id == created.Value.Id);
            Assert.DoesNotContain(customers.Search("عميل يُحذف").Value, c => c.Id == created.Value.Id);
            Assert.Null(Service<IPartyRepository<Customer>>().GetByCode(created.Value.Code));
        }

        [Fact]
        public void ADeletedCodedPage_KeepsItsKeyAndIsNotSeededAgain()
        {
            var catalog = Service<IBuilderCatalog>();
            var page = catalog.Modules().First(m => m.Key == "TrialBalance");

            Assert.True(Service<BuilderModulesService>().Delete(page.Id).IsSuccess);

            PrimeERP.Modules.BuilderModuleLoader.RegisterAll(Service<IModuleRegistry>(), _db.Services);

            Assert.DoesNotContain(catalog.Modules(), m => m.Key == "TrialBalance");
            Assert.Contains("TrialBalance", Service<IBuilderRepository>().ModuleKeys());
        }
    }
}
