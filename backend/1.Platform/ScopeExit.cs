using System;

namespace PrimeERP.Platform
{
    /// <summary>يعيد الحالة السابقة عند انتهاء النطاق</summary>
    internal sealed class ScopeExit : IDisposable
    {
        private Action _exit;

        public ScopeExit(Action exit) => _exit = exit;

        public void Dispose()
        {
            _exit?.Invoke();
            _exit = null;
        }
    }
}
