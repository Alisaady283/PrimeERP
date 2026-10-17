using System.Text.Json;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using PrimeERP.Api.Features.Admin;
using PrimeERP.Api.Features.Auth;
using PrimeERP.Api.Features.Builder;
using PrimeERP.Api.Features.Categories;
using PrimeERP.Api.Features.Notifications;
using PrimeERP.Api.Features.Operations;
using PrimeERP.Api.Infrastructure.Pages;
using PrimeERP.Api.Infrastructure.Reports;
using PrimeERP.Api.Infrastructure;
using PrimeERP.Application;
using PrimeERP.Data;
using PrimeERP.Platform;
using PrimeERP.Platform.Localization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddPlatform().AddData().AddApplication();

var jwt = JwtSettings.From(builder.Configuration, builder.Environment);
builder.Services.AddSingleton(jwt);
builder.Services.AddSingleton<TokenIssuer>();
// مدخلان لهويةٍ واحدة: رمز الدخول، ومفتاح الربط للأنظمة الخارجية — يُختار بالترويسة
builder.Services.AddAuthentication("Session")
    .AddPolicyScheme("Session", "Session", options => options.ForwardDefaultSelector = context =>
        context.Request.Headers.ContainsKey(PrimeERP.Application.Services.Integration.ApiKeys.Header)
            ? ApiKeyAuthentication.Scheme : JwtBearerDefaults.AuthenticationScheme)
    .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, ApiKeyAuthentication>(ApiKeyAuthentication.Scheme, null)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = jwt.Validation();
        // WebSocket لا يحمل ترويسة: رمز قناة الإشعارات في access_token لمسارها وحده
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                if (context.Request.Path.StartsWithSegments(NotificationEndpoints.HubPath))
                    context.Token = context.Request.Query["access_token"];
                return Task.CompletedTask;
            }
        };
    });
builder.Services.AddSignalR();
builder.Services.AddSingleton<PrimeERP.Application.Services.Integration.INotificationPush, SignalRPush>();
builder.Services.AddAuthorization();

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
});
builder.Services.AddMemoryCache();
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddResponseCompression(options => options.EnableForHttps = true);
builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy
    .WithOrigins(builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? Array.Empty<string>())
    .AllowAnyHeader()
    .AllowAnyMethod()));
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy(AuthEndpoints.LoginPolicy, context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1) }));
});

var app = builder.Build();

app.Services.EnsureDatabaseReady(LocalizationService.Use);

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseResponseCompression();
app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseMiddleware<RequestSession>();
app.UseAuthorization();

if (app.Environment.IsDevelopment()) app.MapOpenApi();

var api = app.MapGroup("/api");
api.MapAuth();
api.MapPages();
api.MapCategories();
api.MapReports();
api.MapAdmin();
api.MapOperations();
api.MapBuilder();
api.MapNotifications();
app.MapHub<NotificationsHub>(NotificationEndpoints.HubPath);

// التنبيهات وصندوق الصادر يعملان مع الخادم كما مع الدسكتوب
app.Services.GetRequiredService<PrimeERP.Application.Services.Integration.AlertRunner>().Start();

app.Run();
