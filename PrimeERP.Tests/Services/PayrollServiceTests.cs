using PrimeERP.Application.Services.Ledger;
using PrimeERP.Application.Services.Entities;
using PrimeERP.Tests.Helpers;
using PrimeERP.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Application.DTOs.HR;
using PrimeERP.Application.Legacy.Accounting;
using PrimeERP.Application.Legacy.HR;
using PrimeERP.Platform.Permissions;
using Xunit;

namespace PrimeERP.Tests.Services
{
    /// <summary>المسير يُنشأ مسوّدةً ثم يُرحَّل</summary>
    public class PayrollServiceTests : IDisposable
    {
        private readonly TestDatabaseFixture _db = new();
        private readonly string _employeeCode;

        public PayrollServiceTests()
        {
            AppSession.DevMode = true;

            var departments = _db.Services.GetRequiredService<Lookup<Department>>();
            var jobTitles = _db.Services.GetRequiredService<Lookup<JobTitle>>();
            var deptId = departments.Add("قسم");
            var jobId = jobTitles.Add("وظيفة");

            _employeeCode = _db.Services.GetRequiredService<IEmployeeService>().Create(new CreateEmployeeDto
            { Name = "موظف", DepartmentId = deptId, JobTitleId = jobId, HireDate = DateTime.Today, BasicSalary = 5000 }).Value.Code;
        }

        public void Dispose() => _db.Dispose();

        private IPayrollService Payrolls => _db.Services.GetRequiredService<IPayrollService>();

        private CreatePayrollDto TheExample() => new()
        {
            PeriodStart = DateTime.Today.AddDays(-30), PeriodEnd = DateTime.Today, PaymentDate = DateTime.Today,
            Lines =
            {
                new CreatePayrollLineDto
                {
                    LineNo = 1, EmployeeCode = _employeeCode,
                    BasicSalary = 24000, Allowances = 1300,
                    Insurance = 1000, Tax = 500, Advances = 500
                }
            }
        };

        [Fact]
        public void ANewPayroll_IsADraftWithNoJournal()
        {
            var result = Payrolls.Create(TheExample());

            Assert.True(result.IsSuccess, result.ErrorMessage);
            Assert.False(result.Value.IsPosted, "المسير رُحِّل عند الإنشاء — والترحيل زرٌّ مستقلّ");
            Assert.Equal(23300m, result.Value.NetTotal);   // 25300 − 2000
        }

        [Fact]
        public void TheNetIsDerived_NotTaken()
        {
            var dto = TheExample();
            dto.Lines[0].Overtime = 700;
            dto.Lines[0].Deductions = 300;

            Assert.Equal(23700m, Payrolls.Create(dto).Value.NetTotal);
        }

        [Fact]
        public void WithholdingsBeyondEntitlement_AreRefused()
        {
            var dto = TheExample();
            dto.Lines[0].Advances = 99999;

            Assert.False(Payrolls.Create(dto).IsSuccess, "استقطاعٌ يتجاوز الاستحقاق مرّ");
        }

        [Fact]
        public void PostingProvesTheAccrual_AndLeavesTheTreasuryUntouched()
        {
            var payrollId = Payrolls.Create(TheExample()).Value.Id;

            var posted = Payrolls.Post(payrollId);
            Assert.True(posted.IsSuccess, posted.ErrorMessage);
            Assert.True(Payrolls.GetById(payrollId).Value.IsPosted);

            var balances = PostedBalances();

            Assert.Equal(24000m, balances["5101"]);
            Assert.Equal(1300m, balances["5102"]);

            Assert.Equal(-23300m, balances["2102"]);
            Assert.Equal(-1000m, balances["2103"]);
            Assert.Equal(-500m, balances["2104"]);

            Assert.False(balances.ContainsKey("1204"), "المسير مسّ الخزينة — والصرف مستندٌ مستقلّ");
        }

        [Fact]
        public void TheAdvanceIsWithheld_OnTheEmployeesOwnAccount()
        {
            var payrollId = Payrolls.Create(TheExample()).Value.Id;
            Payrolls.Post(payrollId);

            var employeeAccount = _db.Services.GetRequiredService<IEmployeeService>()
                .GetPaged(1, 10).Value.Items.First(e => e.Code == _employeeCode);

            var account = _db.Services.GetRequiredService<IAccountService>()
                .GetByCode(employeeAccount.AccountCode);

            Assert.True(account.IsSuccess, "الموظف بلا حساب سلفة");
            Assert.Equal(-500m, account.Value.Balance);   // دائن بقسط السلفة
        }

        [Fact]
        public void TheEntryIsBalanced()
        {
            Payrolls.Post(Payrolls.Create(TheExample()).Value.Id);

            var trial = _db.Services.GetRequiredService<IJournalService>()
                .GetTrialBalance(DateTime.Today.AddDays(-1), DateTime.Today.AddDays(1)).Value;

            Assert.Equal(trial.Sum(l => l.PeriodDebit), trial.Sum(l => l.PeriodCredit));
            Assert.Equal(25300m, trial.Sum(l => l.PeriodDebit));
        }

        [Fact]
        public void UnpostingRemovesTheEntry()
        {
            var payrollId = Payrolls.Create(TheExample()).Value.Id;
            Payrolls.Post(payrollId);

            Assert.True(Payrolls.Unpost(payrollId).IsSuccess);
            Assert.False(Payrolls.GetById(payrollId).Value.IsPosted);
            Assert.Empty(PostedBalances());
        }

        [Fact]
        public void AnEmptyPayroll_GeneratesALineForEveryActiveEmployee()
        {
            var result = Payrolls.Create(new CreatePayrollDto
            {
                PeriodStart = DateTime.Today.AddDays(-30), PeriodEnd = DateTime.Today, PaymentDate = DateTime.Today
            });

            Assert.True(result.IsSuccess, result.ErrorMessage);
            Assert.Single(result.Value.Lines);
            Assert.Equal(5000m, result.Value.Lines[0].BasicSalary);   // مسحوبٌ من بطاقة الموظف
            Assert.Equal(5000m, result.Value.Lines[0].NetSalary);
        }

        [Fact]
        public void EachLine_ShowsItsNet()
        {
            var line = Payrolls.Create(TheExample()).Value.Lines[0];

            Assert.Equal(23300m, line.NetSalary);
        }


        [Fact]
        public void PostingTwice_IsRefused()
        {
            var payrollId = Payrolls.Create(TheExample()).Value.Id;
            Payrolls.Post(payrollId);

            Assert.False(Payrolls.Post(payrollId).IsSuccess);
        }

        private Dictionary<string, decimal> PostedBalances() =>
            _db.Services.GetRequiredService<IJournalService>()
                .GetTrialBalance(DateTime.Today.AddDays(-1), DateTime.Today.AddDays(1)).Value
                .Where(l => l.PeriodDebit != 0 || l.PeriodCredit != 0)
                .ToDictionary(l => l.Code, l => l.PeriodDebit - l.PeriodCredit);
    }
}
