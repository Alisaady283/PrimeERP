using System;
using System.Globalization;
using System.Linq;

namespace PrimeERP.Domain.Calculations
{
    public record ScaleFormat(string Prefix, int ItemDigits, bool Weighted, int Decimals);

    public record ScannedCode(string Key, decimal? Qty = null, decimal? Price = null, string Batch = null, DateTime? Expiry = null);

    public static class BarcodeCalc
    {
        private const char GroupSeparator = '\u001D';

        public static bool IsNumeric(string code) => !string.IsNullOrEmpty(code) && code.All(char.IsAsciiDigit);

        public static bool IsGtinLength(string code) => code?.Length is 8 or 12 or 13 or 14;

        public static int CheckDigit(string body)
        {
            var sum = body.Reverse().Select((c, i) => (c - '0') * (i % 2 == 0 ? 3 : 1)).Sum();
            return (10 - sum % 10) % 10;
        }

        public static bool IsValidGtin(string code) =>
            IsNumeric(code) && IsGtinLength(code) && CheckDigit(code[..^1]) == code[^1] - '0';

        public static string Internal(string prefix, string code)
        {
            var digits = new string((code ?? "").Where(char.IsAsciiDigit).ToArray());
            if (digits.Length == 0 || prefix.Length + digits.Length > 12) return null;

            var body = prefix + digits.PadLeft(12 - prefix.Length, '0');
            return body + CheckDigit(body);
        }

        public static string Normalize(string gtin) =>
            gtin.Length == 14 && gtin[0] == '0' ? gtin[1..] : gtin;

        public static ScannedCode Read(string raw, ScaleFormat scale)
        {
            var code = (raw ?? "").Trim();
            if (code.StartsWith("]")) code = code.Length > 3 ? code[3..] : "";

            if (code.StartsWith("01") && code.Length >= 16 && IsNumeric(code[2..16]))
                return Gs1(code);

            if (scale != null && code.Length == 13 && IsNumeric(code) && code.StartsWith(scale.Prefix)
                && IsValidGtin(code) && scale.Prefix.Length + scale.ItemDigits + 5 <= 12)
            {
                var item = code.Substring(scale.Prefix.Length, scale.ItemDigits);
                var value = decimal.Parse(code.Substring(12 - 5, 5), CultureInfo.InvariantCulture) / (decimal)Math.Pow(10, scale.Decimals);
                return scale.Weighted
                    ? new ScannedCode(scale.Prefix + item, Qty: value)
                    : new ScannedCode(scale.Prefix + item, Price: value);
            }

            return new ScannedCode(IsValidGtin(code) ? Normalize(code) : code);
        }

        private static ScannedCode Gs1(string code)
        {
            string gtin = null, batch = null;
            DateTime? expiry = null;
            var i = 0;

            while (i + 2 <= code.Length)
            {
                var ai = code.Substring(i, 2);
                i += 2;
                switch (ai)
                {
                    case "01" when i + 14 <= code.Length:
                        gtin = code.Substring(i, 14); i += 14; break;
                    case "17" when i + 6 <= code.Length:
                        expiry = Expiry(code.Substring(i, 6)); i += 6; break;
                    case "10":
                    case "21":
                        var end = code.IndexOf(GroupSeparator, i);
                        var value = end < 0 ? code[i..] : code[i..end];
                        if (ai == "10") batch = value;
                        i = end < 0 ? code.Length : end + 1;
                        break;
                    default:
                        i = code.Length; break;
                }
                if (i < code.Length && code[i] == GroupSeparator) i++;
            }

            return new ScannedCode(gtin == null ? code : Normalize(gtin), Batch: batch, Expiry: expiry);
        }

        private static DateTime? Expiry(string yymmdd)
        {
            if (!IsNumeric(yymmdd)) return null;
            var year = 2000 + int.Parse(yymmdd[..2], CultureInfo.InvariantCulture);
            var month = int.Parse(yymmdd.Substring(2, 2), CultureInfo.InvariantCulture);
            var day = int.Parse(yymmdd.Substring(4, 2), CultureInfo.InvariantCulture);
            if (month is < 1 or > 12) return null;
            return day == 0 ? new DateTime(year, month, DateTime.DaysInMonth(year, month)) : new DateTime(year, month, Math.Min(day, DateTime.DaysInMonth(year, month)));
        }
    }
}
