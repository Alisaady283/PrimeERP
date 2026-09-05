using System;
using PrimeERP.Domain.Results;

namespace PrimeERP.Application.DTOs.Assets
{
    public class AssetDto
    {
        public int      Id           { get; set; }
        public string   Code         { get; set; }
        public string   Name         { get; set; }
        public int?     CategoryId   { get; set; }
        public string   CategoryName { get; set; }
        public DateTime? PurchaseDate { get; set; }
        public decimal  PurchaseCost { get; set; }
        public decimal  CurrentValue { get; set; }
        public int      UsefulLifeYears { get; set; }
        public decimal  SalvageValue    { get; set; }
        public decimal  AccumulatedDepreciation { get; set; }
        public string   Location     { get; set; }
        public string   Notes        { get; set; }
        public bool     IsActive     { get; set; }
        public string   StatusText   { get; set; }
        public DateTime CreatedAt    { get; set; }
        public DateTime UpdatedAt    { get; set; }
        public StatusVariant StatusVariant { get; set; }

        public bool CanEdit   { get; set; }
        public bool CanDelete { get; set; }
    }

    public class CreateAssetDto
    {
        public string   Name         { get; set; }
        public int?     CategoryId   { get; set; }
        public DateTime? PurchaseDate { get; set; }
        public decimal  PurchaseCost { get; set; }
        public decimal  CurrentValue { get; set; }
        public int      UsefulLifeYears { get; set; }
        public decimal  SalvageValue    { get; set; }
        public string   Location     { get; set; }
        public string   Notes        { get; set; }
        public bool     IsActive     { get; set; } = true;
    }

    public class UpdateAssetDto
    {
        public int      Id           { get; set; }
        public int      UsefulLifeYears { get; set; }
        public decimal  SalvageValue    { get; set; }
        public string   Name         { get; set; }
        public int?     CategoryId   { get; set; }
        public DateTime? PurchaseDate { get; set; }
        public decimal  PurchaseCost { get; set; }
        public decimal  CurrentValue { get; set; }
        public string   Location     { get; set; }
        public string   Notes        { get; set; }
        public bool     IsActive     { get; set; }
    }

    public class AssetFilter
    {
        public string SearchText     { get; set; }
        public bool?  IsActive       { get; set; }
        public int?   CategoryId     { get; set; }
        public string SortBy         { get; set; } = "Name";
        public bool   SortDescending { get; set; }
    }
}
