using System;
using System.Collections.Generic;
using PrimeERP.Domain.Enums;

namespace PrimeERP.Application.DTOs.Cheques
{
    public class ChequeDto
    {
        public int      Id            { get; set; }
        public string   ChequeNo      { get; set; }
        public string   DirectionName { get; set; }
        public int?     PartyId       { get; set; }
        public string   PartyName     { get; set; }
        public decimal  Amount        { get; set; }
        public DateTime IssueDate     { get; set; }
        public DateTime DueDate       { get; set; }
        public string   BankName      { get; set; }
        public string   StatusName    { get; set; }
        public string   TreasuryName  { get; set; }
        public string   Notes         { get; set; }
    }

    public class ChequeDetailDto : ChequeDto
    {
        public ChequeStatus    Status    { get; set; }
        public ChequeDirection Direction { get; set; }
        public List<ChequeMovementDto> Movements { get; set; } = new();
    }

    public class ChequeMovementDto
    {
        public DateTime MovementDate   { get; set; }
        public string   FromStatusName { get; set; }
        public string   ToStatusName   { get; set; }
        public string   Notes          { get; set; }
        public string   CreatedBy      { get; set; }
    }

    /// <summary>نقل شيك لحالة جديدة — TreasuryId مطلوب عملياً للإيداع والتحصيل (أين ذهب المال).</summary>
    public class MoveChequeDto
    {
        public int      ChequeId     { get; set; }
        public int      ToStatus     { get; set; }
        public DateTime MovementDate { get; set; } = DateTime.Today;
        public int?     TreasuryId   { get; set; }
        public string   Notes        { get; set; }
    }

    /// <summary>مستند استلام/صرف شيكات — رأس واحد وعدة شيكات، بنفس شكل أي مستند رأس+سطور.</summary>
    public class CreateChequeDocumentDto
    {
        public int       Id      { get; set; }
        public DateTime  DocDate { get; set; } = DateTime.Today;
        public string    Notes   { get; set; }
        public List<CreateChequeLineDto> Lines { get; set; } = new();
    }

    public class CreateChequeLineDto
    {
        public int       LineNo   { get; set; }
        public string    ChequeNo { get; set; }
        public string    BankName { get; set; }
        public decimal   Amount   { get; set; }
        public int?      PartyId  { get; set; }
        public DateTime? DueDate  { get; set; }
        public string    Notes    { get; set; }
    }

    public class ChequeDocumentResultDto
    {
        public List<int> ChequeIds { get; set; } = new();
    }

    public class ChequeFilter
    {
        public string SearchText { get; set; }
        public int    Direction  { get; set; }
        public int    Status     { get; set; }
    }
}
