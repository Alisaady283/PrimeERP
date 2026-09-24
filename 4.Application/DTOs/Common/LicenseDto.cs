using System;
using System.Collections.Generic;

namespace PrimeERP.Application.DTOs.Common
{
    /// <summary>بيانات الترخيص</summary>
    public class LicenseDto
    {
        public int      Id           { get; set; }
        public string   CustomerName { get; set; }
        public string   Location     { get; set; }
        public string   Serial       { get; set; }
        public List<string> ModuleKeys { get; set; } = new();
        public bool     Simplified   { get; set; }
        public bool     IsActivated  { get; set; }
        public DateTime CreatedAt    { get; set; }
    }

    /// <summary>طلب سريال</summary>
    public class CreateLicenseDto
    {
        public string CustomerName { get; set; }
        public string Location     { get; set; }
        public List<string> ModuleKeys { get; set; } = new();
        public bool   Simplified   { get; set; }
    }
}
