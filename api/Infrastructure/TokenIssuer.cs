using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using PrimeERP.Application.PageServices.Security;

namespace PrimeERP.Api.Infrastructure
{
    /// <summary>يُصدر رمز الدخول بهوية المستخدم المتحقَّق منه</summary>
    public sealed class TokenIssuer
    {
        private readonly JwtSettings _settings;
        private readonly JsonWebTokenHandler _handler = new();

        public TokenIssuer(JwtSettings settings) => _settings = settings;

        public (string Token, DateTime ExpiresAt) Issue(SignedInUser user)
        {
            var expiresAt = DateTime.UtcNow.Add(_settings.Lifetime);
            var token = _handler.CreateToken(new SecurityTokenDescriptor
            {
                Issuer = _settings.Issuer,
                Audience = _settings.Audience,
                Expires = expiresAt,
                SigningCredentials = new SigningCredentials(_settings.Key, SecurityAlgorithms.HmacSha256),
                Claims = SessionClaims.Of(user),
            });
            return (token, expiresAt);
        }
    }
}
