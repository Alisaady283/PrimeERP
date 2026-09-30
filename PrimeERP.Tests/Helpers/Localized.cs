using System.Text.RegularExpressions;
using PrimeERP.Platform.Localization;

namespace PrimeERP.Tests.Helpers
{
    /// <summary>رسالةٌ من القاموس</summary>
    public static class Localized
    {
        public static bool Says(string message, string key)
        {
            var template = Regex.Escape(LocalizationService.Get(key));
            var pattern = Regex.Replace(template, @"\\\{\d+(:[^}]*)?}", ".*");
            return message != null && Regex.IsMatch(message, pattern, RegexOptions.Singleline);
        }
    }
}
