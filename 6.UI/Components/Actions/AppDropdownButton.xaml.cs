using System;
using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace PrimeERP.UI.Components.Actions
{
    /// <summary>أزرار AppDropdownButton</summary>
    public partial class AppDropdownButton : UserControl
    {
        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register(nameof(Text), typeof(string), typeof(AppDropdownButton),
                new PropertyMetadata("", (d, e) => ((AppDropdownButton)d).txt.Text = (string)e.NewValue));

        public static readonly DependencyProperty ItemsProperty =
            DependencyProperty.Register(nameof(Items), typeof(IEnumerable), typeof(AppDropdownButton),
                new PropertyMetadata(null, (d, e) => ((AppDropdownButton)d).lst.ItemsSource = (IEnumerable)e.NewValue));

        public static readonly DependencyProperty DisplayMemberPathProperty =
            DependencyProperty.Register(nameof(DisplayMemberPath), typeof(string), typeof(AppDropdownButton),
                new PropertyMetadata("", (d, e) => ((AppDropdownButton)d).lst.DisplayMemberPath = (string)e.NewValue));

        public string      Text              { get => (string)GetValue(TextProperty);              set => SetValue(TextProperty, value); }
        public IEnumerable Items             { get => (IEnumerable)GetValue(ItemsProperty);         set => SetValue(ItemsProperty, value); }
        public string      DisplayMemberPath { get => (string)GetValue(DisplayMemberPathProperty);  set => SetValue(DisplayMemberPathProperty, value); }

        public event EventHandler<object> ItemSelected;

        public AppDropdownButton()
        {
            InitializeComponent();
        }

        private void btn_Click(object sender, RoutedEventArgs e) => popup.IsOpen = true;

        private void lst_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (lst.SelectedItem == null) return;
            ItemSelected?.Invoke(this, lst.SelectedItem);
            popup.IsOpen = false;
        }
    }
}
