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
    /// <summary>
    /// القاعدة الموحّدة لكل نوافذ الحوار (تأكيد/رسالة/تقدّم وأي حوار أعمال لاحقاً): هيدر ملوّن + محتوى + فوتر.
    /// الحوارات المشتقة (AppConfirmDialog, AppMessageDialog, AppProgressDialog) تبنى بالكامل من C# دون XAML خاص بها،
    /// وتضبط Body/Footer/HeaderVariant عبر الخصائص المحمية هنا.
    /// </summary>
    public partial class AppDialogWindow : Window
    {
        public AppDialogWindow()
        {
            InitializeComponent();
            var active = FindActiveWindow();
            if (Owner == null && active != this) Owner = active;
            // السقف من مساحة العمل لا من ارتفاع الشاشة: الأخير يتجاهل شريط المهام، ومع SizeToContent
            // تنمو النافذة بكل سطر يُضاف حتى يهبط الفوتر تحت حافة الشاشة فيغيب زر الحفظ.
            const double Chrome = 150;   // الرأس والفوتر
            MaxHeight = SystemParameters.WorkArea.Height * 0.92;
            contentScroll.MaxHeight = MaxHeight - VerticalGutter - Chrome;
            CardWidth = (double)FindResource("C.Dialog.Width.Sm");

            card.SizeChanged += (_, __) => ClipCorners();
        }

        private double HorizontalGutter => shell.Margin.Left + shell.Margin.Right;
        private double VerticalGutter   => shell.Margin.Top + shell.Margin.Bottom;

        /// <summary>
        /// عرض البطاقة المرئية — لا عرض النافذة. النافذة أوسع منها بهامش الظل من الجانبين، فيُضاف الهامش
        /// هنا مرة واحدة بدل أن يتذكّره كل نداء. ضبط Width مباشرةً يُنتج بطاقةً أضيق ممّا طُلب.
        /// </summary>
        public double CardWidth
        {
            get => (double.IsNaN(Width) ? ActualWidth : Width) - HorizontalGutter;
            set { Width = value + HorizontalGutter; MinWidth = Width; }
        }

        /// <summary>ارتفاع البطاقة المرئية — يُلغي SizeToContent لأن الطلب صريح.</summary>
        public double CardHeight
        {
            set { SizeToContent = SizeToContent.Manual; Height = value + VerticalGutter; }
        }

        /// <summary>
        /// Border لا يقصّ أبناءه باستدارته، فالهيدر الملوّن يربّع الزاويتين العلويتين والفوتر السفليتين.
        /// القصّ يُحسب من استدارة البطاقة نفسها فيتبع هوية التصميم النشطة بلا رقم مكرّر.
        /// </summary>
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

        /// <summary>
        /// اللون في الرأس إشارةُ حالة لا زينة: العلامة التجارية ليست حالة، فنموذج الأعمال يأخذ رأساً
        /// محايداً من سطح الثيم (درجة عن جسم البطاقة، صعوداً في الداكن ونزولاً في الفاتح). أما الحالات —
        /// تأكيد حذف، تحذير، نجاح — فتُلوَّن بتدرّجها الخفيف ونصّها المقروء عليه.
        /// </summary>
        protected StatusVariant HeaderVariant
        {
            set
            {
                var isPlain = value == StatusVariant.Brand;

                headerBorder.Background = isPlain
                    ? (Brush)FindResource("C.Dialog.Header.Bg")
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

        /// <summary>ينفَّذ عند Enter، إلا لو التركيز داخل TextBox متعدد الأسطر (AcceptsReturn) فيُترك السطر الجديد يعمل طبيعياً.</summary>
        protected virtual void OnEnterPressed() { }

        protected virtual void OnEscapePressed()
        {
            DialogResult = false;
            Close();
        }

        // WindowStyle=None يلغي شريط عنوان النظام (كان يكرّر عنوان الحوار مرتين) — والسحب يعود عبر الهيدر نفسه.
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
