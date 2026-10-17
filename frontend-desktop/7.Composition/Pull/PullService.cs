using System.Collections.Generic;
using PrimeERP.Application.Services.Documents;
using PrimeERP.Composition.Definitions;
using PrimeERP.Domain.Results;

namespace PrimeERP.Composition.Pull
{
    /// <summary>السحب بتعريف مصدره — يستدعي PullCandidates</summary>
    public interface IPullService
    {
        Result<List<PullCandidate>> GetAvailable(PullSource source, IDictionary<string, object> matchValues);
    }

    public class PullService : IPullService
    {
        private readonly IPullCandidates _candidates;

        public PullService(IPullCandidates candidates) => _candidates = candidates;

        public Result<List<PullCandidate>> GetAvailable(PullSource source, IDictionary<string, object> matchValues) =>
            _candidates.Available(source.SourceKind, source.MatchFields, matchValues, source.Scope);
    }
}
