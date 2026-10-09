using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using PrimeERP.Domain.Results;
using PrimeERP.UI.Converters;

namespace PrimeERP.UI.Components.Feedback
{
    /// <summary>القاعدة الموحّدة لكل نوافذ الحوار</summary>
    public partial class AppDialogWindow : Window
    {
        public AppDialogWindow()
        {
            InitializeComponent();
            FlowDirection = PrimeERP.Platform.Localization.LocalizationService.Flow;
            var active = FindActiveWindow();
            if (Owner == null && active != this) Owner = active;
            const double Chrome = 150;   // الرأس والفوتر
            MaxHeight = SystemParameters.WorkArea.Height * 0.92;
            contentScroll.MaxHeight = MaxHeight - VerticalGutter - Chrome;
            CardWidth = (double)FindResource("C.Dialog.Width.Sm");

            card.SizeChanged += (_, __) => ClipCorners();
            SizeChanged += (_, e) => { if (e.HeightChanged) KeepOnScreen(); };
        }

        public void FillHeight()
        {
            SizeToContent = SizeToContent.Manual;
            Height = MaxHeight;
            card.RowDefinitions[1].Height = new GridLength(1, GridUnitType.Star);
            contentScroll.MaxHeight = double.PositiveInfinity;
        }

        private void KeepOnScreen()
        {
            var area = SystemParameters.WorkArea;
            if (Top + ActualHeight > area.Bottom) Top = Math.Max(area.Top, area.Bottom - ActualHeight);
        }

        private double HorizontalGutter => shell.Margin.Left + shell.Margin.Right;
        private double VerticalGutter   => shell.Margin.Top + shell.Margin.Bottom;

        public double CardWidth
        {
            get => (double.IsNaN(Width) ? ActualWidth : Width) - HorizontalGutter;
            set { Width = value + HorizontalGutter; MinWidth = Width; }
        }

        public double CardHeight
        {
            set { SizeToContent = SizeToContent.Manual; Height = value + VerticalGutter; }
        }

        private void ClipCorners()
        {
            var radius = Math.Max(0, shell.CornerRadius.TopLeft - shell.BorderThickness.Left);
            card.Clip = new RectangleGeometry(new Rect(card.RenderSize), radius, radius);
        }

        protected string HeaderTitle
        {
            set => txtHeaderTitle.Text = value;
        }

        protected string HeaderSubtitle
        {
            set
            {
                txtHeaderSubtitle.Text = value;
                txtHeaderSubtitle.Visibility = string.IsNullOrEmpty(value) ? Visibility.Collapsed : Visibility.Visible;
            }
        }

        protected Geometry HeaderIcon
        {
            set
            {
                headerIcon.Data = value;
                headerIconWrap.Visibility = value != null ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private static readonly VariantToBrushConverter VariantConverter = new();

        protected StatusVariant HeaderVariant
        {
            set
            {
                var isPlain = value == StatusVariant.Brand;

                headerBorder.Background = isPlain
                    ? (Brush)FindResource("SurfaceHeader")
                    : Variant(value, "Soft");

                var foreground = isPlain ? (Brush)FindResource("TextPrimary") : Variant(value, "SoftText");
                txtHeaderTitle.Foreground = foreground;
                txtHeaderSubtitle.Foreground = foreground;
                headerIcon.Stroke = foreground;
                closeIcon.Stroke = foreground;
                headerIconBackdrop.Background = foreground;
            }
        }

        private static Brush Variant(StatusVariant variant, string part) =>
            (Brush)VariantConverter.Convert(variant, typeof(Brush), part, CultureInfo.CurrentCulture);

        protected object Body
        {
            set => contentHost.Content = value;
        }

        protected object Footer
        {
            set => footerHost.Content = value;
        }

        protected virtual void OnEnterPressed() { }

        protected virtual void OnEscapePressed()
        {
            DialogResult = false;
            Close();
        }

        private void headerBorder_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ButtonState == System.Windows.Input.MouseButtonState.Pressed) DragMove();
        }

        private void AppDialogWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                OnEscapePressed();
                e.Handled = true;
            }
            else if (e.Key == Key.Enter)
            {
                if (Keyboard.FocusedElement is TextBox { AcceptsReturn: true })
                    return;

                OnEnterPressed();
                e.Handled = true;
            }
        }

        private void btnClose_Click(object sender, RoutedEventArgs e) => OnEscapePressed();

        private static Window FindActiveWindow()
        {
            foreach (Window w in System.Windows.Application.Current?.Windows ?? new WindowCollection())
                if (w.IsActive) return w;
            return System.Windows.Application.Current?.MainWindow;
        }
    }
}
