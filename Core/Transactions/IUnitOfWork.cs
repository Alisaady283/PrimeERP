using System;

namespace PrimeERP.Core.Transactions
{
    public interface IUnitOfWork : IDisposable
    {
        void Begin();
        void Commit();
        void Rollback();
    }
}
