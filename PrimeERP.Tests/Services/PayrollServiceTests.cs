using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Application.DTOs.Accounting;
using PrimeERP.Application.DTOs.HR;
using PrimeERP.Application.Services;
using PrimeERP.Application.Services.Accounting;
using PrimeERP.Application.Services.HR;
using PrimeERP.Platform.Permissions;
using PrimeERP.Platform.Settings;
using Xunit;

namespace PrimeERP.Tests.Services
{
    public class PayrollServiceTests : IDisposable
    {
        private readonly TestDatabaseFixture _db = new();
        private readonly string _employeeCode;

        public PayrollServiceTests()
        {
            AppSession.DevMode = true;

            var accounts = _db.Services.GetRequiredService<IAccountService>();
            var settings = _db.Services.GetRequiredService<ISettingsService>();
            string LeafUnder(string parentCode, string name)
            {
                var parent = accounts.GetByCode(parentCode).Value;
                return accounts.Create(new CreateAccountDto { ParentId = parent.Id, Name = name, IsLeaf = true, SkipAutoLink = true }).Value.Code;
            }
            settings.Set(SettingKeys.Accounts.Salaries, LeafUnder("51", "مرتبات"));
            settings.Set(SettingKeys.Accounts.Cash, LeafUnder("1204", "الصندوق"));

            var departments = _db.Services.GetRequiredService<IDepartmentService>();
            var jobTitles = _db.Services.GetRequiredService<IJobTitleService>();
            var deptId = departments.Create(new CreateDepartmentDto { Name = "قسم" }).Value.Id;
            var jobId = jobTitles.Create(new CreateJobTitleDto { Name = "وظيفة" }).Value.Id;

            var employees = _db.Services.GetRequiredService<IEmployeeService>();
            _employeeCode = employees.Create(new CreateEmployeeDto { Name = "موظف", DepartmentId = deptId, JobTitleId = jobId, HireDate = DateTime.Today, BasicSalary = 5000 }).Value.Code;
        }

        public void Dispose() => _db.Dispose();

        [Fact]
        public void Create_ValidPayroll_PostsBalancedJournal()
        {
            var payroll = _db.Services.GetRequiredService<IPayrollService>();
            var result = payroll.Create(new CreatePayrollDto
            {
                PeriodStart = DateTime.Today.AddDays(-30), PeriodEnd = DateTime.Today, PaymentDate = DateTime.Today,
                Lines = { new CreatePayrollLineDto { LineNo = 1, EmployeeCode = _employeeCode, BasicSalary = 5000, Allowances = 500, Deductions = 200 } }
            });

            Assert.True(result.IsSuccess, result.ErrorMessage);
            Assert.Equal(5300, result.Value.NetTotal);

            var journal = _db.Services.GetRequiredService<IJournalService>();
            var tb = journal.GetTrialBalance(DateTime.Today.AddDays(-1), DateTime.Today.AddDays(1)).Value;
            Assert.Equal(tb.Sum(l => l.PeriodDebit), tb.Sum(l => l.PeriodCredit));
            Assert.True(tb.Sum(l => l.PeriodDebit) > 0);
        }
    }
}
