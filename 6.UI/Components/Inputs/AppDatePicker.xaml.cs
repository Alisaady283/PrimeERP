using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace PrimeERP.UI.Components.Inputs
{
    /// <summary>حقل إدخال AppDatePicker</summary>
    public partial class AppDatePicker : UserControl
    {
        public static readonly DependencyProperty SelectedDateProperty =
            DependencyProperty.Register(nameof(SelectedDate), typeof(DateTime?), typeof(AppDatePicker),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedDateChanged));

        public static readonly DependencyProperty MinProperty =
            DependencyProperty.Register(nameof(Min), typeof(DateTime?), typeof(AppDatePicker),
                new PropertyMetadata(null, OnMinChanged));

        public static readonly DependencyProperty MaxProperty =
            DependencyProperty.Register(nameof(Max), typeof(DateTime?), typeof(AppDatePicker),
                new PropertyMetadata(null, OnMaxChanged));

        public static readonly DependencyProperty LabelProperty =
            DependencyProperty.Register(nameof(Label), typeof(string), typeof(AppDatePicker),
                new PropertyMetadata("", OnLabelChanged));

        public static readonly DependencyProperty ErrorTextProperty =
            DependencyProperty.Register(nameof(ErrorText), typeof(string), typeof(AppDatePicker),
                new PropertyMetadata(null, OnErrorChanged));

        public static readonly DependencyProperty IsRequiredProperty =
            DependencyProperty.Register(nameof(IsRequired), typeof(bool), typeof(AppDatePicker),
                new PropertyMetadata(false, OnLabelChanged));

        public DateTime? SelectedDate { get => (DateTime?)GetValue(SelectedDateProperty); set => SetValue(SelectedDateProperty, value); }
        public DateTime? Min          { get => (DateTime?)GetValue(MinProperty);           set => SetValue(MinProperty, value); }
        public DateTime? Max          { get => (DateTime?)GetValue(MaxProperty);           set => SetValue(MaxProperty, value); }
        public string     Label        { get => (string)GetValue(LabelProperty);             set => SetValue(LabelProperty, value); }
        public string      ErrorText    { get => (string)GetValue(ErrorTextProperty);          set => SetValue(ErrorTextProperty, value); }
        public bool          IsRequired   { get => (bool)GetValue(IsRequiredProperty);           set => SetValue(IsRequiredProperty, value); }

        public event EventHandler<SelectionChangedEventArgs> SelectedDateChanged;

        public AppDatePicker()
        {
            InitializeComponent();
            IsEnabledChanged += (s, e) => ApplyEnabledVisual();
            Loaded += (_, __) => AttachTextBox();
        }

        private void AttachTextBox()
        {
            var box = picker.Template?.FindName("PART_TextBox", picker) as System.Windows.Controls.Primitives.DatePickerTextBox;
            if (box == null || _textBox != null) return;

            _textBox = box;
            _textBox.PreviewTextInput += TextBox_PreviewTextInput;
            System.Windows.DataObject.AddPastingHandler(_textBox, (_, e) => e.CancelCommand());
        }

        private System.Windows.Controls.Primitives.DatePickerTextBox _textBox;

        private void TextBox_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            if (e.Text.Length != 1 || !char.IsDigit(e.Text[0])) { e.Handled = true; return; }

            var box = (System.Windows.Controls.TextBox)sender;
            var digits = new string(box.Text.Where(char.IsDigit).ToArray());
            if (box.SelectionLength == 0 && digits.Length >= 8) { e.Handled = true; return; }

            if (box.SelectionLength > 0) digits = "";
            digits += e.Text;

            box.Text = FormatDigits(digits);
            box.CaretIndex = box.Text.Length;
            e.Handled = true;
        }

        private static string FormatDigits(string digits)
        {
            if (digits.Length <= 2) return digits;
            if (digits.Length <= 4) return $"{digits[..2]}/{digits[2..]}";

            return $"{digits[..2]}/{digits[2..4]}/{digits[4..]}";
        }

        private static void OnSelectedDateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var c = (AppDatePicker)d;
            if (c.picker.SelectedDate != (DateTime?)e.NewValue)
                c.picker.SelectedDate = (DateTime?)e.NewValue;
        }

        private static void OnMinChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((AppDatePicker)d).picker.DisplayDateStart = (DateTime?)e.NewValue;
        }

        private static void OnMaxChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((AppDatePicker)d).picker.DisplayDateEnd = (DateTime?)e.NewValue;
        }

        private static void OnLabelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var c = (AppDatePicker)d;
            c.txtLabel.Text = c.Label;
            c.pnlLabel.Visibility = string.IsNullOrEmpty(c.Label) ? Visibility.Collapsed : Visibility.Visible;
            c.txtRequired.Visibility = c.IsRequired ? Visibility.Visible : Visibility.Collapsed;
        }

        private static void OnErrorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var c = (AppDatePicker)d;
            var hasError = !string.IsNullOrEmpty(c.ErrorText);
            c.txtError.Text = c.ErrorText;
            c.txtError.Visibility = hasError ? Visibility.Visible : Visibility.Collapsed;
            c.border.BorderBrush = hasError ? (Brush)c.FindResource("Danger") : (Brush)c.FindResource("OutlineDefault");
        }

        private void ApplyEnabledVisual()
        {
            border.Background  = IsEnabled ? (Brush)FindResource("SurfaceDefault")   : (Brush)FindResource("SurfaceSunken");
            border.BorderBrush = IsEnabled ? (Brush)FindResource("OutlineDefault") : (Brush)FindResource("OutlineSubtle");
        }

        private void picker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectedDate = picker.SelectedDate;
            SelectedDateChanged?.Invoke(this, e);
        }

        private void picker_GotFocus(object sender, RoutedEventArgs e)
        {
            border.BorderBrush = (Brush)FindResource("OutlineFocus");
            border.BorderThickness = new Thickness(2);
        }

        private void picker_LostFocus(object sender, RoutedEventArgs e)
        {
            var hasError = !string.IsNullOrEmpty(ErrorText);
            border.BorderBrush = hasError ? (Brush)FindResource("Danger") : (Brush)FindResource("OutlineDefault");
            border.BorderThickness = new Thickness(1);
        }
    }
}
