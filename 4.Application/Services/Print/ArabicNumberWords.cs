using System;
using System.Collections.Generic;
using System.Linq;

namespace PrimeERP.Application.Services.Print
{
    /// <summary>المبلغ كتابةً بالعربية — السند لا يُعتدّ به قانوناً بالرقم وحده، فالكتابة تمنع التحريف.</summary>
    public static class ArabicNumberWords
    {
        private static readonly string[] Ones =
        { "", "واحد", "اثنان", "ثلاثة", "أربعة", "خمسة", "ستة", "سبعة", "ثمانية", "تسعة", "عشرة",
          "أحد عشر", "اثنا عشر", "ثلاثة عشر", "أربعة عشر", "خمسة عشر", "ستة عشر", "سبعة عشر", "ثمانية عشر", "تسعة عشر" };

        private static readonly string[] Tens =
        { "", "", "عشرون", "ثلاثون", "أربعون", "خمسون", "ستون", "سبعون", "ثمانون", "تسعون" };

        private static readonly string[] Hundreds =
        { "", "مائة", "مائتان", "ثلاثمائة", "أربعمائة", "خمسمائة", "ستمائة", "سبعمائة", "ثمانمائة", "تسعمائة" };

        // (مفرد، مثنى، جمع) لكل مرتبة — العربية تصرّف العدد لا تكرّر اللفظ.
        private static readonly (string One, string Two, string Many)[] Scales =
        { ("", "", ""), ("ألف", "ألفان", "آلاف"), ("مليون", "مليونان", "ملايين"), ("مليار", "ملياران", "مليارات") };

        public static string Convert(decimal amount, string currency = "جنيه", string fraction = "قرش")
        {
            if (amount < 0) return "سالب " + Convert(-amount, currency, fraction);

            var whole = (long)decimal.Truncate(amount);
            var cents = (int)Math.Round((amount - whole) * 100, MidpointRounding.AwayFromZero);

            var text = whole == 0 ? "صفر" : ThreeDigitGroups(whole);
            var result = $"{text} {currency}";

            if (cents > 0) result += $" و{ThreeDigitGroups(cents)} {fraction}";

            return result + " فقط لا غير";
        }

        private static string ThreeDigitGroups(long value)
        {
            var groups = new List<string>();

            for (int scale = 0; value > 0 && scale < Scales.Length; scale++)
            {
                var part = (int)(value % 1000);
                value /= 1000;
                if (part == 0) continue;

                groups.Insert(0, scale == 0 ? UnderThousand(part) : WithScale(part, scale));
            }

            return string.Join(" و", groups);
        }

        private static string WithScale(int part, int scale)
        {
            var (one, two, many) = Scales[scale];

            if (part == 1) return one;
            if (part == 2) return two;
            if (part <= 10) return $"{UnderThousand(part)} {many}";

            return $"{UnderThousand(part)} {one}";
        }

        private static string UnderThousand(int value)
        {
            var parts = new List<string>();

            if (value >= 100) { parts.Add(Hundreds[value / 100]); value %= 100; }
            if (value == 0) return string.Join(" و", parts);

            if (value < 20) parts.Add(Ones[value]);
            else
            {
                var unit = value % 10;
                parts.Add(unit > 0 ? $"{Ones[unit]} و{Tens[value / 10]}" : Tens[value / 10]);
            }

            return string.Join(" و", parts.Where(p => !string.IsNullOrEmpty(p)));
        }
    }
}
