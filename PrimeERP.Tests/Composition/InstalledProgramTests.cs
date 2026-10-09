using System;
using System.IO;
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
            }
            finally
            {
                File.Delete(file);
                AppSession.DevMode = dev;
            }
        }
    }
}
