using System;
using PrimeERP.Domain.Results;

namespace PrimeERP.Application.DTOs.Assets
{
    /// <summary>بيانات إعادة التقييم</summary>
    public class AssetRevaluationDto
    {
        public int      Id           { get; set; }
        public int      AssetId      { get; set; }
        public string   AssetCode    { get; set; }
        public string   AssetName    { get; set; }
        public DateTime RevaluationDate { get; set; }
        public decimal  OldValue     { get; set; }
        public decimal  NewValue     { get; set; }
        public decimal  Difference   { get; set; }

        public string   KindText     { get; set; }
        public StatusVariant KindVariant { get; set; }

        public string   Notes        { get; set; }
        public int?     JournalEntryId { get; set; }
        public DateTime CreatedAt    { get; set; }
        public DateTime UpdatedAt    { get; set; }

        public bool CanEdit   { get; set; }
        public bool CanDelete { get; set; }
    }

    public class CreateAssetRevaluationDto
    {
        public int      AssetId      { get; set; }
        public DateTime RevaluationDate { get; set; } = DateTime.Today;
        public decimal  NewValue     { get; set; }
        public string   Notes        { get; set; }
    }

    public class UpdateAssetRevaluationDto
    {
        public int      Id           { get; set; }
        public int      AssetId      { get; set; }
        public DateTime RevaluationDate { get; set; }
        public decimal  NewValue     { get; set; }
        public string   Notes        { get; set; }
    }

    public class AssetRevaluationFilter
    {
        public string SearchText     { get; set; }
        public int?   AssetId        { get; set; }
        public string SortBy         { get; set; } = "RevaluationDate";
        public bool   SortDescending { get; set; } = true;
    }
}
