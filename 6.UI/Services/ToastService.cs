using System.Collections.Generic;
using System.Windows;
using PrimeERP.UI.Components.Feedback;

namespace PrimeERP.UI.Services
{
    /// <summary>يعرض إشعارات Toast فوق أي نافذة نشطة عبر نافذة مضيفة شفافة مستقلة — لا يعتمد على نافذة تطبيق محددة.</summary>
    public class ToastService : IToastService
    {
        private const int MaxVisible = 4;

        private ToastHostWindow _host;
        private readonly Queue<(string Message, string Variant, int Duration)> _pending = new();
        private int _visibleCount;

        public void Success(string message, int durationMs = 3000) => Enqueue(message, "success", durationMs);
        public void Error(string message) => Enqueue(message, "error", 0);
        public void Warning(string message, int durationMs = 4000) => Enqueue(message, "warning", durationMs);
        public void Info(string message, int durationMs = 3000) => Enqueue(message, "info", durationMs);

        private void Enqueue(string message, string variant, int duration)
        {
            var dispatcher = System.Windows.Application.Current?.Dispatcher;
            if (dispatcher == null) return;

            dispatcher.Invoke(() =>
            {
                EnsureHost();

                if (_visibleCount >= MaxVisible)
                    _pending.Enqueue((message, variant, duration));
                else
                    ShowNow(message, variant, duration);
            });
        }

        private void EnsureHost()
        {
            if (_host == null)
            {
                _host = new ToastHostWindow();
                _host.Closed += (s, e) => _host = null;
                _host.Show();
            }
        }

        private void ShowNow(string message, string variant, int duration)
        {
            _visibleCount++;

            var toast = new AppToast { Message = message, Variant = variant, Duration = duration };
            toast.Dismissed += (s, e) =>
            {
                _host?.RemoveToast(toast);
                _visibleCount--;

                if (_pending.Count > 0)
                {
                    var next = _pending.Dequeue();
                    ShowNow(next.Message, next.Variant, next.Duration);
                }
            };

            _host.AddToast(toast);
        }
    }
}
