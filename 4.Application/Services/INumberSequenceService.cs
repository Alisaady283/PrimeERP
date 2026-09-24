using PrimeERP.Data.Core;

namespace PrimeERP.Application.Services
{
    /// <summary>عقد الترقيم التسلسلي</summary>
    public interface INumberSequenceService
    {
        string Next(string key);

        string Next(PrimeDbContext db, string key);

        string Peek(string key);
    }
}
