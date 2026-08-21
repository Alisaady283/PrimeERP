using System.Data.Common;
using PrimeERP.Core.Database;

namespace PrimeERP.Core.Transactions
{
    /// <summary>
    /// يغلّف اتصالاً ومعاملة واحدة لعمليات متعددة الجداول تمتد عبر عدة استدعاءات —
    /// Commit/Rollback صريحان، وRollback تلقائي عند Dispose بلا Commit سابق.
    /// للعمليات ذات الخطوة الواحدة يفضَّل DbHelper.RunTransaction الأبسط.
    /// </summary>
    public class UnitOfWork : IUnitOfWork
    {
        private DbConnection  _connection;
        private DbTransaction _transaction;
        private bool _committed;
        private bool _disposed;

        public DbConnection  Connection  => _connection;
        public DbTransaction Transaction => _transaction;

        public void Begin()
        {
            _connection  = DbHelper.GetConnection();
            _transaction = _connection.BeginTransaction();
            _committed   = false;
        }

        public void Commit()
        {
            _transaction?.Commit();
            _committed = true;
        }

        public void Rollback()
        {
            _transaction?.Rollback();
            _committed = true; // يمنع محاولة Rollback إضافية عند Dispose
        }

        public void Dispose()
        {
            if (_disposed) return;

            if (!_committed)
            {
                try { _transaction?.Rollback(); }
                catch { /* المعاملة ربما أُغلقت بالفعل — لا داعي لإيقاف التخلص من الموارد */ }
            }

            _transaction?.Dispose();
            _connection?.Dispose();
            _disposed = true;
        }
    }
}
