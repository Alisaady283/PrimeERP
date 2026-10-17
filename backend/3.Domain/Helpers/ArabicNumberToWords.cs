using System;
using System.Collections.Generic;
using System.Linq;

namespace PrimeERP.Domain.Helpers
{
    /// <summary>التفقيط بالعربية</summary>
    public static class ArabicNumberToWords
    {
        private static readonly string[] Ones =
        { "", "واحد", "اثنان", "ثلاثة", "أربعة", "خمسة", "ستة", "سبعة", "ثمانية", "تسعة", "عشرة",
          "أحد عشر", "اثنا عشر", "ثلاثة عشر", "أربعة عشر", "خمسة عشر", "ستة عشر", "سبعة عشر", "ثمانية عشر", "تسعة عشر" };

        private static readonly string[] Tens =
        { "", "", "عشرون", "ثلاثون", "أربعون", "خمسون", "ستون", "سبعون", "ثمانون", "تسعون" };

        private static readonly string[] Hundreds =
        { "", "مائة", "مائتا", "ثلاثمائة", "أربعمائة", "خمسمائة", "ستمائة", "سبعمائة", "ثمانمائة", "تسعمائة" };

        private static readonly (string One, string DualBound, string Dual, string Many)[] Scales =
        {
            ("", "", "", ""),
            ("ألف", "ألفا", "ألفان", "آلاف"),
            ("مليون", "مليونا", "مليونان", "ملايين"),
            ("مليار", "مليارا", "ملياران", "مليارات"),
        };

        private static readonly Dictionary<string, string> IrregularPlurals = new()
        {
            ["قرش"] = "قروش", ["ريال"] = "ريالات", ["درهم"] = "دراهم", ["دينار"] = "دنانير", ["فلس"] = "فلوس",
        };

        public static string Convert(decimal amount, string currency = null, string subUnit = null, int decimals = 2)
        {
            currency = string.IsNullOrWhiteSpace(currency) ? "جنيه" : currency;
            subUnit = string.IsNullOrWhiteSpace(subUnit) ? "قرش" : subUnit;
            if (amount < 0) return "سالب " + Convert(-amount, currency, subUnit, decimals);

            var whole = (long)decimal.Truncate(amount);
            var scale = (decimal)Math.Pow(10, decimals);
            var fraction = (long)Math.Round((amount - whole) * scale, MidpointRounding.AwayFromZero);

            var parts = new List<string>();
            if (whole > 0) parts.Add(Phrase(whole, currency));
            if (fraction > 0) parts.Add(Phrase(fraction, subUnit));

            if (parts.Count == 0) return $"صفر {currency} فقط";

            return string.Join(" و", parts) + " فقط";
        }

        private static string Phrase(long value, string unit)
        {
            if (value == 1) return $"واحد {unit}";
            if (value == 2) return Dual(unit);

            return $"{Words(value)} {UnitForm(value, unit)}";
        }

        private static string UnitForm(long value, string unit)
        {
            var lastTwo = value % 100;

            if (lastTwo >= 3 && lastTwo <= 10) return Plural(unit);
            if (lastTwo >= 11 && lastTwo <= 99) return unit + "اً";

            return unit;
        }

        private static string Dual(string unit) => unit + "ان";

        private static string Plural(string unit) =>
            IrregularPlurals.TryGetValue(unit, out var plural) ? plural : unit + "ات";

        private static string Words(long value)
        {
            var groups = new List<string>();
            var scaleIndex = 0;

            while (value > 0 && scaleIndex < Scales.Length)
            {
                var part = (int)(value % 1000);
                value /= 1000;

                if (part > 0)
                    groups.Insert(0, scaleIndex == 0 ? UnderThousand(part) : ScaleWords(part, scaleIndex, isLast: groups.Count == 0));

                scaleIndex++;
            }

            return string.Join(" و", groups);
        }

        private static string ScaleWords(int part, int scaleIndex, bool isLast)
        {
            var (one, dualBound, dual, many) = Scales[scaleIndex];

            if (part == 1) return one;
            if (part == 2) return isLast ? dualBound : dual;
            if (part <= 10) return $"{UnderThousand(part)} {many}";

            return $"{UnderThousand(part)} {one}";
        }

        private static string UnderThousand(int value)
        {
            var parts = new List<string>();

            if (value >= 100)
            {
                parts.Add(Hundreds[value / 100]);
                value %= 100;
            }

            if (value > 0)
                parts.Add(value < 20
                    ? Ones[value]
                    : value % 10 > 0 ? $"{Ones[value % 10]} و{Tens[value / 10]}" : Tens[value / 10]);

            return string.Join(" و", parts.Where(p => !string.IsNullOrEmpty(p)));
        }
    }
}
