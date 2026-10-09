using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Application.DTOs.Parties;
using PrimeERP.Application.PageServices.Parties;
using PrimeERP.Domain.Entities;
using PrimeERP.Platform.Permissions;
using PrimeERP.UI.ViewModels;
using Xunit;

namespace PrimeERP.Tests.ViewModels
{
    /// <summary>يثبت أن CustomersViewModel</summary>
    public class CustomersViewModelTests : IDisposable
    {
        private readonly TestDatabaseFixture _db = new();
        private readonly ICustomerService _customers;

        public CustomersViewModelTests()
        {
            AppSession.DevMode = true;
            _customers = _db.Services.GetRequiredService<ICustomerService>();
        }

        public void Dispose() => _db.Dispose();

        [Fact]
        public async Task ResolvedFromContainer_LoadsRealCustomers()
        {
            _customers.Create(new Customer { Name = "عميل DI" });

            var vm = _db.Services.GetRequiredService<CustomersViewModel>();
            await vm.LoadAsync();

            Assert.Single(vm.Items);
            Assert.Equal("عميل DI", vm.Items[0].Name);
        }
    }
}
