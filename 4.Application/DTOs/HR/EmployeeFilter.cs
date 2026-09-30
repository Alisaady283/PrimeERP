namespace PrimeERP.Application.DTOs.HR
{
    /// <summary>مرشّح الموظفين</summary>
    public class EmployeeFilter
    {
        public string SearchText     { get; set; }
        public int?   DepartmentId   { get; set; }
        public string SortBy         { get; set; } = "Name";
        public bool   SortDescending { get; set; }
    }
}
