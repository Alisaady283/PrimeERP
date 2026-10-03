using PrimeERP.Application.Services.Documents;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Application.DTOs.Documents;
using PrimeERP.Application.Legacy.Documents;
using PrimeERP.Composition.Definitions;
using PrimeERP.Composition.Registry;
using PrimeERP.Domain.Contracts;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Localization;

namespace PrimeERP.Composition.Pull
{
    /// <summary>محرّك السحب العام</summary>
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
        public string   PartyName  { get; init; }
        public List<PullCandidateLine> Lines { get; init; } = new();

        public Dictionary<string, object> HeaderValues { get; init; } = new();

        public int     OpenLineCount => Lines.Count;
        public decimal RemainingQty  => Lines.Sum(l => l.RemainingQty);
        public string  Display       => LocalizationService.Get("Str.Pull.CandidateDisplay", SourceNo, DocDate, Lines.Count);
    }

    public interface IPullService
    {
        Result<List<PullCandidate>> GetAvailable(PullSource source, IDictionary<string, object> matchValues);
    }

    public class PullService : IPullService, IPullSourceReader
    {
        private readonly IServiceProvider _services;
        private readonly IModuleRegistry _registry;

        public PullService(IServiceProvider services, IModuleRegistry registry)
        {
            _services = services; _registry = registry;
        }

        private IDocumentPull Links => _services.GetRequiredService<IDocumentPull>();

        public Result<List<PullCandidate>> GetAvailable(PullSource source, IDictionary<string, object> matchValues)
        {
            var service = ResolveService(source.SourceKind);
            if (service == null) return Result.Fail<List<PullCandidate>>(LocalizationService.Get("Str.Pull.NoModule", source.SourceKind), ErrorCode.NotFound);

            var headers = ReadValue(Invoke(service, "GetPaged", 1, MaxSourceDocuments, null), "Value");
            if (headers == null) return Result.Ok(new List<PullCandidate>());

            var candidates = new List<PullCandidate>();
            foreach (var header in (IEnumerable)ReadValue(headers, "Items"))
            {
                if (!Matches(header, source.MatchFields, matchValues)) continue;

                var id = (int)ReadValue(header, "Id");
                var value = ReadValue(Invoke(service, "GetById", id), "Value");
                if (value == null) continue;
                if (value is not ISourceDocument detail)
                    return Result.Fail<List<PullCandidate>>(LocalizationService.Get("Str.Document.NotPullSource", source.SourceKind), ErrorCode.ValidationFailed);

                var pulled = Links.GetPulledBySource(source.SourceKind, id);
                var lines = new List<PullCandidateLine>();

                foreach (var line in detail.Lines)
                {
                    var already = pulled.TryGetValue(line.Id, out var p) ? p : 0m;
                    var remaining = line.Qty - already;
                    if (remaining <= 0) continue;   // المسحوب بالكامل لا يظهر أصلاً في نافذة السحب

                    lines.Add(new PullCandidateLine
                    {
                        SourceLineId = line.Id,
                        ProductCode  = line.ProductCode,
                        ProductName  = line.ProductName,
                        OriginalQty  = line.Qty, PulledQty = already, RemainingQty = remaining,
                        UnitValue    = line.UnitPrice
                    });
                }

                if (lines.Count == 0) continue;

                candidates.Add(new PullCandidate
                {
                    SourceType = source.SourceKind, SourceId = id,
                    SourceNo = detail.DocNo, DocDate = detail.DocDate, PartyName = detail.PartyName,
                    HeaderValues = ReadHeaderValues(detail),
                    Lines = lines
                });
            }

            return Result.Ok(candidates);
        }

        public decimal GetSourceLineQty(string sourceType, int sourceId, int sourceLineId)
        {
            var service = ResolveService(sourceType);
            if (service == null) return 0m;

            return ReadValue(Invoke(service, "GetById", sourceId), "Value") is ISourceDocument detail
                ? detail.Lines.FirstOrDefault(l => l.Id == sourceLineId)?.Qty ?? 0m
                : 0m;
        }

        private static readonly string[] HeaderKeys = { "PartyId", "CustomerId", "SupplierId", "WarehouseId" };

        private static Dictionary<string, object> ReadHeaderValues(object detail)
        {
            var values = new Dictionary<string, object>();
            foreach (var key in HeaderKeys)
            {
                var value = ReadValue(detail, key);
                if (value != null) values[key] = value;
            }

            var party = values.Values.FirstOrDefault();
            if (party != null)
                foreach (var key in HeaderKeys.Take(3))
                    values.TryAdd(key, party);

            return values;
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
