using System.Collections.Generic;
using PrimeERP.Application.Services.Entities;
using PrimeERP.Composition.Definitions;
using PrimeERP.Composition.Registry;
using PrimeERP.Domain.Entities;
using PrimeERP.Platform.Localization;
using PrimeERP.UI.Components.Display;

namespace PrimeERP.Modules
{
    /// <summary>
    /// المشاريع: المشروع ومهامه وساعاته ومستخلصاته — صفحات قوائم من Lookup، والساعات والمستخلصات تحمل قيودها.
    /// أيّها يظهر في النسخة يحكمه جدول الصفحات في البناء، وسلوكها من إعدادات «المشاريع».
    /// </summary>
    public static class ProjectRegistrations
    {
        private const string Domain = "Projects";

        public static void RegisterAll(IModuleRegistry registry)
        {
            registry.Register(new ModuleDefinition
            {
                Key = "Projects", TitleKey = "Str.Module.Projects", PermissionPrefix = Domain,
                ViewModelFactory = s => RowPage.ViewModel<Lookup<Project>>(s, Domain),
                Columns = new()
                {
                    new() { Header = LocalizationService.Get("Str.Code"), Binding = nameof(Project.Code), Width = 90, Align = ColumnAlign.Center },
                    new() { Header = LocalizationService.Get("Str.Name"), Binding = nameof(Project.Name), Width = 200, IsStarWidth = true },
                    new() { Header = LocalizationService.Get("Str.Project.Customer"), Binding = "CustomerName", Width = 170 },
                    new() { Header = LocalizationService.Get("Str.Project.Manager"), Binding = "ManagerName", Width = 150 },
                    new() { Header = LocalizationService.Get("Str.Project.StartDate"), Binding = nameof(Project.StartDate), Width = 105, Format = "yyyy-MM-dd", Align = ColumnAlign.Center },
                    new() { Header = LocalizationService.Get("Str.Project.EndDate"), Binding = nameof(Project.EndDate), Width = 105, Format = "yyyy-MM-dd", Align = ColumnAlign.Center },
                    new() { Header = LocalizationService.Get("Str.Project.ContractValue"), Binding = nameof(Project.ContractValue), Width = 130, Format = "N2", Footer = FooterAggregate.Sum },
                    new() { Header = LocalizationService.Get("Str.Project.Budget"), Binding = nameof(Project.Budget), Width = 120, Format = "N2", Footer = FooterAggregate.Sum },
                    new() { Header = LocalizationService.Get("Str.Project.Status"), Binding = "StatusText", Width = 110, Align = ColumnAlign.Center },
                },
                Dialog = RowPage.Dialog<Lookup<Project>>("Str.Projects.Add", new List<FieldDefinition>
                {
                    new() { Key = nameof(Project.Name), LabelKey = "Str.Name", Kind = FieldKind.Text, IsRequired = true, MaxLength = 200 },
                    new() { Key = nameof(Project.CustomerId), LabelKey = "Str.Project.Customer", Kind = FieldKind.Picker, PickerType = "Customer" },
                    new() { Key = nameof(Project.ManagerId), LabelKey = "Str.Project.Manager", Kind = FieldKind.Picker, PickerType = "Employee" },
                    new() { Key = nameof(Project.Status), LabelKey = "Str.Project.Status", Kind = FieldKind.Picker, PickerType = "ProjectStatus", IsRequired = true, DefaultValue = 1 },
                    new() { Key = nameof(Project.StartDate), LabelKey = "Str.Project.StartDate", Kind = FieldKind.Date },
                    new() { Key = nameof(Project.EndDate), LabelKey = "Str.Project.EndDate", Kind = FieldKind.Date },
                    new() { Key = nameof(Project.ContractValue), LabelKey = "Str.Project.ContractValue", Kind = FieldKind.Number, Min = 0 },
                    new() { Key = nameof(Project.Budget), LabelKey = "Str.Project.Budget", Kind = FieldKind.Number, Min = 0 },
                    new() { Key = nameof(Project.RetentionPercent), LabelKey = "Str.Project.RetentionPercent", Kind = FieldKind.Number, Min = 0 },
                    new() { Key = nameof(Project.Notes), LabelKey = "Str.Notes", Kind = FieldKind.TextArea, MaxLength = 1000 },
                    new() { Key = nameof(Project.IsActive), LabelKey = "Str.Active", Kind = FieldKind.Check, DefaultValue = true },
                }, "Str.Projects.Edit", gridColumns: 2)
            });

            registry.Register(new ModuleDefinition
            {
                Key = "ProjectTasks", TitleKey = "Str.Module.ProjectTasks", PermissionPrefix = Domain,
                ViewModelFactory = s => RowPage.ViewModel<Lookup<ProjectTask>>(s, Domain),
                Columns = new()
                {
                    new() { Header = LocalizationService.Get("Str.Code"), Binding = nameof(ProjectTask.Code), Width = 90, Align = ColumnAlign.Center },
                    new() { Header = LocalizationService.Get("Str.Name"), Binding = nameof(ProjectTask.Name), Width = 200, IsStarWidth = true },
                    new() { Header = LocalizationService.Get("Str.Project"), Binding = "ProjectName", Width = 170 },
                    new() { Header = LocalizationService.Get("Str.ProjectTask.Assignee"), Binding = "EmployeeName", Width = 150 },
                    new() { Header = LocalizationService.Get("Str.ProjectTask.DueDate"), Binding = nameof(ProjectTask.DueDate), Width = 105, Format = "yyyy-MM-dd", Align = ColumnAlign.Center },
                    new() { Header = LocalizationService.Get("Str.ProjectTask.Progress"), Binding = nameof(ProjectTask.Progress), Width = 90, Format = "N0", Align = ColumnAlign.Center },
                    new() { Header = LocalizationService.Get("Str.ProjectTask.Status"), Binding = "StatusText", Width = 110, Align = ColumnAlign.Center },
                    new() { Header = LocalizationService.Get("Str.ProjectTask.IsMilestone"), Binding = nameof(ProjectTask.IsMilestone), Width = 90, Align = ColumnAlign.Center },
                },
                Dialog = RowPage.Dialog<Lookup<ProjectTask>>("Str.ProjectTasks.Add", new List<FieldDefinition>
                {
                    new() { Key = nameof(ProjectTask.ProjectId), LabelKey = "Str.Project", Kind = FieldKind.Picker, PickerType = "Project", IsRequired = true },
                    new() { Key = nameof(ProjectTask.Name), LabelKey = "Str.Name", Kind = FieldKind.Text, IsRequired = true, MaxLength = 200 },
                    new() { Key = nameof(ProjectTask.EmployeeId), LabelKey = "Str.ProjectTask.Assignee", Kind = FieldKind.Picker, PickerType = "Employee" },
                    new() { Key = nameof(ProjectTask.Status), LabelKey = "Str.ProjectTask.Status", Kind = FieldKind.Picker, PickerType = "ProjectTaskStatus", IsRequired = true, DefaultValue = 1 },
                    new() { Key = nameof(ProjectTask.StartDate), LabelKey = "Str.Project.StartDate", Kind = FieldKind.Date },
                    new() { Key = nameof(ProjectTask.DueDate), LabelKey = "Str.ProjectTask.DueDate", Kind = FieldKind.Date },
                    new() { Key = nameof(ProjectTask.Weight), LabelKey = "Str.ProjectTask.Weight", Kind = FieldKind.Number, Min = 0, DefaultValue = 1m },
                    new() { Key = nameof(ProjectTask.Progress), LabelKey = "Str.ProjectTask.Progress", Kind = FieldKind.Number, Min = 0, Max = 100 },
                    new() { Key = nameof(ProjectTask.IsMilestone), LabelKey = "Str.ProjectTask.IsMilestone", Kind = FieldKind.Check },
                    new() { Key = nameof(ProjectTask.BillingPercent), LabelKey = "Str.ProjectTask.BillingPercent", Kind = FieldKind.Number, Min = 0, Max = 100 },
                    new() { Key = nameof(ProjectTask.Notes), LabelKey = "Str.Notes", Kind = FieldKind.TextArea, MaxLength = 1000 },
                    new() { Key = nameof(ProjectTask.IsActive), LabelKey = "Str.Active", Kind = FieldKind.Check, DefaultValue = true },
                }, "Str.ProjectTasks.Edit", gridColumns: 2)
            });

            registry.Register(new ModuleDefinition
            {
                Key = "ProjectTimesheets", TitleKey = "Str.Module.ProjectTimesheets", PermissionPrefix = Domain,
                ViewModelFactory = s => RowPage.ViewModel<Lookup<ProjectTimesheet>>(s, Domain),
                Columns = new()
                {
                    new() { Header = LocalizationService.Get("Str.Code"), Binding = nameof(ProjectTimesheet.Code), Width = 90, Align = ColumnAlign.Center },
                    new() { Header = LocalizationService.Get("Str.Date"), Binding = nameof(ProjectTimesheet.Date), Width = 105, Format = "yyyy-MM-dd", Align = ColumnAlign.Center },
                    new() { Header = LocalizationService.Get("Str.Project"), Binding = "ProjectName", Width = 180, IsStarWidth = true },
                    new() { Header = LocalizationService.Get("Str.ProjectTask"), Binding = "TaskName", Width = 160 },
                    new() { Header = LocalizationService.Get("Str.Employee"), Binding = "EmployeeName", Width = 160 },
                    new() { Header = LocalizationService.Get("Str.ProjectTimesheet.Hours"), Binding = nameof(ProjectTimesheet.Hours), Width = 90, Format = "N2", Align = ColumnAlign.Center, Footer = FooterAggregate.Sum },
                    new() { Header = LocalizationService.Get("Str.ProjectTimesheet.HourlyCost"), Binding = nameof(ProjectTimesheet.HourlyCost), Width = 110, Format = "N2" },
                    new() { Header = LocalizationService.Get("Str.ProjectTimesheet.Cost"), Binding = nameof(ProjectTimesheet.Cost), Width = 120, Format = "N2", Footer = FooterAggregate.Sum },
                },
                Dialog = RowPage.Dialog<Lookup<ProjectTimesheet>>("Str.ProjectTimesheets.Add", new List<FieldDefinition>
                {
                    new() { Key = nameof(ProjectTimesheet.ProjectId), LabelKey = "Str.Project", Kind = FieldKind.Picker, PickerType = "Project", IsRequired = true },
                    new() { Key = nameof(ProjectTimesheet.TaskId), LabelKey = "Str.ProjectTask", Kind = FieldKind.Picker, PickerType = "ProjectTask" },
                    new() { Key = nameof(ProjectTimesheet.EmployeeId), LabelKey = "Str.Employee", Kind = FieldKind.Picker, PickerType = "Employee", IsRequired = true },
                    new() { Key = nameof(ProjectTimesheet.Date), LabelKey = "Str.Date", Kind = FieldKind.Date, IsRequired = true },
                    new() { Key = nameof(ProjectTimesheet.Hours), LabelKey = "Str.ProjectTimesheet.Hours", Kind = FieldKind.Number, Positive = true, IsRequired = true },
                    new() { Key = nameof(ProjectTimesheet.HourlyCost), LabelKey = "Str.ProjectTimesheet.HourlyCost", Kind = FieldKind.Number, Min = 0 },
                    new() { Key = nameof(ProjectTimesheet.Notes), LabelKey = "Str.Notes", Kind = FieldKind.TextArea, MaxLength = 1000 },
                }, "Str.ProjectTimesheets.Edit", gridColumns: 2)
            });

            registry.Register(new ModuleDefinition
            {
                Key = "ProjectBills", TitleKey = "Str.Module.ProjectBills", PermissionPrefix = Domain,
                ViewModelFactory = s => RowPage.ViewModel<Lookup<ProjectBill>>(s, Domain),
                Columns = new()
                {
                    new() { Header = LocalizationService.Get("Str.Code"), Binding = nameof(ProjectBill.Code), Width = 90, Align = ColumnAlign.Center },
                    new() { Header = LocalizationService.Get("Str.Date"), Binding = nameof(ProjectBill.Date), Width = 105, Format = "yyyy-MM-dd", Align = ColumnAlign.Center },
                    new() { Header = LocalizationService.Get("Str.Project"), Binding = "ProjectName", Width = 180, IsStarWidth = true },
                    new() { Header = LocalizationService.Get("Str.ProjectTask"), Binding = "TaskName", Width = 150 },
                    new() { Header = LocalizationService.Get("Str.ProjectBill.Percent"), Binding = nameof(ProjectBill.Percent), Width = 100, Format = "N1", Align = ColumnAlign.Center },
                    new() { Header = LocalizationService.Get("Str.ProjectBill.Amount"), Binding = nameof(ProjectBill.Amount), Width = 120, Format = "N2", Footer = FooterAggregate.Sum },
                    new() { Header = LocalizationService.Get("Str.ProjectBill.Retention"), Binding = nameof(ProjectBill.Retention), Width = 110, Format = "N2", Footer = FooterAggregate.Sum },
                    new() { Header = LocalizationService.Get("Str.ProjectBill.Net"), Binding = nameof(ProjectBill.Net), Width = 120, Format = "N2", Footer = FooterAggregate.Sum },
                },
                Dialog = RowPage.Dialog<Lookup<ProjectBill>>("Str.ProjectBills.Add", new List<FieldDefinition>
                {
                    new() { Key = nameof(ProjectBill.ProjectId), LabelKey = "Str.Project", Kind = FieldKind.Picker, PickerType = "Project", IsRequired = true },
                    new() { Key = nameof(ProjectBill.TaskId), LabelKey = "Str.ProjectTask", Kind = FieldKind.Picker, PickerType = "ProjectTask" },
                    new() { Key = nameof(ProjectBill.Date), LabelKey = "Str.Date", Kind = FieldKind.Date, IsRequired = true },
                    new() { Key = nameof(ProjectBill.Percent), LabelKey = "Str.ProjectBill.Percent", Kind = FieldKind.Number, Min = 0, Max = 100 },
                    new() { Key = nameof(ProjectBill.Amount), LabelKey = "Str.ProjectBill.Amount", Kind = FieldKind.Number, Min = 0 },
                    new() { Key = nameof(ProjectBill.Notes), LabelKey = "Str.Notes", Kind = FieldKind.TextArea, MaxLength = 1000 },
                }, "Str.ProjectBills.Edit", gridColumns: 2)
            });
        }
    }
}
