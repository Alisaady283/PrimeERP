using PrimeERP.Application.Legacy.Builder;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Permissions;
using PrimeERP.UI.Services;
using PrimeERP.UI.ViewModels.Base;
using System.Dynamic;

namespace PrimeERP.UI.ViewModels
{
    /// <summary>نموذج عرض أي شاشة صفوفها</summary>
    public class DynamicViewModel : CrudViewModelBase<IDictionary<string, object>, DynamicFilter>
    {
        private readonly IRowService _service;
        private readonly string _permissionPrefix;

        public DynamicViewModel(IRowService service, string permissionPrefix,
            IPermissionService permissions, IToastService toast, IDialogService dialogs)
            : base(permissions, toast, dialogs)
        {
            _service = service;
            _permissionPrefix = permissionPrefix;

            MoveUpCommand = new RelayCommand(_ => Swap(-1), _ => Neighbour(-1) != null);
            MoveDownCommand = new RelayCommand(_ => Swap(1), _ => Neighbour(1) != null);
        }

        public ICommand MoveUpCommand { get; }
        public ICommand MoveDownCommand { get; }

        private IDictionary<string, object> Neighbour(int step)
        {
            if (SelectedItem == null) return null;

            var index = Items.IndexOf(SelectedItem) + step;
            return index >= 0 && index < Items.Count && Items[index].ContainsKey("SortOrder") ? Items[index] : null;
        }

        private async void Swap(int step)
        {
            var other = Neighbour(step);
            if (other == null || !Can($"{_permissionPrefix}.Edit")) return;

            var current = SelectedItem;
            (current["SortOrder"], other["SortOrder"]) = (other["SortOrder"], current["SortOrder"]);

            foreach (var row in new[] { current, other })
            {
                var saved = _service.Update(row);
                if (!saved.IsSuccess) { Toast.Error(saved.ErrorMessage); return; }
            }

            var keep = IdOf(current);
            await LoadAsync();
            SelectedItem = Items.FirstOrDefault(row => IdOf(row) == keep);
        }

        protected override string PermissionPrefix => _permissionPrefix;

        protected override Result<PagedResult<IDictionary<string, object>>> FetchPage(int page, int pageSize, DynamicFilter filter)
        {
            filter ??= new DynamicFilter();
            filter.SearchText = SearchText;

            return _service.GetPaged(page, pageSize, filter);
        }

        protected override int IdOf(IDictionary<string, object> item) =>
            item.TryGetValue("Id", out var raw) && raw != null ? System.Convert.ToInt32(raw) : 0;

        protected override Result DeleteItem(int id) => _service.Delete(id);
    }
}
