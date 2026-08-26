using System;
using System.Threading;
using System.Windows;
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
    ///
    /// ShutdownMode = OnExplicitShutdown إلزامي هنا (نفس اختيار App.xaml.cs الحقيقي): الافتراضي
    /// OnLastWindowClose يُطلق Application.Shutdown() تلقائياً بمجرد إغلاق أي نافذة اختبار وحيدة تفتحها
    /// (IdentityServiceTests مثلاً) — Shutdown() يُصفّر Application.Current للأبد بينما علم "أُنشئت بالفعل"
    /// الداخلي لا يُصفَّر أبداً، فيرمي أي new Application() لاحق (كحارس PrintService.Theme الدفاعي)
    /// "Cannot create more than one Application instance" — اكتُشف فعلياً عند إضافة اختبار يُغلق نافذته.
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

        /// <summary>يضمن وجود Application (وتسجيل مخطَّط pack:// معها — يُسجَّل ضمن مُنشئها الساكن) بلا تنفيذ
        /// أي إجراء. تُستدعى من TestDatabaseFixture نفسها (تُبنى في كل اختبار تقريباً) لضمان تسجيل pack://
        /// **قبل** أي اختبار طباعة يحتاجه، بصرف النظر عن ترتيب تشغيل xUnit غير الحتمي — راجع توقف 11.</summary>
        public static void Ensure() => EnsureStarted();

        private static void EnsureStarted()
        {
            lock (_lock)
            {
                if (_dispatcher != null) return;

                var ready = new ManualResetEventSlim(false);
                var thread = new Thread(() =>
                {
                    if (System.Windows.Application.Current == null)
                        new System.Windows.Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };

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
