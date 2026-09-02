using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Application.Services;
using PrimeERP.Platform.Design;
using PrimeERP.Platform.Settings;
using Xunit;

namespace PrimeERP.Tests.Design
{
    [Collection("WpfApplication")]
    public class IdentityUpgradeTests : System.IDisposable
    {
        private readonly TestDatabaseFixture _db = new();
        public void Dispose() => _db.Dispose();

        [Fact]
        public void ExistingDatabaseOnOldPack_IsUpgradedOnce_ThenUserChoiceSticks()
        {
            WpfApplicationFixture.Run(() =>
            {
                var settings = _db.Services.GetRequiredService<ISettingsService>();
                var identity = _db.Services.GetRequiredService<IIdentityService>();

                settings.Set(SettingKeys.UI.Identity, "Default");
                settings.Set(SettingKeys.UI.IdentityBaseline, "");

                identity.Initialize();
                Assert.Equal("Signature", settings.Get(SettingKeys.UI.Identity, ""));

                settings.Set(SettingKeys.UI.Identity, "Corporate");
                identity.Initialize();
                Assert.Equal("Corporate", settings.Get(SettingKeys.UI.Identity, ""));
            });
        }
    }
}
