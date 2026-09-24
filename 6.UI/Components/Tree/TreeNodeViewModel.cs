using System.Collections.ObjectModel;
using PrimeERP.UI.ViewModels;

namespace PrimeERP.UI.Components.Tree
{
    /// <summary>عقدة شجرة قابلة للمراقبة</summary>
    public class TreeNodeViewModel : BaseViewModel
    {
        public string Id          { get; set; }
        public string Code        { get; set; }
        public string Name        { get; set; }
        public string DisplayText { get; set; }
        public bool   IsLeaf      { get; set; }

        public object Data { get; set; }

        public TreeNodeViewModel Parent { get; private set; }

        public ObservableCollection<TreeNodeViewModel> Children { get; } = new();

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

        private bool _isMatch;
        public bool IsMatch
        {
            get => _isMatch;
            set => SetProperty(ref _isMatch, value);
        }

        private bool _isVisible = true;
        public bool IsVisible
        {
            get => _isVisible;
            set => SetProperty(ref _isVisible, value);
        }

        private bool _isSelectable = true;
        public bool IsSelectable
        {
            get => _isSelectable;
            set => SetProperty(ref _isSelectable, value);
        }

        private NodeCheckState _checkState = NodeCheckState.Unchecked;
        public NodeCheckState CheckState
        {
            get => _checkState;
            set => SetProperty(ref _checkState, value);
        }

        public bool IsCheckable { get; set; } = true;

        private bool _isCheckEnabled = true;
        public bool IsCheckEnabled
        {
            get => _isCheckEnabled;
            set { if (_isCheckEnabled == value) return; _isCheckEnabled = value; OnPropertyChanged(); }
        }

        public string InheritedHint { get; set; }

        private bool _inheritedAllowed;
        public bool InheritedAllowed
        {
            get => _inheritedAllowed;
            set { if (_inheritedAllowed == value) return; _inheritedAllowed = value; OnPropertyChanged(); }
        }

        public void AddChild(TreeNodeViewModel child)
        {
            child.Parent = this;
            Children.Add(child);
            VisibleChildren.Add(child);
        }
    }
}
