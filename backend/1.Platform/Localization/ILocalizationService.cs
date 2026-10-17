namespace PrimeERP.Platform.Localization
{
    /// <summary>عقد النصوص للحقن</summary>
    public interface ILocalizationService
    {
        string Get(string key);
        string Get(string key, params object[] args);
    }
}
