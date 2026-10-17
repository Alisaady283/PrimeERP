using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace PrimeERP.UI.Components.Inputs
{
    /// <summary>حقل إدخال AppTextArea</summary>
    public partial class AppTextArea : UserControl
    {
        public static readonly DependencyProperty LabelProperty =
            DependencyProperty.Register(nameof(Label), typeof(string), typeof(AppTextArea),
                new PropertyMetadata("", OnLabelChanged));

        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register(nameof(Text), typeof(string), typeof(AppTextArea),
                new FrameworkPropertyMetadata("", FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnTextChanged));

        public static readonly DependencyProperty PlaceholderProperty =
            DependencyProperty.Register(nameof(Placeholder), typeof(string), typeof(AppTextArea),
                new PropertyMetadata("", OnPlaceholderChanged));

        public static readonly DependencyProperty ErrorTextProperty =
            DependencyProperty.Register(nameof(ErrorText), typeof(string), typeof(AppTextArea),
                new PropertyMetadata(null, OnErrorChanged));

        public static readonly DependencyProperty IsRequiredProperty =
            DependencyProperty.Register(nameof(IsRequired), typeof(bool), typeof(AppTextArea),
                new PropertyMetadata(false, OnLabelChanged));

        public static readonly DependencyProperty RowsProperty =
            DependencyProperty.Register(nameof(Rows), typeof(int), typeof(AppTextArea),
                new PropertyMetadata(4, OnRowsChanged));

        public static readonly DependencyProperty MaxLengthProperty =
            DependencyProperty.Register(nameof(MaxLength), typeof(int), typeof(AppTextArea),
                new PropertyMetadata(0, OnMaxLengthChanged));

        public static readonly DependencyProperty ShowCounterProperty =
            DependencyProperty.Register(nameof(ShowCounter), typeof(bool), typeof(AppTextArea),
                new PropertyMetadata(false, OnShowCounterChanged));

        public static readonly DependencyProperty IsReadOnlyProperty =
            DependencyProperty.Register(nameof(IsReadOnly), typeof(bool), typeof(AppTextArea),
                new PropertyMetadata(false, OnIsReadOnlyChanged));

        public string Label       { get => (string)GetValue(LabelProperty);       set => SetValue(LabelProperty, value); }
        public string Text        { get => (string)GetValue(TextProperty);        set => SetValue(TextProperty, value); }
        public string Placeholder { get => (string)GetValue(PlaceholderProperty); set => SetValue(PlaceholderProperty, value); }
        public string ErrorText   { get => (string)GetValue(ErrorTextProperty);   set => SetValue(ErrorTextProperty, value); }
        public bool   IsRequired  { get => (bool)GetValue(IsRequiredProperty);    set => SetValue(IsRequiredProperty, value); }
        public int    Rows        { get => (int)GetValue(RowsProperty);           set => SetValue(RowsProperty, value); }
        public int    MaxLength   { get => (int)GetValue(MaxLengthProperty);      set => SetValue(MaxLengthProperty, value); }
        public bool   ShowCounter { get => (bool)GetValue(ShowCounterProperty);   set => SetValue(ShowCounterProperty, value); }
        public bool   IsReadOnly  { get => (bool)GetValue(IsReadOnlyProperty);    set => SetValue(IsReadOnlyProperty, value); }

        public event TextChangedEventHandler TextChanged;

        private bool _suppressTextChanged;

        public AppTextArea()
        {
            InitializeComponent();
            IsEnabledChanged += (s, e) => ApplyEnabledVisual();
        }

        private static void OnLabelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var c = (AppTextArea)d;
            c.txtLabel.Text = c.Label;
            c.pnlLabel.Visibility = string.IsNullOrEmpty(c.Label) ? Visibility.Collapsed : Visibility.Visible;
            c.txtRequired.Visibility = c.IsRequired ? Visibility.Visible : Visibility.Collapsed;
        }

        private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var c = (AppTextArea)d;
            if (c._suppressTextChanged) return;
            c._suppressTextChanged = true;
            c.txt.Text = (string)e.NewValue ?? "";
            c._suppressTextChanged = false;
            c.placeholder.Visibility = string.IsNullOrEmpty(c.txt.Text) ? Visibility.Visible : Visibility.Collapsed;
            c.UpdateCounter();
        }

        private static void OnPlaceholderChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((AppTextArea)d).placeholder.Text = (string)e.NewValue;
        }

        private static void OnErrorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var c = (AppTextArea)d;
            var hasError = !string.IsNullOrEmpty(c.ErrorText);
            c.txtError.Text = c.ErrorText;
            c.txtError.Visibility = hasError ? Visibility.Visible : Visibility.Collapsed;
            c.border.BorderBrush = hasError ? (Brush)c.FindResource("Danger") : (Brush)c.FindResource("OutlineDefault");
        }

        private static void OnRowsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var c = (AppTextArea)d;
            c.txt.Height = c.Rows * 20 + 16;
        }

        private static void OnMaxLengthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var c = (AppTextArea)d;
            c.txt.MaxLength = c.MaxLength;
            c.UpdateCounter();
        }

        private static void OnShowCounterChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((AppTextArea)d).UpdateCounter();
        }

        private static void OnIsReadOnlyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((AppTextArea)d).txt.IsReadOnly = (bool)e.NewValue;
        }

        private void UpdateCounter()
        {
            if (!ShowCounter || MaxLength <= 0)
            {
                txtCounter.Visibility = Visibility.Collapsed;
                return;
            }

            txtCounter.Visibility = Visibility.Visible;
            txtCounter.Text = $"{(Text ?? "").Length}/{MaxLength}";
        }

        private void ApplyEnabledVisual()
        {
            border.Background  = IsEnabled ? (Brush)FindResource("SurfaceDefault")   : (Brush)FindResource("SurfaceSunken");
            border.BorderBrush = IsEnabled ? (Brush)FindResource("OutlineDefault") : (Brush)FindResource("OutlineSubtle");
        }

        private void txt_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_suppressTextChanged) return;
            _suppressTextChanged = true;
            Text = txt.Text;
            _suppressTextChanged = false;
            placeholder.Visibility = string.IsNullOrEmpty(txt.Text) ? Visibility.Visible : Visibility.Collapsed;
            UpdateCounter();
            TextChanged?.Invoke(this, e);
        }

        private void txt_GotFocus(object sender, RoutedEventArgs e)
        {
            border.BorderBrush = (Brush)FindResource("OutlineFocus");
            border.BorderThickness = new Thickness(2);
        }

        private void txt_LostFocus(object sender, RoutedEventArgs e)
        {
            var hasError = !string.IsNullOrEmpty(ErrorText);
            border.BorderBrush = hasError ? (Brush)FindResource("Danger") : (Brush)FindResource("OutlineDefault");
            border.BorderThickness = new Thickness(1);
        }
    }
}
