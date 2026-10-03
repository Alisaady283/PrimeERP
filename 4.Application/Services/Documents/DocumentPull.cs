using PrimeERP.Application.Validation;
using PrimeERP.Application.Services.Core;
using PrimeERP.Data.Core;
using System.Collections.Generic;
using System.Linq;
using PrimeERP.Application.DTOs.Documents;
using PrimeERP.Data.Repositories;
using PrimeERP.Domain.Contracts;
using PrimeERP.Domain.Entities;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Audit;
using PrimeERP.Platform.Localization;
using PrimeERP.Platform.Permissions;
using PrimeERP.Platform.Settings;

namespace PrimeERP.Application.Services.Documents
{
    /// <summary>تتبّع السحب بين المستندات</summary>
    public class ChainNode
    {
        public string DocType { get; init; }
        public int    DocId   { get; init; }
        public string DocNo   { get; init; }
        public decimal Qty    { get; init; }
    }

    public interface IDocumentPull
    {
        decimal GetRemainingQty(string sourceType, int sourceLineId, decimal originalQty, string exceptTargetType = null, int exceptTargetId = 0);
        Dictionary<int, decimal> GetPulledBySource(string sourceType, int sourceId);
        bool IsPulledFrom(string sourceType, int sourceId);
        Result RecordPull(IEnumerable<DocumentLink> links, PrimeDbContext db = null);

        Result ValidatePulls(IEnumerable<(IPullableLine Line, decimal Qty)> lines, string exceptTargetType = null, int exceptTargetId = 0);

        Result RecordPulls(PrimeDbContext db, string targetType, int targetId,
            IEnumerable<(IPullableLine Line, int TargetLineId, decimal Qty)> lines);
        Result RemovePull(string targetType, int targetId, PrimeDbContext db = null);
        void Attach<TLine>(string targetType, int targetId, IEnumerable<TLine> lines) where TLine : class, ISourceLine, IPullableLine;
        Result<List<ChainNode>> GetChain(string docType, int docId);
    }

    public class DocumentPull : ServiceBase, IDocumentPull
    {
        private readonly IDocumentLinkRepository _links;
        private readonly IPullSourceReader _sources;

        public DocumentPull(IDocumentLinkRepository links, IPermissionService permissions,
            ISettingsProvider settings, ILocalizationService localization, IAuditLogger audit,
            IPullSourceReader sources = null)
            : base(permissions, settings, localization, audit) { _links = links; _sources = sources; }

        protected override string PermissionPrefix => "Inventory";
        protected override string StringPrefix => "Str.Document";
        protected override string EntityName => "DocumentLink";

        public decimal GetRemainingQty(string sourceType, int sourceLineId, decimal originalQty, string exceptTargetType = null, int exceptTargetId = 0)
        {
            var pulled = _links.GetPulledQty(sourceType, sourceLineId, null, exceptTargetType, exceptTargetId);
            var remaining = originalQty - pulled;
            return remaining > 0 ? remaining : 0m;
        }

        public bool IsPulledFrom(string sourceType, int sourceId) => _links.AnyPulledFrom(sourceType, sourceId);

        public Dictionary<int, decimal> GetPulledBySource(string sourceType, int sourceId) =>
            _links.GetPulledBySource(sourceType, sourceId);

        private Result ValidatePull(string sourceType, int sourceId, int sourceLineId, decimal qty, string sourceNo,
            string exceptTargetType, int exceptTargetId)
        {
            if (_sources == null || sourceLineId <= 0) return Result.Ok();

            var originalQty = _sources.GetSourceLineQty(sourceType, sourceId, sourceLineId);
            if (originalQty <= 0) return Result.Ok();

            var remaining = GetRemainingQty(sourceType, sourceLineId, originalQty, exceptTargetType, exceptTargetId);
            return qty <= remaining
                ? Result.Ok()
                : Result.Fail(Msg("PullExceeds", qty, remaining, sourceNo), ErrorCode.ValidationFailed);
        }

        public Result RecordPull(IEnumerable<DocumentLink> links, PrimeDbContext db = null)
        {
            foreach (var link in links)
            {
                if (link.PulledQty <= 0) continue;
                _links.Insert(link, db);
            }
            return Result.Ok();
        }

        /// <summary>مجموع كل سطر مصدر مقابل متبقيه</summary>
        public Result ValidatePulls(IEnumerable<(IPullableLine Line, decimal Qty)> lines, string exceptTargetType = null, int exceptTargetId = 0)
        {
            foreach (var source in lines.Where(x => x.Line != null && x.Line.SourceLineId > 0)
                                        .GroupBy(x => (x.Line.SourceType, x.Line.SourceId, x.Line.SourceLineId)))
            {
                var line = source.First().Line;
                var check = ValidatePull(line.SourceType, line.SourceId, line.SourceLineId, source.Sum(x => x.Qty), line.SourceNo,
                    exceptTargetType, exceptTargetId);
                if (check.IsFailure) return check;
            }
            return Result.Ok();
        }

        public Result RecordPulls(PrimeDbContext db, string targetType, int targetId,
            IEnumerable<(IPullableLine Line, int TargetLineId, decimal Qty)> lines) =>
            RecordPull(lines
                .Where(x => x.Line != null && x.Line.SourceLineId > 0)
                .Select(x => new DocumentLink
                {
                    SourceType = x.Line.SourceType, SourceId = x.Line.SourceId, SourceNo = x.Line.SourceNo,
                    SourceLineId = x.Line.SourceLineId, TargetType = targetType, TargetId = targetId,
                    TargetLineId = x.TargetLineId, PulledQty = x.Qty
                }), db);

        public Result RemovePull(string targetType, int targetId, PrimeDbContext db = null)
        {
            _links.DeleteByTarget(targetType, targetId, db);
            return Result.Ok();
        }

        /// <summary>مصدر كل سطرٍ مسحوب</summary>
        public void Attach<TLine>(string targetType, int targetId, IEnumerable<TLine> lines) where TLine : class, ISourceLine, IPullableLine
        {
            var links = _links.GetByTarget(targetType, targetId).ToLookup(l => l.TargetLineId);
            foreach (var line in lines)
                if (links[line.Id].FirstOrDefault() is { } link)
                    (line.SourceType, line.SourceId, line.SourceNo, line.SourceLineId) = (link.SourceType, link.SourceId, link.SourceNo, link.SourceLineId);
        }

        public Result<List<ChainNode>> GetChain(string docType, int docId)
        {
            var chain = new List<ChainNode>();

            foreach (var link in _links.GetByTarget(docType, docId))
                chain.Add(new ChainNode { DocType = link.SourceType, DocId = link.SourceId, DocNo = link.SourceNo, Qty = link.PulledQty });

            foreach (var link in _links.GetBySource(docType, docId))
                chain.Add(new ChainNode { DocType = link.TargetType, DocId = link.TargetId, Qty = link.PulledQty });

            return Result.Ok(chain
                .GroupBy(c => new { c.DocType, c.DocId })
                .Select(g => new ChainNode { DocType = g.Key.DocType, DocId = g.Key.DocId, DocNo = g.First().DocNo, Qty = g.Sum(x => x.Qty) })
                .ToList());
        }
    }
}
