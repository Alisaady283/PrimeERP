using System.Linq;
using System.Windows;
using System.Windows.Input;
using PrimeERP.Platform.Localization;
using PrimeERP.Platform.Permissions;
using PrimeERP.Platform.Security;

namespace PrimeERP.App
{
    /// <summary>أول نافذة حقيقية</summary>
    public partial class LoginWindow : Window
    {
        private readonly IPermissionService _permissions;
        private readonly IPermissionStore _store;

        public bool LoginSucceeded { get; private set; }

        public LoginWindow(IPermissionService permissions, IPermissionStore store)
        {
            InitializeComponent();
            FlowDirection = PrimeERP.Platform.Localization.LocalizationService.Flow;
            _permissions = permissions;
            _store = store;
            txtUsername.Focus();
        }

        private void txtPassword_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) TryLogin();
        }

        private void btnLogin_Click(object sender, RoutedEventArgs e) => TryLogin();

        private void btnCancel_Click(object sender, RoutedEventArgs e) => Close();

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed) DragMove();
        }

        private void TryLogin()
        {
            var username = txtUsername.Text?.Trim() ?? "";
            var password = txtPassword.Password;

            var user = _store.FindByUsername(username);
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
            _store.UpdateLastLogin(user.Id);

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
