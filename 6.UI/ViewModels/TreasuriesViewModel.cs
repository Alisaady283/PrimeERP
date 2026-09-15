using System;
using System.Linq;
using PrimeERP.Application.DTOs.Treasury;
using PrimeERP.Application.Services.Treasury;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Permissions;
using PrimeERP.UI.Services;
using PrimeERP.UI.ViewModels.Base;

namespace PrimeERP.UI.ViewModels
{
    public class TreasuriesViewModel : CrudViewModelBase<TreasuryDto, TreasuryFilter>
    {
        private readonly ITreasuryService _treasuries;

        public TreasuriesViewModel(ITreasuryService treasuries, IPermissionService permissions, IToastService toast, IDialogService dialogs)
            : base(permissions, toast, dialogs) => _treasuries = treasuries;

        protected override string PermissionPrefix => "Treasuries";

        protected override Result<PagedResult<TreasuryDto>> FetchPage(int page, int pageSize, TreasuryFilter filter) =>
            AllRows(_treasuries.GetAll(), t => t.Name, t => t.Code);

        protected override int IdOf(TreasuryDto item) => item.Id;
        protected override Result DeleteItem(int id) => _treasuries.Delete(id);
    }
}
