using PrimeERP.Api.Infrastructure;
using PrimeERP.Application.PageServices.Security;
using PrimeERP.Platform.Permissions;

namespace PrimeERP.Api.Features.Auth
{
    public sealed record LoginRequest(string Username, string Password);

    public sealed record UserResponse(int Id, string Username, string DisplayName, string RoleName, int? BranchId, IReadOnlyList<string> Permissions)
    {
        /// <summary>الصفحات التي لها صلاحياتها باسمها (PermissionKeys.RegisterPage)</summary>
        public IReadOnlyList<string> Pages { get; init; } = PermissionKeys.PageKeys();
    }

    public sealed record SessionResponse(string Token, DateTime ExpiresAt, UserResponse User);

    /// <summary>الدخول بالخدمة نفسها التي تستدعيها نافذة سطح المكتب، ومن أنا في الطلب الحالي</summary>
    public static class AuthEndpoints
    {
        public const string LoginPolicy = "login";

        public static RouteGroupBuilder MapAuth(this RouteGroupBuilder api)
        {
            var auth = api.MapGroup("/auth").WithTags("Auth");

            auth.MapPost("/login", (LoginRequest request, IAuthService service, TokenIssuer tokens) =>
            {
                var signed = service.Verify(request.Username, request.Password);
                if (!signed.IsSuccess) return Results.Problem(detail: signed.ErrorMessage, statusCode: StatusCodes.Status401Unauthorized);

                var user = signed.Value;
                var (token, expiresAt) = tokens.Issue(user);
                return Results.Ok(new SessionResponse(token, expiresAt,
                    new UserResponse(user.Id, user.Username, user.DisplayName, user.RoleName, user.BranchId, user.Permissions)));
            })
            .AllowAnonymous()
            .RequireRateLimiting(LoginPolicy);

            auth.MapGet("/me", () => Results.Ok(new UserResponse(AppSession.UserId, AppSession.Username, AppSession.DisplayName,
                    AppSession.RoleName, AppSession.BranchId, AppSession.Permissions.ToList())))
                .RequireAuthorization();

            auth.MapPut("/password", (PrimeERP.Application.DTOs.Security.ChangePasswordDto dto, PrimeERP.Application.PageServices.Security.IUserService users) =>
                    users.ChangeOwnPassword(dto).ToHttp())
                .RequireAuthorization();

            return auth;
        }
    }
}
