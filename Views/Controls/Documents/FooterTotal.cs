namespace PrimeERP.Views.Controls.Documents
{
    /// <summary>
    /// عنصر إجمالي واحد في DocumentFooter. Variant يستخدم نفس مفردات AppBadge بالضبط
    /// ("success"/"danger"/"warning"/"info"/"brand" وأي شيء آخر = محايد) لتوحيد الألوان الدلالية في النظام.
    /// </summary>
    public class FooterTotal
    {
        public string  Key     { get; set; }
        public string  Label   { get; set; }
        public decimal Value   { get; set; }
        public string  Variant { get; set; } = "neutral";
        public bool    IsBold  { get; set; }
        public bool    IsLarge { get; set; }
    }
}
