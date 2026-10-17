using System.Collections;
using System.Collections.Concurrent;
using System.Reflection;
using PrimeERP.Domain.Results;

namespace PrimeERP.Api.Infrastructure.Pages
{
    /// <summary>عمليات الصفحة الخمس كما تعلنها خدمتها (GetPaged · GetById · Create · Update · Delete)، مقروءةً مرّةً لكل نوع</summary>
    public sealed class ServiceShape
    {
        private static readonly ConcurrentDictionary<Type, ServiceShape> Cache = new();

        public MethodInfo Paged { get; private init; }
        public MethodInfo ById { get; private init; }
        public MethodInfo Create { get; private init; }
        public MethodInfo Update { get; private init; }
        public MethodInfo Delete { get; private init; }
        public MethodInfo All { get; private init; }

        public Type FilterType => Paged?.GetParameters()[2].ParameterType;
        public Type CreateType => Create?.GetParameters()[0].ParameterType;
        public Type UpdateType => Update?.GetParameters()[0].ParameterType;

        public static ServiceShape Of(Type service) => Cache.GetOrAdd(service, Read);

        private static ServiceShape Read(Type service)
        {
            var methods = service.GetMethods().Concat(service.GetInterfaces().SelectMany(i => i.GetMethods())).ToList();

            MethodInfo Find(string name, int count, Func<ParameterInfo[], bool> accept = null) =>
                methods.FirstOrDefault(m => m.Name == name && m.GetParameters() is var p && p.Length == count && (accept == null || accept(p)));

            static bool IsInput(ParameterInfo[] p) => p[0].ParameterType != typeof(int) && p[0].ParameterType.Name != "PrimeDbContext";

            return new ServiceShape
            {
                Paged = Find("GetPaged", 3, p => p[0].ParameterType == typeof(int) && p[1].ParameterType == typeof(int)),
                ById = Find("GetById", 1, p => p[0].ParameterType == typeof(int)),
                Create = Find("Create", 1, IsInput),
                Update = Find("Update", 1, IsInput),
                Delete = Find("Delete", 1, p => p[0].ParameterType == typeof(int)),
                All = methods.FirstOrDefault(m => m.Name == "GetAll" && m.GetParameters().All(p => p.IsOptional)),
            };
        }

        public static Result Invoke(MethodInfo method, object service, params object[] args)
        {
            try { return (Result)method.Invoke(service, args); }
            catch (TargetInvocationException ex) when (ex.InnerException != null) { throw ex.InnerException; }
        }

        public static Result InvokeAll(MethodInfo method, object service) =>
            Invoke(method, service, method.GetParameters().Select(p => p.DefaultValue).ToArray());

        public static object ValueOf(Result result) => result.GetType().GetProperty("Value")?.GetValue(result);

        public static (IEnumerable Items, int Total, int Page, int PageSize) PageOf(object paged)
        {
            var type = paged.GetType();
            return ((IEnumerable)type.GetProperty(nameof(PagedResult<object>.Items))!.GetValue(paged),
                (int)type.GetProperty(nameof(PagedResult<object>.TotalCount))!.GetValue(paged),
                (int)type.GetProperty(nameof(PagedResult<object>.Page))!.GetValue(paged),
                (int)type.GetProperty(nameof(PagedResult<object>.PageSize))!.GetValue(paged));
        }
    }
}
