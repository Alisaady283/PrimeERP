using System;

namespace PrimeERP.Services
{
    public interface IProgressHandle : IDisposable
    {
        void Report(double percent);
        void Report(double percent, string message);
        void Close();
        event EventHandler CancelRequested;
    }
}
