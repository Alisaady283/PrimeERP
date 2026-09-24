using System;
using System.Linq;
using System.Windows;
using PrimeERP.Platform.Design;

namespace PrimeERP.UI.Services
{
    /// <summary>تحميل موارد التصميم</summary>
    public class IdentityService : IIdentityService
    {
        private const string ThemeDictSuffix = "Theme.xaml";
        private static readonly string[] BaseFiles = { "Sizes.xaml", "Colors.xaml" };

        public void Initialize()
        {
            var app = System.Windows.Application.Current;
            if (app == null) return;

            var asmName = typeof(IdentityService).Assembly.GetName().Name;

            var preserved = app.Resources.MergedDictionaries
                .Where(d => d.Source == null ||
                            (!d.Source.OriginalString.EndsWith(ThemeDictSuffix) &&
                             !BaseFiles.Any(f => d.Source.OriginalString.EndsWith(f))))
                .ToList();

            // ⚠️ order matters: empty dictionary first, then merge
            app.Resources = new ResourceDictionary();
            var dicts = app.Resources.MergedDictionaries;

            foreach (var file in BaseFiles)
                dicts.Add(new ResourceDictionary
                {
                    Source = new Uri($"pack://application:,,,/{asmName};component/5.Design/{file}", UriKind.Absolute)
                });

            dicts.Add(new ResourceDictionary
            {
                Source = new Uri($"pack://application:,,,/{asmName};component/5.Design/Theme.xaml", UriKind.Absolute)
            });

            foreach (var old in preserved)
                dicts.Add(old);
        }
    }
}
