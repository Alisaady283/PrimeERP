using System;
using System.Threading.Tasks;
using System.Windows.Input;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Localization;
using PrimeERP.Platform.Permissions;
using PrimeERP.UI.Services;

namespace PrimeERP.UI.ViewModels.Base
{
    /// <summary>
    /// يضيف على PagedViewModelBase حذف عام (Delete(int)→Result موحّد فعلاً عبر كل الخدمات) فوق SelectedItem
    /// الموروثة، بنفس نمط أحداث AddRequested/EditRequested في TreeViewModelBase — الصفحة (Renderer) هي من
    /// تشترك فيهما وتفتح الحوار المناسب (Dialog أو DocumentDialog) عبر DialogDefinition/DocumentDialogDefinition
    /// المُعرَّفة في ModuleDefinition، لا الـVM نفسها (كانت الحاجة القديمة لتنفيذ AddNew/EditSelected داخل كل
    /// VM قبل وجود نظام Composition الحالي؛ الآن الشكل مختلف بين الكيانات محلول عبر Type properties إعلانية،
    /// لا حاجة لكود VM مخصّص بعد الآن).
    /// </summary>
    public abstract class CrudViewModelBase<TDto, TFilter> : PagedViewModelBase<TDto, TFilter> where TFilter : new()
    {
        protected readonly IDialogService Dialogs;

        public ICommand AddCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand DeleteCommand { get; }

        public event Action AddRequested;
        public event Action<object> EditRequested;

        protected CrudViewModelBase(IPermissionService permissions, IToastService toast, IDialogService dialogs)
            : base(permissions, toast)
        {
            Dialogs = dialogs;
            AddCommand = GuardedCommand(() => AddRequested?.Invoke(), () => $"{PermissionPrefix}.Create");
            EditCommand = new RelayCommand(
                () => { if (SelectedItem != null) EditRequested?.Invoke(SelectedItem); },
                () => SelectedItem != null && Can($"{PermissionPrefix}.Edit"));
            DeleteCommand = new RelayCommand(
                async () => await DeleteSelectedAsync(),
                () => SelectedItem != null && Can($"{PermissionPrefix}.Delete"));
        }

        protected abstract int IdOf(TDto item);
        protected abstract Result DeleteItem(int id);

        protected async Task DeleteSelectedAsync()
        {
            if (SelectedItem == null) return;

            var confirmed = await Dialogs.ConfirmAsync(
                LocalizationService.Get("Str.Delete"),
                LocalizationService.Get("Str.ConfirmDeleteMessage"),
                isDangerous: true);
            if (!confirmed) return;

            var result = DeleteItem(IdOf(SelectedItem));
            if (!result.IsSuccess)
            {
                Toast.Error(result.ErrorMessage);
                return;
            }

            Toast.Success(LocalizationService.Get("Str.Success"));
            await LoadAsync();
        }
    }
}
