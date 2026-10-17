using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Application.PageServices.HR;
using PrimeERP.Application.PageServices.Parties;
using PrimeERP.Application.Reporting;
using PrimeERP.Application.Services.Entities;
using PrimeERP.Data.Repositories;
using PrimeERP.Domain.Calculations;
using PrimeERP.Domain.Entities;
using PrimeERP.Platform.Permissions;
using PrimeERP.Platform.Settings;
using Xunit;

namespace PrimeERP.Tests.Services
{
    /// <summary>المشاريع: حساباتها تُنشأ في الإعدادات، والمستخلص والساعات تُقيَّد عند الحفظ وتُعكس عند الحذف، والربحية منهما</summary>
    public class ProjectTests : IDisposable
    {
        private readonly TestDatabaseFixture _db = new();

        public ProjectTests() => AppSession.DevMode = true;

        public void Dispose() => _db.Dispose();

        private T Service<T>() => _db.Services.GetRequiredService<T>();

        private string Setting(string key) => Service<ISettingsProvider>().Get(key, "");

        private int NewProject(decimal contract, decimal retention)
        {
            var customer = Service<ICustomerService>().Create(new Customer { Name = $"عميل {Guid.NewGuid():N}" }).Value;
            var created = Service<Lookup<Project>>().Create(new Dictionary<string, object>
            {
                ["Name"] = $"مشروع {Guid.NewGuid():N}", ["CustomerId"] = customer.Id, ["ContractValue"] = contract, ["RetentionPercent"] = retention, ["Budget"] = 50_000m,
            });
            Assert.True(created.IsSuccess, created.ErrorMessage);
            return Convert.ToInt32(created.Value["Id"]);
        }

        private List<JournalLine> LinesOf(object entryId) => Service<IJournalRepository>().GetLines(Convert.ToInt32(entryId));

        [Fact]
        public void TheProjectAccounts_AreCreatedAndSetOnStartup()
        {
            foreach (var key in new[] { SettingKeys.Accounts.ProjectRevenue, SettingKeys.Accounts.ProjectCost, SettingKeys.Accounts.RetentionReceivable })
                Assert.NotNull(Service<IAccountRepository>().GetByCode(Setting(key)));
        }

        [Fact]
        public void AProgressBill_PostsTheNetToTheCustomer_TheRetentionAside_AndTheRevenue()
        {
            var project = NewProject(contract: 100_000m, retention: 10m);
            var bill = Service<Lookup<ProjectBill>>().Create(new Dictionary<string, object> { ["ProjectId"] = project, ["Date"] = DateTime.Today, ["Percent"] = 30m });
            Assert.True(bill.IsSuccess, bill.ErrorMessage);

            Assert.Equal((30_000m, 3_000m, 27_000m), ((decimal)bill.Value["Amount"], (decimal)bill.Value["Retention"], (decimal)bill.Value["Net"]));
            var lines = LinesOf(bill.Value["JournalEntryId"]);
            Assert.Equal(lines.Sum(l => l.Debit), lines.Sum(l => l.Credit));
            Assert.Equal(3_000m, lines.Single(l => l.AccountCode == Setting(SettingKeys.Accounts.RetentionReceivable)).Debit);
            Assert.Equal(30_000m, lines.Single(l => l.AccountCode == Setting(SettingKeys.Accounts.ProjectRevenue)).Credit);

            Assert.True(Service<Lookup<ProjectBill>>().Delete(Convert.ToInt32(bill.Value["Id"])).IsSuccess);
            Assert.Null(Service<IJournalRepository>().GetById(Convert.ToInt32(bill.Value["JournalEntryId"])));
        }

        [Fact]
        public void Timesheets_CostTheHours_PostThemToTheProject_AndFeedProfitability()
        {
            var project = NewProject(contract: 10_000m, retention: 0);
            var employee = Service<IEmployeeService>().Create(new Employee { Name = "موظف", HireDate = DateTime.Today.AddYears(-1), BasicSalary = 3000 }).Value.Id;

            var sheet = Service<Lookup<ProjectTimesheet>>().Create(new Dictionary<string, object>
            {
                ["ProjectId"] = project, ["EmployeeId"] = employee, ["Date"] = DateTime.Today, ["Hours"] = 8m, ["HourlyCost"] = 50m,
            });
            Assert.True(sheet.IsSuccess, sheet.ErrorMessage);
            Assert.Equal(400m, (decimal)sheet.Value["Cost"]);
            Assert.Equal(400m, LinesOf(sheet.Value["JournalEntryId"]).Single(l => l.AccountCode == Setting(SettingKeys.Accounts.ProjectCost)).Debit);

            Assert.True(Service<Lookup<ProjectBill>>().Create(new Dictionary<string, object> { ["ProjectId"] = project, ["Date"] = DateTime.Today, ["Amount"] = 1_000m }).IsSuccess);

            var report = Service<IProjectReportService>().Profitability(DateTime.Today.AddDays(-1), DateTime.Today);
            Assert.True(report.IsSuccess, report.ErrorMessage);
            var row = report.Value.Rows.Cast<ProjectRow>().Single(r => r.Billed == 1_000m && r.Cost == 400m);
            Assert.Equal((600m, 60m, 8m), (row.Profit, row.Margin, row.Hours));
        }

        [Fact]
        public void ABill_WithoutPercentOrAmount_IsRefused()
        {
            var project = NewProject(contract: 5_000m, retention: 0);
            Assert.False(Service<Lookup<ProjectBill>>().Create(new Dictionary<string, object> { ["ProjectId"] = project, ["Date"] = DateTime.Today }).IsSuccess);
        }

        [Fact]
        public void Progress_IsWeightedByTasks()
        {
            Assert.Equal(75m, ProjectCalc.Progress(new[] { (3m, 100m), (1m, 0m) }));
            Assert.Equal(0m, ProjectCalc.Progress(Array.Empty<(decimal, decimal)>()));
        }
    }
}
