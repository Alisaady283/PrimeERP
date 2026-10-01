using System.Reflection;

namespace PrimeERP.Platform
{
    /// <summary>النسخة المُشغَّلة ومجلّد بياناتها</summary>
    public static class AppInfo
    {
        public static string Version =>
            typeof(AppInfo).Assembly.GetName().Version?.ToString(3) ?? "1.0.0";

        public static string DataFolder =>
            System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData), "PrimeERP");
    }
}
