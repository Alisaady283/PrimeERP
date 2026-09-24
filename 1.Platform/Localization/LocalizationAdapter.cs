namespace PrimeERP.Platform.Localization
{
    /// <summary>جسر الحقن لقاموس النصوص</summary>
    public class LocalizationAdapter : ILocalizationService
    {
        public string Get(string key) => LocalizationService.Get(key);

        public string Get(string key, params object[] args) =>
            string.Format(LocalizationService.Get(key), args);
    }
}
