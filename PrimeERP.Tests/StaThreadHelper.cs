using System;
using System.Runtime.ExceptionServices;
using System.Threading;

namespace PrimeERP.Tests
{
    /// <summary>أنواع WPF تفرض خيط STA</summary>
    public static class StaThreadHelper
    {
        public static void Run(Action action)
        {
            ExceptionDispatchInfo? capturedException = null;

            var thread = new Thread(() =>
            {
                try { action(); }
                catch (Exception ex) { capturedException = ExceptionDispatchInfo.Capture(ex); }
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();

            capturedException?.Throw();
        }

        public static T Run<T>(Func<T> function)
        {
            var result = default(T);
            Run(() => { result = function(); });
            return result;
        }
    }
}
