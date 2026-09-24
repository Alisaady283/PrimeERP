using System;
using PrimeERP.Domain.Results;

namespace PrimeERP.Application.DTOs.Assets
{
    /// <summary>بيانات استبعاد الأصل</summary>
    public class AssetDisposalDto
    {
        public int      Id           { get; set; }
        public int      AssetId      { get; set; }
        public string   AssetCode    { get; set; }
        public string   AssetName    { get; set; }
        public DateTime DisposalDate { get; set; }
        public int      TreasuryId   { get; set; }
        public string   TreasuryName { get; set; }
        public decimal  SalePrice    { get; set; }
        public decimal  BookValue    { get; set; }
        public decimal  GainOrLoss   { get; set; }

        public string   KindText     { get; set; }
        public StatusVariant KindVariant { get; set; }

        public string   Notes        { get; set; }
        public int?     JournalEntryId { get; set; }
        public DateTime CreatedAt    { get; set; }
        public DateTime UpdatedAt    { get; set; }

        public bool CanEdit   { get; set; }
        public bool CanDelete { get; set; }
    }

    public class CreateAssetDisposalDto
    {
        public int      AssetId      { get; set; }
        public DateTime DisposalDate { get; set; } = DateTime.Today;
        public int      TreasuryId   { get; set; }
        public decimal  SalePrice    { get; set; }
        public string   Notes        { get; set; }
    }

    public class UpdateAssetDisposalDto
    {
        public int      Id           { get; set; }
        public int      AssetId      { get; set; }
        public DateTime DisposalDate { get; set; }
        public int      TreasuryId   { get; set; }
        public decimal  SalePrice    { get; set; }
        public string   Notes        { get; set; }
    }

    public class AssetDisposalFilter
    {
        public string SearchText     { get; set; }
        public int?   AssetId        { get; set; }
        public string SortBy         { get; set; } = "DisposalDate";
        public bool   SortDescending { get; set; } = true;
    }
}
