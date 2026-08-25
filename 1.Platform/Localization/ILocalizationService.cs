namespace PrimeERP.Platform.Localization
{
    /// <summary>واجهة قابلة للحقن لطبقة الخدمات — بديل استدعاء LocalizationService.Get الساكن من 4.Application. الأخيرة تبقى كما هي، مستخدَمة فقط من 6.UI/App (تبديل لغة الواجهة الحيّة).</summary>
    public interface ILocalizationService
    {
        string Get(string key);
        string Get(string key, params object[] args);
    }
}
