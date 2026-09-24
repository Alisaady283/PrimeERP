using System;

namespace PrimeERP.Application.DTOs.Security
{
    /// <summary>بيانات المستخدم</summary>
    public class UserDto
    {
        public int      Id          { get; set; }
        public string   Username    { get; set; }
        public string   DisplayName { get; set; }
        public int      RoleId      { get; set; }
        public string   RoleName    { get; set; }
        public bool     IsActive    { get; set; }
        public string   StatusText  { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public PrimeERP.Domain.Results.StatusVariant StatusVariant { get; set; }
    }

    public class CreateUserDto
    {
        public string Username    { get; set; }
        public string Password    { get; set; }
        public string DisplayName { get; set; }
        public int    RoleId      { get; set; }
        public bool   IsActive    { get; set; } = true;
    }

    public class UpdateUserDto
    {
        public int    Id          { get; set; }
        public string Password    { get; set; }
        public string DisplayName { get; set; }
        public int    RoleId      { get; set; }
        public bool   IsActive    { get; set; }
    }

    public class UserFilter { }
}
