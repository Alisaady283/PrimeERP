using System.Collections.Frozen;
using System.Globalization;
using PrimeERP.Api.Infrastructure.Pages;
using PrimeERP.Application.Analytics;
using PrimeERP.Application.Reporting;
using PrimeERP.Domain.Results;

namespace PrimeERP.Api.Infrastructure.Reports
{
    public sealed record ReportResponse(object Rows, Dictionary<string, string> Totals);

    /// <summary>التقارير ولوحات التحليل بدليلها: معاملات الطلب تُقرأ بأسماء الدالّة وترتيبها، والصلاحية في الخدمة</summary>
    public static class ReportEndpoints
    {
        private static readonly FrozenDictionary<string, Func<IAnalyticsService, DateTime, DateTime, AnalyticsFilter, Result<AnalyticsData>>> Dashboards =
            new Dictionary<string, Func<IAnalyticsService, DateTime, DateTime, AnalyticsFilter, Result<AnalyticsData>>>
            {
                ["ExecutiveAnalytics"] = (a, f, t, x) => a.Executive(f, t, x),
                ["SalesAnalytics"] = (a, f, t, x) => a.Sales(f, t, x),
                ["PurchaseAnalytics"] = (a, f, t, x) => a.Purchases(f, t, x),
                ["InventoryAnalytics"] = (a, f, t, x) => a.Inventory(f, t, x),
                ["LiquidityAnalytics"] = (a, f, t, x) => a.Liquidity(f, t, x),
                ["PeopleAnalytics"] = (a, f, t, x) => a.People(f, t, x),
                ["ProductionAnalytics"] = (a, f, t, x) => a.Production(f, t, x),
            }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

        public static RouteGroupBuilder MapReports(this RouteGroupBuilder api)
        {
            api.MapGet("/reports/{key}", (string key, HttpContext http, IServiceProvider services) =>
            {
                if (!ReportCatalog.TryGet(key, out var entry)) return Results.NotFound();
                var service = services.GetRequiredService(entry.Service);
                var method = entry.Service.GetMethods().FirstOrDefault(m => m.Name == entry.Method && m.GetParameters().Length == entry.Arguments.Length);
                if (method == null) return Results.NotFound();

                var args = method.GetParameters().Select((p, i) => Argument(p.ParameterType, http.Request.Query[entry.Arguments[i]].ToString(), entry.Arguments[i])).ToArray();
                var result = ServiceShape.Invoke(method, service, args);
                if (!result.IsSuccess) return ResultHttp.Problem(result);
                var data = (ReportData)ServiceShape.ValueOf(result);
                return Results.Ok(new ReportResponse(data.Rows, data.Totals));
            }).WithTags("Reports").RequireAuthorization();

            // dimension وkey: تصفية اللوحة بما نُقر عليه (شهر · طرف · صنف · تصنيف) — كتصفية BI
            api.MapGet("/analytics/{key}", (string key, DateTime? from, DateTime? to, string dimension, string filterKey, IAnalyticsService analytics) =>
            {
                if (!Dashboards.TryGetValue(key, out var run)) return Results.NotFound();
                var today = DateTime.Today;
                var filter = string.IsNullOrEmpty(dimension) || string.IsNullOrEmpty(filterKey) ? null : new AnalyticsFilter(dimension, filterKey);
                var result = run(analytics, from ?? new DateTime(today.Year, today.Month, 1).AddMonths(-11), to ?? today, filter);
                return result.IsSuccess ? Results.Ok(result.Value) : ResultHttp.Problem(result);
            }).WithTags("Analytics").RequireAuthorization();

            return api;
        }

        private static object Argument(Type type, string raw, string name)
        {
            var target = Nullable.GetUnderlyingType(type) ?? type;
            if (string.IsNullOrWhiteSpace(raw))
            {
                if (target != typeof(DateTime)) return type.IsValueType && Nullable.GetUnderlyingType(type) == null ? Activator.CreateInstance(type) : null;
                var today = DateTime.Today;
                return name == "From" ? new DateTime(today.Year, today.Month, 1) : today;
            }
            if (target == typeof(DateTime)) return DateTime.Parse(raw, CultureInfo.InvariantCulture);
            if (target.IsEnum) return int.TryParse(raw, out var n) ? Enum.ToObject(target, n) : Enum.Parse(target, raw, true);
            return Convert.ChangeType(raw, target, CultureInfo.InvariantCulture);
        }
    }
}
