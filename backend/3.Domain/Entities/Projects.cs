using System;
using PrimeERP.Domain.Entities.Common;

namespace PrimeERP.Domain.Entities
{
    /// <summary>مشروعٌ بعقده وميزانيته وعميله — مركز تكلفةٍ تُجمع عليه ساعاته ومستخلصاته</summary>
    public class Project : BaseModel
    {
        public string    Code             { get; set; }
        public string    Name             { get; set; }
        public int?      CustomerId       { get; set; }
        public int?      ManagerId        { get; set; }
        public DateTime? StartDate        { get; set; }
        public DateTime? EndDate          { get; set; }
        public decimal   ContractValue    { get; set; }
        public decimal   Budget           { get; set; }
        public decimal   RetentionPercent { get; set; }
        public int       Status           { get; set; } = 1;
        public string    Notes            { get; set; }
        public bool      IsActive         { get; set; } = true;
    }

    /// <summary>مهمةٌ أو مرحلةٌ في مشروع — وزنها يحكم نسبة إنجازه، والمعلَم منها يُستخلص بنسبته</summary>
    public class ProjectTask : BaseModel
    {
        public string    Code           { get; set; }
        public string    Name           { get; set; }
        public int       ProjectId      { get; set; }
        public int?      EmployeeId     { get; set; }
        public DateTime? StartDate      { get; set; }
        public DateTime? DueDate        { get; set; }
        public decimal   Weight         { get; set; } = 1;
        public decimal   Progress       { get; set; }
        public int       Status         { get; set; } = 1;
        public bool      IsMilestone    { get; set; }
        public decimal   BillingPercent { get; set; }
        public string    Notes          { get; set; }
        public bool      IsActive       { get; set; } = true;
    }

    /// <summary>ساعات موظفٍ على مشروع — تكلفتها تُحمَّل على المشروع بقيدها</summary>
    public class ProjectTimesheet : BaseModel
    {
        public string   Code           { get; set; }
        public int      ProjectId      { get; set; }
        public int?     TaskId         { get; set; }
        public int      EmployeeId     { get; set; }
        public DateTime Date           { get; set; } = DateTime.Today;
        public decimal  Hours          { get; set; }
        public decimal  HourlyCost     { get; set; }
        public decimal  Cost           { get; set; }
        public string   Notes          { get; set; }
        public int?     JournalEntryId { get; set; }
        public bool     IsActive       { get; set; } = true;
    }

    /// <summary>مستخلصٌ على العميل بنسبة إنجازٍ أو بمعلَم — يُحجز منه ضمان الأعمال ويُقيَّد إيراداً</summary>
    public class ProjectBill : BaseModel
    {
        public string   Code           { get; set; }
        public int      ProjectId      { get; set; }
        public int?     TaskId         { get; set; }
        public DateTime Date           { get; set; } = DateTime.Today;
        public decimal  Percent        { get; set; }
        public decimal  Amount         { get; set; }
        public decimal  Retention      { get; set; }
        public decimal  Net            { get; set; }
        public string   Notes          { get; set; }
        public int?     JournalEntryId { get; set; }
        public bool     IsActive       { get; set; } = true;
    }
}
