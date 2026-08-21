using System.Windows;
using PrimeERP.UI.DevTools;

namespace PrimeERP.App
{
    /// <summary>مؤقت — يعرض ControlsGalleryPage مباشرة بدل تسجيل دخول/صفحة حقيقية. يُستبدل بـ AppShell في R9.</summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            pageContainer.Content = new ControlsGalleryPage();
        }
    }
}
