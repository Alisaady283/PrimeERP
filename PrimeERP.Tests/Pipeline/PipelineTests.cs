using System.Collections.Generic;
using PrimeERP.Application.Pipeline;
using PrimeERP.Application.Pipeline.Steps;
using PrimeERP.Domain.Contracts;
using PrimeERP.Domain.Enums;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Audit;
using PrimeERP.Platform.Permissions;
using Xunit;
using Db = PrimeERP.Data.Core.DbHelper;

namespace PrimeERP.Tests.Pipeline
{
    public class TestEntity
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class TestEntityValidator : IValidator<TestEntity>
    {
        public ValidationResult Validate(TestEntity item)
        {
            var result = new ValidationResult();
            if (string.IsNullOrWhiteSpace(item.Name)) result.AddError("Name", "Name required");
            return result;
        }
    }

    public class FakePermissionService : IPermissionService
    {
        public bool Allow = true;
        public bool Can(string key) => Allow;
        public bool CanAny(params string[] keys) => Allow;
        public bool CanAll(params string[] keys) => Allow;
        public void LoadForUser(int userId) { }
        public IEnumerable<string> GetUserPermissions(int userId) => new List<string>();
    }

    public class FakeAuditLogger : IAuditLogger
    {
        public int CallCount;
        public void Log(string tableName, int recordId, AuditAction action, object oldValue = null, object newValue = null, string details = null)
            => CallCount++;
    }

    /// <summary>يختبر البنية التحتية للـ Pipeline بكيان وهمي قبل استخدامها مع أي كيان محاسبي حقيقي (شرط إلزامي في R6).</summary>
    [Collection("Database")]
    public class PipelineTests
    {
        public PipelineTests(TestDatabaseFixture db) { }

        [Fact]
        public void Execute_runs_steps_in_order_and_produces_output()
        {
            var log = new List<string>();
            var pipeline = new Pipeline<string>()
                .Step(new FuncStep("A", ctx => { log.Add("A"); return Result.Ok(); }))
                .Step(new FuncStep("B", ctx => { log.Add("B"); ctx.Output = "done"; return Result.Ok(); }));

            var result = pipeline.Execute(null);

            Assert.True(result.IsSuccess);
            Assert.Equal("done", result.Value);
            Assert.Equal(new[] { "A", "B" }, log);
        }

        [Fact]
        public void Execute_stops_at_first_failure()
        {
            var log = new List<string>();
            var pipeline = new Pipeline<string>()
                .Step(new FuncStep("A", ctx => Result.Fail("boom")))
                .Step(new FuncStep("B", ctx => { log.Add("B"); return Result.Ok(); }));

            var result = pipeline.Execute(null);

            Assert.True(result.IsFailure);
            Assert.Equal("boom", result.ErrorMessage);
            Assert.Empty(log);
        }

        [Fact]
        public void PermissionStep_fails_with_unauthorized_when_denied()
        {
            var step = new PermissionStep(new FakePermissionService { Allow = false }, "Test.View", "denied");
            var result = step.Execute(new PipelineContext());

            Assert.True(result.IsFailure);
            Assert.Equal(ErrorCode.Unauthorized, result.ErrorCode);
        }

        [Fact]
        public void PermissionStep_succeeds_when_allowed()
        {
            var step = new PermissionStep(new FakePermissionService { Allow = true }, "Test.View", "denied");
            Assert.True(step.Execute(new PipelineContext()).IsSuccess);
        }

        [Fact]
        public void ValidationStep_fails_and_reports_validation_error_code()
        {
            var step = new ValidationStep<TestEntity>(new TestEntityValidator());
            var ctx = new PipelineContext { Input = new TestEntity { Name = "" } };

            var result = step.Execute(ctx);

            Assert.True(result.IsFailure);
            Assert.Equal(ErrorCode.ValidationFailed, result.ErrorCode);
        }

        [Fact]
        public void ValidationStep_succeeds_for_valid_entity()
        {
            var step = new ValidationStep<TestEntity>(new TestEntityValidator());
            var ctx = new PipelineContext { Input = new TestEntity { Name = "ok" } };

            Assert.True(step.Execute(ctx).IsSuccess);
        }

        [Fact]
        public void AuditStep_invokes_logger_with_recordId_from_context_items()
        {
            var audit = new FakeAuditLogger();
            var step = new AuditStep(audit, "TestEntities", AuditAction.Insert);
            var ctx = new PipelineContext();
            ctx.Items["AuditRecordId"] = 42;

            step.Execute(ctx);

            Assert.Equal(1, audit.CallCount);
        }

        [Fact]
        public void TransactionStep_commits_when_all_inner_steps_succeed()
        {
            Db.Execute("CREATE TABLE IF NOT EXISTS PipelineTestScratch (Id INTEGER PRIMARY KEY, Name TEXT)");
            Db.Execute("DELETE FROM PipelineTestScratch");

            var step = new TransactionStep(new List<IStep>
            {
                new FuncStep("Insert", ctx =>
                {
                    using var cmd = Db.CreateCommand(ctx.Conn, ctx.Tx,
                        "INSERT INTO PipelineTestScratch (Name) VALUES (@n)", Db.Params(("@n", "committed")));
                    cmd.ExecuteNonQuery();
                    return Result.Ok();
                })
            });

            var result = step.Execute(new PipelineContext());

            Assert.True(result.IsSuccess);
            Assert.Equal(1L, (long)Db.Scalar("SELECT COUNT(*) FROM PipelineTestScratch"));
        }

        [Fact]
        public void TransactionStep_rolls_back_when_an_inner_step_fails()
        {
            Db.Execute("CREATE TABLE IF NOT EXISTS PipelineTestScratch (Id INTEGER PRIMARY KEY, Name TEXT)");
            Db.Execute("DELETE FROM PipelineTestScratch");

            var step = new TransactionStep(new List<IStep>
            {
                new FuncStep("Insert", ctx =>
                {
                    using var cmd = Db.CreateCommand(ctx.Conn, ctx.Tx,
                        "INSERT INTO PipelineTestScratch (Name) VALUES (@n)", Db.Params(("@n", "rolledback")));
                    cmd.ExecuteNonQuery();
                    return Result.Ok();
                }),
                new FuncStep("Fail", ctx => Result.Fail("boom"))
            });

            var result = step.Execute(new PipelineContext());

            Assert.True(result.IsFailure);
            Assert.Equal(0L, (long)Db.Scalar("SELECT COUNT(*) FROM PipelineTestScratch"));
        }

        [Fact]
        public void TransactionStep_reuses_existing_connection_instead_of_opening_a_new_one()
        {
            Db.Execute("CREATE TABLE IF NOT EXISTS PipelineTestScratch (Id INTEGER PRIMARY KEY, Name TEXT)");
            Db.Execute("DELETE FROM PipelineTestScratch");

            using var conn = Db.GetConnection();
            using var tx = conn.BeginTransaction();

            var step = new TransactionStep(new List<IStep>
            {
                new FuncStep("Insert", ctx =>
                {
                    Assert.Same(conn, ctx.Conn); // نفس الاتصال الخارجي — لم يُفتح اتصال جديد
                    using var cmd = Db.CreateCommand(ctx.Conn, ctx.Tx,
                        "INSERT INTO PipelineTestScratch (Name) VALUES (@n)", Db.Params(("@n", "outer")));
                    cmd.ExecuteNonQuery();
                    return Result.Ok();
                })
            });

            var ctx2 = new PipelineContext { Conn = conn, Tx = tx };
            var result = step.Execute(ctx2);
            tx.Commit();

            Assert.True(result.IsSuccess);
            Assert.Equal(1L, (long)Db.Scalar("SELECT COUNT(*) FROM PipelineTestScratch"));
        }

        [Fact]
        public void FullPipeline_permission_validate_transaction_audit_endtoend()
        {
            Db.Execute("CREATE TABLE IF NOT EXISTS PipelineTestScratch (Id INTEGER PRIMARY KEY, Name TEXT)");
            Db.Execute("DELETE FROM PipelineTestScratch");

            var audit = new FakeAuditLogger();
            var pipeline = new Pipeline<TestEntity>()
                .Permission(new FakePermissionService { Allow = true }, "Test.Create", "denied")
                .Validate(new TestEntityValidator())
                .InTransaction(tx => tx.Save("Save", ctx =>
                {
                    var entity = ctx.InputAs<TestEntity>();
                    using var cmd = Db.CreateCommand(ctx.Conn, ctx.Tx,
                        "INSERT INTO PipelineTestScratch (Name) VALUES (@n)", Db.Params(("@n", entity.Name)));
                    cmd.ExecuteNonQuery();
                    entity.Id = 1;
                    ctx.Output = entity;
                    ctx.Items["AuditRecordId"] = entity.Id;
                    return Result.Ok();
                }))
                .Audit(audit, "TestEntities", AuditAction.Insert);

            var result = pipeline.Execute(new TestEntity { Name = "fake" });

            Assert.True(result.IsSuccess);
            Assert.Equal("fake", result.Value.Name);
            Assert.Equal(1, audit.CallCount);
            Assert.Equal(1L, (long)Db.Scalar("SELECT COUNT(*) FROM PipelineTestScratch"));
        }
    }
}
