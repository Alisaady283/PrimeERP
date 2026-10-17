using System;
using System.Linq;
using PrimeERP.Data.Repositories;
using PrimeERP.Data.Repositories.Base;
using PrimeERP.Domain.Calculations;
using PrimeERP.Domain.Entities;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Audit;
using PrimeERP.Platform.Localization;
using PrimeERP.Platform.Permissions;
using PrimeERP.Platform.Settings;

namespace PrimeERP.Application.Reporting
{
    public interface IProjectReportService
    {
        Result<ReportData> Profitability(DateTime from, DateTime to);
    }

    /// <summary>ربحية المشاريع في الفترة: مستخلصاتها إيراداً وساعاتها تكلفةً، وإنجازها من مهامها</summary>
    public class ProjectReportService : ReportServiceBase, IProjectReportService
    {
        private readonly ILookupRepository<Project> _projects;
        private readonly ILookupRepository<ProjectTask> _tasks;
        private readonly ILookupRepository<ProjectTimesheet> _sheets;
        private readonly ILookupRepository<ProjectBill> _bills;
        private readonly IPartyRepository<Customer> _customers;

        public ProjectReportService(ILookupRepository<Project> projects, ILookupRepository<ProjectTask> tasks, ILookupRepository<ProjectTimesheet> sheets,
            ILookupRepository<ProjectBill> bills, IPartyRepository<Customer> customers,
            IPermissionService permissions, ISettingsProvider settings, ILocalizationService localization, IAuditLogger audit)
            : base(permissions, settings, localization, audit)
        {
            _projects = projects; _tasks = tasks; _sheets = sheets; _bills = bills; _customers = customers;
        }

        public Result<ReportData> Profitability(DateTime from, DateTime to)
        {
            var gate = Gate(); if (gate != null) return gate;
            bool Within(DateTime date) => date.Date >= from.Date && date.Date <= to.Date;

            var projects = _projects.GetAll(includeInactive: true);
            var tasks = _tasks.GetAll().ToLookup(t => t.ProjectId);
            var sheets = _sheets.GetAll().Where(s => Within(s.Date)).ToLookup(s => s.ProjectId);
            var bills = _bills.GetAll().Where(b => Within(b.Date)).ToLookup(b => b.ProjectId);
            var customers = _customers.NamesOf(projects.Where(p => p.CustomerId > 0).Select(p => p.CustomerId.Value));

            var rows = projects.Select(p =>
            {
                var billed = bills[p.Id].Sum(b => b.Amount);
                var cost = sheets[p.Id].Sum(s => s.Cost);
                var (profit, margin) = ProjectCalc.Profit(billed, cost);
                return new ProjectRow
                {
                    Code = p.Code, Name = p.Name, Customer = p.CustomerId is int c ? customers.GetValueOrDefault(c) : null,
                    Contract = p.ContractValue, Billed = billed, Retention = bills[p.Id].Sum(b => b.Retention), Cost = cost,
                    Profit = profit, Margin = margin, Hours = sheets[p.Id].Sum(s => s.Hours),
                    Progress = ProjectCalc.Progress(tasks[p.Id].Select(t => (t.Weight, t.Progress))),
                    BudgetUsed = ProjectCalc.BudgetUsed(cost, p.Budget),
                };
            }).Where(r => r.Billed != 0 || r.Cost != 0 || r.Contract != 0).OrderBy(r => r.Code).ToList();

            return Result.Ok(new ReportData
            {
                Rows = rows,
                Totals = new()
                {
                    ["Billed"] = $"{LocalizationService.Get("Str.Project.Billed")}: {rows.Sum(r => r.Billed):N2}",
                    ["Cost"] = $"{LocalizationService.Get("Str.Project.Cost")}: {rows.Sum(r => r.Cost):N2}",
                    ["Profit"] = $"{LocalizationService.Get("Str.Project.Profit")}: {rows.Sum(r => r.Profit):N2}",
                }
            });
        }
    }
}
