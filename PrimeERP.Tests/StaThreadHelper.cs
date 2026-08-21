using System;
using System.Runtime.ExceptionServices;
using System.Threading;

namespace PrimeERP.Tests
{
    /// <summary>
    /// أنواع WPF (FlowDocument/Table/...) تشتق من DispatcherObject وتفرض تشغيلها على خيط STA — xUnit يشغّل
    /// الاختبارات على خيط عادي (MTA) افتراضياً. حزمة Xunit.StaFact الحالية مبنية حصراً على xunit v3 وتتعارض
    /// مع xunit v2 المستخدَمة هنا (CS0433 على FactAttribute/CollectionAttribute...) — هذا الحل اليدوي يعمل
    /// مع أي إصدار xUnit بلا اعتمادية جديدة.
    /// </summary>
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
    }
}
