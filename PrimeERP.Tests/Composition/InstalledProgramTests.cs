using System;
using System.IO;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Composition.Registry;
using PrimeERP.Platform.Permissions;
using PrimeERP.Platform.Settings;
using Xunit;

namespace PrimeERP.Tests.Composition
{
    [Collection("Database")]
    public class InstalledProgramTests
    {
        [Fact]
        public void TheInstallersLicenseFile_LimitsTheProgramToTheSelectedPages()
        {
            var file = Path.Combine(AppContext.BaseDirectory, "license.json");
            var dev = AppSession.DevMode;
            try
            {
                AppSession.SignOut();
                AppSession.DevMode = false;
                File.WriteAllText(file, "{\"serial\":\"AAAAA-BBBBB\",\"customer\":\"عميل\",\"manifest\":\"Accounts,Journals,Settings\",\"simplified\":true}");

                using var db = new TestDatabaseFixture();
                var registry = db.Services.GetRequiredService<IModuleRegistry>();

                Assert.NotNull(registry.Get("Accounts"));
                Assert.Null(registry.Get("Payroll"));
                Assert.Null(registry.Get("SalesInvoices"));
                Assert.Equal("AAAAA-BBBBB", db.Services.GetRequiredService<ISettingsProvider>().Get(SettingKeys.License.Serial, ""));
                Assert.False(File.Exists(file));

                var groups = PrimeERP.Composition.Definitions.PermissionTreeFactory.Build(registry).Select(g => g.Id).ToList();
                Assert.Contains("Accounts", groups);
                Assert.DoesNotContain("HR", groups);
                Assert.DoesNotContain("Sales", groups);
            }
            finally
            {
                File.Delete(file);
                AppSession.DevMode = dev;
            }
        }

        [Fact]
        public void TheFullProgram_ShowsEveryPermissionGroup()
        {
            using var db = new TestDatabaseFixture();
            var registry = db.Services.GetRequiredService<IModuleRegistry>();

            Assert.Equal(PrimeERP.Composition.Definitions.PermissionTreeFactory.Build().Count,
                PrimeERP.Composition.Definitions.PermissionTreeFactory.Build(registry).Count);
        }
    }
}
