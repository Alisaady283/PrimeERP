using System;

namespace PrimeERP.UI.Services
{
    public interface IProgressHandle : IDisposable
    {
        void Report(double percent);
        void Report(double percent, string message);
        void Close();
        event EventHandler CancelRequested;
    }
}
