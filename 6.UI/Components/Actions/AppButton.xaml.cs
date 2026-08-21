using PrimeERP.UI.Services;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using PrimeERP.Platform.Permissions;

namespace PrimeERP.UI.Components.Actions
{
    public partial class AppButton : UserControl
    {
        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register(nameof(Text), typeof(string), typeof(AppButton),
                new PropertyMetadata("", OnContentChanged));

        public static readonly DependencyProperty IconProperty =
            DependencyProperty.Register(nameof(Icon), typeof(Geometry), typeof(AppButton),
                new PropertyMetadata(null, OnContentChanged));

        public static readonly DependencyProperty VariantProperty =
            DependencyProperty.Register(nameof(Variant), typeof(string), typeof(AppButton),
                new PropertyMetadata("primary"));

        public static readonly DependencyProperty SizeProperty =
            DependencyProperty.Register(nameof(Size), typeof(string), typeof(AppButton),
                new PropertyMetadata("md"));

        public static readonly DependencyProperty IsLoadingProperty =
            DependencyProperty.Register(nameof(IsLoading), typeof(bool), typeof(AppButton),
                new PropertyMetadata(false, OnContentChanged));

        public static readonly DependencyProperty IsDangerousProperty =
            DependencyProperty.Register(nameof(IsDangerous), typeof(bool), typeof(AppButton),
                new PropertyMetadata(false));

        public static readonly DependencyProperty PermissionKeyProperty =
            DependencyProperty.Register(nameof(PermissionKey), typeof(string), typeof(AppButton),
                new PropertyMetadata(null, OnPermissionKeyChanged));

        public static readonly DependencyProperty CommandProperty =
            DependencyProperty.Register(nameof(Command), typeof(ICommand), typeof(AppButton),
                new PropertyMetadata(null, (d, e) => ((AppButton)d).btn.Command = (ICommand)e.NewValue));

        public static readonly DependencyProperty CommandParameterProperty =
            DependencyProperty.Register(nameof(CommandParameter), typeof(object), typeof(AppButton),
                new PropertyMetadata(null, (d, e) => ((AppButton)d).btn.CommandParameter = e.NewValue));

        /// <summary>تحدّدها AppButtonStyle حسب Size — الأيقونة الداخلية تقرأها بدل حجم مكتوب في القطعة.</summary>
        public static readonly DependencyProperty IconSizeProperty =
            DependencyProperty.Register(nameof(IconSize), typeof(double), typeof(AppButton),
                new PropertyMetadata(16.0));

        public double IconSize { get => (double)GetValue(IconSizeProperty); set => SetValue(IconSizeProperty, value); }

        public string   Text            { get => (string)GetValue(TextProperty);            set => SetValue(TextProperty, value); }
        public Geometry Icon            { get => (Geometry)GetValue(IconProperty);           set => SetValue(IconProperty, value); }

        /// <summary>"primary" (افتراضي) / "secondary" / "danger" / "success" / "warning" / "ghost" — الشكل الفعلي في AppButtonStyle.</summary>
        public string   Variant         { get => (string)GetValue(VariantProperty);          set => SetValue(VariantProperty, value); }

        /// <summary>"sm" / "md" (افتراضي) / "lg" — الأبعاد الفعلية في AppButtonStyle.</summary>
        public string   Size            { get => (string)GetValue(SizeProperty);             set => SetValue(SizeProperty, value); }
        public bool     IsLoading       { get => (bool)GetValue(IsLoadingProperty);          set => SetValue(IsLoadingProperty, value); }

        /// <summary>يفرض شكل "danger" بصرف النظر عن Variant — لعمليات الحذف/الإلغاء الحرجة.</summary>
        public bool     IsDangerous     { get => (bool)GetValue(IsDangerousProperty);        set => SetValue(IsDangerousProperty, value); }
        public string   PermissionKey   { get => (string)GetValue(PermissionKeyProperty);    set => SetValue(PermissionKeyProperty, value); }
        public ICommand Command         { get => (ICommand)GetValue(CommandProperty);        set => SetValue(CommandProperty, value); }
        public object   CommandParameter{ get => GetValue(CommandParameterProperty);         set => SetValue(CommandParameterProperty, value); }

        public event RoutedEventHandler Click;

        public AppButton()
        {
            InitializeComponent();
            Loaded += (s, e) => { ApplyContent(); AppSession.PermissionsChanged += OnPermissionsChanged; };
            Unloaded += (s, e) => AppSession.PermissionsChanged -= OnPermissionsChanged;
        }

        private void OnPermissionsChanged(object sender, System.EventArgs e) =>
            ApplyPermissionVisibility();

        private static void OnContentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
            ((AppButton)d).ApplyContent();

        private static void OnPermissionKeyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
            ((AppButton)d).ApplyPermissionVisibility();

        private void ApplyPermissionVisibility()
        {
            Visibility = string.IsNullOrEmpty(PermissionKey) || UIServices.Permissions.Can(PermissionKey)
                ? Visibility.Visible : Visibility.Collapsed;
        }

        private void ApplyContent()
        {
            if (IsLoading)
            {
                txt.Text = "جارٍ التحميل...";
                icon.Visibility = Visibility.Collapsed;
                btn.IsEnabled = false;
                return;
            }

            btn.IsEnabled = true;
            txt.Text = Text;
            icon.Data = Icon;
            icon.Visibility = Icon != null ? Visibility.Visible : Visibility.Collapsed;
        }

        private void btn_Click(object sender, RoutedEventArgs e) => Click?.Invoke(this, e);
    }
}
