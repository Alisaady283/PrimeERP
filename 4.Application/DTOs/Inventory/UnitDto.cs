namespace PrimeERP.Application.DTOs.Inventory
{
    /// <summary>بيانات الوحدة</summary>
    public class UnitDto
    {
        public int    Id       { get; set; }
        public string Name     { get; set; }
        public string NameEn   { get; set; }
        public string Symbol   { get; set; }
        public bool   IsActive { get; set; }
    }

    public class CreateUnitDto
    {
        public string Name     { get; set; }
        public string NameEn   { get; set; }
        public string Symbol   { get; set; }
        public bool   IsActive { get; set; } = true;
    }

    public class UpdateUnitDto
    {
        public int    Id       { get; set; }
        public string Name     { get; set; }
        public string NameEn   { get; set; }
        public string Symbol   { get; set; }
        public bool   IsActive { get; set; }
    }

    public class UnitFilter { }
}
