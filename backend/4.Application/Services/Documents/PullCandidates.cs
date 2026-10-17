using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Application.DTOs.Documents;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Localization;

namespace PrimeERP.Application.Services.Documents
{
    /// <summary>المتبقّي للسحب</summary>
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

    /// <summary>نطاقٌ يضيّق مستندات السحب باسمه (مثل مرتجع نقطة البيع)، ويُفرض بالقاعدة نفسها عند الحفظ</summary>
    public interface IPullScope
    {
        string Name { get; }
        bool Allows(string sourceKind, int sourceId);
    }

    public interface IPullCandidates
    {
        Result<List<PullCandidate>> Available(string sourceKind, IReadOnlyCollection<string> matchFields, IDictionary<string, object> matchValues, string scope = null);
    }

    public class PullCandidates : IPullCandidates
    {
        private readonly IServiceProvider _services;
        private readonly SourceDocuments _sources;
        private readonly IEnumerable<IPullScope> _scopes;

        public PullCandidates(IServiceProvider services, SourceDocuments sources, IEnumerable<IPullScope> scopes)
        {
            _services = services; _sources = sources; _scopes = scopes;
        }

        private IDocumentPull Links => _services.GetRequiredService<IDocumentPull>();

        public Result<List<PullCandidate>> Available(string sourceKind, IReadOnlyCollection<string> matchFields, IDictionary<string, object> matchValues, string scope = null)
        {
            var narrow = scope == null ? null : _scopes.FirstOrDefault(s => s.Name == scope);
            var service = _sources.ServiceOf(sourceKind);
            if (service == null) return Result.Fail<List<PullCandidate>>(LocalizationService.Get("Str.Pull.NoModule", sourceKind), ErrorCode.NotFound);

            var headers = SourceDocuments.ValueOf(SourceDocuments.Invoke(service, "GetPaged", 1, MaxSourceDocuments, null));
            if (headers == null) return Result.Ok(new List<PullCandidate>());

            var candidates = new List<PullCandidate>();
            foreach (var header in (IEnumerable)ReadValue(headers, "Items"))
            {
                if (!Matches(header, matchFields, matchValues)) continue;

                var id = (int)ReadValue(header, "Id");
                if (narrow != null && !narrow.Allows(sourceKind, id)) continue;
                var value = SourceDocuments.ValueOf(SourceDocuments.Invoke(service, "GetById", id));
                if (value == null) continue;
                if (value is not ISourceDocument detail)
                    return Result.Fail<List<PullCandidate>>(LocalizationService.Get("Str.Document.NotPullSource", sourceKind), ErrorCode.ValidationFailed);

                var pulled = Links.GetPulledBySource(sourceKind, id);
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
                    SourceType = sourceKind, SourceId = id,
                    SourceNo = detail.DocNo, DocDate = detail.DocDate, PartyName = detail.PartyName,
                    HeaderValues = ReadHeaderValues(detail),
                    Lines = lines
                });
            }

            return Result.Ok(candidates);
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

        private static bool Matches(object header, IReadOnlyCollection<string> matchFields, IDictionary<string, object> matchValues)
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

        private static object ReadValue(object target, string property) =>
            target?.GetType().GetProperty(property)?.GetValue(target);
    }
}
