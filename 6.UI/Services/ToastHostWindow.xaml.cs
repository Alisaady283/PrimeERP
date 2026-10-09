using System.Windows;
using System.Windows.Threading;
using PrimeERP.UI.Components.Feedback;

namespace PrimeERP.UI.Services
{
    /// <summary>نافذة الإشعارات العائمة</summary>
    public partial class ToastHostWindow : Window
    {
        public ToastHostWindow()
        {
            InitializeComponent();
            FlowDirection = PrimeERP.Platform.Localization.LocalizationService.Flow;
            Loaded += (s, e) => Reposition();
        }

        public void AddToast(AppToast toast)
        {
            toast.Margin = new Thickness(0, 0, 0, 10);
            pnlToasts.Children.Add(toast);
            Dispatcher.BeginInvoke(DispatcherPriority.Loaded, Reposition);
        }

        public void RemoveToast(AppToast toast)
        {
            pnlToasts.Children.Remove(toast);
            Dispatcher.BeginInvoke(DispatcherPriority.Loaded, Reposition);
        }

        private void Reposition()
        {
            var workArea = SystemParameters.WorkArea;
            Left = workArea.Left + 16;
            Top = workArea.Bottom - ActualHeight - 16;
        }
    }
}
