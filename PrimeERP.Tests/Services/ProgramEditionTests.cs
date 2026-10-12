using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Application.DTOs.Common;
using PrimeERP.Application.PageServices.Admin;
using PrimeERP.Platform.Permissions;
using PrimeERP.Tests.Helpers;
using Xunit;

namespace PrimeERP.Tests.Services
{
    [Collection("Database")]
    public class ProgramEditionTests : IDisposable
    {
        private readonly TestDatabaseFixture _db = new();
        private readonly string _target = TestPaths.Unique("PrimeERP.Tests.Edition");

        public ProgramEditionTests() => AppSession.DevMode = true;

        public void Dispose()
        {
            _db.Dispose();
            if (Directory.Exists(_target)) Directory.Delete(_target, recursive: true);
        }

        [Fact]
        public void TheCreatedProgram_StartsFromAnEmptySeededDatabase_WithTheSavedCustomer()
        {
            var result = _db.Services.GetRequiredService<IProgramEditionService>().Create(new CreateEditionDto
            {
                ModuleKeys = new List<string> { "Accounts", "SalesInvoices" },
                Simplified = true,
                Serial = "AAAAA-BBBBB-CCCCC-DDDDD",
                Customer = "عميل",
                Version = "1.0.3",
                TargetFolder = _target
            });
            Assert.True(result.IsSuccess, result.ErrorMessage);

            Assert.Empty(Directory.GetFiles(_target, "*.db", SearchOption.AllDirectories));

            var database = JsonDocument.Parse(File.ReadAllText(Path.Combine(_target, "appsettings.json"))).RootElement.GetProperty("Database");
            Assert.Equal("Sqlite", database.GetProperty("Provider").GetString());
            Assert.Equal(Path.Combine(_target, "PrimeERP.db"), database.GetProperty("FilePath").GetString());

            var license = JsonDocument.Parse(File.ReadAllText(Path.Combine(_target, "license.json"))).RootElement;
            Assert.Equal("AAAAA-BBBBB-CCCCC-DDDDD", license.GetProperty("serial").GetString());
            Assert.Equal("عميل", license.GetProperty("customer").GetString());
            Assert.Equal("Accounts,SalesInvoices", license.GetProperty("manifest").GetString());
            Assert.True(license.GetProperty("simplified").GetBoolean());
            Assert.Equal("1.0.3", license.GetProperty("version").GetString());
        }
    }
}
