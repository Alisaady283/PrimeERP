using System;
using PrimeERP.Application.Services.Entities;
using PrimeERP.Application.Services.Ledger;
using PrimeERP.Data.Core;
using PrimeERP.Data.Repositories;
using PrimeERP.Data.Repositories.Base;
using PrimeERP.Domain.Calculations;
using PrimeERP.Domain.Entities;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Localization;
using PrimeERP.Platform.Settings;

namespace PrimeERP.Application.Services.Projects
{
    /// <summary>ما يتقاسمه قيدا المشروع: عكس القديم، والحسابات من الإعدادات، والفشل رسالةً لا استثناء</summary>
    public abstract class ProjectPosting
    {
        protected readonly Entries Entries;
        protected readonly ISettingsProvider Settings;
        protected readonly ILookupRepository<Project> Projects;

        protected ProjectPosting(Entries entries, ISettingsProvider settings, ILookupRepository<Project> projects) =>
            (Entries, Settings, Projects) = (entries, settings, projects);

        protected string Account(string key) => Settings.Get(key, "");

        protected static Result Missing() => Result.Fail(LocalizationService.Get("Str.Project.AccountsMissing"), ErrorCode.ValidationFailed);

        public Result CanReverse(int? entryId) => Posting.EnsureReversible(Entries, entryId);

        /// <summary>قيدٌ جديد بعد عكس القديم — ورقم القيد أو الفشل</summary>
        protected Result<int?> Write(PrimeDbContext db, int? previous, Func<int?> post)
        {
            try
            {
                Posting.Reverse(Entries, db, previous);
                return Result.Ok(post());
            }
            catch (InvalidOperationException ex)
            {
                return Result.Fail<int?>(ex.Message, ErrorCode.ValidationFailed);
            }
        }
    }

    /// <summary>ساعات المشروع: تكلفتها من سعر الساعة (أو الافتراضي)، وتُحمَّل على المشروع من الرواتب — مدين تكاليف المشاريع، دائن الرواتب</summary>
    public sealed class TimesheetPosting : ProjectPosting, IRowPosting<ProjectTimesheet>
    {
        public const string Source = "ProjectTimesheet";

        public TimesheetPosting(Entries entries, ISettingsProvider settings, ILookupRepository<Project> projects) : base(entries, settings, projects) { }

        public Result Prepare(ProjectTimesheet sheet)
        {
            if (sheet.Hours <= 0) return Result.Fail(LocalizationService.Get("Str.Project.HoursRequired"), ErrorCode.ValidationFailed);
            var fallback = Settings.Get(SettingKeys.Projects.DefaultHourlyCost, 0m);
            if (sheet.HourlyCost <= 0) sheet.HourlyCost = fallback;
            sheet.Cost = ProjectCalc.LaborCost(sheet.Hours, sheet.HourlyCost, fallback);
            return Result.Ok();
        }

        public Result CanReverse(ProjectTimesheet sheet) => CanReverse(sheet.JournalEntryId);

        public Result Post(PrimeDbContext db, ProjectTimesheet sheet, ProjectTimesheet stored)
        {
            var posts = Settings.Get(SettingKeys.Projects.PostTimesheets, true) && sheet.Cost > 0;
            var (debit, credit) = (Account(SettingKeys.Accounts.ProjectCost), Account(SettingKeys.Accounts.SalaryExpense));
            if (posts && (debit == "" || credit == "")) return Missing();

            var project = Projects.GetById(sheet.ProjectId, db)?.Name;
            var entry = Write(db, stored?.JournalEntryId, () => !posts ? null
                : Posting.Entry(Entries, db, sheet.Date, LocalizationService.Get("Str.Project.TimesheetEntry", project, sheet.Hours), Source, debit, credit, sheet.Cost, project));
            if (entry.IsFailure) return entry;
            sheet.JournalEntryId = entry.Value;
            return Result.Ok();
        }

        public void Reverse(PrimeDbContext db, ProjectTimesheet sheet) => Posting.Reverse(Entries, db, sheet.JournalEntryId);
    }

    /// <summary>المستخلص: قيمته بنسبته من العقد (أو معلَمه)، ويُحجز ضمانه — مدين العميل بالصافي وضمان الأعمال بالمحجوز، دائن إيرادات المشاريع</summary>
    public sealed class BillPosting : ProjectPosting, IRowPosting<ProjectBill>
    {
        public const string Source = "ProjectBill";

        private readonly ILookupRepository<ProjectTask> _tasks;
        private readonly IPartyRepository<Customer> _customers;

        public BillPosting(Entries entries, ISettingsProvider settings, ILookupRepository<Project> projects, ILookupRepository<ProjectTask> tasks,
            IPartyRepository<Customer> customers) : base(entries, settings, projects) => (_tasks, _customers) = (tasks, customers);

        public Result Prepare(ProjectBill bill)
        {
            var project = Projects.GetById(bill.ProjectId);
            if (project == null) return Result.Fail(LocalizationService.Get("Str.Project.NotFound"), ErrorCode.NotFound);
            if (project.CustomerId is not > 0) return Result.Fail(LocalizationService.Get("Str.Project.CustomerRequired"), ErrorCode.ValidationFailed);

            var milestone = bill.TaskId is > 0 ? _tasks.GetById(bill.TaskId.Value) : null;
            if (bill.Percent <= 0 && milestone?.IsMilestone == true) bill.Percent = milestone.BillingPercent;

            bill.Amount = ProjectCalc.BillAmount(bill.Amount, bill.Percent, project.ContractValue);
            if (bill.Amount <= 0) return Result.Fail(LocalizationService.Get("Str.Project.BillAmountRequired"), ErrorCode.ValidationFailed);

            var retention = project.RetentionPercent > 0 ? project.RetentionPercent : Settings.Get(SettingKeys.Projects.DefaultRetentionPercent, 0m);
            bill.Retention = ProjectCalc.Retention(bill.Amount, retention);
            bill.Net = bill.Amount - bill.Retention;
            return Result.Ok();
        }

        public Result CanReverse(ProjectBill bill) => CanReverse(bill.JournalEntryId);

        public Result Post(PrimeDbContext db, ProjectBill bill, ProjectBill stored)
        {
            var project = Projects.GetById(bill.ProjectId, db);
            var customer = _customers.GetById(project.CustomerId.Value, db)?.AccountCode ?? "";
            var (revenue, retention) = (Account(SettingKeys.Accounts.ProjectRevenue), Account(SettingKeys.Accounts.RetentionReceivable));
            if (customer == "" || revenue == "" || (bill.Retention > 0 && retention == "")) return Missing();

            var note = LocalizationService.Get("Str.Project.BillEntry", project.Name, bill.Code);
            var entry = Write(db, stored?.JournalEntryId, () => Posting.Entry(Entries, db, bill.Date, note, Source, new JournalLines()
                .Debit(customer, bill.Net, note)
                .Debit(retention, bill.Retention, note)
                .Credit(revenue, bill.Amount, note)
                .ToList()));
            if (entry.IsFailure) return entry;
            bill.JournalEntryId = entry.Value;
            return Result.Ok();
        }

        public void Reverse(PrimeDbContext db, ProjectBill bill) => Posting.Reverse(Entries, db, bill.JournalEntryId);
    }
}
