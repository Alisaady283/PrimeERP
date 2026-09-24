namespace PrimeERP.Application.DTOs.Security
{
    /// <summary>بيانات الدور</summary>
    public class RoleDto
    {
        public int    Id       { get; set; }
        public string Name     { get; set; }
        public string NameAr   { get; set; }
        public bool   IsSystem { get; set; }
    }

    public class CreateRoleDto
    {
        public string Name   { get; set; }
        public string NameAr { get; set; }
    }

    public class UpdateRoleDto
    {
        public int    Id     { get; set; }
        public string Name   { get; set; }
        public string NameAr { get; set; }
    }

    public class RoleFilter { }
}
