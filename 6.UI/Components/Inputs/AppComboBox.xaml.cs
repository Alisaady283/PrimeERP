using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace PrimeERP.UI.Components.Inputs
{
    public partial class AppComboBox : UserControl
    {
        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register(nameof(ItemsSource), typeof(IEnumerable), typeof(AppComboBox),
                new PropertyMetadata(null, OnItemsSourceChanged));

        public static readonly DependencyProperty SelectedItemProperty =
            DependencyProperty.Register(nameof(SelectedItem), typeof(object), typeof(AppComboBox),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedItemChanged));

        public static readonly DependencyProperty SelectedValueProperty =
            DependencyProperty.Register(nameof(SelectedValue), typeof(object), typeof(AppComboBox),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public static readonly DependencyProperty DisplayMemberPathProperty =
            DependencyProperty.Register(nameof(DisplayMemberPath), typeof(string), typeof(AppComboBox),
                new PropertyMetadata(""));

        public static readonly DependencyProperty SelectedValuePathProperty =
            DependencyProperty.Register(nameof(SelectedValuePath), typeof(string), typeof(AppComboBox),
                new PropertyMetadata(""));

        public static readonly DependencyProperty IsSearchableProperty =
            DependencyProperty.Register(nameof(IsSearchable), typeof(bool), typeof(AppComboBox),
                new PropertyMetadata(true, OnIsSearchableChanged));

        public static readonly DependencyProperty PlaceholderProperty =
            DependencyProperty.Register(nameof(Placeholder), typeof(string), typeof(AppComboBox),
                new PropertyMetadata("", OnPlaceholderChanged));

        public static readonly DependencyProperty AllowClearProperty =
            DependencyProperty.Register(nameof(AllowClear), typeof(bool), typeof(AppComboBox),
                new PropertyMetadata(false));

        public static readonly DependencyProperty LabelProperty =
            DependencyProperty.Register(nameof(Label), typeof(string), typeof(AppComboBox),
                new PropertyMetadata("", OnLabelChanged));

        public static readonly DependencyProperty ErrorTextProperty =
            DependencyProperty.Register(nameof(ErrorText), typeof(string), typeof(AppComboBox),
                new PropertyMetadata(null, OnErrorChanged));

        public static readonly DependencyProperty HasErrorProperty =
            DependencyProperty.Register(nameof(HasError), typeof(bool), typeof(AppComboBox),
                new PropertyMetadata(false));

        public static readonly DependencyProperty IsRequiredProperty =
            DependencyProperty.Register(nameof(IsRequired), typeof(bool), typeof(AppComboBox),
                new PropertyMetadata(false, OnLabelChanged));

        public IEnumerable ItemsSource       { get => (IEnumerable)GetValue(ItemsSourceProperty);   set => SetValue(ItemsSourceProperty, value); }
        public object      SelectedItem      { get => GetValue(SelectedItemProperty);               set => SetValue(SelectedItemProperty, value); }
        public object      SelectedValue     { get => GetValue(SelectedValueProperty);               set => SetValue(SelectedValueProperty, value); }
        public string      DisplayMemberPath { get => (string)GetValue(DisplayMemberPathProperty);   set => SetValue(DisplayMemberPathProperty, value); }
        public string      SelectedValuePath { get => (string)GetValue(SelectedValuePathProperty);   set => SetValue(SelectedValuePathProperty, value); }
        public bool         IsSearchable      { get => (bool)GetValue(IsSearchableProperty);           set => SetValue(IsSearchableProperty, value); }
        public string        Placeholder       { get => (string)GetValue(PlaceholderProperty);          set => SetValue(PlaceholderProperty, value); }
        public bool           AllowClear        { get => (bool)GetValue(AllowClearProperty);             set => SetValue(AllowClearProperty, value); }
        public string          Label             { get => (string)GetValue(LabelProperty);                set => SetValue(LabelProperty, value); }
        public string           ErrorText         { get => (string)GetValue(ErrorTextProperty);            set => SetValue(ErrorTextProperty, value); }

        /// <summary>محسوبة تلقائياً من ErrorText — الأنماط في Themes/Components/Inputs.xaml تقرأها لإظهار حالة الخطأ.</summary>
        public bool             HasError          { get => (bool)GetValue(HasErrorProperty);               private set => SetValue(HasErrorProperty, value); }
        public bool               IsRequired        { get => (bool)GetValue(IsRequiredProperty);             set => SetValue(IsRequiredProperty, value); }

        public event EventHandler SelectionChanged;

        private List<object> _allItems = new();
        private bool _suppressTextChanged;

        // الفتح بالماوس يقع عند الإفلات لا عند الضغط: Popup بـ StaysOpen=False يُغلق نفسه عند أول إفلات
        // خارج حدوده، فلو فُتح أثناء الضغط (GotFocus يسبق MouseUp) أغلقه الإفلات نفسه فوراً — وهذا سبب
        // "تظهر وتختفي خلال ثانية" عند النقر على الحقل.
        private bool _openOnMouseUp;
        private bool _suppressOpen;

        public AppComboBox()
        {
            InitializeComponent();
        }

        private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var c = (AppComboBox)d;
            c._allItems = e.NewValue is IEnumerable src ? src.Cast<object>().ToList() : new List<object>();
        }

        private static void OnSelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var c = (AppComboBox)d;
            c._suppressTextChanged = true;
            c.txtSearch.Text = e.NewValue != null ? c.GetDisplay(e.NewValue) : "";
            c._suppressTextChanged = false;
            c.SelectedValue = e.NewValue != null ? c.GetValueOf(e.NewValue) : null;
            c.btnClear.Visibility = c.AllowClear && e.NewValue != null ? Visibility.Visible : Visibility.Collapsed;
        }

        private static void OnIsSearchableChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((AppComboBox)d).txtSearch.IsReadOnly = !(bool)e.NewValue;
        }

        private static void OnPlaceholderChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var c = (AppComboBox)d;
            if (string.IsNullOrEmpty(c.txtSearch.Text))
                c.txtSearch.Text = "";
        }

        private static void OnLabelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var c = (AppComboBox)d;
            c.txtLabel.Text = c.Label;
            c.pnlLabel.Visibility = string.IsNullOrEmpty(c.Label) ? Visibility.Collapsed : Visibility.Visible;
            c.txtRequired.Visibility = c.IsRequired ? Visibility.Visible : Visibility.Collapsed;
        }

        private static void OnErrorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var c = (AppComboBox)d;
            var hasError = !string.IsNullOrEmpty(c.ErrorText);
            c.txtError.Text = c.ErrorText;
            c.txtError.Visibility = hasError ? Visibility.Visible : Visibility.Collapsed;
            c.HasError = hasError;
        }

        private string GetDisplay(object item)
        {
            if (item == null) return "";
            if (string.IsNullOrEmpty(DisplayMemberPath)) return item.ToString();
            var prop = item.GetType().GetProperty(DisplayMemberPath);
            return prop?.GetValue(item)?.ToString() ?? "";
        }

        private object GetValueOf(object item)
        {
            if (item == null || string.IsNullOrEmpty(SelectedValuePath)) return item;
            var prop = item.GetType().GetProperty(SelectedValuePath);
            return prop?.GetValue(item);
        }

        private void Filter(string text)
        {
            IEnumerable<object> query = _allItems;

            if (IsSearchable && !string.IsNullOrWhiteSpace(text))
            {
                var t = text.Trim();
                query = _allItems.Where(i => GetDisplay(i).Contains(t, StringComparison.OrdinalIgnoreCase));
            }

            var results = query.Take(100).ToList();
            lst.ItemsSource       = results;
            lst.DisplayMemberPath = DisplayMemberPath;
            popup.IsOpen          = results.Count > 0;
        }

        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_suppressTextChanged || !IsSearchable) return;
            Filter(txtSearch.Text);
        }

        private void txtSearch_GotFocus(object sender, RoutedEventArgs e)
        {
            // تركيز لوحة المفاتيح (Tab) يفتح فوراً؛ تركيز الماوس ينتظر الإفلات.
            if (_suppressOpen || Mouse.LeftButton == MouseButtonState.Pressed) return;
            Filter(IsSearchable ? txtSearch.Text : "");
        }

        private void txtSearch_LostFocus(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Background, () =>
            {
                // الضغط داخل القائمة ينقل التركيز خارج الحقل — إغلاقها هنا كان يسبق وصول النقرة للعنصر،
                // فتُغلق بلا اختيار (أول نقرة تفشل والثانية تنجح).
                if (popup.IsMouseOver) return;

                popup.IsOpen = false;
                if (!_suppressTextChanged)
                    txtSearch.Text = SelectedItem != null ? GetDisplay(SelectedItem) : "";
            });
        }

        private void txtSearch_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e) => _openOnMouseUp = true;

        private void txtSearch_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!_openOnMouseUp) return;
            _openOnMouseUp = false;

            if (popup.IsOpen) popup.IsOpen = false;
            else Filter(IsSearchable ? txtSearch.Text : "");
        }

        private void txtSearch_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Down && lst.Items.Count > 0)
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

        // العنصر يُقرأ من الحاوية المضغوطة لا من lst.SelectedItem — الاعتماد على الأخير كان يفشل في أول
        // نقرة لأن التحديد لم يكن قد وصل بعد.
        private void lst_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var container = ItemsControl.ContainerFromElement(lst, (DependencyObject)e.OriginalSource) as ListBoxItem;
            var item = container?.DataContext ?? lst.SelectedItem;
            if (item == null) return;

            SelectItem(item);
            e.Handled = true;
        }

        private void lst_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && lst.SelectedItem != null)
                SelectItem(lst.SelectedItem);
            else if (e.Key == Key.Escape)
                popup.IsOpen = false;
        }

        private void SelectItem(object item)
        {
            SelectedItem = item;
            popup.IsOpen = false;
            _openOnMouseUp = false;

            // إعادة التركيز للحقل بلا إعادة فتح القائمة — GotFocus يفتحها افتراضياً.
            _suppressOpen = true;
            txtSearch.Focus();
            _suppressOpen = false;

            SelectionChanged?.Invoke(this, EventArgs.Empty);
        }

        // عند الإفلات لا الضغط، لنفس سبب حقل البحث: الإفلات خارج Popup مفتوح يغلقه فوراً.
        private void chevron_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var wasOpen = popup.IsOpen;

            _suppressOpen = true;
            txtSearch.Focus();
            _suppressOpen = false;
            _openOnMouseUp = false;

            popup.IsOpen = false;
            if (!wasOpen) Filter(IsSearchable ? txtSearch.Text : "");
            e.Handled = true;
        }

        private void btnClear_Click(object sender, RoutedEventArgs e)
        {
            SelectedItem = null;
            txtSearch.Text = "";
            SelectionChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
