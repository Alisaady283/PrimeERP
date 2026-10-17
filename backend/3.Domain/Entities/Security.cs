using System;
using PrimeERP.Domain.Entities.Common;

namespace PrimeERP.Domain.Entities
{
    /// <summary>دورٌ ومفاتيحه</summary>
    public class Role : BaseModel
    {
        public string Name     { get; set; }
        public string NameAr   { get; set; }
        public bool   IsSystem { get; set; }
    }

    public class Permission
    {
        public int    Id            { get; set; }
        public string Key           { get; set; }
        public string Module        { get; set; }
        public string DisplayNameAr { get; set; }
        public string DisplayNameEn { get; set; }
    }

    public class RolePermission
    {
        public int    Id            { get; set; }
        public int    RoleId        { get; set; }
        public string PermissionKey { get; set; }
    }

    public class UserPermission
    {
        public int    Id            { get; set; }
        public int    UserId        { get; set; }
        public string PermissionKey { get; set; }
        public bool   IsGranted     { get; set; }
    }

    public class AppSetting
    {
        public int       Id            { get; set; }
        public string    Key           { get; set; }
        public string    Value         { get; set; }
        public string    Category      { get; set; }
        public string    DataType      { get; set; }
        public string    DisplayNameAr { get; set; }
        public string    DisplayNameEn { get; set; }
        public bool      IsSystem      { get; set; }
        public DateTime? ModifiedAt    { get; set; }
        public string    ModifiedBy    { get; set; }
    }

    public class AuditEntry
    {
        public int       Id        { get; set; }
        public string    TableName { get; set; }
        public int       RecordId  { get; set; }
        public string    Action    { get; set; }
        public string    OldValues { get; set; }
        public string    NewValues { get; set; }
        public int?      UserId    { get; set; }
        public string    UserName  { get; set; }
        public DateTime? Timestamp { get; set; }
        public string    IpAddress { get; set; }
    }
}
