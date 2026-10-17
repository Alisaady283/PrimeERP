using System;
using System.IO;
using System.Linq;
using System.Windows;
using PrimeERP.Platform.Design;
using PrimeERP.Platform.Settings;

namespace PrimeERP.UI.Services
{
    /// <summary>تحميل موارد التصميم</summary>
    public class IdentityService : IIdentityService
    {
        private const string ThemeDictSuffix = "Theme.xaml";
        private const string SizesFile = "Sizes.xaml";
        private const string IdentityFolder = "/5.Design/Colors/";
        private const string DefaultIdentity = "Navy";

        private readonly ISettingsProvider _settings;

        public IdentityService(ISettingsProvider settings) => _settings = settings;

        public void Initialize()
        {
            var app = System.Windows.Application.Current;
            if (app == null) return;

            var asmName = typeof(IdentityService).Assembly.GetName().Name;

            var preserved = app.Resources.MergedDictionaries
                .Where(d => d.Source == null ||
                            (!d.Source.OriginalString.EndsWith(ThemeDictSuffix) &&
                             !d.Source.OriginalString.EndsWith(SizesFile) &&
                             !d.Source.OriginalString.Contains(IdentityFolder)))
                .ToList();

            // ⚠️ order matters: empty dictionary first, then merge
            app.Resources = new ResourceDictionary();
            var dicts = app.Resources.MergedDictionaries;

            dicts.Add(Design(asmName, SizesFile));
            dicts.Add(Identity(asmName, _settings.Get(SettingKeys.UI.Identity, DefaultIdentity)));

            dicts.Add(Design(asmName, ThemeDictSuffix));

            foreach (var old in preserved)
                dicts.Add(old);
        }

        private static ResourceDictionary Identity(string asmName, string identity)
        {
            try { return Design(asmName, $"Colors/{identity}.xaml"); }
            catch (IOException) { return Design(asmName, $"Colors/{DefaultIdentity}.xaml"); }
        }

        private static ResourceDictionary Design(string asmName, string file) => new()
        {
            Source = new Uri($"pack://application:,,,/{asmName};component/5.Design/{file}", UriKind.Absolute)
        };
    }
}
