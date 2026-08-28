using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using PrimeERP.Data.Repositories.Base;
using PrimeERP.Data.Schema;
using PrimeERP.Domain.Entities;

namespace PrimeERP.Data.Repositories
{
    public class JobTitleRepository : RepositoryBase<JobTitle>, IJobTitleRepository
    {
        protected override string TableName => "JobTitles";

        public void CreateTable() =>
            SchemaBuilder.Table("JobTitles")
                .Id()
                .Text("Name", 200, required: true)
                .Text("NameEn", 200)
                .Bool("IsActive", defaultValue: true)
                .Audit()
                .Create();

        public List<JobTitle> GetAll(bool includeInactive = false) =>
            includeInactive
                ? Query("SELECT * FROM JobTitles ORDER BY Name")
                : Query("SELECT * FROM JobTitles WHERE IsActive = @a ORDER BY Name", null, null, ("@a", true));

        public override JobTitle GetById(int id, DbConnection conn = null, DbTransaction tx = null) =>
            QueryOne("SELECT * FROM JobTitles WHERE Id = @id", conn, tx, ("@id", id));

        public int Insert(JobTitle j) =>
            InsertGetId("INSERT INTO JobTitles (Name, NameEn, IsActive) VALUES (@name, @nameEn, @active)",
                null, null, ("@name", j.Name), ("@nameEn", j.NameEn), ("@active", j.IsActive));

        public void Update(JobTitle j) =>
            Exec("UPDATE JobTitles SET Name = @name, NameEn = @nameEn, IsActive = @active, UpdatedAt = @now WHERE Id = @id",
                null, null, ("@name", j.Name), ("@nameEn", j.NameEn), ("@active", j.IsActive), ("@now", DateTime.Now), ("@id", j.Id));

        public void Delete(int id) => Exec("UPDATE JobTitles SET IsActive = @a WHERE Id = @id", null, null, ("@a", false), ("@id", id));

        protected override JobTitle Map(DataRow row) => new()
        {
            Id       = Convert.ToInt32(row["Id"]),
            Name     = row["Name"].ToString(),
            NameEn   = row["NameEn"] == DBNull.Value ? null : row["NameEn"].ToString(),
            IsActive = Convert.ToBoolean(row["IsActive"]),
        };
    }
}
