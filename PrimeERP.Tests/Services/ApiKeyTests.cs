using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Application.PageServices.Security;
using PrimeERP.Application.Services.Integration;
using PrimeERP.Data.Core;
using PrimeERP.Data.Repositories.Base;
using PrimeERP.Domain.Entities;
using PrimeERP.Platform.Permissions;
using Xunit;

namespace PrimeERP.Tests.Services
{
    /// <summary>مفاتيح الربط: بصمةٌ لا نص، وتعمل بصفة مستخدمها، وتُبطَل بإيقافها أو انتهائها</summary>
    public class ApiKeyTests : IDisposable
    {
        private readonly TestDatabaseFixture _db = new();
        private readonly int _userId;

        public ApiKeyTests()
        {
            AppSession.DevMode = true;
            var roleId = _db.Permissions.InsertRole("Integrator", "ربط");
            _db.Permissions.ReplaceRolePermissions(roleId, new[] { "Customers.View" });
            _userId = _db.AddUser("n8n", "unused", "n8n", roleId);
        }

        public void Dispose() => _db.Dispose();

        private T Service<T>() => _db.Services.GetRequiredService<T>();

        private int Key(DateTime? expires = null, bool active = true) =>
            Service<ILookupRepository<ApiKey>>().Insert(new ApiKey { Code = "AK", Name = "n8n", UserId = _userId, ExpiresAt = expires, IsActive = active });

        [Fact]
        public void AnIssuedKey_IsStoredAsItsHashOnly_AndActsAsItsUser()
        {
            var id = Key();
            var plain = Service<IApiKeyService>().Issue(id).Value;

            using (var db = DbContextFactory.Open())
            {
                var row = db.Set<ApiKey>().Single(k => k.Id == id);
                Assert.Equal(ApiKeys.Hash(plain), row.KeyHash);
                Assert.NotEqual(plain, row.KeyHash);
                Assert.Equal(plain[..10], row.Prefix);
            }

            var user = Service<IAuthService>().VerifyKey(plain);
            Assert.True(user.IsSuccess, user.ErrorMessage);
            Assert.Equal(_userId, user.Value.Id);
            Assert.Contains("Customers.View", user.Value.Permissions);
        }

        [Fact]
        public void ReissuingAKey_InvalidatesTheOldOne()
        {
            var id = Key();
            var first = Service<IApiKeyService>().Issue(id).Value;
            Service<IApiKeyService>().Issue(id);

            Assert.True(Service<IAuthService>().VerifyKey(first).IsFailure);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void AnExpiredOrDisabledKey_IsRefused(bool expired)
        {
            var id = expired ? Key(expires: DateTime.Now.AddDays(-1)) : Key(active: false);
            var plain = Service<IApiKeyService>().Issue(id).Value;

            Assert.True(Service<IAuthService>().VerifyKey(plain).IsFailure);
            Assert.True(Service<IAuthService>().VerifyKey("pk_guess").IsFailure);
        }
    }
}
