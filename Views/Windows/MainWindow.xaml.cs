using System.Windows;

namespace PrimeERP.Views.Windows
{
    /// <summary>
    /// مؤقت أثناء مرحلة الترحيل — يعرض ControlsGalleryPage مباشرة كشاشة الاختبار الافتراضية بدل الصفحات
    /// القديمة (منقولة إلى _Legacy/). يُستبدل بـ AppShell وحدها في المرحلة H.
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            pageContainer.Content = new Views.Dev.ControlsGalleryPage();
        }
    }
}
