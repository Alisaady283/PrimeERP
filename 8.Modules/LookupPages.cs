using PrimeERP.Application.Services.Entities;

namespace PrimeERP.Modules
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
        public static readonly EntitySpec Warehouses = new() { Key = "Warehouses", Strings = "Str.Warehouse", NameLabel = "Str.Warehouse.Name", Sequence = "Warehouse" };
    }
}
