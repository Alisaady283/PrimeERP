using System.Windows;
using System.Windows.Threading;
using PrimeERP.Views.Controls.Feedback;

namespace PrimeERP.Services
{
    /// <summary>نافذة شفافة عائمة دائماً في الأعلى تستضيف تكديس الـ Toasts — مستقلة عن أي نافذة تطبيق محددة.</summary>
    public partial class ToastHostWindow : Window
    {
        public ToastHostWindow()
        {
            InitializeComponent();
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
