using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace PrimeERP.UI.Components.Display
{
    /// <summary>عرض AppCard</summary>
    public partial class AppCard : UserControl
    {
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(nameof(Title), typeof(string), typeof(AppCard),
                new PropertyMetadata("", OnTitleChanged));

        public static readonly DependencyProperty IconProperty =
            DependencyProperty.Register(nameof(Icon), typeof(Geometry), typeof(AppCard),
                new PropertyMetadata(null, OnIconChanged));

        public static readonly DependencyProperty BodyProperty =
            DependencyProperty.Register(nameof(Body), typeof(object), typeof(AppCard),
                new PropertyMetadata(null, OnBodyChanged));

        public static readonly DependencyProperty ActionsProperty =
            DependencyProperty.Register(nameof(Actions), typeof(object), typeof(AppCard),
                new PropertyMetadata(null, OnActionsChanged));

        public static readonly DependencyProperty IsCollapsibleProperty =
            DependencyProperty.Register(nameof(IsCollapsible), typeof(bool), typeof(AppCard),
                new PropertyMetadata(false, OnIsCollapsibleChanged));

        public static readonly DependencyProperty IsExpandedProperty =
            DependencyProperty.Register(nameof(IsExpanded), typeof(bool), typeof(AppCard),
                new PropertyMetadata(true, OnIsExpandedChanged));

        public string Title         { get => (string)GetValue(TitleProperty);         set => SetValue(TitleProperty, value); }
        public Geometry Icon        { get => (Geometry)GetValue(IconProperty);        set => SetValue(IconProperty, value); }
        public object Body          { get => GetValue(BodyProperty);                  set => SetValue(BodyProperty, value); }
        public object Actions       { get => GetValue(ActionsProperty);               set => SetValue(ActionsProperty, value); }
        public bool   IsCollapsible { get => (bool)GetValue(IsCollapsibleProperty);    set => SetValue(IsCollapsibleProperty, value); }
        public bool   IsExpanded    { get => (bool)GetValue(IsExpandedProperty);       set => SetValue(IsExpandedProperty, value); }

        public AppCard()
        {
            InitializeComponent();
        }

        private static void OnTitleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
            ((AppCard)d).txtTitle.Text = (string)e.NewValue;

        private static void OnIconChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var c = (AppCard)d;
            c.icon.Data = (Geometry)e.NewValue;
            c.icon.Visibility = e.NewValue != null ? Visibility.Visible : Visibility.Collapsed;
        }

        private static void OnBodyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
            ((AppCard)d).bodyPresenter.Content = e.NewValue;

        private static void OnActionsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
            ((AppCard)d).actionsPresenter.Content = e.NewValue;

        private static void OnIsCollapsibleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var c = (AppCard)d;
            c.btnCollapse.Visibility = (bool)e.NewValue ? Visibility.Visible : Visibility.Collapsed;
            c.header.Cursor = (bool)e.NewValue ? System.Windows.Input.Cursors.Hand : System.Windows.Input.Cursors.Arrow;
        }

        private static void OnIsExpandedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var c = (AppCard)d;
            var expanded = (bool)e.NewValue;
            c.separator.Visibility = expanded ? Visibility.Visible : Visibility.Collapsed;
            c.bodyPresenter.Visibility = expanded ? Visibility.Visible : Visibility.Collapsed;

            var transform = new RotateTransform(expanded ? 0 : -90);
            c.collapseIcon.RenderTransform = transform;
            c.collapseIcon.RenderTransformOrigin = new Point(0.5, 0.5);
        }

        private void btnCollapse_Click(object sender, RoutedEventArgs e) => IsExpanded = !IsExpanded;

        private void Header_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (IsCollapsible) IsExpanded = !IsExpanded;
        }
    }
}
