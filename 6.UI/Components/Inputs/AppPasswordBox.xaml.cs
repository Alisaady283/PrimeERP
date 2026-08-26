using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace PrimeERP.UI.Components.Inputs
{
    /// <summary>نفس بنية AppTextBox بالضبط (نفس الأنماط: ارتفاع/حشو/حد/نصف قطر/حالات تركيز وخطأ) لكن
    /// PasswordBox داخلياً — WPF يمنع ربط PasswordBox.Password عبر DependencyProperty عادية لأسباب أمنية
    /// (ليست DP أصلاً)، فـPassword هنا خاصية CLR للقراءة فقط تُفوَّض للعنصر الداخلي مباشرة، بلا Binding ثنائي
    /// الاتجاه — نفس ما كان LoginWindow يفعله يدوياً مع PasswordBox الخام (txtPassword.Password) قبل هذه القطعة.</summary>
    public partial class AppPasswordBox : UserControl
    {
        public static readonly DependencyProperty LabelProperty =
            DependencyProperty.Register(nameof(Label), typeof(string), typeof(AppPasswordBox),
                new PropertyMetadata("", OnLabelChanged));

        public static readonly DependencyProperty PlaceholderProperty =
            DependencyProperty.Register(nameof(Placeholder), typeof(string), typeof(AppPasswordBox),
                new PropertyMetadata("", OnPlaceholderChanged));

        public static readonly DependencyProperty ErrorTextProperty =
            DependencyProperty.Register(nameof(ErrorText), typeof(string), typeof(AppPasswordBox),
                new PropertyMetadata(null, OnErrorChanged));

        public static readonly DependencyProperty HasErrorProperty =
            DependencyProperty.Register(nameof(HasError), typeof(bool), typeof(AppPasswordBox),
                new PropertyMetadata(false));

        public static readonly DependencyProperty IsRequiredProperty =
            DependencyProperty.Register(nameof(IsRequired), typeof(bool), typeof(AppPasswordBox),
                new PropertyMetadata(false, OnLabelChanged));

        public static readonly DependencyProperty PrefixIconProperty =
            DependencyProperty.Register(nameof(PrefixIcon), typeof(Geometry), typeof(AppPasswordBox),
                new PropertyMetadata(null, OnPrefixIconChanged));

        public static readonly DependencyProperty SuffixIconProperty =
            DependencyProperty.Register(nameof(SuffixIcon), typeof(Geometry), typeof(AppPasswordBox),
                new PropertyMetadata(null, OnSuffixIconChanged));

        public string   Label       { get => (string)GetValue(LabelProperty);       set => SetValue(LabelProperty, value); }
        public string   Placeholder { get => (string)GetValue(PlaceholderProperty); set => SetValue(PlaceholderProperty, value); }
        public string   ErrorText   { get => (string)GetValue(ErrorTextProperty);   set => SetValue(ErrorTextProperty, value); }
        public bool     HasError    { get => (bool)GetValue(HasErrorProperty);      private set => SetValue(HasErrorProperty, value); }
        public bool     IsRequired  { get => (bool)GetValue(IsRequiredProperty);    set => SetValue(IsRequiredProperty, value); }
        public Geometry PrefixIcon  { get => (Geometry)GetValue(PrefixIconProperty);set => SetValue(PrefixIconProperty, value); }
        public Geometry SuffixIcon  { get => (Geometry)GetValue(SuffixIconProperty);set => SetValue(SuffixIconProperty, value); }

        /// <summary>للقراءة فقط — راجع تعليق التوثيق أعلى الكلاس.</summary>
        public string Password => pwd.Password;

        public event RoutedEventHandler PasswordChanged;

        public AppPasswordBox()
        {
            InitializeComponent();
        }

        public void Clear() => pwd.Clear();

        private static void OnLabelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var c = (AppPasswordBox)d;
            c.txtLabel.Text = c.Label;
            c.pnlLabel.Visibility = string.IsNullOrEmpty(c.Label) ? Visibility.Collapsed : Visibility.Visible;
            c.txtRequired.Visibility = c.IsRequired ? Visibility.Visible : Visibility.Collapsed;
        }

        private static void OnPlaceholderChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var c = (AppPasswordBox)d;
            c.placeholder.Text = c.Placeholder;
        }

        private static void OnErrorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var c = (AppPasswordBox)d;
            var hasError = !string.IsNullOrEmpty(c.ErrorText);
            c.txtError.Text = c.ErrorText;
            c.txtError.Visibility = hasError ? Visibility.Visible : Visibility.Collapsed;
            c.HasError = hasError;
        }

        private static void OnPrefixIconChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var c = (AppPasswordBox)d;
            c.prefixIcon.Data = c.PrefixIcon;
            c.prefixIcon.Visibility = c.PrefixIcon != null ? Visibility.Visible : Visibility.Collapsed;
        }

        private static void OnSuffixIconChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var c = (AppPasswordBox)d;
            c.suffixIcon.Data = c.SuffixIcon;
            c.suffixIcon.Visibility = c.SuffixIcon != null ? Visibility.Visible : Visibility.Collapsed;
        }

        private void pwd_PasswordChanged(object sender, RoutedEventArgs e)
        {
            placeholder.Visibility = string.IsNullOrEmpty(pwd.Password) ? Visibility.Visible : Visibility.Collapsed;
            PasswordChanged?.Invoke(this, e);
        }
    }
}
