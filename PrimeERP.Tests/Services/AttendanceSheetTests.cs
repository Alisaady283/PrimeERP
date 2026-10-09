using PrimeERP.Application.Services.Entities;
using PrimeERP.Tests.Helpers;
using PrimeERP.Domain.Entities;
using PrimeERP.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Application.DTOs.HR;
using PrimeERP.Application.PageServices.Accounting;
using PrimeERP.Application.PageServices.HR;
using PrimeERP.Data.Repositories.Base;
using PrimeERP.Platform.Permissions;
using Xunit;

namespace PrimeERP.Tests.Services
{
    public class AttendanceSheetTests : IDisposable
    {
        private readonly TestDatabaseFixture _db = new();
        private readonly string _code;
        private readonly int _unpaidLeave;
        private readonly DateTime _day = new(DateTime.Today.Year, DateTime.Today.Month, 1);

        public AttendanceSheetTests()
        {
            AppSession.DevMode = true;
            Settings.Set(PrimeERP.Platform.Settings.SettingKeys.Edition.PayrollPays, false);

            var deptId = _db.Services.GetRequiredService<Lookup<Department>>().Add("قسم");
            var jobId = _db.Services.GetRequiredService<Lookup<JobTitle>>().Add("وظيفة");
            _unpaidLeave = _db.Services.GetRequiredService<ILookupRepository<LeaveType>>()
                .Insert(new LeaveType { Name = "بدون أجر", IsPaid = false });

            _code = _db.Services.GetRequiredService<IEmployeeService>().Create(new Employee
            {
                Name = "موظف", DepartmentId = deptId, JobTitleId = jobId, HireDate = _day, BasicSalary = 3000,
                WorkStart = new TimeSpan(9, 0, 0), WorkEnd = new TimeSpan(17, 0, 0)
            }).Value.Code;
        }

        public void Dispose() => _db.Dispose();

        private IAttendanceService Sheets => _db.Services.GetRequiredService<IAttendanceService>();
        private IPayrollService Payrolls => _db.Services.GetRequiredService<IPayrollService>();
        private PrimeERP.Application.PageServices.Admin.ISettingsService Settings => _db.Services.GetRequiredService<PrimeERP.Application.PageServices.Admin.ISettingsService>();

        private PrimeERP.Domain.Entities.Treasury FundedTreasury(decimal amount)
        {
            var treasury = _db.Services.GetRequiredService<PrimeERP.Application.PageServices.Treasury.ITreasuryService>().GetUsable().Value.First();
            var accounts = _db.Services.GetRequiredService<IAccountService>();
            var capital = accounts.Create(new PrimeERP.Application.DTOs.Accounting.CreateAccountDto
                { ParentId = accounts.GetByCode("31").Value.Id, Name = "رأس مال الاختبار", IsLeaf = true, SkipAutoLink = true }).Value.Code;
            _db.Services.GetRequiredService<IJournalService>().Create(new PrimeERP.Application.DTOs.Accounting.CreateJournalDto
            {
                EntryDate = _day.AddYears(-1), Description = "تمويل",
                Lines =
                {
                    new PrimeERP.Application.DTOs.Accounting.CreateJournalLineDto { LineNo = 1, AccountCode = treasury.AccountCode, Debit = amount },
                    new PrimeERP.Application.DTOs.Accounting.CreateJournalLineDto { LineNo = 2, AccountCode = capital, Credit = amount }
                }
            });
            return treasury;
        }

        private AttendanceSheetDto Save(DateTime date, Action<AttendanceLineDto> change)
        {
            var opened = Sheets.Open(date);
            if (opened.IsSuccess)
            {
                change(opened.Value.Lines.Single(l => l.EmployeeCode == _code));
                var created = Sheets.Create(opened.Value);
                Assert.True(created.IsSuccess, created.ErrorMessage);
                return created.Value;
            }

            var id = Sheets.GetPaged(1, 100).Value.Items.Single(d => d.Date == date).Id;
            var sheet = Sheets.GetById(id).Value;
            change(sheet.Lines.Single(l => l.EmployeeCode == _code));
            var updated = Sheets.Update(sheet);
            Assert.True(updated.IsSuccess, updated.ErrorMessage);
            return Sheets.GetById(Sheets.GetPaged(1, 100).Value.Items.Single(d => d.Date == date).Id).Value;
        }

        [Fact]
        public void TheSheet_ListsEveryActiveEmployee_PresentOnTheirShift()
        {
            var line = Sheets.Open(_day).Value.Lines.Single(l => l.EmployeeCode == _code);

            Assert.Equal((int)AttendanceStatus.Present, line.Status);
            Assert.Equal("09:00", line.CheckIn);
            Assert.Equal("17:00", line.CheckOut);
            Assert.Equal("قسم", line.DepartmentName);
        }

        [Fact]
        public void LateAndOvertime_AreDerivedFromTheShift()
        {
            var line = Save(_day, l => { l.CheckIn = "09:30"; l.CheckOut = "19:00"; }).Lines.Single(l => l.EmployeeCode == _code);

            Assert.Equal("00:30", line.Late);
            Assert.Equal("02:00", line.Overtime);
        }

        [Fact]
        public void SavingTheSameDayAgain_EditsItInPlace()
        {
            Save(_day, l => l.CheckIn = "09:30");
            var line = Save(_day, l => l.CheckIn = "10:00").Lines.Single(l => l.EmployeeCode == _code);

            Assert.Equal("10:00", line.CheckIn);
            Assert.Equal("01:00", line.Late);
            Assert.Single(Sheets.GetPaged(1, 100).Value.Items, d => d.Date == _day);
            Assert.False(Sheets.Open(_day).IsSuccess, "يومٌ مسجَّل فُتح كشفاً جديداً");
        }

        [Fact]
        public void ALeaveWithoutItsType_IsRefused()
        {
            var sheet = Sheets.Open(_day).Value;
            sheet.Lines.Single(l => l.EmployeeCode == _code).Status = (int)AttendanceStatus.Leave;

            Assert.False(Sheets.Create(sheet).IsSuccess, "إجازةٌ بلا نوع مرّت");
        }

        [Fact]
        public void ThePayroll_OpensOnTheCycle_WithTheSheetsDeductionsAndOvertime()
        {
            Save(_day, l => l.Status = (int)AttendanceStatus.Absent);
            Save(_day.AddDays(1), l => { l.Status = (int)AttendanceStatus.Leave; l.LeaveTypeId = _unpaidLeave; });
            Save(_day.AddDays(2), l => l.CheckIn = "09:30");
            Save(_day.AddDays(3), l => l.CheckIn = "09:10");
            Save(_day.AddDays(4), l => l.CheckOut = "19:00");
            Save(_day.AddDays(5), l => l.CheckOut = "17:30");

            var opened = Payrolls.Open(_day.Month, _day.Year);
            Assert.True(opened.IsSuccess, opened.ErrorMessage);
            Assert.Equal(_day, opened.Value.PeriodStart);
            Assert.Equal(_day.AddMonths(1).AddDays(-1), opened.Value.PeriodEnd);

            var line = opened.Value.Lines.Single(l => l.EmployeeCode == _code);
            Assert.Equal(37.5m, line.Overtime);
            Assert.Equal(206.25m, line.Deductions);
            Assert.Equal(2831.25m, line.NetSalary);
        }

        [Fact]
        public void ThePayrollFromTheSheet_PostsABalancedAccrualEntry()
        {
            Save(_day, l => l.Status = (int)AttendanceStatus.Absent);

            var created = Payrolls.Create(Payrolls.Open(_day.Month, _day.Year).Value);
            Assert.True(created.IsSuccess, created.ErrorMessage);

            var lines = _db.Services.GetRequiredService<IJournalService>()
                .GetTrialBalance(_day.AddDays(-1), _day.AddMonths(2)).Value
                .Where(l => l.PeriodDebit != 0 || l.PeriodCredit != 0).ToList();

            Assert.Equal(lines.Sum(l => l.PeriodDebit), lines.Sum(l => l.PeriodCredit));
            Assert.Equal(2900m, -lines.Single(l => l.Code == "2202004").PeriodDebit + lines.Single(l => l.Code == "2202004").PeriodCredit);
        }

        [Fact]
        public void ASavedPayroll_CannotBeEdited()
        {
            var dto = Payrolls.Open(_day.Month, _day.Year).Value;
            dto.Id = Payrolls.Create(dto).Value.Id;
            dto.Lines[0].Allowances = 100;

            Assert.False(Payrolls.Update(dto).IsSuccess, "المسير المحفوظ عُدِّل");
        }

        [Fact]
        public void APayrollMonth_IsTakenOnce_AndTheNextOneOpensAfterIt()
        {
            Assert.True(Payrolls.Create(Payrolls.Open(_day.Month, _day.Year).Value).IsSuccess);

            Assert.False(Payrolls.Open(_day.Month, _day.Year).IsSuccess, "شهرٌ له مسير فُتح ثانيةً");
            var next = Payrolls.Open().Value;
            Assert.Equal((_day.AddMonths(1).Month, _day.AddMonths(1).Year), (next.Month, next.Year));
        }

        [Fact]
        public void AnAllowance_IsEnteredByDate_AndLandsInItsPayrollMonth()
        {
            var created = _db.Services.GetRequiredService<IAllowanceService>()
                .Create(new CreateEmployeeMovementDto { EmployeeCode = _code, Date = _day.AddDays(9), Amount = 250 });
            Assert.True(created.IsSuccess, created.ErrorMessage);
            Assert.Equal((_day.Month, _day.Year), (created.Value.Month, created.Value.Year));

            Assert.Equal(250m, Payrolls.Open(_day.Month, _day.Year).Value.Lines.Single(l => l.EmployeeCode == _code).Allowances);
        }

        [Fact]
        public void AnAdvance_IsPaidFromTheTreasury_WithheldInPayroll_AndReversedOnDelete()
        {
            var treasury = FundedTreasury(1000);
            var advances = _db.Services.GetRequiredService<IAdvanceService>();

            var created = advances.Create(new CreateEmployeeMovementDto { EmployeeCode = _code, Date = _day, Amount = 400, TreasuryId = treasury.Id });
            Assert.True(created.IsSuccess, created.ErrorMessage);
            Assert.Equal(400m, Payrolls.Open(_day.Month, _day.Year).Value.Lines.Single(l => l.EmployeeCode == _code).Advances);

            Assert.True(advances.Delete(created.Value.Id).IsSuccess);
            Assert.Equal(0m, Payrolls.Open(_day.Month, _day.Year).Value.Lines.Single(l => l.EmployeeCode == _code).Advances);
        }

        [Fact]
        public void WithPostingOff_AnAdvanceIsSavedWithoutAnEntry()
        {
            _db.Services.GetRequiredService<PrimeERP.Application.PageServices.Admin.ISettingsService>()
                .Set(PrimeERP.Platform.Settings.SettingKeys.Edition.AdvancesPosted, false);
            var treasury = _db.Services.GetRequiredService<PrimeERP.Application.PageServices.Treasury.ITreasuryService>().GetUsable().Value.First();

            var created = _db.Services.GetRequiredService<IAdvanceService>()
                .Create(new CreateEmployeeMovementDto { EmployeeCode = _code, Date = _day, Amount = 400, TreasuryId = treasury.Id });

            Assert.True(created.IsSuccess, created.ErrorMessage);
            Assert.Empty(_db.Services.GetRequiredService<IJournalService>()
                .GetTrialBalance(_day.AddYears(-1), _day.AddYears(1)).Value.Where(l => l.PeriodDebit != 0 || l.PeriodCredit != 0));
        }

        [Fact]
        public void PayingFromTheTreasury_CreditsItWithTheNet()
        {
            var treasury = FundedTreasury(10000);
            Settings.Set(PrimeERP.Platform.Settings.SettingKeys.Edition.PayrollPays, true);
            Settings.Set(PrimeERP.Platform.Settings.SettingKeys.HR.PayrollTreasury, treasury.Id);

            var created = Payrolls.Create(Payrolls.Open(_day.Month, _day.Year).Value);
            Assert.True(created.IsSuccess, created.ErrorMessage);

            var balance = _db.Services.GetRequiredService<IAccountService>().GetByCode(treasury.AccountCode).Value.Balance;
            Assert.Equal(10000m - created.Value.NetTotal, balance);
        }

        [Fact]
        public void AForgottenAllowance_IsCarriedIntoTheNextPayroll()
        {
            var allowances = _db.Services.GetRequiredService<IAllowanceService>();
            allowances.Create(new CreateEmployeeMovementDto { EmployeeCode = _code, Date = _day, Amount = 500 });
            Assert.True(Payrolls.Create(Payrolls.Open(_day.Month, _day.Year).Value).IsSuccess);

            allowances.Create(new CreateEmployeeMovementDto { EmployeeCode = _code, Date = _day.AddDays(5), Amount = 500 });
            allowances.Create(new CreateEmployeeMovementDto { EmployeeCode = _code, Date = _day.AddMonths(1), Amount = 500 });

            var next = Payrolls.Open();
            Assert.True(next.IsSuccess, next.ErrorMessage);
            Assert.Equal((_day.AddMonths(1).Month, _day.AddMonths(1).Year), (next.Value.Month, next.Value.Year));
            Assert.Equal(1000m, next.Value.Lines.Single(l => l.EmployeeCode == _code).Allowances);
        }

        [Fact]
        public void TheFirstPayroll_OpensOnTheEarliestHireMonth()
        {
            var first = Payrolls.Open().Value;
            Assert.Equal((_day.Month, _day.Year), (first.Month, first.Year));
        }
    }
}
