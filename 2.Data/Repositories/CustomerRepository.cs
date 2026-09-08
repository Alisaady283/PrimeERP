using System;
using System.Data;
using System.Data.Common;
using System.Collections.Generic;
using PrimeERP.Data.Core;
using PrimeERP.Data.Query;
using PrimeERP.Data.Repositories.Base;
using PrimeERP.Data.Schema;
using PrimeERP.Domain.Entities;

namespace PrimeERP.Data.Repositories
{
    /// <summary>طبقة وصول بيانات العملاء — SQL خام ↔ Models فقط. بلا تحقق، بلا معاملات ذاتية، بلا حساب رصيد، بلا إنشاء/حذف حساب (كل ذلك مسؤولية ICustomerService).</summary>
    public class CustomerRepository : RepositoryBase<Customer>, ICustomerRepository
    {
        protected override string TableName => "Customers";

        public void CreateTable() =>
            SchemaBuilder.Table("Customers")
                .Id()
                .Text("Code", 30, required: true, unique: true)
                .Text("Name", 200, required: true)
                .Text("NameEn", 200)
                .Text("Phone", 30)
                .Text("Phone2", 30)
                .Text("Email", 150)
                .Text("Address", 400)
                .Text("City", 100)
                .Text("Country", 100)
                .Text("TaxNumber", 50)
                .Text("CommercialRegNo", 50)
                .Text("AccountCode", 30)
                .Decimal("Balance")
                .Decimal("CreditLimit")
                .Int("PaymentTermDays", nullable: false, defaultValue: 0)
                .Int("CurrencyId")
                .Int("CategoryId")
                .Text("Notes")
                .Bool("IsActive", defaultValue: true)
                .Audit()
                .SoftDelete()
                .Concurrency()
                .Index("AccountCode")
                .Create();

        protected override Customer Map(DataRow row) => new()
        {
            Id              = Convert.ToInt32(row["Id"]),
            Code            = row["Code"].ToString(),
            Name            = row["Name"].ToString(),
            NameEn          = row["NameEn"] == DBNull.Value ? null : row["NameEn"].ToString(),
            Phone           = row["Phone"] == DBNull.Value ? null : row["Phone"].ToString(),
            Phone2          = row["Phone2"] == DBNull.Value ? null : row["Phone2"].ToString(),
            Email           = row["Email"] == DBNull.Value ? null : row["Email"].ToString(),
            Address         = row["Address"] == DBNull.Value ? null : row["Address"].ToString(),
            City            = row["City"] == DBNull.Value ? null : row["City"].ToString(),
            Country         = row["Country"] == DBNull.Value ? null : row["Country"].ToString(),
            TaxNumber       = row["TaxNumber"] == DBNull.Value ? null : row["TaxNumber"].ToString(),
            CommercialRegNo = row["CommercialRegNo"] == DBNull.Value ? null : row["CommercialRegNo"].ToString(),
            AccountCode     = row["AccountCode"] == DBNull.Value ? null : row["AccountCode"].ToString(),
            Balance         = Convert.ToDecimal(row["Balance"]),
            CreditLimit     = Convert.ToDecimal(row["CreditLimit"]),
            PaymentTermDays = Convert.ToInt32(row["PaymentTermDays"]),
            CurrencyId      = row["CurrencyId"] == DBNull.Value ? null : Convert.ToInt32(row["CurrencyId"]),
            CategoryId      = row["CategoryId"] == DBNull.Value ? null : Convert.ToInt32(row["CategoryId"]),
            Notes           = row["Notes"] == DBNull.Value ? null : row["Notes"].ToString(),
            IsActive        = Convert.ToBoolean(row["IsActive"]),
            CreatedAt       = row["CreatedAt"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(row["CreatedAt"]),
            CreatedBy       = row["CreatedBy"] == DBNull.Value ? null : row["CreatedBy"].ToString(),
            UpdatedAt       = row["UpdatedAt"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(row["UpdatedAt"]),
            UpdatedBy       = row["UpdatedBy"] == DBNull.Value ? null : row["UpdatedBy"].ToString(),
            IsDeleted       = Convert.ToBoolean(row["IsDeleted"]),
            DeletedAt       = row["DeletedAt"] == DBNull.Value ? null : Convert.ToDateTime(row["DeletedAt"]),
            DeletedBy       = row["DeletedBy"] == DBNull.Value ? null : row["DeletedBy"].ToString(),
            RowVersion      = Convert.ToInt64(row["RowVersion"])
        };

        // ===== قراءة =====

        public override Customer GetById(int id, DbConnection conn = null, DbTransaction tx = null) =>
            QueryOne("SELECT * FROM Customers WHERE Id = @id AND IsDeleted = @d", conn, tx, ("@id", id), ("@d", false));

        public Customer GetByCode(string code) =>
            QueryOne("SELECT * FROM Customers WHERE Code = @c AND IsDeleted = @d", null, null, ("@c", code), ("@d", false));

        public Customer GetByAccountCode(string accountCode, DbConnection conn = null, DbTransaction tx = null) =>
            QueryOne("SELECT * FROM Customers WHERE AccountCode = @a AND IsDeleted = @d", conn, tx, ("@a", accountCode), ("@d", false));

        public List<Customer> GetAll(bool activeOnly = true)
        {
            var where = new WhereBuilder().Eq("IsDeleted", false).Eq("IsActive", activeOnly ? true : (bool?)null);
            return Query($"SELECT * FROM Customers {where.Sql} ORDER BY Name", null, null, where.Parameters);
        }

        public List<Customer> Search(string term, int maxResults) =>
            Query($@"SELECT * FROM Customers WHERE IsDeleted = @d AND IsActive = @a AND (Name LIKE @t OR Code LIKE @t OR Phone LIKE @t)
                     ORDER BY Name {DbFactory.Current.LimitClause(0, maxResults)}",
                null, null, ("@d", false), ("@a", true), ("@t", $"%{term}%"));

        public int CountAll(bool activeOnly = true)
        {
            var where = new WhereBuilder().Eq("IsDeleted", false).Eq("IsActive", activeOnly ? true : (bool?)null);
            return Convert.ToInt32(Scalar($"SELECT COUNT(*) FROM Customers {where.Sql}", where.Parameters));
        }

        public bool ExistsCode(string code, int? excludeId = null) => ExistsBy("Code", code, excludeId);
        public bool ExistsPhone(string phone, int? excludeId = null) =>
            !string.IsNullOrWhiteSpace(phone) && ExistsBy("Phone", phone, excludeId);
        public bool ExistsName(string name, int? excludeId = null) => ExistsBy("Name", name, excludeId);

        private bool ExistsBy(string column, string value, int? excludeId)
        {
            var where = new WhereBuilder().Eq(column, value).Eq("IsDeleted", false).RawWithParam(p => $"Id != {p}", excludeId);
            return Convert.ToInt64(Scalar($"SELECT COUNT(*) FROM Customers {where.Sql}", where.Parameters)) > 0;
        }

        /// <summary>قائمة مُرقَّمة مع الفلاتر — sortColumn يُطابَق بقائمة أعمدة مسموحة صراحة (لا يُدرَج كنص حر في ORDER BY).</summary>
        public (List<Customer> Items, int Total) GetPaged(
            int page, int pageSize,
            string searchText = null, bool? isActive = null, bool? hasBalance = null, bool? overCreditLimit = null,
            int? categoryId = null, string sortColumn = "Name", bool sortDescending = false)
        {
            var where = new WhereBuilder()
                .Eq("IsDeleted", false)
                .LikeAny(searchText, "Name", "Code", "Phone")
                .Eq("IsActive", isActive)
                .Raw("Balance != 0", hasBalance == true)
                .Raw("Balance = 0", hasBalance == false)
                .Raw("(CreditLimit > 0 AND Balance > CreditLimit)", overCreditLimit == true)
                .Raw("NOT (CreditLimit > 0 AND Balance > CreditLimit)", overCreditLimit == false)
                .Eq("CategoryId", categoryId);

            var column = sortColumn switch
            {
                "Name" => "Name", "Balance" => "Balance", "CreditLimit" => "CreditLimit", "CreatedAt" => "CreatedAt", _ => "Code"
            };
            var direction = sortDescending ? "DESC" : "ASC";

            var total = Convert.ToInt32(Scalar($"SELECT COUNT(*) FROM Customers {where.Sql}", where.Parameters));

            var pageSql = $@"SELECT * FROM Customers {where.Sql}
                              {OrderBuilder.By(column, sortDescending)}
                              {DbFactory.Current.LimitClause(Math.Max(0, page - 1) * pageSize, pageSize)}";

            return (Query(pageSql, null, null, where.Parameters), total);
        }

        // ===== كتابة =====

        private const string InsertSql = @"
            INSERT INTO Customers
                (Code, Name, NameEn, Phone, Phone2, Email, Address, City, Country, TaxNumber, CommercialRegNo,
                 AccountCode, Balance, CreditLimit, PaymentTermDays, CurrencyId, CategoryId, Notes, IsActive, CreatedBy)
            VALUES
                (@code, @name, @nameEn, @phone, @phone2, @email, @address, @city, @country, @taxNumber, @commercialRegNo,
                 @accountCode, @balance, @creditLimit, @paymentTermDays, @currencyId, @categoryId, @notes, @isActive, @createdBy)";

        public int Insert(Customer c, DbConnection conn = null, DbTransaction tx = null) =>
            InsertGetId(InsertSql, conn, tx,
                ("@code", c.Code), ("@name", c.Name), ("@nameEn", c.NameEn), ("@phone", c.Phone), ("@phone2", c.Phone2),
                ("@email", c.Email), ("@address", c.Address), ("@city", c.City), ("@country", c.Country),
                ("@taxNumber", c.TaxNumber), ("@commercialRegNo", c.CommercialRegNo), ("@accountCode", c.AccountCode),
                ("@balance", c.Balance), ("@creditLimit", c.CreditLimit), ("@paymentTermDays", c.PaymentTermDays),
                ("@currencyId", c.CurrencyId), ("@categoryId", c.CategoryId), ("@notes", c.Notes ?? ""),
                ("@isActive", c.IsActive), ("@createdBy", c.CreatedBy));

        private const string UpdateSql = @"
            UPDATE Customers SET
                Name = @name, NameEn = @nameEn, Phone = @phone, Phone2 = @phone2, Email = @email, Address = @address,
                City = @city, Country = @country, TaxNumber = @taxNumber, CommercialRegNo = @commercialRegNo,
                CreditLimit = @creditLimit, PaymentTermDays = @paymentTermDays, CurrencyId = @currencyId,
                CategoryId = @categoryId, Notes = @notes, IsActive = @isActive, UpdatedAt = @now, UpdatedBy = @updatedBy
            WHERE Id = @id";

        public void Update(Customer c, DbConnection conn = null, DbTransaction tx = null) =>
            Exec(UpdateSql, conn, tx,
                ("@name", c.Name), ("@nameEn", c.NameEn), ("@phone", c.Phone), ("@phone2", c.Phone2), ("@email", c.Email),
                ("@address", c.Address), ("@city", c.City), ("@country", c.Country), ("@taxNumber", c.TaxNumber),
                ("@commercialRegNo", c.CommercialRegNo), ("@creditLimit", c.CreditLimit), ("@paymentTermDays", c.PaymentTermDays),
                ("@currencyId", c.CurrencyId), ("@categoryId", c.CategoryId), ("@notes", c.Notes ?? ""),
                ("@isActive", c.IsActive), ("@now", DateTime.Now), ("@updatedBy", c.UpdatedBy), ("@id", c.Id));

        /// <summary>يحدّث اسم العميل فقط عبر AccountCode — تستخدمها CustomerService.UpdateNameFromAccount عندما يتغيّر اسم الحساب المرتبط.</summary>
        public void UpdateNameByAccountCode(DbConnection conn, DbTransaction tx, string accountCode, string name) =>
            Exec("UPDATE Customers SET Name = @name, UpdatedAt = @now WHERE AccountCode = @code",
                conn, tx, ("@name", name), ("@now", DateTime.Now), ("@code", accountCode));

        /// <summary>حذف منطقي (IsDeleted=true).</summary>
        public void Delete(int id, string deletedBy, DbConnection conn = null, DbTransaction tx = null) =>
            Exec("UPDATE Customers SET IsDeleted = @d, DeletedAt = @now, DeletedBy = @by WHERE Id = @id",
                conn, tx, ("@d", true), ("@now", DateTime.Now), ("@by", deletedBy), ("@id", id));

        public void SetBalance(int id, decimal balance, DbConnection conn = null, DbTransaction tx = null) =>
            Exec("UPDATE Customers SET Balance = @b, UpdatedAt = @now WHERE Id = @id",
                conn, tx, ("@b", balance), ("@now", DateTime.Now), ("@id", id));
    }
}
