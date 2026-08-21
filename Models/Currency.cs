using PrimeERP.Models.Common;

namespace PrimeERP.Models
{
    public class Currency : BaseModel
    {
        public string Code          { get; set; }
        public string Name          { get; set; }
        public string Symbol        { get; set; }
        public bool   IsBase        { get; set; }
        public int    DecimalPlaces { get; set; } = 2;
    }
}
