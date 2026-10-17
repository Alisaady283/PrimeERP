using PrimeERP.Platform.Localization;
using System;
using System.Threading.Tasks;
using System.Windows;
using PrimeERP.Domain.Results;
using PrimeERP.UI.Components.Feedback;

namespace PrimeERP.UI.Services
{
    /// <summary>واجهة async فوق حوارات معتمدة</summary>
    public class DialogService : IDialogService
    {
        public Task<bool> ConfirmAsync(string title, string message, string confirmText = null, bool isDangerous = false)
        {
            var dlg = new AppConfirmDialog(title, message,
                confirmText ?? LocalizationService.Get("Str.Confirm"),
                LocalizationService.Get("Str.Cancel"),
                isDangerous);
            return Task.FromResult(dlg.ShowDialog() == true);
        }

        public Task ShowMessageAsync(string title, string message, StatusVariant variant = StatusVariant.Info)
        {
            new AppMessageDialog(title, message, variant).ShowDialog();
            return Task.CompletedTask;
        }

        public Task ShowErrorAsync(string title, string message, Exception exception = null)
        {
            var full = exception != null ? $"{message}\n\n{exception.Message}" : message;
            new AppMessageDialog(title, full, StatusVariant.Danger).ShowDialog();
            return Task.CompletedTask;
        }

        public Task<TResult> ShowDialogAsync<TResult>(Window dialog)
        {
            var shown = dialog.ShowDialog();

            if (dialog is IResultDialog<TResult> typed)
                return Task.FromResult(typed.Result);

            if (typeof(TResult) == typeof(bool))
                return Task.FromResult((TResult)(object)(shown == true));

            return Task.FromResult(default(TResult));
        }

        public IProgressHandle ShowProgress(string title, string message, bool allowCancel = false)
        {
            var dlg = new AppProgressDialog(title, message, allowCancel);
            dlg.Show();
            return new ProgressHandle(dlg);
        }

        private class ProgressHandle : IProgressHandle
        {
            private readonly AppProgressDialog _dialog;

            public event EventHandler CancelRequested;

            public ProgressHandle(AppProgressDialog dialog)
            {
                _dialog = dialog;
                _dialog.CancelRequested += (s, e) => CancelRequested?.Invoke(this, EventArgs.Empty);
            }

            public void Report(double percent) =>
                _dialog.Dispatcher.Invoke(() => _dialog.SetProgress(percent));

            public void Report(double percent, string message) =>
                _dialog.Dispatcher.Invoke(() => _dialog.SetProgress(percent, message));

            public void Close() => _dialog.Dispatcher.Invoke(_dialog.Close);

            public void Dispose() => Close();
        }
    }
}
