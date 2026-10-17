using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace PrimeERP.UI.Services
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

        /// <summary>لون العلامة على الورق من الهوية الحالية، والباقي ورقٌ ثابت</summary>
        private static readonly Dictionary<string, string> FromIdentity = new()
        {
            ["BrandSolid"] = "BrandDefault", ["BrandSoft"] = "BrandSoft", ["BrandSoftText"] = "BrandSoftText",
        };

        public static T Value<T>(string key) => (T)Raw(key);

        public static object Raw(string key) => Identity(key) ?? Theme[key];

        public static string Hex(string key) => ((SolidColorBrush)Raw(key)).Color is var c ? $"#{c.R:X2}{c.G:X2}{c.B:X2}" : null;

        private static object Identity(string key)
        {
            if (!FromIdentity.TryGetValue(key, out var identityKey)) return null;
            if (System.Windows.Application.Current?.TryFindResource(identityKey) is not SolidColorBrush brush) return null;
            var copy = brush.Clone();
            copy.Freeze();
            return copy;
        }

        public static float Points(string key) => (float)(Value<double>(key) * 0.75);
    }
}
