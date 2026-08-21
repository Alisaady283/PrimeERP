using System;
using System.Security.Cryptography;

namespace PrimeERP.Platform.Security
{
    /// <summary>تجزئة كلمات المرور عبر PBKDF2 — لا يُخزَّن أي كلمة مرور كنص صريح في أي مكان.</summary>
    public static class PasswordHasher
    {
        private const int SaltSize   = 16;
        private const int KeySize    = 32;
        private const int Iterations = 100_000;

        public static (string Hash, string Salt) Hash(string password)
        {
            var saltBytes = RandomNumberGenerator.GetBytes(SaltSize);
            var hashBytes = Rfc2898DeriveBytes.Pbkdf2(
                password, saltBytes, Iterations, HashAlgorithmName.SHA256, KeySize);

            return (Convert.ToBase64String(hashBytes), Convert.ToBase64String(saltBytes));
        }

        public static bool Verify(string password, string hash, string salt)
        {
            var saltBytes  = Convert.FromBase64String(salt);
            var expected   = Convert.FromBase64String(hash);
            var actual     = Rfc2898DeriveBytes.Pbkdf2(
                password, saltBytes, Iterations, HashAlgorithmName.SHA256, KeySize);

            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }
    }
}
