using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace PrimeERP.UI.Components.Inputs
{
    public partial class AppNumericBox : UserControl
    {
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(nameof(Value), typeof(decimal), typeof(AppNumericBox),
                new FrameworkPropertyMetadata(0m, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnValueChanged));

        public static readonly DependencyProperty MinProperty =
            DependencyProperty.Register(nameof(Min), typeof(decimal), typeof(AppNumericBox),
                new PropertyMetadata(decimal.MinValue));

        public static readonly DependencyProperty MaxProperty =
            DependencyProperty.Register(nameof(Max), typeof(decimal), typeof(AppNumericBox),
                new PropertyMetadata(decimal.MaxValue));

        public static readonly DependencyProperty DecimalsProperty =
            DependencyProperty.Register(nameof(Decimals), typeof(int), typeof(AppNumericBox),
                new PropertyMetadata(2, OnValueChanged));

        public static readonly DependencyProperty StepProperty =
            DependencyProperty.Register(nameof(Step), typeof(decimal), typeof(AppNumericBox),
                new PropertyMetadata(1m));

        public static readonly DependencyProperty ThousandSeparatorProperty =
            DependencyProperty.Register(nameof(ThousandSeparator), typeof(bool), typeof(AppNumericBox),
                new PropertyMetadata(true, OnValueChanged));

        public static readonly DependencyProperty ShowSpinnerProperty =
            DependencyProperty.Register(nameof(ShowSpinner), typeof(bool), typeof(AppNumericBox),
                new PropertyMetadata(false, OnShowSpinnerChanged));

        public static readonly DependencyProperty AllowNegativeProperty =
            DependencyProperty.Register(nameof(AllowNegative), typeof(bool), typeof(AppNumericBox),
                new PropertyMetadata(true));

        public static readonly DependencyProperty SuffixProperty =
            DependencyProperty.Register(nameof(Suffix), typeof(string), typeof(AppNumericBox),
                new PropertyMetadata("", OnSuffixChanged));

        public static readonly DependencyProperty LabelProperty =
            DependencyProperty.Register(nameof(Label), typeof(string), typeof(AppNumericBox),
                new PropertyMetadata("", OnLabelChanged));

        public static readonly DependencyProperty ErrorTextProperty =
            DependencyProperty.Register(nameof(ErrorText), typeof(string), typeof(AppNumericBox),
                new PropertyMetadata(null, OnErrorChanged));

        public static readonly DependencyProperty IsRequiredProperty =
            DependencyProperty.Register(nameof(IsRequired), typeof(bool), typeof(AppNumericBox),
                new PropertyMetadata(false, OnLabelChanged));

        public decimal Value              { get => (decimal)GetValue(ValueProperty);              set => SetValue(ValueProperty, value); }
        public decimal Min                { get => (decimal)GetValue(MinProperty);                set => SetValue(MinProperty, value); }
        public decimal Max                { get => (decimal)GetValue(MaxProperty);                set => SetValue(MaxProperty, value); }
        public int     Decimals           { get => (int)GetValue(DecimalsProperty);                set => SetValue(DecimalsProperty, value); }
        public decimal Step               { get => (decimal)GetValue(StepProperty);                set => SetValue(StepProperty, value); }
        public bool    ThousandSeparator  { get => (bool)GetValue(ThousandSeparatorProperty);       set => SetValue(ThousandSeparatorProperty, value); }
        public bool    ShowSpinner        { get => (bool)GetValue(ShowSpinnerProperty);             set => SetValue(ShowSpinnerProperty, value); }
        public bool    AllowNegative      { get => (bool)GetValue(AllowNegativeProperty);           set => SetValue(AllowNegativeProperty, value); }
        public string  Suffix             { get => (string)GetValue(SuffixProperty);                set => SetValue(SuffixProperty, value); }
        public string  Label              { get => (string)GetValue(LabelProperty);                 set => SetValue(LabelProperty, value); }
        public string  ErrorText          { get => (string)GetValue(ErrorTextProperty);              set => SetValue(ErrorTextProperty, value); }
        public bool    IsRequired         { get => (bool)GetValue(IsRequiredProperty);               set => SetValue(IsRequiredProperty, value); }

        private bool _isFocused;

        public AppNumericBox()
        {
            InitializeComponent();
            IsEnabledChanged += (s, e) => ApplyEnabledVisual();
        }

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var c = (AppNumericBox)d;
            if (!c._isFocused)
                c.txt.Text = c.FormatValue(c.Value);
        }

        private static void OnShowSpinnerChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((AppNumericBox)d).pnlSpinner.Visibility = (bool)e.NewValue ? Visibility.Visible : Visibility.Collapsed;
        }

        private static void OnSuffixChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var c = (AppNumericBox)d;
            c.txtSuffix.Text = c.Suffix;
            c.txtSuffix.Visibility = string.IsNullOrEmpty(c.Suffix) ? Visibility.Collapsed : Visibility.Visible;
        }

        private static void OnLabelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var c = (AppNumericBox)d;
            c.txtLabel.Text = c.Label;
            c.pnlLabel.Visibility = string.IsNullOrEmpty(c.Label) ? Visibility.Collapsed : Visibility.Visible;
            c.txtRequired.Visibility = c.IsRequired ? Visibility.Visible : Visibility.Collapsed;
        }

        private static void OnErrorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var c = (AppNumericBox)d;
            var hasError = !string.IsNullOrEmpty(c.ErrorText);
            c.txtError.Text = c.ErrorText;
            c.txtError.Visibility = hasError ? Visibility.Visible : Visibility.Collapsed;
            c.border.BorderBrush = hasError ? (Brush)c.FindResource("Danger") : (Brush)c.FindResource("OutlineDefault");
        }

        private string FormatValue(decimal value)
        {
            var format = ThousandSeparator ? $"N{Decimals}" : $"F{Decimals}";
            return value.ToString(format, CultureInfo.InvariantCulture);
        }

        private void ApplyEnabledVisual()
        {
            border.Background  = IsEnabled ? (Brush)FindResource("C.Input.Bg")   : (Brush)FindResource("SurfaceSunken");
            border.BorderBrush = IsEnabled ? (Brush)FindResource("OutlineDefault") : (Brush)FindResource("OutlineSubtle");
        }

        private void txt_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            var pattern = AllowNegative ? @"^-?[0-9]*\.?[0-9]*$" : @"^[0-9]*\.?[0-9]*$";
            var proposed = txt.Text.Remove(txt.SelectionStart, txt.SelectionLength)
                                    .Insert(txt.SelectionStart, e.Text);
            e.Handled = !Regex.IsMatch(proposed, pattern);
        }

        // Value كانت تُضبَط عند LostFocus فقط — فمن يكتب رقماً ثم يضغط "حفظ"/"سحب" مباشرة (بلا مغادرة الحقل)
        // كان يُقرأ منه الرقم القديم. هنا تُضبَط مع كل حرف بلا لمس نص الحقل أثناء الكتابة (التنسيق والقص
        // على الحدود يبقيان في Commit عند مغادرة الحقل).
        private void txt_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!_isFocused) return;

            Value = decimal.TryParse(txt.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed) ? parsed : 0m;
        }

        private void txt_GotFocus(object sender, RoutedEventArgs e)
        {
            _isFocused = true;
            txt.Text = Value == 0 ? "" : Value.ToString($"F{Decimals}", CultureInfo.InvariantCulture);
            border.BorderBrush = (Brush)FindResource("OutlineFocus");
            border.BorderThickness = new Thickness(2);
        }

        private void txt_LostFocus(object sender, RoutedEventArgs e)
        {
            _isFocused = false;
            Commit();

            var hasError = !string.IsNullOrEmpty(ErrorText);
            border.BorderBrush = hasError ? (Brush)FindResource("Danger") : (Brush)FindResource("OutlineDefault");
            border.BorderThickness = new Thickness(1);
        }

        private void Commit()
        {
            if (!decimal.TryParse(txt.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out var value))
                value = 0;

            if (value < Min) value = Min;
            if (value > Max) value = Max;

            Value = value;
            txt.Text = FormatValue(value);
        }

        private void btnUp_Click(object sender, RoutedEventArgs e)
        {
            var value = Value + Step;
            if (value > Max) value = Max;
            Value = value;
        }

        private void btnDown_Click(object sender, RoutedEventArgs e)
        {
            var value = Value - Step;
            if (!AllowNegative && value < 0) value = 0;
            if (value < Min) value = Min;
            Value = value;
        }
    }
}
