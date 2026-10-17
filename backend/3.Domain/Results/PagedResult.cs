using System;
using System.Collections.Generic;

namespace PrimeERP.Domain.Results
{
    /// <summary>نتيجة صفحة واحدة من قائمة</summary>
    public class PagedResult<T>
    {
        public List<T> Items      { get; set; } = new();
        public int     TotalCount { get; set; }
        public int     Page       { get; set; }
        public int     PageSize   { get; set; }

        public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
    }
}
