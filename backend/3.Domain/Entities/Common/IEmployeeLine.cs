namespace PrimeERP.Domain.Entities.Common
{
    /// <summary>سطرٌ بموظفه</summary>
    public interface IEmployeeLine
    {
        int    EmployeeId   { get; set; }
        string EmployeeCode { get; set; }
        string EmployeeName { get; set; }
    }
}
