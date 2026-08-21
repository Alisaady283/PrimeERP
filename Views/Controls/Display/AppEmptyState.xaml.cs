using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace PrimeERP.Views.Controls.Display
{
    public partial class AppEmptyState : UserControl
    {
        public static readonly DependencyProperty IconProperty =
            DependencyProperty.Register(nameof(Icon), typeof(Geometry), typeof(AppEmptyState),
                new PropertyMetadata(null, OnIconChanged));

        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(nameof(Title), typeof(string), typeof(AppEmptyState),
                new PropertyMetadata("", OnTitleChanged));

        public static readonly DependencyProperty MessageProperty =
            DependencyProperty.Register(nameof(Message), typeof(string), typeof(AppEmptyState),
                new PropertyMetadata("", OnMessageChanged));

        public static readonly DependencyProperty ActionTextProperty =
            DependencyProperty.Register(nameof(ActionText), typeof(string), typeof(AppEmptyState),
                new PropertyMetadata("", OnActionTextChanged));

        public static readonly DependencyProperty ActionCommandProperty =
            DependencyProperty.Register(nameof(ActionCommand), typeof(ICommand), typeof(AppEmptyState),
                new PropertyMetadata(null));

        public Geometry Icon          { get => (Geometry)GetValue(IconProperty);        set => SetValue(IconProperty, value); }
        public string   Title         { get => (string)GetValue(TitleProperty);         set => SetValue(TitleProperty, value); }
        public string   Message       { get => (string)GetValue(MessageProperty);       set => SetValue(MessageProperty, value); }
        public string   ActionText    { get => (string)GetValue(ActionTextProperty);    set => SetValue(ActionTextProperty, value); }
        public ICommand ActionCommand { get => (ICommand)GetValue(ActionCommandProperty); set => SetValue(ActionCommandProperty, value); }

        public AppEmptyState()
        {
            InitializeComponent();
        }

        private static void OnIconChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
            ((AppEmptyState)d).icon.Data = (Geometry)e.NewValue;

        private static void OnTitleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var c = (AppEmptyState)d;
            c.txtTitle.Text = (string)e.NewValue;
            c.txtTitle.Visibility = string.IsNullOrEmpty((string)e.NewValue) ? Visibility.Collapsed : Visibility.Visible;
        }

        private static void OnMessageChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var c = (AppEmptyState)d;
            c.txtMessage.Text = (string)e.NewValue;
            c.txtMessage.Visibility = string.IsNullOrEmpty((string)e.NewValue) ? Visibility.Collapsed : Visibility.Visible;
        }

        private static void OnActionTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var c = (AppEmptyState)d;
            c.txtAction.Text = (string)e.NewValue;
            c.btnAction.Visibility = string.IsNullOrEmpty((string)e.NewValue) ? Visibility.Collapsed : Visibility.Visible;
        }

        private void btnAction_Click(object sender, RoutedEventArgs e)
        {
            if (ActionCommand?.CanExecute(null) == true)
                ActionCommand.Execute(null);
        }
    }
}
