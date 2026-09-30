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
                typeof(IAllowanceService), "نوع البدل", "قيمة البدل");

            Movement(registry, "EmployeeDeductions", "Str.Module.EmployeeDeductions", typeof(DeductionViewModel),
                typeof(IDeductionService), "سبب الخصم", "قيمة الخصم");

            Attendance(registry);
        }

        private static void Movement(IModuleRegistry registry, string key, string titleKey, Type viewModel,
            Type service, string reasonLabel, string amountLabel) =>
            registry.Register(new ModuleDefinition
            {
                Key = key, TitleKey = titleKey, PermissionPrefix = "HR", ViewModelType = viewModel,
                Columns = new List<GridColumn>
                {
                    new() { Header = LocalizationService.Get("Str.Employee"), Binding = nameof(EmployeeMovementDto.EmployeeName), Width = 200, IsStarWidth = true },
                    new() { Header = "الشهر", Binding = nameof(EmployeeMovementDto.Month), Width = 70, Align = ColumnAlign.Center },
                    new() { Header = "السنة", Binding = nameof(EmployeeMovementDto.Year), Width = 80, Align = ColumnAlign.Center },
                    new() { Header = reasonLabel, Binding = nameof(EmployeeMovementDto.Reason), Width = 180 },
                    new() { Header = amountLabel, Binding = nameof(EmployeeMovementDto.Amount), Width = 110, Align = ColumnAlign.Center, Format = "N2", Footer = FooterAggregate.Sum },
                }.Concat(StandardFields.AuditColumns().Where(c => c.Binding != "StatusText")).ToList(),
                Filters = new()
                {
                    new() { Key = nameof(EmployeeMovementFilter.EmployeeId), LabelKey = LocalizationService.Get("Str.Employee"), PickerType = "Employee" },
                },
                Dialog = new DialogDefinition
                {
                    TitleKey = titleKey, TitleEditKey = titleKey, GridColumns = 2,
                    ServiceType = service,
                    CreateDtoType = typeof(CreateEmployeeMovementDto), UpdateDtoType = typeof(CreateEmployeeMovementDto),
                    Fields = new()
                    {
                        new() { Key = nameof(CreateEmployeeMovementDto.EmployeeCode), LabelKey = LocalizationService.Get("Str.Employee"), Kind = FieldKind.Picker, PickerType = "Employee", IsRequired = true },
                        new() { Key = nameof(CreateEmployeeMovementDto.Month), LabelKey = "الشهر", Kind = FieldKind.Number, IsRequired = true },
                        new() { Key = nameof(CreateEmployeeMovementDto.Year), LabelKey = "السنة", Kind = FieldKind.Number, IsRequired = true },
                        new() { Key = nameof(CreateEmployeeMovementDto.Amount), LabelKey = amountLabel, Kind = FieldKind.Number, IsRequired = true },
                        new() { Key = nameof(CreateEmployeeMovementDto.Reason), LabelKey = reasonLabel, Kind = FieldKind.Text, MaxLength = 200, ColumnSpan = 2 },
                        new() { Key = nameof(CreateEmployeeMovementDto.Notes), LabelKey = LocalizationService.Get("Str.Notes"), Kind = FieldKind.TextArea, ColumnSpan = 2 },
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
                    new() { Header = "الحضور", Binding = nameof(AttendanceDto.CheckIn), Width = 90, Align = ColumnAlign.Center },
                    new() { Header = "الانصراف", Binding = nameof(AttendanceDto.CheckOut), Width = 90, Align = ColumnAlign.Center },
                    new() { Header = "ساعات إضافية", Binding = nameof(AttendanceDto.OvertimeHours), Width = 110, Align = ColumnAlign.Center, Format = "N2", Footer = FooterAggregate.Sum },
                    new() { Header = LocalizationService.Get("Str.Status"), Binding = nameof(AttendanceDto.StatusText), Width = 90, Align = ColumnAlign.Center },
                }.Concat(StandardFields.AuditColumns().Where(c => c.Binding != "StatusText")).ToList(),
                Filters = new()
                {
                    new() { Key = nameof(AttendanceFilter.EmployeeId), LabelKey = LocalizationService.Get("Str.Employee"), PickerType = "Employee" },
                },
                Dialog = new DialogDefinition
                {
                    TitleKey = "Str.Module.Attendances", TitleEditKey = "Str.Module.Attendances", GridColumns = 2,
                    ServiceType = typeof(IAttendanceService),
                    CreateDtoType = typeof(CreateAttendanceDto), UpdateDtoType = typeof(CreateAttendanceDto),
                    Fields = new()
                    {
                        new() { Key = nameof(CreateAttendanceDto.EmployeeCode), LabelKey = LocalizationService.Get("Str.Employee"), Kind = FieldKind.Picker, PickerType = "Employee", IsRequired = true },
                        new() { Key = nameof(CreateAttendanceDto.Date), LabelKey = LocalizationService.Get("Str.Date"), Kind = FieldKind.Date, IsRequired = true },
                        new() { Key = nameof(CreateAttendanceDto.CheckIn), LabelKey = "الحضور (HH:mm)", Kind = FieldKind.Text, MaxLength = 5 },
                        new() { Key = nameof(CreateAttendanceDto.CheckOut), LabelKey = "الانصراف (HH:mm)", Kind = FieldKind.Text, MaxLength = 5 },
                        new() { Key = nameof(CreateAttendanceDto.OvertimeHours), LabelKey = "ساعات إضافية", Kind = FieldKind.Number },
                        new() { Key = nameof(CreateAttendanceDto.IsAbsent), LabelKey = "غياب", Kind = FieldKind.Check },
                        new() { Key = nameof(CreateAttendanceDto.Notes), LabelKey = LocalizationService.Get("Str.Notes"), Kind = FieldKind.TextArea, ColumnSpan = 2 },
                    }
                }
            });
    }
}
