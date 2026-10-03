using System;
using System.Collections.Generic;
using System.Linq;
using PrimeERP.Application.DTOs.HR;
using PrimeERP.Application.Legacy.HR;
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
                typeof(IAllowanceService), "Str.Allowance.Type", "Str.Allowance.Amount");

            Movement(registry, "EmployeeDeductions", "Str.Module.EmployeeDeductions", typeof(DeductionViewModel),
                typeof(IDeductionService), "Str.Deduction.Reason", "Str.Deduction.Amount");

            Attendance(registry);
        }

        private static void Movement(IModuleRegistry registry, string key, string titleKey, Type viewModel,
            Type service, string reasonKey, string amountKey) =>
            registry.Register(new ModuleDefinition
            {
                Key = key, TitleKey = titleKey, PermissionPrefix = "HR", ViewModelType = viewModel,
                Columns = new List<GridColumn>
                {
                    new() { Header = LocalizationService.Get("Str.Employee"), Binding = nameof(EmployeeMovementDto.EmployeeName), Width = 200, IsStarWidth = true },
                    new() { Header = LocalizationService.Get("Str.Month"), Binding = nameof(EmployeeMovementDto.Month), Width = 70, Align = ColumnAlign.Center },
                    new() { Header = LocalizationService.Get("Str.Year"), Binding = nameof(EmployeeMovementDto.Year), Width = 80, Align = ColumnAlign.Center },
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
                        new() { Key = nameof(CreateEmployeeMovementDto.Month), LabelKey = "Str.Month", Kind = FieldKind.Number, IsRequired = true },
                        new() { Key = nameof(CreateEmployeeMovementDto.Year), LabelKey = "Str.Year", Kind = FieldKind.Number, IsRequired = true },
                        new() { Key = nameof(CreateEmployeeMovementDto.Amount), LabelKey = amountKey, Kind = FieldKind.Number, IsRequired = true },
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
                    new() { Header = LocalizationService.Get("Str.Date"), Binding = nameof(AttendanceDto.Date), Width = 110, Format = "yyyy-MM-dd" },
                    new() { Header = LocalizationService.Get("Str.Employee"), Binding = nameof(AttendanceDto.EmployeeName), Width = 200, IsStarWidth = true },
                    new() { Header = LocalizationService.Get("Str.Attendance.CheckIn"), Binding = nameof(AttendanceDto.CheckIn), Width = 90, Align = ColumnAlign.Center },
                    new() { Header = LocalizationService.Get("Str.Attendance.CheckOut"), Binding = nameof(AttendanceDto.CheckOut), Width = 90, Align = ColumnAlign.Center },
                    new() { Header = LocalizationService.Get("Str.Attendance.Overtime"), Binding = nameof(AttendanceDto.OvertimeHours), Width = 110, Align = ColumnAlign.Center, Format = "N2", Footer = FooterAggregate.Sum },
                    new() { Header = LocalizationService.Get("Str.Status"), Binding = nameof(AttendanceDto.StatusText), Width = 90, Align = ColumnAlign.Center },
                }.Concat(StandardFields.AuditColumns().Where(c => c.Binding != "StatusText")).ToList(),
                Filters = new()
                {
                    new() { Key = nameof(AttendanceFilter.EmployeeId), LabelKey = "Str.Employee", PickerType = "Employee" },
                },
                Dialog = new DialogDefinition
                {
                    TitleKey = "Str.Module.Attendances", TitleEditKey = "Str.Module.Attendances", GridColumns = 2,
                    ServiceType = typeof(IAttendanceService),
                    CreateDtoType = typeof(CreateAttendanceDto), UpdateDtoType = typeof(CreateAttendanceDto),
                    Fields = new()
                    {
                        new() { Key = nameof(CreateAttendanceDto.EmployeeCode), LabelKey = "Str.Employee", Kind = FieldKind.Picker, PickerType = "Employee", IsRequired = true },
                        new() { Key = nameof(CreateAttendanceDto.Date), LabelKey = "Str.Date", Kind = FieldKind.Date, IsRequired = true },
                        new() { Key = nameof(CreateAttendanceDto.CheckIn), LabelKey = "Str.Attendance.CheckInTime", Kind = FieldKind.Text, MaxLength = 5 },
                        new() { Key = nameof(CreateAttendanceDto.CheckOut), LabelKey = "Str.Attendance.CheckOutTime", Kind = FieldKind.Text, MaxLength = 5 },
                        new() { Key = nameof(CreateAttendanceDto.OvertimeHours), LabelKey = "Str.Attendance.Overtime", Kind = FieldKind.Number },
                        new() { Key = nameof(CreateAttendanceDto.IsAbsent), LabelKey = "Str.Attendance.Absent", Kind = FieldKind.Check },
                        new() { Key = nameof(CreateAttendanceDto.Notes), LabelKey = "Str.Notes", Kind = FieldKind.TextArea, ColumnSpan = 2 },
                    }
                }
            });
    }
}
