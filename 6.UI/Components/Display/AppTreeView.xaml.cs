using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using PrimeERP.UI.Components.Tree;

namespace PrimeERP.UI.Components.Display
{
    /// <summary>
    /// شجرة مربوطة على TreeNodeViewModel.VisibleChildren — فلترة إخفاء حقيقية عبر TreeFilterEngine،
    /// لا مجرد توسيع لمسارات المطابقة كما كانت النسخة الأولى.
    /// </summary>
    public partial class AppTreeView : UserControl
    {
        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register(nameof(ItemsSource), typeof(IEnumerable<TreeNodeViewModel>), typeof(AppTreeView),
                new PropertyMetadata(null, OnItemsSourceChanged));

        public static readonly DependencyProperty SelectedItemProperty =
            DependencyProperty.Register(nameof(SelectedItem), typeof(TreeNodeViewModel), typeof(AppTreeView),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public static readonly DependencyProperty SearchTextProperty =
            DependencyProperty.Register(nameof(SearchText), typeof(string), typeof(AppTreeView),
                new PropertyMetadata("", OnSearchTextChanged));

        public IEnumerable<TreeNodeViewModel> ItemsSource
        {
            get => (IEnumerable<TreeNodeViewModel>)GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        public TreeNodeViewModel SelectedItem
        {
            get => (TreeNodeViewModel)GetValue(SelectedItemProperty);
            set => SetValue(SelectedItemProperty, value);
        }

        public string SearchText
        {
            get => (string)GetValue(SearchTextProperty);
            set => SetValue(SearchTextProperty, value);
        }

        /// <summary>فلتر إضافي (مثل LeafOnly أو TypeFilter عند AccountPicker) — يُعاد تطبيقه مع كل بحث. استدعِ RefreshFilter() بعد تغييره برمجياً.</summary>
        public Func<TreeNodeViewModel, bool> ExtraFilter { get; set; }

        private List<TreeNodeViewModel> _roots = new();

        public AppTreeView()
        {
            InitializeComponent();
        }

        private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var c = (AppTreeView)d;
            c._roots = e.NewValue is IEnumerable<TreeNodeViewModel> src
                ? new List<TreeNodeViewModel>(src)
                : new List<TreeNodeViewModel>();
            c.RefreshFilter();
        }

        private static void OnSearchTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
            ((AppTreeView)d).RefreshFilter();

        /// <summary>يعيد تطبيق TreeFilterEngine بحالة SearchText/ExtraFilter الحالية — استدعِه بعد تغيير ExtraFilter برمجياً.</summary>
        public void RefreshFilter()
        {
            TreeFilterEngine.Apply(_roots, SearchText, ExtraFilter);
            tree.ItemsSource = _roots.FindAll(r => r.IsVisible);
        }

        public void ExpandAll() => SetAllExpanded(_roots, true);

        public void CollapseAll() => SetAllExpanded(_roots, false);

        private static void SetAllExpanded(IEnumerable<TreeNodeViewModel> nodes, bool expanded)
        {
            foreach (var node in nodes)
            {
                node.IsExpanded = expanded;
                SetAllExpanded(node.Children, expanded);
            }
        }

        private void tree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is TreeNodeViewModel node && !node.IsSelectable)
            {
                node.IsSelected = false; // يمنع تحديد عقدة هيكلية غير قابلة للاختيار
                return;
            }

            SelectedItem = e.NewValue as TreeNodeViewModel;
        }
    }
}
