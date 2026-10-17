using System.Text.Json;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Options;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Permissions;

namespace PrimeERP.Api.Infrastructure.Pages
{
    public sealed record OptionItem(object Value, string Label, string Hint);

    /// <summary>عمليات الصفحة الخمس وخياراتها لكل صفحةٍ في الدليل — الصلاحية والتحقق في خدمتها</summary>
    public static class PageEndpoints
    {
        private const int MaxPageSize = 200;
        private const int MaxOptions = 5000;

        public static RouteGroupBuilder MapPages(this RouteGroupBuilder api)
        {
            var pages = api.MapGroup("/pages/{key}").WithTags("Pages").RequireAuthorization();

            pages.MapGet("/", (string key, HttpContext http, IServiceProvider services, IPermissionService permissions,
                int page = 1, int pageSize = 50, string search = null) =>
                With(key, services, out var resource, out var service, out var shape) ?? (
                    shape.Paged != null
                        ? Paged(ServiceShape.Invoke(shape.Paged, service, Math.Max(1, page), Math.Clamp(pageSize, 1, MaxPageSize),
                            Projection.Bind(shape.FilterType, http.Request.Query, search)), resource, permissions)
                        : shape.All != null ? Everything(ServiceShape.InvokeAll(shape.All, service), resource, permissions, search) : Results.NotFound()));

            pages.MapGet("/options", (string key, HttpContext http, IServiceProvider services, string search = null) =>
                With(key, services, out var resource, out var service, out var shape) ?? (
                    shape.Paged != null ? Options(ServiceShape.Invoke(shape.Paged, service, 1, MaxOptions, Projection.Bind(shape.FilterType, http.Request.Query, search)), resource)
                    : shape.All != null ? Options(ServiceShape.InvokeAll(shape.All, service), resource) : Results.NotFound()));

            pages.MapGet("/{id:int}", (string key, int id, IServiceProvider services, IPermissionService permissions) =>
                With(key, services, out var resource, out var service, out var shape) ?? (shape.ById == null ? Results.NotFound() :
                    Single(ServiceShape.Invoke(shape.ById, service, id), resource, permissions)));

            pages.MapPost("/", (string key, JsonElement body, IServiceProvider services, IPermissionService permissions, IOptions<JsonOptions> json) =>
            {
                if (With(key, services, out var resource, out var service, out var shape) is { } missing) return missing;
                if (shape.Create == null) return Results.NotFound();
                var input = Input(shape.CreateType, null, body, resource, permissions, json.Value.SerializerOptions, 0);
                return Single(ServiceShape.Invoke(shape.Create, service, input), resource, permissions);
            });

            pages.MapPut("/{id:int}", (string key, int id, JsonElement body, IServiceProvider services, IPermissionService permissions, IOptions<JsonOptions> json) =>
            {
                if (With(key, services, out var resource, out var service, out var shape) is { } missing) return missing;
                if (shape.Update == null) return Results.NotFound();
                object current = null;
                if (resource.Write != null && shape.ById != null)
                {
                    var loaded = ServiceShape.Invoke(shape.ById, service, id);
                    if (!loaded.IsSuccess) return ResultHttp.Problem(loaded);
                    current = ServiceShape.ValueOf(loaded);
                }
                var input = Input(shape.UpdateType, current, body, resource, permissions, json.Value.SerializerOptions, id);
                return ServiceShape.Invoke(shape.Update, service, input).ToHttp();
            });

            pages.MapDelete("/{id:int}", (string key, int id, IServiceProvider services) =>
                With(key, services, out _, out var service, out var shape) ?? (shape.Delete == null ? Results.NotFound() :
                    ServiceShape.Invoke(shape.Delete, service, id).ToHttp()));

            return pages;
        }

        private static IResult With(string key, IServiceProvider services, out PageResource resource, out object service, out ServiceShape shape)
        {
            service = null;
            shape = null;
            if (!PageCatalog.TryGet(key, out resource)) return Results.NotFound();
            service = services.GetService(resource.Service);
            if (service == null) return Results.NotFound();
            shape = ServiceShape.Of(resource.Service);
            return null;
        }

        private static IResult Paged(Result result, PageResource resource, IPermissionService permissions)
        {
            if (!result.IsSuccess) return ResultHttp.Problem(result);
            var (items, total, page, pageSize) = ServiceShape.PageOf(ServiceShape.ValueOf(result));
            return Results.Ok(new PageResponse<object>(Projection.ShapeAll(items, resource, permissions).ToList(), total, page, pageSize));
        }

        private static IResult Everything(Result result, PageResource resource, IPermissionService permissions, string search)
        {
            if (!result.IsSuccess) return ResultHttp.Problem(result);
            var rows = ((System.Collections.IEnumerable)ServiceShape.ValueOf(result)).Cast<object>()
                .Where(item => string.IsNullOrWhiteSpace(search) || Matches(item, search)).ToList();
            return Results.Ok(new PageResponse<object>(Projection.ShapeAll(rows, resource, permissions).ToList(), rows.Count, 1, rows.Count));
        }

        private static bool Matches(object item, string search) =>
            Projection.PropertiesOf(item.GetType()).Values.Where(p => p.PropertyType == typeof(string))
                .Any(p => (p.GetValue(item) as string)?.Contains(search, StringComparison.OrdinalIgnoreCase) == true);

        private static IResult Single(Result result, PageResource resource, IPermissionService permissions) =>
            result.IsSuccess ? Results.Ok(Projection.Shape(ServiceShape.ValueOf(result), resource, permissions)) : ResultHttp.Problem(result);

        private static IResult Options(Result result, PageResource resource)
        {
            if (!result.IsSuccess) return ResultHttp.Problem(result);
            var value = ServiceShape.ValueOf(result);
            var items = value is System.Collections.IList list ? list : ServiceShape.PageOf(value).Items;
            return Results.Ok(items.Cast<object>().Select(item => new OptionItem(Field(item, "Id"),
                Field(item, resource.OptionLabel)?.ToString() ?? "", Field(item, resource.OptionHint)?.ToString())).ToList());
        }

        private static object Field(object item, string name)
        {
            if (item is IDictionary<string, object> row)
                return row.FirstOrDefault(kv => string.Equals(kv.Key, name, StringComparison.OrdinalIgnoreCase)).Value;
            return Projection.PropertiesOf(item.GetType()).TryGetValue(name, out var p) ? p.GetValue(item) : null;
        }

        private static object Input(Type type, object current, JsonElement body, PageResource resource, IPermissionService permissions, JsonSerializerOptions json, int id)
        {
            if (typeof(IDictionary<string, object>).IsAssignableFrom(type))
            {
                var values = body.EnumerateObject().ToDictionary(p => p.Name, p => Plain(p.Value), StringComparer.OrdinalIgnoreCase);
                foreach (var guarded in resource.Guarded.Where(g => !permissions.Can(g.Value))) values.Remove(guarded.Key);
                if (id > 0) values["Id"] = id;
                return values;
            }

            object target;
            if (resource.Write != null)
            {
                target = current != null && type.IsInstanceOfType(current) ? current : Activator.CreateInstance(type);
                Projection.Apply(target, body, resource, permissions, json);
            }
            else target = body.Deserialize(type, json);

            if (id > 0 && Projection.PropertiesOf(type).TryGetValue("Id", out var idProperty) && idProperty.CanWrite) idProperty.SetValue(target, id);
            return target;
        }

        private static object Plain(JsonElement value) => value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Number => value.TryGetInt32(out var i) ? i : value.GetDecimal(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ => null,
        };
    }
}
