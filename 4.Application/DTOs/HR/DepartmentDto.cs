namespace PrimeERP.Application.DTOs.HR
{
    /// <summary>بيانات القسم</summary>
    public class DepartmentDto
    {
        public int    Id        { get; set; }
        public string Name      { get; set; }
        public string NameEn    { get; set; }
        public int?   ManagerId { get; set; }
        public bool   IsActive  { get; set; }
    }

    public class CreateDepartmentDto
    {
        public string Name      { get; set; }
        public string NameEn    { get; set; }
        public int?   ManagerId { get; set; }
        public bool   IsActive  { get; set; } = true;
    }

    public class UpdateDepartmentDto
    {
        public int    Id        { get; set; }
        public string Name      { get; set; }
        public string NameEn    { get; set; }
        public int?   ManagerId { get; set; }
        public bool   IsActive  { get; set; }
    }

    public class DepartmentFilter { }
}
