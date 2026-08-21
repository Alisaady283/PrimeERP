using System.Collections.Generic;
using System.Linq;

namespace PrimeERP.Core.Validation
{
    public class ValidationResult
    {
        public Dictionary<string, string> Errors { get; } = new();

        public bool   IsValid    => Errors.Count == 0;
        public string FirstError => Errors.Count > 0 ? Errors.Values.First() : null;

        public string this[string field] =>
            Errors.TryGetValue(field, out var msg) ? msg : null;

        public void AddError(string field, string message)
        {
            if (!Errors.ContainsKey(field))
                Errors[field] = message;
        }

        public void Merge(ValidationResult other)
        {
            foreach (var kv in other.Errors)
                AddError(kv.Key, kv.Value);
        }
    }
}
