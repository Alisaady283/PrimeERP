using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace PrimeERP.UI.Components.Inputs
{
    public partial class AppCheckBox : UserControl
    {
        public static readonly DependencyProperty IsCheckedProperty =
            DependencyProperty.Register(nameof(IsChecked), typeof(bool), typeof(AppCheckBox),
                new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnStateChanged));

        public static readonly DependencyProperty IsIndeterminateProperty =
            DependencyProperty.Register(nameof(IsIndeterminate), typeof(bool), typeof(AppCheckBox),
                new PropertyMetadata(false, OnStateChanged));

        public static readonly DependencyProperty LabelProperty =
            DependencyProperty.Register(nameof(Label), typeof(string), typeof(AppCheckBox),
                new PropertyMetadata("", OnLabelChanged));

        public static readonly DependencyProperty DescriptionProperty =
            DependencyProperty.Register(nameof(Description), typeof(string), typeof(AppCheckBox),
                new PropertyMetadata("", OnDescriptionChanged));

        public bool   IsChecked       { get => (bool)GetValue(IsCheckedProperty);       set => SetValue(IsCheckedProperty, value); }
        public bool   IsIndeterminate { get => (bool)GetValue(IsIndeterminateProperty); set => SetValue(IsIndeterminateProperty, value); }
        public string Label           { get => (string)GetValue(LabelProperty);         set => SetValue(LabelProperty, value); }
        public string Description     { get => (string)GetValue(DescriptionProperty);   set => SetValue(DescriptionProperty, value); }

        public event EventHandler CheckedChanged;

        public AppCheckBox()
        {
            InitializeComponent();
        }

        private static void OnStateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
            ((AppCheckBox)d).Render();

        private static void OnLabelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((AppCheckBox)d).txtLabel.Text = (string)e.NewValue;
        }

        private static void OnDescriptionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var c = (AppCheckBox)d;
            c.txtDescription.Text = (string)e.NewValue;
            c.txtDescription.Visibility = string.IsNullOrEmpty((string)e.NewValue) ? Visibility.Collapsed : Visibility.Visible;
        }

        /// <summary>يحدّد أي علامة تظهر (صح/شرطة) من (Checked/Indeterminate) — الشكل نفسه (اللون) من الأنماط.</summary>
        private void Render()
        {
            checkPath.Visibility = IsChecked && !IsIndeterminate ? Visibility.Visible : Visibility.Collapsed;
            dashMark.Visibility  = IsIndeterminate ? Visibility.Visible : Visibility.Collapsed;
        }

        private void Root_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!IsEnabled) return;
            IsIndeterminate = false;
            IsChecked = !IsChecked;
            CheckedChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
