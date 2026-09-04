using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Application.Services.Documents;
using PrimeERP.Composition.Definitions;
using PrimeERP.Composition.Registry;
using PrimeERP.Domain.Contracts;
using PrimeERP.Domain.Results;

namespace PrimeERP.Composition.Pull
{
    public class PullCandidateLine
    {
        public int     SourceLineId { get; init; }
        public string  ProductCode  { get; init; }
        public string  ProductName  { get; init; }
        public decimal OriginalQty  { get; init; }
        public decimal PulledQty    { get; init; }
        public decimal RemainingQty { get; init; }
        public decimal UnitValue    { get; init; }
    }

    public class PullCandidate
    {
        public string   SourceType { get; init; }
        public int      SourceId   { get; init; }
        public string   SourceNo   { get; init; }
        public DateTime DocDate    { get; init; }
        public List<PullCandidateLine> Lines { get; init; } = new();

        public string Display => $"{SourceNo} — {DocDate:yyyy-MM-dd} ({Lines.Count} سطر)";
    }

    public interface IPullService
    {
        Result<List<PullCandidate>> GetAvailable(PullSource source, IDictionary<string, object> matchValues);
    }

    /// <summary>محرّك السحب العام: يقرأ أي مستند مصدر عبر سجل الوحدات وبالاسم (Id/Lines/Qty/ProductCode…) —
    /// نفس فلسفة الربط بالاسم في DocumentRenderer. مستند جديد يصبح قابلاً للسحب منه بمجرّد تسجيله، بلا كود هنا.
    /// المتبقي محسوب دائماً (الأصلي ناقص مجموع الروابط) — لا عمود حالة يُخزَّن ولا يتعارض.</summary>
    public class PullService : IPullService, IPullSourceReader
    {
        private readonly IServiceProvider _services;
        private readonly IModuleRegistry _registry;

        public PullService(IServiceProvider services, IModuleRegistry registry)
        {
            _services = services; _registry = registry;
        }

        // يُحَل عند الطلب لا في المُنشئ: DocumentLinkService تعتمد على IPullSourceReader (هذه الفئة)، فحقنها
        // في المُنشئ يصنع دورة تسجيل. الكسر هنا لا في العقد.
        private IDocumentLinkService Links => _services.GetRequiredService<IDocumentLinkService>();

        public Result<List<PullCandidate>> GetAvailable(PullSource source, IDictionary<string, object> matchValues)
        {
            var service = ResolveService(source.SourceKind);
            if (service == null) return Result.Fail<List<PullCandidate>>($"لا توجد وحدة مسجّلة بالمفتاح {source.SourceKind}", ErrorCode.NotFound);

            var headers = ReadValue(Invoke(service, "GetPaged", 1, MaxSourceDocuments, null), "Value");
            if (headers == null) return Result.Ok(new List<PullCandidate>());

            var candidates = new List<PullCandidate>();
            foreach (var header in (IEnumerable)ReadValue(headers, "Items"))
            {
                if (!Matches(header, source.MatchFields, matchValues)) continue;

                var id = (int)ReadValue(header, "Id");
                var detail = ReadValue(Invoke(service, "GetById", id), "Value");
                if (detail == null) continue;

                var pulled = Links.GetPulledBySource(source.SourceKind, id);
                var lines = new List<PullCandidateLine>();

                foreach (var line in (IEnumerable)ReadValue(detail, "Lines") ?? Array.Empty<object>())
                {
                    var lineId = (int)ReadValue(line, "Id");
                    var qty = Convert.ToDecimal(ReadValue(line, "Qty"));
                    var already = pulled.TryGetValue(lineId, out var p) ? p : 0m;
                    var remaining = qty - already;
                    if (remaining <= 0) continue;   // المسحوب بالكامل لا يظهر أصلاً في نافذة السحب

                    lines.Add(new PullCandidateLine
                    {
                        SourceLineId = lineId,
                        ProductCode  = ReadValue(line, "ProductCode") as string,
                        ProductName  = ReadValue(line, "ProductName") as string,
                        OriginalQty  = qty, PulledQty = already, RemainingQty = remaining,
                        UnitValue    = Convert.ToDecimal(ReadValue(line, "UnitPrice") ?? ReadValue(line, "UnitCost") ?? 0m)
                    });
                }

                if (lines.Count == 0) continue;

                candidates.Add(new PullCandidate
                {
                    SourceType = source.SourceKind, SourceId = id,
                    SourceNo = ReadValue(detail, "DocNo") as string,
                    DocDate = Convert.ToDateTime(ReadValue(detail, "DocDate") ?? ReadValue(detail, "MovementDate")),
                    Lines = lines
                });
            }

            return Result.Ok(candidates);
        }

        public decimal GetSourceLineQty(string sourceType, int sourceId, int sourceLineId)
        {
            var service = ResolveService(sourceType);
            if (service == null) return 0m;

            var detail = ReadValue(Invoke(service, "GetById", sourceId), "Value");
            if (detail == null) return 0m;

            foreach (var line in (IEnumerable)ReadValue(detail, "Lines") ?? Array.Empty<object>())
                if ((int)ReadValue(line, "Id") == sourceLineId)
                    return Convert.ToDecimal(ReadValue(line, "Qty"));

            return 0m;
        }

        private const int MaxSourceDocuments = 500;

        private object ResolveService(string sourceKind)
        {
            var serviceType = _registry.Get(sourceKind)?.DocumentDialog?.ServiceType;
            return serviceType == null ? null : _services.GetService(serviceType);
        }

        private static bool Matches(object header, List<string> matchFields, IDictionary<string, object> matchValues)
        {
            if (matchFields == null || matchValues == null) return true;

            foreach (var field in matchFields)
            {
                if (!matchValues.TryGetValue(field, out var expected) || expected == null) continue;
                if (header.GetType().GetProperty(field) == null) continue;   // المصدر لا يملك هذا الحقل أصلاً — لا قيد
                var actual = ReadValue(header, field);
                if (actual == null || !actual.ToString().Equals(expected.ToString(), StringComparison.OrdinalIgnoreCase)) return false;
            }
            return true;
        }

        private static object Invoke(object service, string method, params object[] args) =>
            service.GetType().GetMethods()
                .First(m => m.Name == method && m.GetParameters().Length == args.Length)
                .Invoke(service, args);

        private static object ReadValue(object target, string property) =>
            target?.GetType().GetProperty(property)?.GetValue(target);
    }
}
