using System.Collections.Generic;
using System.Data.Common;
using PrimeERP.Domain.Entities;

namespace PrimeERP.Data.Repositories
{
    public interface IAccountRepository
    {
        void CreateTable();
        void SeedDefaults();
        List<Account> GetAll(bool includeInactive = false);
        Account GetById(int id, DbConnection conn = null, DbTransaction tx = null);
        Account GetByCode(string code, DbConnection conn = null, DbTransaction tx = null);
        List<Account> GetChildren(string parentCode, DbConnection conn = null, DbTransaction tx = null);

        /// <summary>يشمل المعطَّل — توليد كود ابن جديد يجب ألا يعيد استخدام كود محذوف (محجوز بفهرس فريد).</summary>
        List<Account> GetAllChildren(string parentCode, DbConnection conn = null, DbTransaction tx = null);
        List<Account> GetLeaves();
        int GetLevel(string code);
        int GetTypeOf(string code);
        bool HasChildren(string code);
        int Insert(Account a, DbConnection conn = null, DbTransaction tx = null);
        void Update(Account a, DbConnection conn = null, DbTransaction tx = null);
        void UpdateName(DbConnection conn, DbTransaction tx, string code, string name);
        void SetIsLeaf(string code, bool isLeaf, DbConnection conn = null, DbTransaction tx = null);
        void Delete(string code, DbConnection conn = null, DbTransaction tx = null);
        void UpdateBalance(string code, decimal balance, DbConnection conn = null, DbTransaction tx = null);
    }
}
