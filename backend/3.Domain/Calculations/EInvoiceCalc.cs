using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace PrimeERP.Domain.Calculations
{
    /// <summary>حسابات الفاتورة الإلكترونية الثابتة المواصفة — بلا شبكة ولا مفاتيح</summary>
    public static class EInvoiceCalc
    {
        /// <summary>
        /// رمز QR لهيئة الزكاة (ZATCA — المرحلة الأولى): TLV بوسومٍ 1..5 (البائع، الرقم الضريبي، الوقت ISO، الإجمالي مع الضريبة، الضريبة)
        /// كلٌّ: وسمٌ بايت، وطولٌ بايت، والقيمة UTF-8 — ثم Base64.
        /// </summary>
        public static string ZatcaQr(string seller, string vatNumber, DateTime issuedUtc, decimal totalWithVat, decimal vat)
        {
            using var stream = new MemoryStream();
            void Tag(byte tag, string value)
            {
                var bytes = Encoding.UTF8.GetBytes(value ?? "");
                if (bytes.Length > 255) throw new ArgumentException("TLV value too long", nameof(value));
                stream.WriteByte(tag);
                stream.WriteByte((byte)bytes.Length);
                stream.Write(bytes, 0, bytes.Length);
            }

            Tag(1, seller);
            Tag(2, vatNumber);
            Tag(3, issuedUtc.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture));
            Tag(4, totalWithVat.ToString("0.00", CultureInfo.InvariantCulture));
            Tag(5, vat.ToString("0.00", CultureInfo.InvariantCulture));
            return Convert.ToBase64String(stream.ToArray());
        }

        /// <summary>
        /// الشكل القياسي لمستند مصلحة الضرائب المصرية (ETA) قبل التوقيع والبصمة:
        /// الخاصية "اسمها بحروفٍ كبيرة" ثم قيمتها، والمصفوفة يتكرر اسمها قبل كل عنصر، والقيمة البسيطة بين علامتي تنصيص.
        /// </summary>
        public static string EtaCanonical(JsonElement element)
        {
            var text = new StringBuilder();
            Write(element, text);
            return text.ToString();
        }

        private static void Write(JsonElement element, StringBuilder text)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    foreach (var property in element.EnumerateObject())
                    {
                        var name = $"\"{property.Name.ToUpperInvariant()}\"";
                        if (property.Value.ValueKind == JsonValueKind.Array)
                        {
                            text.Append(name);
                            foreach (var item in property.Value.EnumerateArray())
                            {
                                text.Append(name);
                                Write(item, text);
                            }
                        }
                        else
                        {
                            text.Append(name);
                            Write(property.Value, text);
                        }
                    }
                    break;
                case JsonValueKind.Array:
                    foreach (var item in element.EnumerateArray()) Write(item, text);
                    break;
                case JsonValueKind.String:
                    text.Append('"').Append(element.GetString()).Append('"');
                    break;
                case JsonValueKind.Null:
                    text.Append("\"\"");
                    break;
                default:
                    text.Append('"').Append(element.GetRawText()).Append('"');
                    break;
            }
        }
    }
}
