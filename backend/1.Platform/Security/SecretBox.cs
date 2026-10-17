using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace PrimeERP.Platform.Security
{
    /// <summary>
    /// تشفير القيم السرّية (مفاتيح البوابات والقنوات) بـAES-GCM قبل حفظها.
    /// المفتاح من متغيّر البيئة PRIMEERP_SECRET_KEY (Base64 لـ32 بايت) — يُضبط نفسه للدسكتوب والخادم على قاعدةٍ واحدة —
    /// وإلا من ملفٍ يُنشأ مرةً بجوار القاعدة.
    /// </summary>
    public static class SecretBox
    {
        private const string Prefix = "enc:";
        private const int NonceSize = 12, TagSize = 16;

        private static readonly Lazy<byte[]> Key = new(LoadKey);

        public static bool IsSealed(string value) => value?.StartsWith(Prefix, StringComparison.Ordinal) == true;

        public static string Seal(string plain)
        {
            if (string.IsNullOrEmpty(plain) || IsSealed(plain)) return plain;

            var nonce = RandomNumberGenerator.GetBytes(NonceSize);
            var data = Encoding.UTF8.GetBytes(plain);
            var cipher = new byte[data.Length];
            var tag = new byte[TagSize];
            using (var aes = new AesGcm(Key.Value, TagSize)) aes.Encrypt(nonce, data, cipher, tag);

            var packed = new byte[NonceSize + cipher.Length + TagSize];
            nonce.CopyTo(packed, 0);
            cipher.CopyTo(packed, NonceSize);
            tag.CopyTo(packed, NonceSize + cipher.Length);
            return Prefix + Convert.ToBase64String(packed);
        }

        /// <summary>القيمة المكشوفة، أو فارغٌ إن لم تُفكّ (مفتاحٌ آخر)</summary>
        public static string Open(string value)
        {
            if (!IsSealed(value)) return value ?? "";
            try
            {
                var packed = Convert.FromBase64String(value[Prefix.Length..]);
                var nonce = packed.AsSpan(0, NonceSize);
                var cipher = packed.AsSpan(NonceSize, packed.Length - NonceSize - TagSize);
                var tag = packed.AsSpan(packed.Length - TagSize);
                var plain = new byte[cipher.Length];
                using var aes = new AesGcm(Key.Value, TagSize);
                aes.Decrypt(nonce, cipher, tag, plain);
                return Encoding.UTF8.GetString(plain);
            }
            catch (Exception ex) when (ex is CryptographicException or FormatException) { return ""; }
        }

        private static byte[] LoadKey()
        {
            var fromEnv = Environment.GetEnvironmentVariable("PRIMEERP_SECRET_KEY");
            if (!string.IsNullOrWhiteSpace(fromEnv)) return Convert.FromBase64String(fromEnv);

            var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PrimeERP");
            var file = Path.Combine(folder, "secret.key");
            if (File.Exists(file)) return Convert.FromBase64String(File.ReadAllText(file).Trim());

            Directory.CreateDirectory(folder);
            var key = RandomNumberGenerator.GetBytes(32);
            File.WriteAllText(file, Convert.ToBase64String(key));
            return key;
        }
    }
}
