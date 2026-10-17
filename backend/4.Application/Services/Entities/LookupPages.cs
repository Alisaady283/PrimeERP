using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Data.Repositories;
using PrimeERP.Data.Repositories.Base;
using PrimeERP.Domain.Entities;

namespace PrimeERP.Application.Services.Entities
{
    /// <summary>إعلانات صفحات القوائم</summary>
    public static class LookupPages
    {
        public static readonly EntitySpec Departments = new() { Key = "Departments", Strings = "Str.Department", NameLabel = "Str.Department.Name" };
        public static readonly EntitySpec AllowanceTypes = new() { Key = "AllowanceTypes", Strings = "Str.AllowanceType", NameLabel = "Str.AllowanceType.Name" };
        public static readonly EntitySpec DeductionTypes = new() { Key = "DeductionTypes", Strings = "Str.DeductionType", NameLabel = "Str.DeductionType.Name" };
        public static readonly EntitySpec LeaveTypes = new() { Key = "LeaveTypes", Strings = "Str.LeaveType", NameLabel = "Str.LeaveType.Name" };
        public static readonly EntitySpec JobTitles = new() { Key = "JobTitles", Strings = "Str.JobTitle", NameLabel = "Str.JobTitle.Name" };
        public static readonly EntitySpec Units = new() { Key = "Units", Strings = "Str.Unit", NameLabel = "Str.Unit.Name" };
        public static readonly EntitySpec WorkCenters = new() { Key = "WorkCenters", Strings = "Str.WorkCenter", NameLabel = "Str.Name", Sequence = "WorkCenter" };

        // المشاريع: المشروع ومهامه وساعاته ومستخلصاته — الأسماء تُقرأ بدفعة من مصادرها
        private static readonly NameJoin ProjectName = new("ProjectId", "ProjectName", (s, ids) => s.GetRequiredService<ILookupRepository<Project>>().NamesOf(ids));
        private static readonly NameJoin EmployeeName = new("EmployeeId", "EmployeeName", (s, ids) => s.GetRequiredService<IEmployeeRepository>().NamesOf(ids));
        private static readonly NameJoin TaskName = new("TaskId", "TaskName", (s, ids) => s.GetRequiredService<ILookupRepository<ProjectTask>>().NamesOf(ids));

        public static readonly EntitySpec Projects = new() { Key = "Projects", Strings = "Str.Project", NameLabel = "Str.Name", Sequence = "Project",
            Names = new[] { new NameJoin("CustomerId", "CustomerName", (s, ids) => s.GetRequiredService<IPartyRepository<Customer>>().NamesOf(ids)),
                            new NameJoin("ManagerId", "ManagerName", (s, ids) => s.GetRequiredService<IEmployeeRepository>().NamesOf(ids)) } };
        public static readonly EntitySpec ProjectTasks = new() { Key = "ProjectTasks", Strings = "Str.ProjectTask", NameLabel = "Str.Name", Sequence = "ProjectTask",
            Required = new Dictionary<string, string> { ["ProjectId"] = "Str.Project" }, Names = new[] { ProjectName, EmployeeName } };
        public static readonly EntitySpec ProjectTimesheets = new() { Key = "ProjectTimesheets", Strings = "Str.ProjectTimesheet", Sequence = "ProjectTimesheet",
            Required = new Dictionary<string, string> { ["ProjectId"] = "Str.Project", ["EmployeeId"] = "Str.Employee", ["Date"] = "Str.Date" }, Names = new[] { ProjectName, TaskName, EmployeeName } };
        public static readonly EntitySpec ProjectBills = new() { Key = "ProjectBills", Strings = "Str.ProjectBill", Sequence = "ProjectBill",
            Required = new Dictionary<string, string> { ["ProjectId"] = "Str.Project", ["Date"] = "Str.Date" }, Names = new[] { ProjectName, TaskName } };

        public static readonly EntitySpec Branches = new() { Key = "Branches", Strings = "Str.Branch", NameLabel = "Str.Branch.Name", Sequence = "Branch" };
        public static readonly EntitySpec ApiKeys = new() { Key = "ApiKeys", Strings = "Str.ApiKey", NameLabel = "Str.Name", Sequence = "ApiKey" };
        public static readonly EntitySpec AlertRules = new() { Key = "AlertRules", Strings = "Str.Alert", NameLabel = "Str.Name", Sequence = "AlertRule" };

        public static readonly EntitySpec Warehouses = new() { Key = "Warehouses", Strings = "Str.Warehouse", NameLabel = "Str.Warehouse.Name", Sequence = "Warehouse", BranchScoped = true,
            Defaults = new Dictionary<string, string> { ["Name"] = "Str.Warehouse.Main", ["Location"] = "Str.Warehouse.MainLocation" } };
    }
}
