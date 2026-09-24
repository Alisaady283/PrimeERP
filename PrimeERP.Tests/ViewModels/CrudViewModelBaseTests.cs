using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Application.DTOs.Parties;
using PrimeERP.Application.Services.Parties;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Permissions;
using PrimeERP.UI.Services;
using PrimeERP.UI.ViewModels.Base;
using Xunit;

namespace PrimeERP.Tests.ViewModels
{
    /// <summary>نماذج العرض فوق خدمة حقيقية</summary>
    public class CrudViewModelBaseTests : IDisposable
    {
        private readonly TestDatabaseFixture _db = new();
        private readonly ICustomerService _customers;
        private readonly IPermissionService _permissions;
        private readonly FakeToastService _toast = new();
        private readonly FakeDialogService _dialog = new();

        public CrudViewModelBaseTests()
        {
            AppSession.DevMode = true;
            _customers = _db.Services.GetRequiredService<ICustomerService>();
            _permissions = _db.Services.GetRequiredService<IPermissionService>();
        }

        public void Dispose() => _db.Dispose();

        private TestCustomersViewModel MakeVm() => new(_permissions, _toast, _dialog, _customers);

        [Fact]
        public async Task LoadAsync_PopulatesItemsAndTotalCount_FromRealService()
        {
            _customers.Create(new CreateCustomerDto { Name = "عميل 1" });
            _customers.Create(new CreateCustomerDto { Name = "عميل 2" });

            var vm = MakeVm();
            await vm.LoadAsync();

            Assert.Equal(2, vm.TotalCount);
            Assert.Equal(2, vm.Items.Count);
            Assert.Equal(1, vm.TotalPages);
        }

        [Fact]
        public async Task DeleteCommand_RemovesEntity_AfterConfirmation_ThenReloads()
        {
            var created = _customers.Create(new CreateCustomerDto { Name = "للحذف" });
            Assert.True(created.IsSuccess, created.ErrorMessage);

            var vm = MakeVm();
            await vm.LoadAsync();
            vm.SelectedItem = vm.Items.Single(c => c.Id == created.Value.Id);

            _dialog.ConfirmResult = true;
            Assert.True(vm.DeleteCommand.CanExecute(null));
            await vm.DeleteSelectedForTestAsync();

            var remaining = _customers.GetPaged(1, 20, null);
            Assert.True(remaining.IsSuccess);
            Assert.DoesNotContain(remaining.Value.Items, c => c.Id == created.Value.Id);
            Assert.True(_toast.SuccessCalled);
        }

        [Fact]
        public void DeleteCommand_CanExecute_False_WhenNoSelection()
        {
            var vm = MakeVm();
            Assert.False(vm.DeleteCommand.CanExecute(null));
        }

        private class TestCustomersViewModel : CrudViewModelBase<CustomerDto, CustomerFilter>
        {
            private readonly ICustomerService _service;

            public TestCustomersViewModel(IPermissionService permissions, IToastService toast, IDialogService dialogs, ICustomerService service)
                : base(permissions, toast, dialogs) => _service = service;

            protected override string PermissionPrefix => "Customers";

            protected override Result<PagedResult<CustomerDto>> FetchPage(int page, int pageSize, CustomerFilter filter)
                => _service.GetPaged(page, pageSize, filter);

            protected override int IdOf(CustomerDto item) => item.Id;
            protected override Result DeleteItem(int id) => _service.Delete(id);

            public Task DeleteSelectedForTestAsync() => DeleteSelectedAsync();
        }

        private class FakeToastService : IToastService
        {
            public bool SuccessCalled;
            public string LastError;
            public void Success(string message, int durationMs = 3000) => SuccessCalled = true;
            public void Error(string message) => LastError = message;
            public void Warning(string message, int durationMs = 4000) { }
            public void Info(string message, int durationMs = 3000) { }
        }

        private class FakeDialogService : IDialogService
        {
            public bool ConfirmResult = true;
            public Task<bool> ConfirmAsync(string title, string message, string confirmText = null, bool isDangerous = false)
                => Task.FromResult(ConfirmResult);
            public Task ShowMessageAsync(string title, string message, StatusVariant variant = StatusVariant.Info) => Task.CompletedTask;
            public Task ShowErrorAsync(string title, string message, Exception exception = null) => Task.CompletedTask;
            public Task<TResult> ShowDialogAsync<TResult>(System.Windows.Window dialog) => Task.FromResult(default(TResult));
            public IProgressHandle ShowProgress(string title, string message, bool allowCancel = false) => null;
        }
    }
}
