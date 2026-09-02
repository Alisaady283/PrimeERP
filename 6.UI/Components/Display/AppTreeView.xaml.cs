using System;
using System.Collections.Generic;
using System.Collections.Specialized;
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

        public static readonly DependencyProperty CheckModeProperty =
            DependencyProperty.Register(nameof(CheckMode), typeof(TreeCheckMode), typeof(AppTreeView),
                new PropertyMetadata(TreeCheckMode.None));

        public TreeCheckMode CheckMode
        {
            get => (TreeCheckMode)GetValue(CheckModeProperty);
            set => SetValue(CheckModeProperty, value);
        }

        /// <summary>يُطلق بعد كل تغيّر حالة تأشير (نقر المستخدم أو انتشار من عقدة أب).</summary>
        public event EventHandler<TreeNodeViewModel> CheckStateChanged;

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

        /// <summary>⚠️ ItemsSource قد تُملأ لاحقاً بشكل غير متزامن (TreeRenderer يربط قبل اكتمال LoadAsync ثم
        /// يملأ نفس نسخة RootNodes لاحقاً) — نسخة لقطة واحدة وقت الربط فقط (كما كانت) لا تلتقط ذلك أبداً لأن
        /// قيمة الخاصية نفسها (مرجع المجموعة) لا يتغيّر، فقط محتواها؛ WPF لا يعيد استدعاء معالج تغيّر الخاصية
        /// لمجرد تغيّر المحتوى. الاشتراك في INotifyCollectionChanged هنا يجعل القطعة تتصرّف كأي ItemsControl
        /// حقيقي مربوط بـObservableCollection — اكتُشف فعلياً كسبب صفحة شجرة الحسابات الفارغة رغم بيانات حقيقية.</summary>
        private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var c = (AppTreeView)d;

            if (e.OldValue is INotifyCollectionChanged oldNotify)
                oldNotify.CollectionChanged -= c.OnSourceCollectionChanged;

            c._roots = e.NewValue is IEnumerable<TreeNodeViewModel> src
                ? new List<TreeNodeViewModel>(src)
                : new List<TreeNodeViewModel>();

            if (e.NewValue is INotifyCollectionChanged newNotify)
                newNotify.CollectionChanged += c.OnSourceCollectionChanged;

            c.RefreshFilter();
        }

        private void OnSourceCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            _roots = ItemsSource != null ? new List<TreeNodeViewModel>(ItemsSource) : new List<TreeNodeViewModel>();
            RefreshFilter();
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

        private void CheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not FrameworkElement fe || fe.DataContext is not TreeNodeViewModel node) return;
            e.Handled = true;
            Cycle(node);
        }

        /// <summary>ينقل العقدة للحالة التالية ثم ينشرها لكل الأبناء — عقدة الأب هي "تحديد/إلغاء الكل" لفرعها.</summary>
        public void Cycle(TreeNodeViewModel node)
        {
            var next = CheckMode == TreeCheckMode.ThreeState
                ? node.CheckState switch
                {
                    NodeCheckState.Granted => NodeCheckState.Revoked,
                    NodeCheckState.Revoked => NodeCheckState.Inherited,
                    _ => NodeCheckState.Granted
                }
                : node.CheckState == NodeCheckState.Checked ? NodeCheckState.Unchecked : NodeCheckState.Checked;

            ApplyState(node, next);
        }

        public void ApplyState(TreeNodeViewModel node, NodeCheckState state)
        {
            node.CheckState = state;
            CheckStateChanged?.Invoke(this, node);

            foreach (var child in node.Children)
                ApplyState(child, state);
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
