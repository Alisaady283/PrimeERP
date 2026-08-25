namespace PrimeERP.Platform.Localization
{
    public class LocalizationAdapter : ILocalizationService
    {
        public string Get(string key) => LocalizationService.Get(key);

        public string Get(string key, params object[] args) =>
            string.Format(LocalizationService.Get(key), args);
    }
}
