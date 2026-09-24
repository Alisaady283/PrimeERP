using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace PrimeERP.UI.Components.Shell
{
    /// <summary>القطعة الجامعة</summary>
    public partial class AppShell : UserControl
    {
        public static readonly DependencyProperty NavItemsProperty =
            DependencyProperty.Register(nameof(NavItems), typeof(List<NavItem>), typeof(AppShell),
                new PropertyMetadata(null, OnNavItemsChanged));

        public static readonly DependencyProperty CurrentPageProperty =
            DependencyProperty.Register(nameof(CurrentPage), typeof(object), typeof(AppShell),
                new PropertyMetadata(null, (d, e) => ((AppShell)d).pageHost.Content = e.NewValue));

        public static readonly DependencyProperty SelectedKeyProperty =
            DependencyProperty.Register(nameof(SelectedKey), typeof(string), typeof(AppShell),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedKeyChanged));

        public static readonly DependencyProperty CompanyNameProperty =
            DependencyProperty.Register(nameof(CompanyName), typeof(string), typeof(AppShell),
                new PropertyMetadata("", (d, e) => ((AppShell)d).topBar.CompanyName = (string)e.NewValue));

        public static readonly DependencyProperty UserNameProperty =
            DependencyProperty.Register(nameof(UserName), typeof(string), typeof(AppShell),
                new PropertyMetadata("", OnUserNameChanged));

        public static readonly DependencyProperty UserRoleProperty =
            DependencyProperty.Register(nameof(UserRole), typeof(string), typeof(AppShell),
                new PropertyMetadata("", (d, e) => ((AppShell)d).topBar.UserRole = (string)e.NewValue));

        public List<NavItem> NavItems    { get => (List<NavItem>)GetValue(NavItemsProperty);   set => SetValue(NavItemsProperty, value); }
        public object        CurrentPage { get => GetValue(CurrentPageProperty);                set => SetValue(CurrentPageProperty, value); }
        public string        SelectedKey { get => (string)GetValue(SelectedKeyProperty);        set => SetValue(SelectedKeyProperty, value); }
        public string        CompanyName { get => (string)GetValue(CompanyNameProperty);        set => SetValue(CompanyNameProperty, value); }
        public string        UserName    { get => (string)GetValue(UserNameProperty);           set => SetValue(UserNameProperty, value); }
        public string        UserRole    { get => (string)GetValue(UserRoleProperty);           set => SetValue(UserRoleProperty, value); }

        public event EventHandler<string> NavigationRequested;
        public event EventHandler NotificationsClicked;
        public event EventHandler ProfileClicked;
        public event EventHandler PasswordChangeRequested;
        public event EventHandler LogoutRequested;
        public event EventHandler LanguageToggled;
        public event EventHandler UpdateRequested;

        public object TopBarActionsContent { get => topBar.ActionsContent; set => topBar.ActionsContent = value; }

        public object SidebarLogoContent   { get => sidebar.LogoContent;   set => sidebar.LogoContent = value; }
        public object SidebarFooterContent { get => sidebar.FooterContent; set => sidebar.FooterContent = value; }

        public AppShell()
        {
            InitializeComponent();

            topBar.NotificationsClicked     += (s, e) => NotificationsClicked?.Invoke(this, e);
            topBar.ProfileClicked           += (s, e) => ProfileClicked?.Invoke(this, e);
            topBar.PasswordChangeRequested  += (s, e) => PasswordChangeRequested?.Invoke(this, e);
            topBar.LogoutRequested          += (s, e) => LogoutRequested?.Invoke(this, e);
            topBar.LanguageToggled          += (s, e) => LanguageToggled?.Invoke(this, e);
            topBar.UpdateRequested          += (s, e) => UpdateRequested?.Invoke(this, e);
        }

        private static void OnNavItemsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var c = (AppShell)d;
            c.sidebar.ItemsSource = (List<NavItem>)e.NewValue;
            c.UpdateBreadcrumb();
        }

        private static void OnSelectedKeyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var c = (AppShell)d;
            c.sidebar.SelectedKey = (string)e.NewValue;
            c.UpdateBreadcrumb();
        }

        private static void OnUserNameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var c = (AppShell)d;
            var name = (string)e.NewValue;
            c.topBar.UserName = name;
            c.topBar.UserInitial = string.IsNullOrEmpty(name) ? "" : name.Trim()[0].ToString();
        }

        private void sidebar_NavigationRequested(object sender, string key)
        {
            SelectedKey = key;
            NavigationRequested?.Invoke(this, key);
        }

        private void topBar_SidebarToggled(object sender, EventArgs e) => sidebar.IsCollapsed = !sidebar.IsCollapsed;

        private void UpdateBreadcrumb()
        {
            var path = FindPath(NavItems, SelectedKey);
            topBar.Breadcrumb = path?.Take(path.Count - 1).Select(i => i.Text).ToList() ?? new List<string>();
            topBar.PageTitle = path?.LastOrDefault()?.Text ?? "";
        }

        private static List<NavItem> FindPath(List<NavItem> items, string key)
        {
            if (items == null || string.IsNullOrEmpty(key)) return null;

            foreach (var item in items)
            {
                if (item.Key == key) return new List<NavItem> { item };

                var childPath = FindPath(item.Children, key);
                if (childPath != null)
                {
                    childPath.Insert(0, item);
                    return childPath;
                }
            }
            return null;
        }
    }
}
