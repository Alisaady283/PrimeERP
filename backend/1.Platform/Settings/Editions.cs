namespace PrimeERP.Platform.Settings
{
    public static class Editions
    {
        public static bool Pharmacy(this ISettingsProvider settings) => settings.Get(SettingKeys.Edition.Pharmacy, false);

        public static bool ManufacturingOperations(this ISettingsProvider settings) => settings.Get(SettingKeys.Edition.ManufacturingOperations, false);

        public static bool TracksExpiry(this ISettingsProvider settings) =>
            settings.Pharmacy() || settings.Get(SettingKeys.Edition.TrackExpiry, false);

        /// <summary>القيمة السرّية مكشوفةً للاستعمال وحده</summary>
        public static string Secret(this ISettingsProvider settings, string key) =>
            PrimeERP.Platform.Security.SecretBox.Open(settings.Get(key, ""));
    }
}
