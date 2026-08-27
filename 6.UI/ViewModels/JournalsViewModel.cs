using PrimeERP.Application.DTOs.Accounting;
using PrimeERP.Application.Services.Accounting;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Permissions;
using PrimeERP.UI.Services;
using PrimeERP.UI.ViewModels.Base;

namespace PrimeERP.UI.ViewModels
{
    // أول مستهلك حقيقي لـDocumentDialogDefinition/DocumentRenderer — نفس عقد CrudViewModelBase تماماً
    // (Post/Unpost/ميزان المراجعة تُستكمَل لاحقاً، خارج نطاق "ابدأ قيود اليومية").
    public class JournalsViewModel : CrudViewModelBase<JournalEntryDto, JournalFilter>
    {
        private readonly IJournalService _journal;

        public JournalsViewModel(IJournalService journal, IPermissionService permissions,
                                  IToastService toast, IDialogService dialogs)
            : base(permissions, toast, dialogs) => _journal = journal;

        protected override string PermissionPrefix => "Journal";

        protected override Result<PagedResult<JournalEntryDto>> FetchPage(int page, int pageSize, JournalFilter filter)
        {
            var f = filter ?? new JournalFilter();
            f.SearchText = SearchText;
            return _journal.GetPaged(page, pageSize, f);
        }

        protected override int IdOf(JournalEntryDto item) => item.Id;

        protected override Result DeleteItem(int id) => _journal.Delete(id);
    }
}
