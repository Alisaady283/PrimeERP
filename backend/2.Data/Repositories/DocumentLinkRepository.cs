using System;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Collections.Generic;
using PrimeERP.Data.Core;
using PrimeERP.Data.Repositories.Base;
using PrimeERP.Domain.Entities;

namespace PrimeERP.Data.Repositories
{
    /// <summary>مستودع DocumentLink</summary>
    public interface IDocumentLinkRepository
    {
        int Insert(DocumentLink link, PrimeDbContext db = null);
        List<DocumentLink> GetBySource(string sourceType, int sourceId, PrimeDbContext db = null);
        List<DocumentLink> GetByTarget(string targetType, int targetId, PrimeDbContext db = null);
        decimal GetPulledQty(string sourceType, int sourceLineId, PrimeDbContext db = null, string exceptTargetType = null, int exceptTargetId = 0);
        Dictionary<int, decimal> GetPulledBySource(string sourceType, int sourceId, PrimeDbContext db = null);
        bool AnyPulledFrom(string sourceType, int sourceId);
        Dictionary<int, decimal> PulledInto(string sourceType, IEnumerable<int> sourceLineIds, string targetType);
        Dictionary<int, decimal> PulledThrough(string sourceType, IEnumerable<int> sourceLineIds, string midType, string targetType);
        HashSet<int> LinkedTargets(string targetType, IEnumerable<int> targetLineIds);
        void DeleteByTarget(string targetType, int targetId, PrimeDbContext db = null);
    }

    public class DocumentLinkRepository : RepositoryBase<DocumentLink>, IDocumentLinkRepository
    {
        protected override string TableName => "DocumentLinks";


        public int Insert(DocumentLink l, PrimeDbContext db = null) => Add(l, db);

        public List<DocumentLink> GetBySource(string sourceType, int sourceId,
            PrimeDbContext db = null) =>
            Fetch(q => q.Where(l => l.SourceType == sourceType && l.SourceId == sourceId), db);

        public List<DocumentLink> GetByTarget(string targetType, int targetId,
            PrimeDbContext db = null) =>
            Fetch(q => q.Where(l => l.TargetType == targetType && l.TargetId == targetId), db);

        public decimal GetPulledQty(string sourceType, int sourceLineId,
            PrimeDbContext db = null, string exceptTargetType = null, int exceptTargetId = 0)
        {
            return Scope(db, ctx =>
            {
                return Rows(ctx).AsNoTracking()
                    .Where(l => l.SourceType == sourceType && l.SourceLineId == sourceLineId
                             && (exceptTargetType == null || l.TargetType != exceptTargetType || l.TargetId != exceptTargetId))
                    .Sum(l => (decimal?)l.PulledQty) ?? 0m;
            });
        }

        public bool AnyPulledFrom(string sourceType, int sourceId) =>
            Any(q => q.Where(l => l.SourceType == sourceType && l.SourceId == sourceId));

        public Dictionary<int, decimal> GetPulledBySource(string sourceType, int sourceId,
            PrimeDbContext db = null)
        {
            return Scope(db, ctx =>
            {
                return Rows(ctx).AsNoTracking()
                    .Where(l => l.SourceType == sourceType && l.SourceId == sourceId)
                    .GroupBy(l => l.SourceLineId)
                    .Select(g => new { g.Key, Pulled = g.Sum(l => l.PulledQty) })
                    .ToDictionary(x => x.Key, x => x.Pulled);
            });
        }

        public Dictionary<int, decimal> PulledInto(string sourceType, IEnumerable<int> sourceLineIds, string targetType)
        {
            var ids = sourceLineIds.Distinct().ToList();
            return Scope(null, ctx => Rows(ctx).AsNoTracking()
                .Where(l => l.SourceType == sourceType && l.TargetType == targetType && ids.Contains(l.SourceLineId))
                .GroupBy(l => l.SourceLineId)
                .Select(g => new { g.Key, Qty = g.Sum(l => l.PulledQty) })
                .ToDictionary(x => x.Key, x => x.Qty));
        }

        public Dictionary<int, decimal> PulledThrough(string sourceType, IEnumerable<int> sourceLineIds, string midType, string targetType)
        {
            var ids = sourceLineIds.Distinct().ToList();
            return Scope(null, ctx => Rows(ctx).AsNoTracking()
                .Where(a => a.SourceType == sourceType && a.TargetType == midType && ids.Contains(a.SourceLineId))
                .Join(Rows(ctx).AsNoTracking().Where(b => b.SourceType == midType && b.TargetType == targetType),
                    a => a.TargetLineId, b => b.SourceLineId, (a, b) => new { a.SourceLineId, b.PulledQty })
                .GroupBy(x => x.SourceLineId)
                .Select(g => new { g.Key, Qty = g.Sum(x => x.PulledQty) })
                .ToDictionary(x => x.Key, x => x.Qty));
        }

        public HashSet<int> LinkedTargets(string targetType, IEnumerable<int> targetLineIds)
        {
            var ids = targetLineIds.Distinct().ToList();
            return Scope(null, ctx => Rows(ctx).AsNoTracking()
                .Where(l => l.TargetType == targetType && ids.Contains(l.TargetLineId))
                .Select(l => l.TargetLineId).Distinct().ToHashSet());
        }

        public void DeleteByTarget(string targetType, int targetId,
            PrimeDbContext db = null) =>
            Remove(l => l.TargetType == targetType && l.TargetId == targetId, db);
    }
}
