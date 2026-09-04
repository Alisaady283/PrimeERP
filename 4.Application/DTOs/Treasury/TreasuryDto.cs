using PrimeERP.Domain.Enums;

namespace PrimeERP.Application.DTOs.Treasury
{
    public class TreasuryDto
    {
        public int          Id            { get; set; }
        public string       Code          { get; set; }
        public string       Name          { get; set; }
        public TreasuryKind Kind          { get; set; }
        public string       KindName      { get; set; }
        public string       AccountCode   { get; set; }
        public string       BankName      { get; set; }
        public string       AccountNumber { get; set; }
        public string       Notes         { get; set; }
        public bool         IsActive      { get; set; }
    }

    public class CreateTreasuryDto
    {
        public string Name          { get; set; }
        public bool   IsBank        { get; set; }
        public string AccountCode   { get; set; }
        public string BankName      { get; set; }
        public string AccountNumber { get; set; }
        public string Notes         { get; set; }
        public bool   IsActive      { get; set; } = true;
    }

    public class UpdateTreasuryDto : CreateTreasuryDto
    {
        public int Id { get; set; }
    }

    public class TreasuryFilter
    {
        public string SearchText { get; set; }
    }
}
