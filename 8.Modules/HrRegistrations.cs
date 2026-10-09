using System;
using System.Collections.Generic;
using System.Linq;
using PrimeERP.Application.DTOs.HR;
using PrimeERP.Application.PageServices.HR;
using PrimeERP.Composition.Definitions;
using PrimeERP.Composition.Registry;
using PrimeERP.Platform.Localization;
using PrimeERP.UI.Components.Display;
using PrimeERP.UI.ViewModels;

namespace PrimeERP.Modules
{
    /// <summary>شاشات الموارد البشرية التي يجمع</summary>
    public static class HrRegistrations
    {
        public static void RegisterAll(IModuleRegistry registry)
        {
            Movement(registry, "EmployeeAllowances", "Str.Module.EmployeeAllowances", typeof(AllowanceViewModel),
                typeof(IAllowanceService), "AllowanceType", "Str.Allowance.Type", "Str.Allowance.Reason", "Str.Allowance.Amount");

            Movement(registry, "EmployeeDeductions", "Str.Module.EmployeeDeductions", typeof(DeductionViewModel),
                typeof(IDeductionService), "DeductionType", "Str.Deduction.Type", "Str.Deduction.Reason", "Str.Deduction.Amount");

            Movement(registry, "EmployeeAdvances", "Str.Module.EmployeeAdvances", typeof(AdvanceViewModel),
                typeof(IAdvanceService), "Treasury", "Str.Treasury", "Str.Allowance.Reason", "Str.Amount",
                nameof(CreateEmployeeMovementDto.TreasuryId), nameof(EmployeeMovementDto.TreasuryName));

            Attendance(registry);
        }

        private static void Movement(IModuleRegistry registry, string key, string titleKey, Type viewModel,
            Type service, string typePicker, string typeKey, string reasonKey, string amountKey,
            string typeField = nameof(CreateEmployeeMovementDto.TypeId), string typeColumn = nameof(EmployeeMovementDto.TypeName)) =>
            registry.Register(new ModuleDefinition
            {
                Key = key, TitleKey = titleKey, PermissionPrefix = "HR", ViewModelType = viewModel,
                Columns = new List<GridColumn>
                {
                    new() { Header = LocalizationService.Get("Str.Employee"), Binding = nameof(EmployeeMovementDto.EmployeeName), Width = 200, IsStarWidth = true },
                    new() { Header = LocalizationService.Get("Str.Date"), Binding = nameof(EmployeeMovementDto.Date), Width = 110, Format = "yyyy-MM-dd" },
                    new() { Header = LocalizationService.Get("Str.Month"), Binding = nameof(EmployeeMovementDto.Month), Width = 70, Align = ColumnAlign.Center },
                    new() { Header = LocalizationService.Get("Str.Year"), Binding = nameof(EmployeeMovementDto.Year), Width = 80, Align = ColumnAlign.Center },
                    new() { Header = LocalizationService.Get(typeKey), Binding = typeColumn, Width = 140 },
                    new() { Header = LocalizationService.Get(reasonKey), Binding = nameof(EmployeeMovementDto.Reason), Width = 180 },
                    new() { Header = LocalizationService.Get(amountKey), Binding = nameof(EmployeeMovementDto.Amount), Width = 110, Align = ColumnAlign.Center, Format = "N2", Footer = FooterAggregate.Sum },
                }.Concat(StandardFields.AuditColumns().Where(c => c.Binding != "StatusText")).ToList(),
                Filters = new()
                {
                    new() { Key = nameof(EmployeeMovementFilter.EmployeeId), LabelKey = "Str.Employee", PickerType = "Employee" },
                },
                Dialog = new DialogDefinition
                {
                    TitleKey = titleKey, TitleEditKey = titleKey, GridColumns = 2,
                    ServiceType = service,
                    CreateDtoType = typeof(CreateEmployeeMovementDto), UpdateDtoType = typeof(CreateEmployeeMovementDto),
                    Fields = new()
                    {
                        new() { Key = nameof(CreateEmployeeMovementDto.EmployeeCode), LabelKey = "Str.Employee", Kind = FieldKind.Picker, PickerType = "Employee", IsRequired = true },
                        new() { Key = nameof(CreateEmployeeMovementDto.Date), LabelKey = "Str.Date", Kind = FieldKind.Date, IsRequired = true },
                        new() { Key = typeField, LabelKey = typeKey, Kind = FieldKind.Picker, PickerType = typePicker, IsRequired = true },
                        new() { Key = nameof(CreateEmployeeMovementDto.Amount), LabelKey = amountKey, Kind = FieldKind.Number, Positive = true },
                        new() { Key = nameof(CreateEmployeeMovementDto.Reason), LabelKey = reasonKey, Kind = FieldKind.Text, MaxLength = 200, ColumnSpan = 2 },
                        new() { Key = nameof(CreateEmployeeMovementDto.Notes), LabelKey = "Str.Notes", Kind = FieldKind.TextArea, ColumnSpan = 2 },
                    }
                }
            });

        private static void Attendance(IModuleRegistry registry) =>
            registry.Register(new ModuleDefinition
            {
                Key = "Attendances", TitleKey = "Str.Module.Attendances", PermissionPrefix = "HR",
                ViewModelType = typeof(AttendanceViewModel),
                Columns = new List<GridColumn>
                {
                    new() { Header = LocalizationService.Get("Str.Date"), Binding = nameof(AttendanceDayDto.Date), Width = 110, Format = "yyyy-MM-dd" },
                    new() { Header = LocalizationService.Get("Str.Attendance.Status.Present"), Binding = nameof(AttendanceDayDto.Present), Width = 80, Align = ColumnAlign.Center },
                    new() { Header = LocalizationService.Get("Str.Attendance.Status.Absent"), Binding = nameof(AttendanceDayDto.Absent), Width = 80, Align = ColumnAlign.Center },
                    new() { Header = LocalizationService.Get("Str.Attendance.Status.Leave"), Binding = nameof(AttendanceDayDto.Leave), Width = 80, Align = ColumnAlign.Center },
                    new() { Header = LocalizationService.Get("Str.Attendance.Status.Mission"), Binding = nameof(AttendanceDayDto.Mission), Width = 80, Align = ColumnAlign.Center },
                    new() { Header = LocalizationService.Get("Str.Attendance.Status.Holiday"), Binding = nameof(AttendanceDayDto.Holiday), Width = 80, Align = ColumnAlign.Center },
                    new() { Header = LocalizationService.Get("Str.Notes"), Binding = nameof(AttendanceDayDto.Notes), Width = 200, IsStarWidth = true },
                },
                DocumentDialog = new DocumentDialogDefinition
                {
                    PrintTitleKey = "Str.Print.Title.Attendance",
                    TitleKey = "Str.Attendance.Add", TitleEditKey = "Str.Attendance.Edit",
                    ServiceType = typeof(IAttendanceService), DtoType = typeof(AttendanceSheetDto), LineDtoType = typeof(AttendanceLineDto),
                    LinesPropertyName = nameof(AttendanceSheetDto.Lines),
                    OpenBy = new[] { nameof(AttendanceSheetDto.Date) }, FixedLines = true,
                    HeaderFields = new()
                    {
                        new() { Key = nameof(AttendanceSheetDto.Date), LabelKey = "Str.Date", Kind = FieldKind.Date, IsRequired = true },
                        new() { Key = nameof(AttendanceSheetDto.Notes), LabelKey = "Str.Notes", Kind = FieldKind.Text, MaxLength = 300 },
                    },
                    LineFields = new()
                    {
                        new() { Key = nameof(AttendanceLineDto.EmployeeCode), Header = LocalizationService.Get("Str.Code"), Kind = FieldKind.ReadOnly, Width = 90 },
                        new() { Key = nameof(AttendanceLineDto.EmployeeName), Header = LocalizationService.Get("Str.Employee"), Kind = FieldKind.ReadOnly, Width = 180 },
                        new() { Key = nameof(AttendanceLineDto.DepartmentName), Header = LocalizationService.Get("Str.Department"), Kind = FieldKind.ReadOnly, Width = 130 },
                        new() { Key = nameof(AttendanceLineDto.Status), Header = LocalizationService.Get("Str.Status"), Kind = FieldKind.Picker, PickerType = "AttendanceStatus", Width = 110, IsRequired = true },
                        new() { Key = nameof(AttendanceLineDto.LeaveTypeId), Header = LocalizationService.Get("Str.LeaveType"), Kind = FieldKind.Picker, PickerType = "LeaveType", Width = 130 },
                        new() { Key = nameof(AttendanceLineDto.CheckIn), Header = LocalizationService.Get("Str.Attendance.CheckIn"), Width = 80 },
                        new() { Key = nameof(AttendanceLineDto.CheckOut), Header = LocalizationService.Get("Str.Attendance.CheckOut"), Width = 80 },
                        new() { Key = nameof(AttendanceLineDto.Late), Header = LocalizationService.Get("Str.Attendance.Late"), Kind = FieldKind.ReadOnly, Width = 80 },
                        new() { Key = nameof(AttendanceLineDto.Overtime), Header = LocalizationService.Get("Str.Attendance.Overtime"), Kind = FieldKind.ReadOnly, Width = 80 },
                        new() { Key = nameof(AttendanceLineDto.Notes), Header = LocalizationService.Get("Str.Notes"), Width = 160 },
                    }
                }
            });
    }
}
