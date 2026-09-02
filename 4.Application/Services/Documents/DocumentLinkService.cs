using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
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
    public class ChainNode
    {
        public string DocType { get; init; }
        public int    DocId   { get; init; }
        public string DocNo   { get; init; }
        public decimal Qty    { get; init; }
    }

    public interface IDocumentLinkService
    {
        decimal GetRemainingQty(string sourceType, int sourceLineId, decimal originalQty);
        Result RecordPull(IEnumerable<DocumentLink> links, DbConnection conn = null, DbTransaction tx = null);
        Result RemovePull(string targetType, int targetId, DbConnection conn = null, DbTransaction tx = null);
        Result<List<ChainNode>> GetChain(string docType, int docId);
    }

    /// <summary>تتبّع السحب بين المستندات — جدول واحد لدورتي الشراء والبيع. لا يعرف شيئاً عن نوع مستند بعينه:
    /// المستندات تمرّر روابطها فقط، فأي مستند جديد يعمل بلا تعديل هنا.</summary>
    public class DocumentLinkService : ServiceBase, IDocumentLinkService
    {
        private readonly IDocumentLinkRepository _links;

        public DocumentLinkService(IDocumentLinkRepository links, IPermissionService permissions,
            ISettingsProvider settings, ILocalizationService localization, IAuditLogger audit)
            : base(permissions, settings, localization, audit) => _links = links;

        protected override string PermissionPrefix => "Inventory";
        protected override string StringPrefix => "Str.Document";
        protected override string EntityName => "DocumentLink";

        public decimal GetRemainingQty(string sourceType, int sourceLineId, decimal originalQty)
        {
            var pulled = _links.GetPulledQty(sourceType, sourceLineId);
            var remaining = originalQty - pulled;
            return remaining > 0 ? remaining : 0m;
        }

        public Result RecordPull(IEnumerable<DocumentLink> links, DbConnection conn = null, DbTransaction tx = null)
        {
            foreach (var link in links)
            {
                if (link.PulledQty <= 0) continue;
                link.CreatedBy ??= AppSession.Username;
                _links.Insert(link, conn, tx);
            }
            return Result.Ok();
        }

        public Result RemovePull(string targetType, int targetId, DbConnection conn = null, DbTransaction tx = null)
        {
            _links.DeleteByTarget(targetType, targetId, conn, tx);
            return Result.Ok();
        }

        /// <summary>سلسلة المستند كاملة في الاتجاهين — ما سُحب منه وما سُحب إليه.</summary>
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
