using System;

namespace PrimeERP.Application.DTOs.Security
{
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

    // Password هنا نص عادٍ من الحوار فقط — يُهاش داخل الخدمة قبل التخزين، لا يُخزَّن ولا يُعرَض أبداً كنص صريح.
    public class CreateUserDto
    {
        public string Username    { get; set; }
        public string Password    { get; set; }
        public string DisplayName { get; set; }
        public int    RoleId      { get; set; }
        public bool   IsActive    { get; set; } = true;
    }

    // Password فارغة = بلا تغيير (DialogRenderer.ApplyFields يتجاهل حقل كلمة المرور الفارغ عند التعديل).
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
