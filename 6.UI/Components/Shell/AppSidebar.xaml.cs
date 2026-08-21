using PrimeERP.Platform.Permissions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace PrimeERP.UI.Components.Shell
{
    /// <summary>
    /// شريط تنقّل جانبي هرمي — يختفي أي عنصر بلا صلاحية تلقائياً (ويختفي الأب لو كل أبنائه محجوبون)،
    /// ويستمع لـ AppSession.PermissionsChanged لإعادة حساب الظهور حياً بلا إعادة بناء الشجرة.
    /// </summary>
    public partial class AppSidebar : UserControl
    {
        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register(nameof(ItemsSource), typeof(List<NavItem>), typeof(AppSidebar),
                new PropertyMetadata(null, OnItemsSourceChanged));

        public static readonly DependencyProperty SelectedKeyProperty =
            DependencyProperty.Register(nameof(SelectedKey), typeof(string), typeof(AppSidebar),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedKeyChanged));

        public static readonly DependencyProperty IsCollapsedProperty =
            DependencyProperty.Register(nameof(IsCollapsed), typeof(bool), typeof(AppSidebar),
                new PropertyMetadata(false, OnIsCollapsedChanged));

        public static readonly DependencyProperty LogoContentProperty =
            DependencyProperty.Register(nameof(LogoContent), typeof(object), typeof(AppSidebar),
                new PropertyMetadata(null, (d, e) => ((AppSidebar)d).logoPresenter.Content = e.NewValue));

        public static readonly DependencyProperty FooterContentProperty =
            DependencyProperty.Register(nameof(FooterContent), typeof(object), typeof(AppSidebar),
                new PropertyMetadata(null, (d, e) => ((AppSidebar)d).footerPresenter.Content = e.NewValue));

        public List<NavItem> ItemsSource  { get => (List<NavItem>)GetValue(ItemsSourceProperty); set => SetValue(ItemsSourceProperty, value); }
        public string        SelectedKey  { get => (string)GetValue(SelectedKeyProperty);         set => SetValue(SelectedKeyProperty, value); }
        public bool          IsCollapsed  { get => (bool)GetValue(IsCollapsedProperty);            set => SetValue(IsCollapsedProperty, value); }
        public object        LogoContent  { get => GetValue(LogoContentProperty);                  set => SetValue(LogoContentProperty, value); }
        public object        FooterContent{ get => GetValue(FooterContentProperty);                set => SetValue(FooterContentProperty, value); }

        public event EventHandler<string> NavigationRequested;

        private readonly ObservableCollection<NavItemViewModel> _rootItems = new();

        public AppSidebar()
        {
            InitializeComponent();
            itemsHost.ItemsSource = _rootItems;

            Loaded += (s, e) => AppSession.PermissionsChanged += OnPermissionsChanged;
            Unloaded += (s, e) => AppSession.PermissionsChanged -= OnPermissionsChanged;
        }

        private void OnPermissionsChanged(object sender, EventArgs e) => RecomputeVisibility();

        private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var c = (AppSidebar)d;
            c._rootItems.Clear();
            foreach (var item in c.ItemsSource ?? Enumerable.Empty<NavItem>())
                c._rootItems.Add(new NavItemViewModel(item));

            c.RecomputeVisibility();
            c.RefreshActiveState();
        }

        private void RecomputeVisibility()
        {
            foreach (var item in _rootItems)
                item.RecomputeVisibility();
        }

        private static void OnSelectedKeyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
            ((AppSidebar)d).RefreshActiveState();

        private void RefreshActiveState()
        {
            var all = _rootItems.SelectMany(r => r.Flatten()).ToList();
            foreach (var item in all)
                item.IsActive = !string.IsNullOrEmpty(SelectedKey) && item.Key == SelectedKey;

            var active = all.FirstOrDefault(i => i.IsActive);
            if (active != null)
                foreach (var root in _rootItems)
                    ExpandIfContains(root, active);
        }

        private static bool ExpandIfContains(NavItemViewModel node, NavItemViewModel target)
        {
            if (node == target) return true;
            foreach (var child in node.Children)
            {
                if (ExpandIfContains(child, target))
                {
                    node.IsExpanded = true;
                    return true;
                }
            }
            return false;
        }

        private static void OnIsCollapsedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var c = (AppSidebar)d;
            var targetWidth = (bool)e.NewValue ? 56.0 : 240.0;
            c.root.BeginAnimation(WidthProperty, new DoubleAnimation(targetWidth, TimeSpan.FromSeconds(0.2)));
        }

        private void NavRow_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is not FrameworkElement fe || fe.DataContext is not NavItemViewModel item) return;

            if (item.HasChildren)
            {
                if (!IsCollapsed) item.IsExpanded = !item.IsExpanded;
                return;
            }

            SelectedKey = item.Key;
            NavigationRequested?.Invoke(this, item.Key);
        }
    }
}
