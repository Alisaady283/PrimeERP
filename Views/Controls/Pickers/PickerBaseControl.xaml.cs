using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace PrimeERP.Views.Controls.Pickers
{
    /// <summary>
    /// القاعدة غير المعمَّمة لكل Picker — الشكل المضغوط (نص + بحث فوري + زر مسح + زر بحث كامل) وسلوكه الأساسي.
    /// لا تُستخدم مباشرة؛ الطبقة المعمَّمة PickerBase&lt;T&gt; تنفّذ SearchAsync/ResolveExactCodeAsync/OpenSelectionWindow
    /// عبر IPickerDataSource&lt;T&gt;. لا تستدعي أي قاعدة بيانات هنا إطلاقاً.
    /// </summary>
    public partial class PickerBaseControl : UserControl
    {
        public static readonly DependencyProperty SelectedIdProperty =
            DependencyProperty.Register(nameof(SelectedId), typeof(int?), typeof(PickerBaseControl),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public static readonly DependencyProperty SelectedCodeProperty =
            DependencyProperty.Register(nameof(SelectedCode), typeof(string), typeof(PickerBaseControl),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public static readonly DependencyProperty SelectedNameProperty =
            DependencyProperty.Register(nameof(SelectedName), typeof(string), typeof(PickerBaseControl),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public static readonly DependencyProperty DisplayTextProperty =
            DependencyProperty.Register(nameof(DisplayText), typeof(string), typeof(PickerBaseControl),
                new PropertyMetadata(""));

        public static readonly DependencyProperty LabelProperty =
            DependencyProperty.Register(nameof(Label), typeof(string), typeof(PickerBaseControl),
                new PropertyMetadata("", OnLabelChanged));

        public static readonly DependencyProperty PlaceholderProperty =
            DependencyProperty.Register(nameof(Placeholder), typeof(string), typeof(PickerBaseControl),
                new PropertyMetadata("", (d, e) => { }));

        public static readonly DependencyProperty ErrorTextProperty =
            DependencyProperty.Register(nameof(ErrorText), typeof(string), typeof(PickerBaseControl),
                new PropertyMetadata(null, (d, e) => ((PickerBaseControl)d).UpdateErrorDisplay()));

        public static readonly DependencyProperty IsRequiredProperty =
            DependencyProperty.Register(nameof(IsRequired), typeof(bool), typeof(PickerBaseControl),
                new PropertyMetadata(false, OnLabelChanged));

        public static readonly DependencyProperty IsReadOnlyProperty =
            DependencyProperty.Register(nameof(IsReadOnly), typeof(bool), typeof(PickerBaseControl),
                new PropertyMetadata(false, (d, e) => ((PickerBaseControl)d).txt.IsReadOnly = (bool)e.NewValue));

        public static readonly DependencyProperty AllowClearProperty =
            DependencyProperty.Register(nameof(AllowClear), typeof(bool), typeof(PickerBaseControl),
                new PropertyMetadata(true));

        public static readonly DependencyProperty AllowQuickAddProperty =
            DependencyProperty.Register(nameof(AllowQuickAdd), typeof(bool), typeof(PickerBaseControl),
                new PropertyMetadata(false));

        public int?   SelectedId    { get => (int?)GetValue(SelectedIdProperty);      set => SetValue(SelectedIdProperty, value); }
        public string SelectedCode  { get => (string)GetValue(SelectedCodeProperty);  set => SetValue(SelectedCodeProperty, value); }
        public string SelectedName  { get => (string)GetValue(SelectedNameProperty);  set => SetValue(SelectedNameProperty, value); }
        public string DisplayText   { get => (string)GetValue(DisplayTextProperty);   private set => SetValue(DisplayTextProperty, value); }
        public string Label         { get => (string)GetValue(LabelProperty);         set => SetValue(LabelProperty, value); }
        public string Placeholder   { get => (string)GetValue(PlaceholderProperty);   set => SetValue(PlaceholderProperty, value); }
        public string ErrorText     { get => (string)GetValue(ErrorTextProperty);     set => SetValue(ErrorTextProperty, value); }
        public bool   IsRequired    { get => (bool)GetValue(IsRequiredProperty);      set => SetValue(IsRequiredProperty, value); }
        public bool   IsReadOnly    { get => (bool)GetValue(IsReadOnlyProperty);      set => SetValue(IsReadOnlyProperty, value); }
        public bool   AllowClear    { get => (bool)GetValue(AllowClearProperty);      set => SetValue(AllowClearProperty, value); }
        public bool   AllowQuickAdd { get => (bool)GetValue(AllowQuickAddProperty);   set => SetValue(AllowQuickAddProperty, value); }

        public event EventHandler<PickerResultItem> SelectionChanged;
        public event EventHandler QuickAddRequested;

        private readonly DispatcherTimer _debounce;
        private bool _suppressTextChanged;
        private bool _notFound;

        public PickerBaseControl()
        {
            InitializeComponent();

            _debounce = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
            _debounce.Tick += async (s, e) =>
            {
                _debounce.Stop();
                await RunSearchAsync(txt.Text);
            };
        }

        private static void OnLabelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var c = (PickerBaseControl)d;
            c.txtLabel.Text = c.Label;
            c.pnlLabel.Visibility = string.IsNullOrEmpty(c.Label) ? Visibility.Collapsed : Visibility.Visible;
            c.txtRequired.Visibility = c.IsRequired ? Visibility.Visible : Visibility.Collapsed;
        }

        // ===== نقاط الامتداد التي تنفّذها PickerBase<T> =====

        protected virtual Task<IEnumerable<PickerResultItem>> SearchAsync(string term) =>
            Task.FromResult(Enumerable.Empty<PickerResultItem>());

        protected virtual Task<PickerResultItem> ResolveExactCodeAsync(string code) =>
            Task.FromResult<PickerResultItem>(null);

        protected virtual void OpenSelectionWindow() { }

        // ===== السلوك المشترك =====

        private async Task RunSearchAsync(string term)
        {
            if (string.IsNullOrWhiteSpace(term))
            {
                popup.IsOpen = false;
                _notFound = false;
                UpdateErrorDisplay();
                return;
            }

            var exact = await ResolveExactCodeAsync(term);
            if (exact != null)
            {
                SelectResult(exact);
                return;
            }

            var results = (await SearchAsync(term)).Take(50).ToList();

            if (results.Count == 0)
            {
                _notFound = true;
                popup.IsOpen = false;
            }
            else
            {
                _notFound = false;
                lst.ItemsSource = results;
                popup.IsOpen = true;
            }

            UpdateErrorDisplay();
        }

        protected void SelectResult(PickerResultItem item)
        {
            SelectedId   = item?.Id;
            SelectedCode = item?.Code;
            SelectedName = item?.Name;
            DisplayText  = item?.DisplayText ?? "";

            _suppressTextChanged = true;
            txt.Text = DisplayText;
            _suppressTextChanged = false;

            popup.IsOpen = false;
            _notFound = false;
            UpdateErrorDisplay();

            btnClear.Visibility = AllowClear && item != null ? Visibility.Visible : Visibility.Collapsed;
            SelectionChanged?.Invoke(this, item);
        }

        private void UpdateErrorDisplay()
        {
            if (!string.IsNullOrEmpty(ErrorText))
            {
                txtError.Text = ErrorText;
                txtError.Visibility = Visibility.Visible;
                border.BorderBrush = (Brush)FindResource("CriticalBrush");
            }
            else if (_notFound)
            {
                txtError.Text = "غير موجود";
                txtError.Visibility = Visibility.Visible;
                border.BorderBrush = (Brush)FindResource("CautionBrush");
            }
            else
            {
                txtError.Visibility = Visibility.Collapsed;
                border.BorderBrush = (Brush)FindResource("OutlineBrush");
            }
        }

        private void txt_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_suppressTextChanged) return;

            btnClear.Visibility = AllowClear && !string.IsNullOrEmpty(txt.Text) ? Visibility.Visible : Visibility.Collapsed;

            if (string.IsNullOrEmpty(txt.Text) && SelectedId != null)
                SelectResult(null);

            _debounce.Stop();
            _debounce.Start();
        }

        private void txt_GotFocus(object sender, RoutedEventArgs e)
        {
            border.BorderBrush = (Brush)FindResource("OutlineFocusBrush");
            border.BorderThickness = new Thickness(2);
        }

        private void txt_LostFocus(object sender, RoutedEventArgs e)
        {
            border.BorderThickness = new Thickness(1);
            UpdateErrorDisplay();
        }

        private void txt_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F4)
            {
                OpenSelectionWindow();
                e.Handled = true;
            }
            else if (e.Key == Key.Down && lst.Items.Count > 0)
            {
                lst.Focus();
                lst.SelectedIndex = 0;
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                popup.IsOpen = false;
            }
        }

        private void lst_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (lst.SelectedItem is PickerResultItem item)
                SelectResult(item);
        }

        private void lst_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && lst.SelectedItem is PickerResultItem item)
                SelectResult(item);
            else if (e.Key == Key.Escape)
                popup.IsOpen = false;
        }

        private void btnClear_Click(object sender, RoutedEventArgs e) => SelectResult(null);

        private void btnSearch_Click(object sender, RoutedEventArgs e) => OpenSelectionWindow();

        protected void RaiseQuickAddRequested() => QuickAddRequested?.Invoke(this, EventArgs.Empty);

        /// <summary>يفتح نافذة الاختيار الكاملة المناسبة لهذا النوع (شجرة أو جدول) — تُستخدم من مستهلكين خارجيين
        /// (مثل خلايا DocumentLinesGrid المدمجة) بدل تضمين الـ PickerBaseControl كاملاً.</summary>
        public void OpenPicker() => OpenSelectionWindow();

        /// <summary>يبحث عن كود دقيق دون فتح أي نافذة — تُستخدم للملء التلقائي عند الكتابة المباشرة.</summary>
        public Task<PickerResultItem> ResolveCodeAsync(string code) => ResolveExactCodeAsync(code);
    }
}
