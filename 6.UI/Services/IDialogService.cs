using System;
using System.Threading.Tasks;
using System.Windows;
using PrimeERP.Domain.Results;

namespace PrimeERP.UI.Services
{
    /// <summary>حوار يريد إرجاع نتيجة نمطية</summary>
    public interface IResultDialog<TResult>
    {
        TResult Result { get; }
    }

    public interface IDialogService
    {
        Task<bool> ConfirmAsync(string title, string message, string confirmText = null, bool isDangerous = false);
        Task ShowMessageAsync(string title, string message, StatusVariant variant = StatusVariant.Info);
        Task ShowErrorAsync(string title, string message, Exception exception = null);
        Task<TResult> ShowDialogAsync<TResult>(Window dialog);
        IProgressHandle ShowProgress(string title, string message, bool allowCancel = false);
    }
}
