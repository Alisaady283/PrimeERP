using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Application.DTOs.Parties;
using PrimeERP.Application.Services.Parties;
using PrimeERP.Platform.Permissions;
using PrimeERP.UI.ViewModels;
using Xunit;

namespace PrimeERP.Tests.ViewModels
{
    /// <summary>يثبت أن CustomersViewModel — أول مستهلك حقيقي لـCrudViewModelBase — يُبنى فعلياً عبر حاوية
    /// DI الحقيقية (لا new يدوي) ويعمل فوق ICustomerService حقيقية. يُثبت شرط إغلاق R7 (Gallery/الأساس يعمل
    /// من طرف لطرف قبل اعتبار البند مكتملاً).</summary>
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
            _customers.Create(new CreateCustomerDto { Name = "عميل DI" });

            var vm = _db.Services.GetRequiredService<CustomersViewModel>();
            await vm.LoadAsync();

            Assert.Single(vm.Items);
            Assert.Equal("عميل DI", vm.Items[0].Name);
        }
    }
}
