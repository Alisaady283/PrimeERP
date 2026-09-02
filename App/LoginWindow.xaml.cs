using System.Linq;
using System.Windows;
using System.Windows.Input;
using PrimeERP.Platform.Localization;
using PrimeERP.Platform.Permissions;
using PrimeERP.Platform.Security;

namespace PrimeERP.App
{
    /// <summary>أول نافذة حقيقية — تُنشأ يدوياً من App.xaml.cs.OnStartup (لا StartupUri)، تحقن IPermissionService
    /// مباشرة عبر المُنشئ لأن هذا استدعاء new صريح لا تحليل XAML ضمني. عند النجاح: AppSession.SignIn +
    /// IPermissionService.LoadForUser (⚠️ R9 — أول مسار حي فعلي لها).
    ///
    /// ⚠️ لا ShowDialog() — راجع توقف 10 في ARCHITECTURE.md: تُعلَّق للأبد في بيئة التشغيل الفعلية هنا (Show()
    /// تعمل فوراً). LoginSucceeded + Show() + حلقة Dispatcher يدوية في App.xaml.cs تُحاكي سلوك ShowDialog
    /// المطلوب (حجب حتى الإغلاق) بلا استخدام آلية ShowDialog الداخلية المُعطَّلة هنا — لذا لا DialogResult
    /// (يتطلب ShowDialog صراحة، يرمي استثناء بلا ذلك).</summary>
    public partial class LoginWindow : Window
    {
        private readonly IPermissionService _permissions;

        public bool LoginSucceeded { get; private set; }

        public LoginWindow(IPermissionService permissions)
        {
            InitializeComponent();
            _permissions = permissions;
            txtUsername.Focus();
        }

        private void txtPassword_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) TryLogin();
        }

        private void btnLogin_Click(object sender, RoutedEventArgs e) => TryLogin();

        private void btnCancel_Click(object sender, RoutedEventArgs e) => Close();

        // النافذة بلا إطار ويندوز، فالسحب يدوي.
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed) DragMove();
        }

        private void TryLogin()
        {
            var username = txtUsername.Text?.Trim() ?? "";
            var password = txtPassword.Password;

            var user = PermissionDb.FindByUsername(username);
            if (user == null || !PasswordHasher.Verify(password, user.PasswordHash, user.Salt))
            {
                ShowError(LocalizationService.Get("Str.Login.InvalidCredentials"));
                return;
            }
            if (!user.IsActive)
            {
                ShowError(LocalizationService.Get("Str.Login.AccountInactive"));
                return;
            }

            _permissions.LoadForUser(user.Id);
            AppSession.SignIn(user.Id, user.Username, user.DisplayName, user.RoleId, user.RoleName, AppSession.Permissions.ToList());
            PermissionDb.UpdateLastLogin(user.Id);

            LoginSucceeded = true;
            Close();
        }

        private void ShowError(string message)
        {
            txtError.Text = message;
            txtError.Visibility = Visibility.Visible;
        }
    }
}
