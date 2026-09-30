namespace PrimeERP.Application.DTOs.Common
{
    /// <summary>بيانات الفئة</summary>
    public class CategoryDto
    {
        public int    Id         { get; set; }
        public string Name       { get; set; }
        public int?   ParentId   { get; set; }
        public string ParentName { get; set; }
        public string ModuleKey  { get; set; }
        public bool   IsActive   { get; set; }
        public string Notes      { get; set; }
        public string AccountCode { get; set; }
        public string DepreciationAccountCode { get; set; }
        public bool   HasChildren { get; set; }
    }

    public class CreateCategoryDto
    {
        public string Name      { get; set; }
        public int?   ParentId  { get; set; }
        public string ModuleKey { get; set; }
        public string Notes     { get; set; }
    }

    public class UpdateCategoryDto
    {
        public int    Id       { get; set; }
        public string Name     { get; set; }
        public bool   IsActive { get; set; }
        public string Notes    { get; set; }
    }
}
