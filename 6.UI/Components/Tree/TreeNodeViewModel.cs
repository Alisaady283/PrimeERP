using System.Collections.ObjectModel;
using PrimeERP.UI.ViewModels;

namespace PrimeERP.UI.Components.Tree
{
    /// <summary>
    /// عقدة شجرة قابلة للمراقبة — تحمل الكائن الأصلي (Data) بجانب حقول العرض،
    /// وتفصل بين كل الأبناء (Children) والأبناء الظاهرين بعد الفلترة (VisibleChildren).
    /// </summary>
    public class TreeNodeViewModel : BaseViewModel
    {
        public string Id          { get; set; }
        public string Code        { get; set; }
        public string Name        { get; set; }
        public string DisplayText { get; set; }
        public bool   IsLeaf      { get; set; }

        /// <summary>الكائن الأصلي (Account/Customer/...) — يُستخدم عند الاختيار الفعلي.</summary>
        public object Data { get; set; }

        public TreeNodeViewModel Parent { get; private set; }

        public ObservableCollection<TreeNodeViewModel> Children { get; } = new();

        /// <summary>الأبناء بعد الفلترة — AppTreeView يربط على هذه لا على Children مباشرة.</summary>
        public ObservableCollection<TreeNodeViewModel> VisibleChildren { get; } = new();

        private bool _isExpanded;
        public bool IsExpanded
        {
            get => _isExpanded;
            set => SetProperty(ref _isExpanded, value);
        }

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        /// <summary>true لو هذه العقدة نفسها (لا أبناؤها) طابقت نص البحث الحالي — تُستخدم للتمييز اللوني.</summary>
        private bool _isMatch;
        public bool IsMatch
        {
            get => _isMatch;
            set => SetProperty(ref _isMatch, value);
        }

        /// <summary>هل تظهر هذه العقدة أصلاً ضمن VisibleChildren لأبيها (يحسبها TreeFilterEngine).</summary>
        private bool _isVisible = true;
        public bool IsVisible
        {
            get => _isVisible;
            set => SetProperty(ref _isVisible, value);
        }

        /// <summary>false يعني عقدة هيكلية فقط (مثل فرع غير قابل للاختيار في AccountPicker مع LeafOnly) — تظهر رمادية وغير قابلة للنقر.</summary>
        private bool _isSelectable = true;
        public bool IsSelectable
        {
            get => _isSelectable;
            set => SetProperty(ref _isSelectable, value);
        }

        public void AddChild(TreeNodeViewModel child)
        {
            child.Parent = this;
            Children.Add(child);
            VisibleChildren.Add(child);
        }
    }
}
