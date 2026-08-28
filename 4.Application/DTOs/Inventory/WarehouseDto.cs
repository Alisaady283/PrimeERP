namespace PrimeERP.Application.DTOs.Inventory
{
    public class WarehouseDto
    {
        public int    Id          { get; set; }
        public string Code        { get; set; }
        public string Name        { get; set; }
        public string Location    { get; set; }
        public string ManagerName { get; set; }
        public bool   IsActive    { get; set; }
    }

    public class CreateWarehouseDto
    {
        public string Name        { get; set; }
        public string Location    { get; set; }
        public string ManagerName { get; set; }
        public bool   IsActive    { get; set; } = true;
    }

    public class UpdateWarehouseDto
    {
        public int    Id          { get; set; }
        public string Name        { get; set; }
        public string Location    { get; set; }
        public string ManagerName { get; set; }
        public bool   IsActive    { get; set; }
    }

    public class WarehouseFilter { }
}
