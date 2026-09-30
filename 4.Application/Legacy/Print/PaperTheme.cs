using System;
using System.Windows;

namespace PrimeERP.Application.Legacy.Print
{
    /// <summary>قيم الورق كلها من PrintTheme.xaml</summary>
    public static class PaperTheme
    {
        private static ResourceDictionary _theme;

        private static ResourceDictionary Theme

        {

            get

            {

                if (_theme == null)

                {

                    // ⚠️ pack:// scheme needs a live Application first




                    if (System.Windows.Application.Current == null) new System.Windows.Application();





                    var asmName = typeof(PaperTheme).Assembly.GetName().Name;

                    var dict = new ResourceDictionary

                    {

                        Source = new Uri($"pack://application:,,,/{asmName};component/5.Design/Surfaces/PrintTheme.xaml", UriKind.Absolute)

                    };








                    foreach (var value in dict.Values)

                        if (value is Freezable freezable && freezable.CanFreeze)

                            freezable.Freeze();



                    _theme = dict;

                }

                return _theme;

            }

        }

        public static T Value<T>(string key) => (T)Theme[key];

        public static object Raw(string key) => Theme[key];

        public static float Points(string key) => (float)(Value<double>(key) * 0.75);
    }
}
