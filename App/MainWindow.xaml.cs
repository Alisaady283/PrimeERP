using System.Windows;
using PrimeERP.UI.DevTools;

namespace PrimeERP.App
{
    // TEMPORARY — يُستبدل محتواه بـ AppShell حقيقية في R9. لا تبنِ عليه.
    /// <summary>مؤقت — يعرض ControlsGalleryPage مباشرة بدل تسجيل دخول/صفحة حقيقية.</summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            pageContainer.Content = new ControlsGalleryPage();
        }
    }
}
