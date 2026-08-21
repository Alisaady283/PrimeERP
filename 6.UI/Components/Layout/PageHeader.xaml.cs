using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace PrimeERP.UI.Components.Layout
{
    public partial class PageHeader : UserControl
    {
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(nameof(Title), typeof(string), typeof(PageHeader),
                new PropertyMetadata("", OnTitleChanged));

        public static readonly DependencyProperty SubtitleProperty =
            DependencyProperty.Register(nameof(Subtitle), typeof(string), typeof(PageHeader),
                new PropertyMetadata("", OnSubtitleChanged));

        public static readonly DependencyProperty IconProperty =
            DependencyProperty.Register(nameof(Icon), typeof(Geometry), typeof(PageHeader),
                new PropertyMetadata(null, OnIconChanged));

        public static readonly DependencyProperty ActionsContentProperty =
            DependencyProperty.Register(nameof(ActionsContent), typeof(object), typeof(PageHeader),
                new PropertyMetadata(null, OnActionsContentChanged));

        public string   Title          { get => (string)GetValue(TitleProperty);          set => SetValue(TitleProperty, value); }
        public string   Subtitle       { get => (string)GetValue(SubtitleProperty);        set => SetValue(SubtitleProperty, value); }
        public Geometry Icon           { get => (Geometry)GetValue(IconProperty);          set => SetValue(IconProperty, value); }
        public object   ActionsContent { get => GetValue(ActionsContentProperty);          set => SetValue(ActionsContentProperty, value); }

        public PageHeader()
        {
            InitializeComponent();
        }

        private static void OnTitleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
            ((PageHeader)d).txtTitle.Text = (string)e.NewValue;

        private static void OnSubtitleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var c = (PageHeader)d;
            c.txtSubtitle.Text = (string)e.NewValue;
            c.txtSubtitle.Visibility = string.IsNullOrEmpty((string)e.NewValue) ? Visibility.Collapsed : Visibility.Visible;
        }

        private static void OnIconChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var c = (PageHeader)d;
            c.icon.Data = (Geometry)e.NewValue;
            c.iconWrap.Visibility = e.NewValue != null ? Visibility.Visible : Visibility.Collapsed;
        }

        private static void OnActionsContentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
            ((PageHeader)d).actionsPresenter.Content = e.NewValue;
    }
}
