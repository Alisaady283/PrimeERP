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
    /// الموروثة.
    /// Create/Update عمداً غير معمَّمين — نفس قرار CrudServiceBase: DTOs الإنشاء/التعديل تختلف شكلاً بين
    /// كيان وآخر لدرجة أن قالباً عاماً يخفي المنطق بدل أن يلخّصه (مثال حقيقي: JournalService.Update يأخذ
    /// CreateJournalDto لا UpdateJournalDto مستقلة). كل ViewModel فعلي ينفّذ AddNew/EditSelected بفتح حواره
    /// الخاص واستدعاء خدمته الخاصة، تماماً كما تفعل كل خدمة مع Create/Update الخاصين بها.
    /// </summary>
    public abstract class CrudViewModelBase<TDto, TFilter> : PagedViewModelBase<TDto, TFilter>
    {
        protected readonly IDialogService Dialogs;

        public ICommand AddCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand DeleteCommand { get; }

        protected CrudViewModelBase(IPermissionService permissions, IToastService toast, IDialogService dialogs)
            : base(permissions, toast)
        {
            Dialogs = dialogs;
            AddCommand = GuardedCommand(AddNew, $"{PermissionPrefix}.Add");
            EditCommand = GuardedCommand(EditSelected, $"{PermissionPrefix}.Edit");
            DeleteCommand = new RelayCommand(
                async () => await DeleteSelectedAsync(),
                () => SelectedItem != null && Can($"{PermissionPrefix}.Delete"));
        }

        protected abstract void AddNew();
        protected abstract void EditSelected();
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
