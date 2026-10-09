using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using PrimeERP.Platform.Localization;

namespace PrimeERP.UI.Components.Shell
{
    /// <summary>شريط علوي عام</summary>
    public partial class AppTopBar : UserControl
    {
        private readonly List<(string Label, Action Raise)> _userMenu;

        public static readonly DependencyProperty BreadcrumbProperty =
            DependencyProperty.Register(nameof(Breadcrumb), typeof(List<string>), typeof(AppTopBar),
                new PropertyMetadata(null, (d, e) => ((AppTopBar)d).breadcrumb.Items = (List<string>)e.NewValue ?? new List<string>()));

        public static readonly DependencyProperty PageTitleProperty =
            DependencyProperty.Register(nameof(PageTitle), typeof(string), typeof(AppTopBar),
                new PropertyMetadata("", (d, e) => ((AppTopBar)d).pageTitleText.Text = (string)e.NewValue));

        public static readonly DependencyProperty CompanyNameProperty =
            DependencyProperty.Register(nameof(CompanyName), typeof(string), typeof(AppTopBar),
                new PropertyMetadata("", (d, e) => ((AppTopBar)d).companyText.Text = (string)e.NewValue));

        public static readonly DependencyProperty ShowCompanyNameProperty =
            DependencyProperty.Register(nameof(ShowCompanyName), typeof(bool), typeof(AppTopBar),
                new PropertyMetadata(true, (d, e) => ((AppTopBar)d).companyText.Visibility = (bool)e.NewValue ? Visibility.Visible : Visibility.Collapsed));

        public static readonly DependencyProperty ShowDateProperty =
            DependencyProperty.Register(nameof(ShowDate), typeof(bool), typeof(AppTopBar),
                new PropertyMetadata(true, (d, e) => ((AppTopBar)d).datePanel.Visibility = (bool)e.NewValue ? Visibility.Visible : Visibility.Collapsed));

        public static readonly DependencyProperty NotificationCountProperty =
            DependencyProperty.Register(nameof(NotificationCount), typeof(int), typeof(AppTopBar),
                new PropertyMetadata(0, OnNotificationCountChanged));

        public static readonly DependencyProperty UserNameProperty =
            DependencyProperty.Register(nameof(UserName), typeof(string), typeof(AppTopBar),
                new PropertyMetadata("", (d, e) => ((AppTopBar)d).userMenu.Text = (string)e.NewValue));

        public static readonly DependencyProperty UserRoleProperty =
            DependencyProperty.Register(nameof(UserRole), typeof(string), typeof(AppTopBar), new PropertyMetadata(""));

        public static readonly DependencyProperty UserInitialProperty =
            DependencyProperty.Register(nameof(UserInitial), typeof(string), typeof(AppTopBar),
                new PropertyMetadata("", (d, e) => ((AppTopBar)d).userInitialText.Text = (string)e.NewValue));

        public static readonly DependencyProperty ActionsContentProperty =
            DependencyProperty.Register(nameof(ActionsContent), typeof(object), typeof(AppTopBar),
                new PropertyMetadata(null, (d, e) => ((AppTopBar)d).actionsPresenter.Content = e.NewValue));

        public List<string> Breadcrumb        { get => (List<string>)GetValue(BreadcrumbProperty);   set => SetValue(BreadcrumbProperty, value); }
        public string       PageTitle         { get => (string)GetValue(PageTitleProperty);           set => SetValue(PageTitleProperty, value); }
        public string       CompanyName       { get => (string)GetValue(CompanyNameProperty);         set => SetValue(CompanyNameProperty, value); }
        public bool         ShowCompanyName   { get => (bool)GetValue(ShowCompanyNameProperty);       set => SetValue(ShowCompanyNameProperty, value); }
        public bool         ShowDate          { get => (bool)GetValue(ShowDateProperty);              set => SetValue(ShowDateProperty, value); }
        public int          NotificationCount { get => (int)GetValue(NotificationCountProperty);      set => SetValue(NotificationCountProperty, value); }
        public string       UserName          { get => (string)GetValue(UserNameProperty);            set => SetValue(UserNameProperty, value); }
        public string       UserRole          { get => (string)GetValue(UserRoleProperty);            set => SetValue(UserRoleProperty, value); }
        public string       UserInitial       { get => (string)GetValue(UserInitialProperty);         set => SetValue(UserInitialProperty, value); }
        public object       ActionsContent    { get => GetValue(ActionsContentProperty);               set => SetValue(ActionsContentProperty, value); }

        public event EventHandler SidebarToggled;
        public event EventHandler PasswordChangeRequested;
        public event EventHandler LogoutRequested;
        public event EventHandler UpdateRequested;
        public event EventHandler LanguageToggled;

        public AppTopBar()
        {
            InitializeComponent();
            _userMenu = new()
            {
                (LocalizationService.Get("Str.TopBar.ChangePassword"), () => PasswordChangeRequested?.Invoke(this, EventArgs.Empty)),
                (LocalizationService.Get("Str.Settings.Update"),       () => UpdateRequested?.Invoke(this, EventArgs.Empty)),
                (LocalizationService.Get("Str.TopBar.Language"),       () => LanguageToggled?.Invoke(this, EventArgs.Empty)),
                (LocalizationService.Get("Str.Logout"),                () => LogoutRequested?.Invoke(this, EventArgs.Empty)),
            };
            userMenu.Items = _userMenu.Select(m => m.Label).ToArray();
            RefreshDate();
        }

        private void RefreshDate()
        {
            var now = DateTime.Now;
            gregorianText.Text = now.ToString("dd/MM/yyyy");

            var hijri = new HijriCalendar();
            hijriText.Text = LocalizationService.Get("Str.TopBar.Hijri", hijri.GetDayOfMonth(now), hijri.GetMonth(now), hijri.GetYear(now));
        }

        private static void OnNotificationCountChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var c = (AppTopBar)d;
            var count = (int)e.NewValue;
            c.notifBadge.Visibility = count > 0 ? Visibility.Visible : Visibility.Collapsed;
            c.notifBadgeText.Text = count > 99 ? "99+" : count.ToString();
        }

        private void btnToggleSidebar_Click(object sender, RoutedEventArgs e) => SidebarToggled?.Invoke(this, EventArgs.Empty);


        public void HideLanguage()
        {
            _userMenu.RemoveAll(m => m.Label == LocalizationService.Get("Str.TopBar.Language"));
            userMenu.Items = _userMenu.Select(m => m.Label).ToArray();
        }

        private void userMenu_ItemSelected(object sender, object item)
        {
            var label = item as string;
            _userMenu.FirstOrDefault(m => m.Label == label).Raise?.Invoke();
        }
    }
}
