using System;
using System.Collections.Generic;
using PrimeERP.Platform.Permissions;
using PrimeERP.Domain.Enums;
using PrimeERP.Domain.Results;

namespace PrimeERP.Application.DTOs.Accounting
{
    /// <summary>للجداول/القوائم</summary>
    public class JournalEntryDto
    {
        public int      Id          { get; set; }
        public string   EntryNo     { get; set; }
        public DateTime EntryDate   { get; set; }
        public string   Description { get; set; }
        public decimal  TotalDebit  { get; set; }
        public decimal  TotalCredit { get; set; }
        public decimal  Difference  => TotalDebit - TotalCredit;
        public bool     IsBalanced  => TotalDebit == TotalCredit;
        public string   Source      { get; set; }
        public string   SourceText  { get; set; }
        public bool     IsPosted    { get; set; }
        public StatusVariant StatusVariant { get; set; }
        public string   StatusText  { get; set; }
        public DateTime? PostedAt   { get; set; }
        public string   PostedBy    { get; set; }
        public DateTime CreatedAt   { get; set; }
        public string   CreatedBy   { get; set; }
        public int      LinesCount  { get; set; }
        public string   FiscalPeriodName { get; set; }

        public bool CanEdit   { get; set; }
        public bool CanDelete { get; set; }
        public bool CanPost   { get; set; }
        public bool CanUnpost { get; set; }
    }

    /// <summary>للحوار</summary>
    public class JournalEntryDetailDto : JournalEntryDto
    {
        public List<JournalLineDto> Lines { get; set; } = new();
    }

    public class JournalLineDto
    {
        public int     Id          { get; set; }
        public int     LineNo      { get; set; }
        public int?    AccountId   { get; set; }
        public string  AccountCode { get; set; }
        public string  AccountName { get; set; }
        public AccountType AccountType { get; set; }
        public decimal Debit       { get; set; }
        public decimal Credit      { get; set; }
        public string  Notes       { get; set; }
    }

    public class CreateJournalLineDto
    {
        public int     LineNo      { get; set; }
        public string  AccountCode { get; set; }
        public decimal Debit       { get; set; }
        public decimal Credit      { get; set; }
        public string  Notes       { get; set; }
    }

    /// <summary>لِكل من Create وUpdate</summary>
    public class CreateJournalDto
    {
        public int      Id          { get; set; }
        public DateTime EntryDate   { get; set; }
        public string   Description { get; set; }
        public string   Source      { get; set; }
        public List<CreateJournalLineDto> Lines { get; set; } = new();
    }

    public class JournalFilter
    {
        public string   SearchText { get; set; }
        public DateTime? DateFrom  { get; set; }
        public DateTime? DateTo    { get; set; }
        public string   Source     { get; set; }
        public bool?    IsPosted   { get; set; }
        public string   AccountCode { get; set; }
        public decimal? MinAmount  { get; set; }
        public decimal? MaxAmount  { get; set; }
        public string   SortBy     { get; set; } = "EntryDate";
        public bool     SortDescending { get; set; } = true;
    }

    /// <summary>سطر ميزان مراجعة</summary>
    public class TrialBalanceLine
    {
        public string      Code    { get; set; }
        public string      Name    { get; set; }
        public string      ParentCode { get; set; }
        public string      ParentName { get; set; }
        public int         Level   { get; set; }
        public AccountType Type    { get; set; }
        public bool        IsLeaf  { get; set; }
        public decimal     OpeningDebit  { get; set; }
        public decimal     OpeningCredit { get; set; }
        public decimal     PeriodDebit   { get; set; }
        public decimal     PeriodCredit  { get; set; }
        public decimal     ClosingDebit  { get; set; }
        public decimal     ClosingCredit { get; set; }
    }

    public class JournalBatchResult
    {
        public int SuccessCount { get; set; }
        public int FailedCount  { get; set; }
        public List<(int Id, string Error)> Failures { get; set; } = new();
    }
}
