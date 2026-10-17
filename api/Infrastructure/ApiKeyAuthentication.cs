using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using PrimeERP.Application.PageServices.Security;
using PrimeERP.Application.Services.Integration;

namespace PrimeERP.Api.Infrastructure
{
    /// <summary>
    /// دخول الأنظمة الخارجية بمفتاح X-Api-Key: يتحوّل لمطالبات مستخدمه نفسها (SessionClaims.Of)، فيمرّ بـRequestSession
    /// وصلاحياته من القاعدة كأي مستخدم — لا مسار صلاحياتٍ ثانٍ.
    /// </summary>
    public sealed class ApiKeyAuthentication : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public const string Scheme = "ApiKey";

        private readonly IAuthService _auth;

        public ApiKeyAuthentication(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder, IAuthService auth)
            : base(options, logger, encoder) => _auth = auth;

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.TryGetValue(ApiKeys.Header, out var key)) return Task.FromResult(AuthenticateResult.NoResult());

            var user = _auth.VerifyKey(key.ToString());
            if (!user.IsSuccess) return Task.FromResult(AuthenticateResult.Fail(user.ErrorMessage));

            var identity = new ClaimsIdentity(SessionClaims.Of(user.Value).Select(c => new Claim(c.Key, c.Value?.ToString() ?? "")), Scheme);
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme)));
        }
    }
}
