using System;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Win32;

namespace PrimeERP.Platform.Net
{
    /// <summary>
    /// بصمة الجهاز: تجزئةٌ لقيمتين ثابتتين (معرّف ويندوز للجهاز واسمه) لا تُقرأ عكسياً ولا تحمل بياناً
    /// شخصياً. تُرسَل مرّةً عند التفعيل ثم لا يحتاج البرنامج شبكةً بعدها — فلا قفل عند انقطاعها.
    /// </summary>
    public static class MachineFingerprint
    {
        public static string Value()
        {
            var parts = string.Join("|", Environment.MachineName, WindowsGuid());
            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(parts)))[..32];
        }

        private static string WindowsGuid()
        {
            try
            {
                using var key = RegistryKey
                    .OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64)
                    .OpenSubKey(@"SOFTWARE\Microsoft\Cryptography");

                return key?.GetValue("MachineGuid")?.ToString() ?? "";
            }
            catch
            {
                return "";
            }
        }
    }
}
