using System.Reflection;

namespace PrimeERP.Platform
{
    /// <summary>هوية النسخة المُشغَّلة — مصدرٌ واحد يقرؤه التحديث و«عن البرنامج» معاً.</summary>
    public static class AppInfo
    {
        public static string Version =>
            typeof(AppInfo).Assembly.GetName().Version?.ToString(3) ?? "1.0.0";
    }
}
