using System;
using System.Collections.Generic;

namespace PrimeERP.Services
{
    /// <summary>
    /// موقع خدمات مؤقت حتى يُبنى حاوي DI كامل — App.xaml.cs يسجّل كل خدمة مرة واحدة عند الإقلاع،
    /// وأي ViewModel/قطعة تطلبها عبر Get&lt;T&gt; بدل الإشارة مباشرة لكلاس التنفيذ.
    /// </summary>
    public static class ServiceLocator
    {
        private static readonly Dictionary<Type, object> _services = new();

        public static void Register<T>(T instance) => _services[typeof(T)] = instance;

        public static T Get<T>()
        {
            if (_services.TryGetValue(typeof(T), out var instance))
                return (T)instance;

            throw new InvalidOperationException($"لا خدمة مسجَّلة من النوع '{typeof(T).Name}'");
        }

        /// <summary>لاعتماديات اختيارية بين الخدمات (مثال: AccountService يربط عميلاً تلقائياً لو ICustomerService مسجَّلة، ويتجاهل الربط بأمان لو لم تُبنَ بعد).</summary>
        public static bool TryGet<T>(out T service)
        {
            if (_services.TryGetValue(typeof(T), out var instance))
            {
                service = (T)instance;
                return true;
            }

            service = default;
            return false;
        }
    }
}
