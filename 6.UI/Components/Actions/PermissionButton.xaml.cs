using PrimeERP.UI.Services;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using PrimeERP.Platform.Permissions;

namespace PrimeERP.UI.Components.Actions
{
    /// <summary>زر يتعطل أو يختفي (حسب HideIfDenied) لو المستخدم الحالي لا يملك PermissionKey.</summary>
    public partial class PermissionButton : UserControl
    {
        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register(nameof(Text), typeof(string), typeof(PermissionButton),
                new PropertyMetadata("", (d, e) => ((PermissionButton)d).button.Text = (string)e.NewValue));

        public static readonly DependencyProperty IconProperty =
            DependencyProperty.Register(nameof(Icon), typeof(Geometry), typeof(PermissionButton),
                new PropertyMetadata(null, (d, e) => ((PermissionButton)d).button.Icon = (Geometry)e.NewValue));

        public static readonly DependencyProperty VariantProperty =
            DependencyProperty.Register(nameof(Variant), typeof(string), typeof(PermissionButton),
                new PropertyMetadata("primary", (d, e) => ((PermissionButton)d).button.Variant = (string)e.NewValue));

        public static readonly DependencyProperty PermissionKeyProperty =
            DependencyProperty.Register(nameof(PermissionKey), typeof(string), typeof(PermissionButton),
                new PropertyMetadata(null, OnPermissionStateChanged));

        public static readonly DependencyProperty HideIfDeniedProperty =
            DependencyProperty.Register(nameof(HideIfDenied), typeof(bool), typeof(PermissionButton),
                new PropertyMetadata(true, OnPermissionStateChanged));

        public static readonly DependencyProperty CommandProperty =
            DependencyProperty.Register(nameof(Command), typeof(ICommand), typeof(PermissionButton),
                new PropertyMetadata(null, (d, e) => ((PermissionButton)d).button.Command = (ICommand)e.NewValue));

        public string   Text          { get => (string)GetValue(TextProperty);          set => SetValue(TextProperty, value); }
        public Geometry Icon          { get => (Geometry)GetValue(IconProperty);        set => SetValue(IconProperty, value); }
        public string   Variant       { get => (string)GetValue(VariantProperty);       set => SetValue(VariantProperty, value); }
        public string   PermissionKey { get => (string)GetValue(PermissionKeyProperty); set => SetValue(PermissionKeyProperty, value); }
        public bool     HideIfDenied  { get => (bool)GetValue(HideIfDeniedProperty);    set => SetValue(HideIfDeniedProperty, value); }
        public ICommand Command       { get => (ICommand)GetValue(CommandProperty);     set => SetValue(CommandProperty, value); }

        public event RoutedEventHandler Click;

        public PermissionButton()
        {
            InitializeComponent();
            Loaded += (s, e) => AppSession.PermissionsChanged += OnPermissionsChanged;
            Unloaded += (s, e) => AppSession.PermissionsChanged -= OnPermissionsChanged;
        }

        private void OnPermissionsChanged(object sender, System.EventArgs e) => ApplyPermissionState();

        private static void OnPermissionStateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
            ((PermissionButton)d).ApplyPermissionState();

        private void ApplyPermissionState()
        {
            var allowed = string.IsNullOrEmpty(PermissionKey) || UIServices.Permissions.Can(PermissionKey);

            if (HideIfDenied)
            {
                Visibility = allowed ? Visibility.Visible : Visibility.Collapsed;
                button.IsEnabled = true;
            }
            else
            {
                Visibility = Visibility.Visible;
                button.IsEnabled = allowed;
            }
        }

        private void button_Click(object sender, RoutedEventArgs e) => Click?.Invoke(this, e);
    }
}
