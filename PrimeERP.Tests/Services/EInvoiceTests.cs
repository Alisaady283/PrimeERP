using System;
using System.Text.Json;
using PrimeERP.Domain.Calculations;
using Xunit;

namespace PrimeERP.Tests.Services
{
    /// <summary>حسابات الفاتورة الإلكترونية الثابتة المواصفة</summary>
    public class EInvoiceTests
    {
        [Fact]
        public void TheZatcaQr_MatchesTheAuthoritysPublishedSample()
        {
            var qr = EInvoiceCalc.ZatcaQr("Bobs Records", "310122393500003", new DateTime(2022, 4, 25, 15, 30, 0, DateTimeKind.Utc), 1000m, 150m);

            Assert.Equal("AQxCb2JzIFJlY29yZHMCDzMxMDEyMjM5MzUwMDAwMwMUMjAyMi0wNC0yNVQxNTozMDowMFoEBzEwMDAuMDAFBjE1MC4wMA==", qr);
        }

        [Fact]
        public void TheZatcaQr_CountsArabicNamesInBytes()
        {
            var bytes = Convert.FromBase64String(EInvoiceCalc.ZatcaQr("شركة", "300000000000003", DateTime.UtcNow, 115m, 15m));

            Assert.Equal(1, bytes[0]);
            Assert.Equal(8, bytes[1]);   // أربعة أحرف عربية = 8 بايت UTF-8
        }

        [Fact]
        public void TheEtaCanonicalForm_UppercasesNames_RepeatsArrayNames_AndQuotesValues()
        {
            using var doc = JsonDocument.Parse("{\"issuer\":{\"type\":\"B\",\"id\":\"1\"},\"lines\":[{\"qty\":2},{\"qty\":3.5}],\"total\":10}");

            Assert.Equal("\"ISSUER\"\"TYPE\"\"B\"\"ID\"\"1\"\"LINES\"\"LINES\"\"QTY\"\"2\"\"LINES\"\"QTY\"\"3.5\"\"TOTAL\"\"10\"",
                EInvoiceCalc.EtaCanonical(doc.RootElement));
        }
    }
}
