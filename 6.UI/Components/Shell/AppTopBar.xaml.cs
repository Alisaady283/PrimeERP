using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace PrimeERP.UI.Components.Shell
{
    /// <summary>شريط علوي عام — عنوان/breadcrumb، تاريخ هجري وميلادي، إشعارات، وقائمة مستخدم منسدلة.</summary>
    public partial class AppTopBar : UserControl
    {
        private const string NightModeLabel = "الوضع الليلي";

        private static string[] BuildUserMenu(bool darkMode) => new[]
        {
            "الملف الشخصي", "تغيير كلمة المرور", "تحديث",
            darkMode ? NightModeLabel + "  ✓" : NightModeLabel,
            "اللغة", "تسجيل الخروج"
        };

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

        public static readonly DependencyProperty IsDarkModeProperty =
            DependencyProperty.Register(nameof(IsDarkMode), typeof(bool), typeof(AppTopBar),
                new PropertyMetadata(false, (d, e) => ((AppTopBar)d).userMenu.Items = BuildUserMenu((bool)e.NewValue)));

        public bool IsDarkMode { get => (bool)GetValue(IsDarkModeProperty); set => SetValue(IsDarkModeProperty, value); }

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
        public event EventHandler NotificationsClicked;
        public event EventHandler ProfileClicked;
        public event EventHandler PasswordChangeRequested;
        public event EventHandler LogoutRequested;
        public event EventHandler UpdateRequested;
        public event EventHandler ThemeToggled;
        public event EventHandler LanguageToggled;

        public AppTopBar()
        {
            InitializeComponent();
            userMenu.Items = BuildUserMenu(IsDarkMode);
            RefreshDate();
        }

        private void RefreshDate()
        {
            var now = DateTime.Now;
            gregorianText.Text = now.ToString("dd/MM/yyyy");

            var hijri = new HijriCalendar();
            hijriText.Text = $"{hijri.GetDayOfMonth(now)}/{hijri.GetMonth(now)}/{hijri.GetYear(now)} هـ";
        }

        private static void OnNotificationCountChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var c = (AppTopBar)d;
            var count = (int)e.NewValue;
            c.notifBadge.Visibility = count > 0 ? Visibility.Visible : Visibility.Collapsed;
            c.notifBadgeText.Text = count > 99 ? "99+" : count.ToString();
        }

        private void btnToggleSidebar_Click(object sender, RoutedEventArgs e) => SidebarToggled?.Invoke(this, EventArgs.Empty);

        private void btnNotifications_Click(object sender, RoutedEventArgs e) => NotificationsClicked?.Invoke(this, EventArgs.Empty);

        private void userMenu_ItemSelected(object sender, object item)
        {
            var label = item as string ?? "";

            // بادئة لا مطابقة تامة — بند الوضع الليلي يحمل علامة صح حين يكون مفعّلاً.
            if (label.StartsWith(NightModeLabel)) { ThemeToggled?.Invoke(this, EventArgs.Empty); return; }

            switch (label)
            {
                case "الملف الشخصي":       ProfileClicked?.Invoke(this, EventArgs.Empty); break;
                case "تغيير كلمة المرور":  PasswordChangeRequested?.Invoke(this, EventArgs.Empty); break;
                case "اللغة":              LanguageToggled?.Invoke(this, EventArgs.Empty); break;
                case "تحديث":              UpdateRequested?.Invoke(this, EventArgs.Empty); break;
                case "تسجيل الخروج":       LogoutRequested?.Invoke(this, EventArgs.Empty); break;
            }
        }
    }
}
