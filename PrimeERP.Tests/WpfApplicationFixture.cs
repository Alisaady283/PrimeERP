using System;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

namespace PrimeERP.Tests
{
    /// <summary>تطبيق WPF لخيط STA</summary>
    public static class WpfApplicationFixture
    {
        private static readonly object _lock = new();
        private static Dispatcher _dispatcher;

        public static void Run(Action action)
        {
            EnsureStarted();
            _dispatcher.Invoke(action);
        }

        public static void Ensure() => EnsureStarted();

        /// <summary>موارد البرنامج نفسها بترتيبها</summary>
        private static void LoadResources()
        {
            var main = typeof(PrimeERP.App.Bootstrap.DependencyInjection).Assembly.GetName().Name;

            foreach (var file in new[] { "5.Design/Sizes.xaml", "5.Design/Colors.xaml",
                                         "5.Design/Theme.xaml", "5.Design/Strings/Strings.ar.xaml" })
                System.Windows.Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary
                {
                    Source = new Uri($"pack://application:,,,/{main};component/{file}", UriKind.Absolute)
                });
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
                        new System.Windows.Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };

                    LoadResources();

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
