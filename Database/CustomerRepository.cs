using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using PrimeERP.Core.Database;
using PrimeERP.Models;
using Db = PrimeERP.Core.Database.DbHelper;

namespace PrimeERP.Database
{
    /// <summary>طبقة وصول بيانات العملاء — SQL خام ↔ Models فقط. بلا تحقق، بلا معاملات ذاتية، بلا حساب رصيد، بلا إنشاء/حذف حساب (كل ذلك مسؤولية Services/Parties/ICustomerService — F.3.1).</summary>
    public static class CustomerRepository
    {
        public static void CreateTable()
        {
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
        }

        private static Customer Map(DataRow row) => new()
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

        public static Customer GetById(int id) =>
            Db.Query("SELECT * FROM Customers WHERE Id = @id AND IsDeleted = @d", Db.Params(("@id", id), ("@d", false)))
              .AsEnumerable().Select(Map).FirstOrDefault();

        public static Customer GetByCode(string code) =>
            Db.Query("SELECT * FROM Customers WHERE Code = @c AND IsDeleted = @d", Db.Params(("@c", code), ("@d", false)))
              .AsEnumerable().Select(Map).FirstOrDefault();

        public static Customer GetByAccountCode(string accountCode) =>
            Db.Query("SELECT * FROM Customers WHERE AccountCode = @a AND IsDeleted = @d", Db.Params(("@a", accountCode), ("@d", false)))
              .AsEnumerable().Select(Map).FirstOrDefault();

        /// <summary>نفس GetByAccountCode أعلاه من داخل معاملة قائمة — تستخدمها CustomerService.DeleteByAccountCode المستدعاة من AccountService.Delete ضمن معاملته.</summary>
        public static Customer GetByAccountCode(DbConnection conn, DbTransaction tx, string accountCode) =>
            Db.Query(conn, tx, "SELECT * FROM Customers WHERE AccountCode = @a AND IsDeleted = @d", Db.Params(("@a", accountCode), ("@d", false)))
              .AsEnumerable().Select(Map).FirstOrDefault();

        public static List<Customer> GetAll(bool activeOnly = true)
        {
            var sql = activeOnly
                ? "SELECT * FROM Customers WHERE IsDeleted = @d AND IsActive = @a ORDER BY Name"
                : "SELECT * FROM Customers WHERE IsDeleted = @d ORDER BY Name";
            var parameters = activeOnly ? Db.Params(("@d", false), ("@a", true)) : Db.Params(("@d", false));
            return Db.Query(sql, parameters).AsEnumerable().Select(Map).ToList();
        }

        public static List<Customer> Search(string term, int maxResults)
        {
            var sql = $@"SELECT * FROM Customers
                         WHERE IsDeleted = @d AND IsActive = @a AND (Name LIKE @t OR Code LIKE @t OR Phone LIKE @t)
                         ORDER BY Name {DbFactory.Current.LimitClause(0, maxResults)}";
            return Db.Query(sql, Db.Params(("@d", false), ("@a", true), ("@t", $"%{term}%")))
                .AsEnumerable().Select(Map).ToList();
        }

        public static int CountAll(bool activeOnly = true)
        {
            var sql = activeOnly
                ? "SELECT COUNT(*) FROM Customers WHERE IsDeleted = @d AND IsActive = @a"
                : "SELECT COUNT(*) FROM Customers WHERE IsDeleted = @d";
            var parameters = activeOnly ? Db.Params(("@d", false), ("@a", true)) : Db.Params(("@d", false));
            return Convert.ToInt32(Db.Scalar(sql, parameters));
        }

        public static bool ExistsCode(string code, int? excludeId = null)
        {
            var sql = "SELECT COUNT(*) FROM Customers WHERE Code = @c AND IsDeleted = @d";
            var parameters = new List<(string, object)> { ("@c", code), ("@d", false) };
            if (excludeId.HasValue) { sql += " AND Id != @ex"; parameters.Add(("@ex", excludeId.Value)); }
            return Convert.ToInt64(Db.Scalar(sql, Db.Params(parameters.ToArray()))) > 0;
        }

        public static bool ExistsPhone(string phone, int? excludeId = null)
        {
            if (string.IsNullOrWhiteSpace(phone)) return false;
            var sql = "SELECT COUNT(*) FROM Customers WHERE Phone = @p AND IsDeleted = @d";
            var parameters = new List<(string, object)> { ("@p", phone), ("@d", false) };
            if (excludeId.HasValue) { sql += " AND Id != @ex"; parameters.Add(("@ex", excludeId.Value)); }
            return Convert.ToInt64(Db.Scalar(sql, Db.Params(parameters.ToArray()))) > 0;
        }

        public static bool ExistsName(string name, int? excludeId = null)
        {
            var sql = "SELECT COUNT(*) FROM Customers WHERE Name = @n AND IsDeleted = @d";
            var parameters = new List<(string, object)> { ("@n", name), ("@d", false) };
            if (excludeId.HasValue) { sql += " AND Id != @ex"; parameters.Add(("@ex", excludeId.Value)); }
            return Convert.ToInt64(Db.Scalar(sql, Db.Params(parameters.ToArray()))) > 0;
        }

        /// <summary>قائمة مُرقَّمة مع الفلاتر — بناء SQL شرطي حسب الفلاتر الممرَّرة (بناء استعلام فقط، لا قرار أعمال). sortColumn يُطابَق بقائمة أعمدة مسموحة صراحة (لا يُدرَج كنص حر في ORDER BY).</summary>
        public static (List<Customer> Items, int Total) GetPaged(
            int page, int pageSize,
            string searchText = null, bool? isActive = null, bool? hasBalance = null, bool? overCreditLimit = null,
            int? categoryId = null, string sortColumn = "Name", bool sortDescending = false)
        {
            var where = new List<string> { "IsDeleted = @deleted" };
            var parameters = new List<(string, object)> { ("@deleted", false) };

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                where.Add("(Name LIKE @search OR Code LIKE @search OR Phone LIKE @search)");
                parameters.Add(("@search", $"%{searchText}%"));
            }
            if (isActive.HasValue)
            {
                where.Add("IsActive = @active");
                parameters.Add(("@active", isActive.Value));
            }
            if (hasBalance.HasValue)
            {
                where.Add(hasBalance.Value ? "Balance != 0" : "Balance = 0");
            }
            if (overCreditLimit.HasValue)
            {
                where.Add(overCreditLimit.Value ? "(CreditLimit > 0 AND Balance > CreditLimit)" : "NOT (CreditLimit > 0 AND Balance > CreditLimit)");
            }
            if (categoryId.HasValue)
            {
                where.Add("CategoryId = @categoryId");
                parameters.Add(("@categoryId", categoryId.Value));
            }

            var whereClause = "WHERE " + string.Join(" AND ", where);

            var column = sortColumn switch
            {
                "Code"        => "Code",
                "Balance"     => "Balance",
                "CreditLimit" => "CreditLimit",
                "CreatedAt"   => "CreatedAt",
                _             => "Name"
            };
            var direction = sortDescending ? "DESC" : "ASC";

            var total = Convert.ToInt32(Db.Scalar($"SELECT COUNT(*) FROM Customers {whereClause}", Db.Params(parameters.ToArray())));

            var pageSql = $@"SELECT * FROM Customers {whereClause}
                              ORDER BY {column} {direction}, Id {direction}
                              {DbFactory.Current.LimitClause(Math.Max(0, page - 1) * pageSize, pageSize)}";

            var items = Db.Query(pageSql, Db.Params(parameters.ToArray())).AsEnumerable().Select(Map).ToList();

            return (items, total);
        }

        // ===== كتابة =====

        private const string InsertSql = @"
            INSERT INTO Customers
                (Code, Name, NameEn, Phone, Phone2, Email, Address, City, Country, TaxNumber, CommercialRegNo,
                 AccountCode, Balance, CreditLimit, PaymentTermDays, CurrencyId, CategoryId, Notes, IsActive, CreatedBy)
            VALUES
                (@code, @name, @nameEn, @phone, @phone2, @email, @address, @city, @country, @taxNumber, @commercialRegNo,
                 @accountCode, @balance, @creditLimit, @paymentTermDays, @currencyId, @categoryId, @notes, @isActive, @createdBy)";

        private static Dictionary<string, object> InsertParams(Customer c) => Db.Params(
            ("@code", c.Code), ("@name", c.Name), ("@nameEn", c.NameEn), ("@phone", c.Phone), ("@phone2", c.Phone2),
            ("@email", c.Email), ("@address", c.Address), ("@city", c.City), ("@country", c.Country),
            ("@taxNumber", c.TaxNumber), ("@commercialRegNo", c.CommercialRegNo), ("@accountCode", c.AccountCode),
            ("@balance", c.Balance), ("@creditLimit", c.CreditLimit), ("@paymentTermDays", c.PaymentTermDays),
            ("@currencyId", c.CurrencyId), ("@categoryId", c.CategoryId), ("@notes", c.Notes ?? ""),
            ("@isActive", c.IsActive), ("@createdBy", c.CreatedBy));

        public static int Insert(Customer c) => Db.InsertAndGetId(InsertSql, InsertParams(c));

        public static int Insert(DbConnection conn, DbTransaction tx, Customer c) => Db.InsertAndGetId(conn, tx, InsertSql, InsertParams(c));

        private const string UpdateSql = @"
            UPDATE Customers SET
                Name = @name, NameEn = @nameEn, Phone = @phone, Phone2 = @phone2, Email = @email, Address = @address,
                City = @city, Country = @country, TaxNumber = @taxNumber, CommercialRegNo = @commercialRegNo,
                CreditLimit = @creditLimit, PaymentTermDays = @paymentTermDays, CurrencyId = @currencyId,
                CategoryId = @categoryId, Notes = @notes, IsActive = @isActive, UpdatedAt = @now, UpdatedBy = @updatedBy
            WHERE Id = @id";

        private static Dictionary<string, object> UpdateParams(Customer c) => Db.Params(
            ("@name", c.Name), ("@nameEn", c.NameEn), ("@phone", c.Phone), ("@phone2", c.Phone2), ("@email", c.Email),
            ("@address", c.Address), ("@city", c.City), ("@country", c.Country), ("@taxNumber", c.TaxNumber),
            ("@commercialRegNo", c.CommercialRegNo), ("@creditLimit", c.CreditLimit), ("@paymentTermDays", c.PaymentTermDays),
            ("@currencyId", c.CurrencyId), ("@categoryId", c.CategoryId), ("@notes", c.Notes ?? ""),
            ("@isActive", c.IsActive), ("@now", DateTime.Now), ("@updatedBy", c.UpdatedBy), ("@id", c.Id));

        public static void Update(Customer c) => Db.Execute(UpdateSql, UpdateParams(c));

        public static void Update(DbConnection conn, DbTransaction tx, Customer c)
        {
            using var cmd = Db.CreateCommand(conn, tx, UpdateSql, UpdateParams(c));
            cmd.ExecuteNonQuery();
        }

        /// <summary>يحدّث اسم العميل فقط عبر AccountCode — تستخدمها CustomerService.UpdateNameFromAccount (المستدعاة من AccountService.Update ضمن معاملته) عندما يتغيّر اسم الحساب المرتبط.</summary>
        public static void UpdateNameByAccountCode(DbConnection conn, DbTransaction tx, string accountCode, string name)
        {
            using var cmd = Db.CreateCommand(conn, tx,
                "UPDATE Customers SET Name = @name, UpdatedAt = @now WHERE AccountCode = @code",
                Db.Params(("@name", name), ("@now", DateTime.Now), ("@code", accountCode)));
            cmd.ExecuteNonQuery();
        }

        /// <summary>حذف منطقي (IsDeleted=true).</summary>
        public static void Delete(int id, string deletedBy) =>
            Db.Execute(
                "UPDATE Customers SET IsDeleted = @d, DeletedAt = @now, DeletedBy = @by WHERE Id = @id",
                Db.Params(("@d", true), ("@now", DateTime.Now), ("@by", deletedBy), ("@id", id)));

        public static void Delete(DbConnection conn, DbTransaction tx, int id, string deletedBy)
        {
            using var cmd = Db.CreateCommand(conn, tx,
                "UPDATE Customers SET IsDeleted = @d, DeletedAt = @now, DeletedBy = @by WHERE Id = @id",
                Db.Params(("@d", true), ("@now", DateTime.Now), ("@by", deletedBy), ("@id", id)));
            cmd.ExecuteNonQuery();
        }

        public static void SetBalance(int id, decimal balance) =>
            Db.Execute("UPDATE Customers SET Balance = @b, UpdatedAt = @now WHERE Id = @id",
                Db.Params(("@b", balance), ("@now", DateTime.Now), ("@id", id)));

        public static void SetBalance(DbConnection conn, DbTransaction tx, int id, decimal balance)
        {
            using var cmd = Db.CreateCommand(conn, tx,
                "UPDATE Customers SET Balance = @b, UpdatedAt = @now WHERE Id = @id",
                Db.Params(("@b", balance), ("@now", DateTime.Now), ("@id", id)));
            cmd.ExecuteNonQuery();
        }
    }
}
