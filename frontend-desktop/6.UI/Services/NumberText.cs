using System;

namespace PrimeERP.UI.Services
{
    /// <summary>الرقم مختصراً بقاعدةٍ واحدة: من الألف K ومن المليون M، وما دونه بتنسيقه — نظير compact في الويب</summary>
    public static class NumberText
    {
        public static string Compact(double value, string format)
        {
            var abs = Math.Abs(value);
            return abs >= 1_000_000 ? $"{value / 1_000_000:0.#}M" : abs >= 1_000 ? $"{value / 1_000:0.#}K" : value.ToString(format);
        }
    }
}
