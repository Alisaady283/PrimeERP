using System;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Application.PageServices.Admin;
using PrimeERP.Platform.Settings;
using Xunit;

namespace PrimeERP.Tests.Services
{
    public class BuilderCopyTests : IDisposable
    {
        private readonly TestDatabaseFixture _db = new();

        public void Dispose() => _db.Dispose();

        [Fact]
        public void TheBuilderCopy_IsNeverUpdated()
        {
            var settings = _db.Services.GetRequiredService<ISettingsProvider>();
            settings.SetRaw(SettingKeys.Developer.AdminToken, "token");
            settings.SetRaw(SettingKeys.License.Serial, "AAAAA-BBBBB");

            var check = _db.Services.GetRequiredService<IUpdateService>().CheckAsync().Result;

            Assert.False(check.IsSuccess);
        }
    }
}
