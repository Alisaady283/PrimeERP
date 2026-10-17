using System.Collections;
using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;
using PrimeERP.Platform.Permissions;

namespace PrimeERP.Api.Infrastructure.Pages
{
    /// <summary>بين الكيان وما يراه العميل: القراءة بحقولٍ معلنة، والكتابة بحقولٍ مسموحة، والمحجوب بصلاحيته</summary>
    public static class Projection
    {
        private static readonly ConcurrentDictionary<Type, Dictionary<string, PropertyInfo>> Properties = new();

        public static Dictionary<string, PropertyInfo> PropertiesOf(Type type) =>
            Properties.GetOrAdd(type, t => t.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.GetIndexParameters().Length == 0)
                .GroupBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase));

        public static object Shape(object item, PageResource page, IPermissionService permissions)
        {
            if (item == null) return null;
            var hidden = page.Guarded.Where(g => !permissions.Can(g.Value)).Select(g => g.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);

            if (item is IDictionary<string, object> row)
                return row.Where(kv => !hidden.Contains(kv.Key)).ToDictionary(kv => kv.Key, kv => kv.Value);

            if (page.Read == null)
            {
                if (hidden.Count == 0) return item;
                return PropertiesOf(item.GetType()).Values.Where(p => !hidden.Contains(p.Name)).ToDictionary(p => p.Name, p => p.GetValue(item));
            }

            var properties = PropertiesOf(item.GetType());
            var shaped = new Dictionary<string, object>();
            foreach (var name in page.Read.Prepend("Id").Distinct())
                if (!hidden.Contains(name) && properties.TryGetValue(name, out var p)) shaped[p.Name] = p.GetValue(item);
            return shaped;
        }

        public static IEnumerable<object> ShapeAll(IEnumerable items, PageResource page, IPermissionService permissions) =>
            items.Cast<object>().Select(i => Shape(i, page, permissions));

        public static void Apply(object target, JsonElement body, PageResource page, IPermissionService permissions, JsonSerializerOptions json)
        {
            var properties = PropertiesOf(target.GetType());
            foreach (var name in page.Write.Append("RowVersion"))
            {
                if (page.Guarded.TryGetValue(name, out var permission) && !permissions.Can(permission)) continue;
                if (!TryGetProperty(body, name, out var value) || !properties.TryGetValue(name, out var property) || !property.CanWrite) continue;
                property.SetValue(target, value.Deserialize(property.PropertyType, json));
            }
        }

        public static object Bind(Type type, IQueryCollection query, string search)
        {
            if (type == null) return null;
            var filter = Activator.CreateInstance(type);
            var properties = PropertiesOf(type);
            if (search != null && properties.TryGetValue("SearchText", out var searchText)) searchText.SetValue(filter, search);

            foreach (var (key, values) in query)
            {
                if (!properties.TryGetValue(key, out var property) || !property.CanWrite || values.Count == 0) continue;
                var target = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
                try
                {
                    object value = target.IsEnum ? Enum.Parse(target, values[0]!, true)
                        : target == typeof(DateTime) ? DateTime.Parse(values[0]!, System.Globalization.CultureInfo.InvariantCulture)
                        : Convert.ChangeType(values[0], target, System.Globalization.CultureInfo.InvariantCulture);
                    property.SetValue(filter, value);
                }
                catch (FormatException) { }
            }
            return filter;
        }

        private static bool TryGetProperty(JsonElement body, string name, out JsonElement value)
        {
            foreach (var p in body.EnumerateObject())
                if (string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase)) { value = p.Value; return true; }
            value = default;
            return false;
        }
    }
}
