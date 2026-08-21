using System;
using PrimeERP.Models.Common;

namespace PrimeERP.Models
{
    public class User : BaseModel
    {
        public string    Username     { get; set; }
        public string    PasswordHash { get; set; }
        public string    Salt         { get; set; }
        public string    DisplayName  { get; set; }
        public int       RoleId       { get; set; }
        public string    RoleName     { get; set; }
        public bool      IsActive     { get; set; } = true;
        public DateTime? LastLoginAt  { get; set; }
    }
}
