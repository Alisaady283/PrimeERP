using System;

namespace PrimeERP.UI.Services
{
    /// <summary>خدمة واجهة ProgressHandle</summary>
    public interface IProgressHandle : IDisposable
    {
        void Report(double percent);
        void Report(double percent, string message);
        void Close();
        event EventHandler CancelRequested;
    }
}
