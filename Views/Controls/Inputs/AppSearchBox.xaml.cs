using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace PrimeERP.Views.Controls.Inputs
{
    public partial class AppSearchBox : UserControl
    {
        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register(nameof(Text), typeof(string), typeof(AppSearchBox),
                new FrameworkPropertyMetadata("", FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnTextChanged));

        public static readonly DependencyProperty PlaceholderProperty =
            DependencyProperty.Register(nameof(Placeholder), typeof(string), typeof(AppSearchBox),
                new PropertyMetadata("", OnPlaceholderChanged));

        public static readonly DependencyProperty DelayMsProperty =
            DependencyProperty.Register(nameof(DelayMs), typeof(int), typeof(AppSearchBox),
                new PropertyMetadata(300));

        public static readonly DependencyProperty ShowClearButtonProperty =
            DependencyProperty.Register(nameof(ShowClearButton), typeof(bool), typeof(AppSearchBox),
                new PropertyMetadata(true));

        public string Text             { get => (string)GetValue(TextProperty);        set => SetValue(TextProperty, value); }
        public string Placeholder      { get => (string)GetValue(PlaceholderProperty);  set => SetValue(PlaceholderProperty, value); }
        public int    DelayMs          { get => (int)GetValue(DelayMsProperty);         set => SetValue(DelayMsProperty, value); }
        public bool   ShowClearButton  { get => (bool)GetValue(ShowClearButtonProperty);set => SetValue(ShowClearButtonProperty, value); }

        /// <summary>يُطلق بعد توقّف الكتابة لمدة DelayMs — استخدمه للبحث الفعلي بدل TextChanged المباشر.</summary>
        public event EventHandler<string> Search;

        private readonly DispatcherTimer _debounce;
        private bool _suppressTextChanged;

        public AppSearchBox()
        {
            InitializeComponent();

            _debounce = new DispatcherTimer();
            _debounce.Tick += (s, e) =>
            {
                _debounce.Stop();
                Search?.Invoke(this, Text);
            };
        }

        private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var c = (AppSearchBox)d;
            if (c._suppressTextChanged) return;
            c._suppressTextChanged = true;
            c.txt.Text = (string)e.NewValue ?? "";
            c._suppressTextChanged = false;
            c.UpdateChrome();
        }

        private static void OnPlaceholderChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((AppSearchBox)d).placeholder.Text = (string)e.NewValue;
        }

        private void UpdateChrome()
        {
            placeholder.Visibility = string.IsNullOrEmpty(txt.Text) ? Visibility.Visible : Visibility.Collapsed;
            btnClear.Visibility = ShowClearButton && !string.IsNullOrEmpty(txt.Text)
                ? Visibility.Visible : Visibility.Collapsed;
        }

        private void txt_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_suppressTextChanged) return;
            _suppressTextChanged = true;
            Text = txt.Text;
            _suppressTextChanged = false;
            UpdateChrome();

            _debounce.Stop();
            _debounce.Interval = TimeSpan.FromMilliseconds(Math.Max(0, DelayMs));
            _debounce.Start();
        }

        private void btnClear_Click(object sender, RoutedEventArgs e)
        {
            Text = "";
            txt.Focus();
            _debounce.Stop();
            Search?.Invoke(this, "");
        }
    }
}
