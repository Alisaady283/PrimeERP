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
            contentScroll.MaxHeight = MaxHeight - Chrome;
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

        /// <summary>يلوّن الهيدر بالكامل حسب مفردات الحالة المقفلة (StatusVariant) — لا نص حر.</summary>
        protected StatusVariant HeaderVariant
        {
            set => headerBorder.Background = (Brush)VariantConverter.Convert(value, typeof(Brush), "Solid", CultureInfo.CurrentCulture);
        }

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
