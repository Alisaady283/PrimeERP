using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Localization;

namespace PrimeERP.Application.Services.Entities
{
    /// <summary>الكيان صفّاً والصفّ كياناً</summary>
    public static class Rows
    {
        public static IDictionary<string, object> Of(object entity)
        {
            IDictionary<string, object> row = new ExpandoObject();
            foreach (var property in entity.GetType().GetProperties())
                if (property.GetIndexParameters().Length == 0) row[property.Name] = property.GetValue(entity);
            return row;
        }

        /// <summary>الصفوف التي يحوي أحد حقولها النصّ</summary>
        public static List<T> Search<T>(IEnumerable<T> items, string term, int maxResults = int.MaxValue) =>
            (string.IsNullOrWhiteSpace(term)
                ? items
                : items.Where(item => Of(item).Values.Any(v => v?.ToString()?.Contains(term, StringComparison.OrdinalIgnoreCase) == true)))
            .Take(maxResults).ToList();

        public static void Fill(object entity, IDictionary<string, object> values)
        {
            foreach (var property in entity.GetType().GetProperties())
            {
                if (!property.CanWrite || property.Name == "Id" || !values.TryGetValue(property.Name, out var value)) continue;
                property.SetValue(entity, To(value, property.PropertyType));
            }
        }

        private static readonly ConcurrentDictionary<(Type, Type), (PropertyInfo From, PropertyInfo To)[]> Pairs = new();

        /// <summary>الحقول المتّفقة اسماً ونوعاً</summary>
        public static T Copy<T>(object source, T target)
        {
            foreach (var (from, to) in Pairs.GetOrAdd((source.GetType(), target.GetType()), PairsOf))
                to.SetValue(target, To(from.GetValue(source), to.PropertyType));
            return target;
        }

        /// <summary>النسخ ثم ما يخصّ الهدف</summary>
        public static T Copy<T>(object source, T target, Action<T> rest)
        {
            Copy(source, target);
            rest(target);
            return target;
        }

        private static (PropertyInfo, PropertyInfo)[] PairsOf((Type Source, Type Target) types)
        {
            var sources = types.Source.GetProperties().Where(p => p.CanRead && p.GetIndexParameters().Length == 0)
                               .GroupBy(p => p.Name).ToDictionary(g => g.Key, g => g.First());
            var fresh = Entity(types.Target) && !Entity(types.Source);
            return types.Target.GetProperties()
                .Where(p => p.CanWrite && p.GetIndexParameters().Length == 0 && !(fresh && p.Name == "Id"))
                .GroupBy(p => p.Name).Select(g => g.First())
                .Where(to => sources.TryGetValue(to.Name, out var from) && Fits(from.PropertyType, to.PropertyType))
                .Select(to => (sources[to.Name], to)).ToArray();
        }

        /// <summary>أول حالةٍ يصدق شرطها</summary>
        public static (StatusVariant Variant, string Text) State(params (bool When, StatusVariant Variant, string Key)[] states)
        {
            var (_, variant, key) = states.FirstOrDefault(s => s.When);
            return (variant, key == null ? null : LocalizationService.Get(key));
        }

        /// <summary>حالة النشاط وصفاً</summary>
        public static (StatusVariant Variant, string Text) Active(bool isActive) =>
            State((isActive, StatusVariant.Success, "Str.Active"), (true, StatusVariant.Danger, "Str.Inactive"));

        public static int Id(IDictionary<string, object> values) =>
            values != null && values.TryGetValue("Id", out var raw) && raw != null ? Convert.ToInt32(raw, CultureInfo.InvariantCulture) : 0;

        private static bool Entity(Type type) => type.Namespace?.StartsWith("PrimeERP.Domain.Entities") == true;

        private static bool Fits(Type source, Type target)
        {
            if (target.IsAssignableFrom(source)) return true;
            var from = Nullable.GetUnderlyingType(source) ?? source;
            var to = Nullable.GetUnderlyingType(target) ?? target;
            return from == to || Scalar(from) && Scalar(to) || Clock(from, to) || Clock(to, from);
        }

        private static bool Clock(Type time, Type text) => time == typeof(TimeSpan) && text == typeof(string);

        private static bool Scalar(Type type) =>
            type.IsEnum || type == typeof(decimal) || type.IsPrimitive && type != typeof(bool) && type != typeof(char);

        public static object To(object value, Type type)
        {
            var target = Nullable.GetUnderlyingType(type) ?? type;
            var empty = type.IsValueType && Nullable.GetUnderlyingType(type) == null ? Activator.CreateInstance(type) : null;
            if (value == null || value is string { Length: 0 } && target != typeof(string)) return empty;
            if (target.IsInstanceOfType(value)) return value;
            if (value is TimeSpan time && target == typeof(string)) return time.ToString(@"hh\:mm");
            if (value is string clock && target == typeof(TimeSpan))
                return TimeSpan.TryParse(clock, CultureInfo.InvariantCulture, out var parsed) ? parsed : empty;
            if (target.IsEnum) return Enum.ToObject(target, Convert.ToInt32(value, CultureInfo.InvariantCulture));

            var converter = TypeDescriptor.GetConverter(target);
            return value is string text && converter.CanConvertFrom(typeof(string))
                ? converter.ConvertFromInvariantString(text)
                : Convert.ChangeType(value, target, CultureInfo.InvariantCulture);
        }
    }
}
