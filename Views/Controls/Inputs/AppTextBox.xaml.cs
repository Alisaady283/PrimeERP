using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace PrimeERP.Views.Controls.Inputs
{
    public partial class AppTextBox : UserControl
    {
        public static readonly DependencyProperty LabelProperty =
            DependencyProperty.Register(nameof(Label), typeof(string), typeof(AppTextBox),
                new PropertyMetadata("", OnLabelChanged));

        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register(nameof(Text), typeof(string), typeof(AppTextBox),
                new FrameworkPropertyMetadata("", FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnTextChanged));

        public static readonly DependencyProperty PlaceholderProperty =
            DependencyProperty.Register(nameof(Placeholder), typeof(string), typeof(AppTextBox),
                new PropertyMetadata("", OnPlaceholderChanged));

        public static readonly DependencyProperty ErrorTextProperty =
            DependencyProperty.Register(nameof(ErrorText), typeof(string), typeof(AppTextBox),
                new PropertyMetadata(null, OnErrorChanged));

        public static readonly DependencyProperty HasErrorProperty =
            DependencyProperty.Register(nameof(HasError), typeof(bool), typeof(AppTextBox),
                new PropertyMetadata(false));

        public static readonly DependencyProperty IsRequiredProperty =
            DependencyProperty.Register(nameof(IsRequired), typeof(bool), typeof(AppTextBox),
                new PropertyMetadata(false, OnLabelChanged));

        public static readonly DependencyProperty PrefixIconProperty =
            DependencyProperty.Register(nameof(PrefixIcon), typeof(Geometry), typeof(AppTextBox),
                new PropertyMetadata(null, OnPrefixIconChanged));

        public static readonly DependencyProperty SuffixIconProperty =
            DependencyProperty.Register(nameof(SuffixIcon), typeof(Geometry), typeof(AppTextBox),
                new PropertyMetadata(null, OnSuffixIconChanged));

        public static readonly DependencyProperty IsReadOnlyProperty =
            DependencyProperty.Register(nameof(IsReadOnly), typeof(bool), typeof(AppTextBox),
                new PropertyMetadata(false, OnIsReadOnlyChanged));

        public static readonly DependencyProperty MaxLengthProperty =
            DependencyProperty.Register(nameof(MaxLength), typeof(int), typeof(AppTextBox),
                new PropertyMetadata(0, OnMaxLengthChanged));

        public string   Label       { get => (string)GetValue(LabelProperty);       set => SetValue(LabelProperty, value); }
        public string   Text        { get => (string)GetValue(TextProperty);        set => SetValue(TextProperty, value); }
        public string   Placeholder { get => (string)GetValue(PlaceholderProperty); set => SetValue(PlaceholderProperty, value); }
        public string   ErrorText   { get => (string)GetValue(ErrorTextProperty);   set => SetValue(ErrorTextProperty, value); }

        /// <summary>محسوبة تلقائياً من ErrorText — الأنماط في Themes/Components/Inputs.xaml تقرأها لإظهار حالة الخطأ (لا لون هنا، فقط منطق).</summary>
        public bool     HasError    { get => (bool)GetValue(HasErrorProperty);      private set => SetValue(HasErrorProperty, value); }
        public bool     IsRequired  { get => (bool)GetValue(IsRequiredProperty);    set => SetValue(IsRequiredProperty, value); }
        public Geometry PrefixIcon  { get => (Geometry)GetValue(PrefixIconProperty);set => SetValue(PrefixIconProperty, value); }
        public Geometry SuffixIcon  { get => (Geometry)GetValue(SuffixIconProperty);set => SetValue(SuffixIconProperty, value); }
        public bool     IsReadOnly  { get => (bool)GetValue(IsReadOnlyProperty);    set => SetValue(IsReadOnlyProperty, value); }
        public int      MaxLength   { get => (int)GetValue(MaxLengthProperty);      set => SetValue(MaxLengthProperty, value); }

        public event TextChangedEventHandler TextChanged;

        private bool _suppressTextChanged;

        public AppTextBox()
        {
            InitializeComponent();
        }

        private static void OnLabelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var c = (AppTextBox)d;
            c.txtLabel.Text = c.Label;
            c.pnlLabel.Visibility = string.IsNullOrEmpty(c.Label) ? Visibility.Collapsed : Visibility.Visible;
            c.txtRequired.Visibility = c.IsRequired ? Visibility.Visible : Visibility.Collapsed;
        }

        private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var c = (AppTextBox)d;
            if (c._suppressTextChanged) return;
            c._suppressTextChanged = true;
            c.txt.Text = (string)e.NewValue ?? "";
            c._suppressTextChanged = false;
            c.placeholder.Visibility = string.IsNullOrEmpty(c.txt.Text) ? Visibility.Visible : Visibility.Collapsed;
        }

        private static void OnPlaceholderChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var c = (AppTextBox)d;
            c.placeholder.Text = c.Placeholder;
        }

        private static void OnErrorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var c = (AppTextBox)d;
            var hasError = !string.IsNullOrEmpty(c.ErrorText);
            c.txtError.Text = c.ErrorText;
            c.txtError.Visibility = hasError ? Visibility.Visible : Visibility.Collapsed;
            c.HasError = hasError;
        }

        private static void OnPrefixIconChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var c = (AppTextBox)d;
            c.prefixIcon.Data = c.PrefixIcon;
            c.prefixIcon.Visibility = c.PrefixIcon != null ? Visibility.Visible : Visibility.Collapsed;
        }

        private static void OnSuffixIconChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var c = (AppTextBox)d;
            c.suffixIcon.Data = c.SuffixIcon;
            c.suffixIcon.Visibility = c.SuffixIcon != null ? Visibility.Visible : Visibility.Collapsed;
        }

        private static void OnIsReadOnlyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((AppTextBox)d).txt.IsReadOnly = (bool)e.NewValue;
        }

        private static void OnMaxLengthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((AppTextBox)d).txt.MaxLength = (int)e.NewValue;
        }

        private void txt_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_suppressTextChanged) return;
            _suppressTextChanged = true;
            Text = txt.Text;
            _suppressTextChanged = false;
            placeholder.Visibility = string.IsNullOrEmpty(txt.Text) ? Visibility.Visible : Visibility.Collapsed;
            TextChanged?.Invoke(this, e);
        }
    }
}
