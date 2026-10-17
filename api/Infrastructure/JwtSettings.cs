using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace PrimeERP.Api.Infrastructure
{
    /// <summary>إعداد رمز الدخول: المُصدِر والجمهور والمدة والمفتاح — المفتاح إلزاميٌّ خارج التطوير</summary>
    public sealed class JwtSettings
    {
        private const int MinimumKeyBytes = 32;

        public string Issuer { get; init; }
        public string Audience { get; init; }
        public TimeSpan Lifetime { get; init; }
        public SymmetricSecurityKey Key { get; init; }

        public static JwtSettings From(IConfiguration configuration, IHostEnvironment environment)
        {
            var section = configuration.GetSection("Jwt");
            var secret = section["Key"];
            if (string.IsNullOrWhiteSpace(secret))
            {
                if (!environment.IsDevelopment()) throw new InvalidOperationException("Jwt:Key must be configured outside development.");
                secret = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
            }

            var bytes = Encoding.UTF8.GetBytes(secret);
            if (bytes.Length < MinimumKeyBytes) throw new InvalidOperationException($"Jwt:Key must be at least {MinimumKeyBytes} bytes.");

            return new JwtSettings
            {
                Issuer = section["Issuer"] ?? "PrimeERP",
                Audience = section["Audience"] ?? "PrimeERP.Web",
                Lifetime = TimeSpan.FromMinutes(section.GetValue("LifetimeMinutes", 480)),
                Key = new SymmetricSecurityKey(bytes),
            };
        }

        public TokenValidationParameters Validation() => new()
        {
            ValidIssuer = Issuer,
            ValidAudience = Audience,
            IssuerSigningKey = Key,
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = SessionClaims.Username,
        };
    }
}
