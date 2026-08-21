using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace PrimeERP.Views.Controls.Inputs
{
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
            c.border.BorderBrush = hasError ? (Brush)c.FindResource("CriticalBrush") : (Brush)c.FindResource("OutlineBrush");
        }

        private void ApplyEnabledVisual()
        {
            border.Background  = IsEnabled ? (Brush)FindResource("FieldBrush")   : (Brush)FindResource("MutedBgBrush");
            border.BorderBrush = IsEnabled ? (Brush)FindResource("OutlineBrush") : (Brush)FindResource("MutedBorderBrush");
        }

        private void picker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectedDate = picker.SelectedDate;
            SelectedDateChanged?.Invoke(this, e);
        }

        private void picker_GotFocus(object sender, RoutedEventArgs e)
        {
            border.BorderBrush = (Brush)FindResource("OutlineFocusBrush");
            border.BorderThickness = new Thickness(2);
        }

        private void picker_LostFocus(object sender, RoutedEventArgs e)
        {
            var hasError = !string.IsNullOrEmpty(ErrorText);
            border.BorderBrush = hasError ? (Brush)FindResource("CriticalBrush") : (Brush)FindResource("OutlineBrush");
            border.BorderThickness = new Thickness(1);
        }
    }
}
