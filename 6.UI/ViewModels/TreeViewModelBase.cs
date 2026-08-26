using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Input;
using PrimeERP.Platform.Permissions;
using PrimeERP.UI.Components.Tree;
using PrimeERP.UI.Services;
using PrimeERP.UI.ViewModels.Base;

namespace PrimeERP.UI.ViewModels
{
    /// <summary>
    /// يرث PagedViewModelBase (نفس عقد Fetch — لا تعميم Create/Update، راجع مبدأ استخراج القطعة). يضيف حالة
    /// عرض الشجرة فقط (عقد جذور/تحديد/توسيع) — لا منطق تحويل List&lt;TDto&gt; إلى شجرة هنا عمداً: ذلك المنطق
    /// يحتاج TreeLayoutOptions (7.Composition، طبقة أعلى من 6.UI — لا يمكن لهذه الطبقة الاعتماد عليها، راجع
    /// قيد حدود الطبقات في check.sh). TreeRenderer (7.Composition، يملك التعريف كاملاً) يبني الشجرة فعلياً بعد
    /// LoadAsync ويملأ RootNodes مباشرة — بالضبط ما يجعل AccountsViewModel بلا أي منطق شجرة إطلاقاً (صفر سطر
    /// إضافي عن CustomersViewModel، فقط قاعدة مختلفة).
    /// </summary>
    public abstract class TreeViewModelBase<TDto, TFilter> : PagedViewModelBase<TDto, TFilter>
    {
        public ObservableCollection<TreeNodeViewModel> RootNodes { get; } = new();

        private TreeNodeViewModel _selectedNode;
        public TreeNodeViewModel SelectedNode { get => _selectedNode; set => SetProperty(ref _selectedNode, value); }

        public ICommand ExpandAllCommand { get; }
        public ICommand CollapseAllCommand { get; }

        protected TreeViewModelBase(IPermissionService permissions, IToastService toast) : base(permissions, toast)
        {
            ExpandAllCommand = new RelayCommand(() => SetAllExpanded(RootNodes, true));
            CollapseAllCommand = new RelayCommand(() => SetAllExpanded(RootNodes, false));
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
