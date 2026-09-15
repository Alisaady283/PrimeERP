using System;
using PrimeERP.Domain.Enums;
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
        public decimal  RevaluedValue { get; set; }
        public decimal  CurrentValue { get; set; }
        public int      UsefulLifeYears { get; set; }
        public decimal  SalvageValue    { get; set; }
        public decimal  AccumulatedDepreciation { get; set; }
        public DateTime? LastDepreciationDate { get; set; }

        /// <summary>المموّل كما حُفظ — يُعاد عرضه في حوار التعديل فلا يُطالب المستخدم بإعادة اختياره.</summary>
        public AssetAcquisition AcquisitionMethod { get; set; }
        public int?     FundingId    { get; set; }
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
        /// <summary>خزينة أو بنك أو مورد — تحكم قائمة المموّل والطرف الدائن في قيد الاقتناء.</summary>
        public AssetAcquisition AcquisitionMethod { get; set; }

        /// <summary>المموّل المختار من القائمة التي تتبع الطريقة: خزينة، أو بنك، أو مورد.</summary>
        public int?     FundingId    { get; set; }
        public int?     CategoryId   { get; set; }
        public DateTime? PurchaseDate { get; set; }
        public decimal  PurchaseCost { get; set; }
        public int      UsefulLifeYears { get; set; }
        public decimal  SalvageValue    { get; set; }
        public string   Location     { get; set; }
        public string   Notes        { get; set; }
        public bool     IsActive     { get; set; } = true;
    }

    public class UpdateAssetDto
    {
        public int      Id           { get; set; }
        /// <summary>مموّل الأصل — يُطلَب عند التعديل ليُرحَّل قيد اقتناء أصلٍ أُنشئ قبل الترحيل.</summary>
        public AssetAcquisition AcquisitionMethod { get; set; }
        public int?     FundingId    { get; set; }
        public int      UsefulLifeYears { get; set; }
        public decimal  SalvageValue    { get; set; }
        public string   Name         { get; set; }
        public int?     CategoryId   { get; set; }
        public DateTime? PurchaseDate { get; set; }
        public decimal  PurchaseCost { get; set; }
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
