using System;
using System.Windows;
using System.Windows.Controls;

namespace PrimeERP.UI.Components.Shell
{
    public partial class AppTitleBar : UserControl
    {
        private Window _window;

        public AppTitleBar()
        {
            InitializeComponent();
            Loaded += (_, _) =>
            {
                _window = Window.GetWindow(this);
                _window.StateChanged += OnStateChanged;
                OnStateChanged(_window, EventArgs.Empty);
            };
            Unloaded += (_, _) => _window.StateChanged -= OnStateChanged;
        }

        private void OnStateChanged(object sender, EventArgs e) =>
            btnMaximize.Content = _window.WindowState == WindowState.Maximized ? "" : "";

        private void btnMinimize_Click(object sender, RoutedEventArgs e) => _window.WindowState = WindowState.Minimized;

        private void btnMaximize_Click(object sender, RoutedEventArgs e) =>
            _window.WindowState = _window.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

        private void btnClose_Click(object sender, RoutedEventArgs e) => _window.Close();
    }
}
