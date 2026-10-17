using System.Linq;
using System.Windows;
using System.Windows.Input;
using PrimeERP.Platform.Localization;
using PrimeERP.Platform.Permissions;
using PrimeERP.Application.PageServices.Security;

namespace PrimeERP.App
{
    /// <summary>أول نافذة حقيقية</summary>
    public partial class LoginWindow : Window
    {
        private readonly IAuthService _auth;

        public bool LoginSucceeded { get; private set; }

        public LoginWindow(IAuthService auth)
        {
            InitializeComponent();
            FlowDirection = PrimeERP.UI.Services.LanguageResources.Flow;
            _auth = auth;
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
            var signed = _auth.Verify(txtUsername.Text, txtPassword.Password);
            if (!signed.IsSuccess)
            {
                ShowError(signed.ErrorMessage);
                return;
            }

            var user = signed.Value;
            AppSession.SignIn(user.Id, user.Username, user.DisplayName, user.RoleId, user.RoleName, user.Permissions, user.BranchId);

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
