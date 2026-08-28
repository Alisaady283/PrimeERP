using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Localization;
using PrimeERP.Platform.Permissions;
using PrimeERP.UI.Components.Tree;
using PrimeERP.UI.Services;
using PrimeERP.UI.ViewModels.Base;

namespace PrimeERP.UI.ViewModels
{
    // بناء الشجرة نفسه في TreeRenderer (7.Composition، يملك TreeLayoutOptions) لا هنا — راجع TreeRenderer.cs.
    public abstract class TreeViewModelBase<TDto, TFilter> : PagedViewModelBase<TDto, TFilter> where TFilter : new()
    {
        protected readonly IDialogService Dialogs;

        public ObservableCollection<TreeNodeViewModel> RootNodes { get; } = new();

        private TreeNodeViewModel _selectedNode;
        public TreeNodeViewModel SelectedNode { get => _selectedNode; set => SetProperty(ref _selectedNode, value); }

        public ICommand AddCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand ExpandAllCommand { get; }
        public ICommand CollapseAllCommand { get; }

        // الحوار الفعلي يُبنى في 7.Composition (DialogRenderer) — الـVM ترفع حدثاً فقط، لا تعرف عنه شيئاً.
        public event Action AddRequested;
        public event Action<object> EditRequested;

        protected abstract int IdOf(TDto item);
        protected abstract Result DeleteItem(int id);

        protected TreeViewModelBase(IPermissionService permissions, IToastService toast, IDialogService dialogs) : base(permissions, toast)
        {
            Dialogs = dialogs;
            AddCommand = GuardedCommand(() => AddRequested?.Invoke(), $"{PermissionPrefix}.Create");
            EditCommand = new RelayCommand(
                () => { if (SelectedNode?.Data is TDto d) EditRequested?.Invoke(d); },
                () => SelectedNode?.Data is TDto && Can($"{PermissionPrefix}.Edit"));
            DeleteCommand = new RelayCommand(async () => await DeleteSelectedAsync(),
                () => SelectedNode?.Data is TDto && Can($"{PermissionPrefix}.Delete"));
            ExpandAllCommand = new RelayCommand(() => SetAllExpanded(RootNodes, true));
            CollapseAllCommand = new RelayCommand(() => SetAllExpanded(RootNodes, false));
        }

        private async Task DeleteSelectedAsync()
        {
            if (SelectedNode?.Data is not TDto item) return;

            var confirmed = await Dialogs.ConfirmAsync(
                LocalizationService.Get("Str.Delete"), LocalizationService.Get("Str.ConfirmDeleteMessage"), isDangerous: true);
            if (!confirmed) return;

            var result = DeleteItem(IdOf(item));
            if (!result.IsSuccess) { Toast.Error(result.ErrorMessage); return; }

            Toast.Success(LocalizationService.Get("Str.Success"));
            await LoadAsync();
        }

        private static void SetAllExpanded(IEnumerable<TreeNodeViewModel> nodes, bool expanded)
        {
            foreach (var node in nodes)
            {
                node.IsExpanded = expanded;
                SetAllExpanded(node.Children, expanded);
            }
        }
    }
}
