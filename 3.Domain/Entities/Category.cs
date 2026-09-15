namespace PrimeERP.Domain.Entities
{
    public class Category
    {
        public int    Id        { get; set; }
        public string Name      { get; set; }
        public int?   ParentId  { get; set; }
        public string ModuleKey { get; set; }
        public bool   IsActive  { get; set; } = true;
        public string Notes     { get; set; }

        /// <summary>حساب الفئة في الشجرة — تجميعيّ تحت جذر الأصول الثابتة، تعيش أصولُها تحته.</summary>
        public string AccountCode { get; set; }

        /// <summary>مرآتها تحت جذر مجمّع الإهلاك — تعيش تحتها مجمّعات أصولها.</summary>
        public string DepreciationAccountCode { get; set; }
    }
}
