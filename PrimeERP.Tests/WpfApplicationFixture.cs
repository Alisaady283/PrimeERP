using System;
using System.Threading;
using System.Windows.Threading;

namespace PrimeERP.Tests
{
    /// <summary>
    /// System.Windows.Application هي DispatcherObject مرتبطة دائماً بخيط STA الذي أنشأها. StaThreadHelper
    /// (خيط STA جديد لكل اختبار، يُنهى بعد Join) يعمل بأمان لأي شيء لا يلمس Application.Current — لكن أي
    /// اختبار ثانٍ ينشئ خيط STA خاصاً به ويحاول استخدام Application.Current المُنشأة من خيط سابق **انتهى
    /// بالفعل** يُعلِّق التنفيذ للأبد (Dispatcher.Invoke على خيط ميت لا يُستجاب له أبداً) — اكتُشف فعلياً عند
    /// إضافة أول مستهلك ثانٍ (CrudPageRendererTests) لنفس نمط IdentityServiceTests.
    ///
    /// الحل: خيط STA واحد دائم (Background) طوال تشغيل عملية الاختبار كلها، يُنشئ Application مرة واحدة فقط
    /// ثم يبقى في Dispatcher.Run() — كل اختبار يحتاج Application.Current يمرّ عبر Invoke على نفس الخيط، لا
    /// خيطاً جديداً.
    /// </summary>
    public static class WpfApplicationFixture
    {
        private static readonly object _lock = new();
        private static Dispatcher _dispatcher;

        public static void Run(Action action)
        {
            EnsureStarted();
            _dispatcher.Invoke(action);
        }

        private static void EnsureStarted()
        {
            lock (_lock)
            {
                if (_dispatcher != null) return;

                var ready = new ManualResetEventSlim(false);
                var thread = new Thread(() =>
                {
                    if (System.Windows.Application.Current == null)
                        new System.Windows.Application();

                    _dispatcher = Dispatcher.CurrentDispatcher;
                    ready.Set();
                    Dispatcher.Run();
                })
                {
                    IsBackground = true
                };
                thread.SetApartmentState(ApartmentState.STA);
                thread.Start();
                ready.Wait();
            }
        }
    }
}
