using System;
using PrimeERP.Domain.Calculations;
using Xunit;

namespace PrimeERP.Tests.Services
{
    public class BarcodeCalcTests
    {
        private static readonly ScaleFormat Scale = new("21", 5, Weighted: true, Decimals: 3);

        [Theory]
        [InlineData("6221031490019", true)]
        [InlineData("6221031490018", false)]
        [InlineData("96385074", true)]
        [InlineData("036000291452", true)]
        [InlineData("ABC123", false)]
        public void Gtin_CheckDigit(string code, bool valid) => Assert.Equal(valid, BarcodeCalc.IsValidGtin(code));

        [Fact]
        public void Internal_IsAValidRestrictedEan13()
        {
            var code = BarcodeCalc.Internal("20", "P0042");
            Assert.Equal(13, code.Length);
            Assert.StartsWith("20", code);
            Assert.True(BarcodeCalc.IsValidGtin(code));
        }

        [Fact]
        public void ScaleBarcode_CarriesItemAndWeight()
        {
            var body = "210012301250";
            var read = BarcodeCalc.Read(body + BarcodeCalc.CheckDigit(body), Scale);
            Assert.Equal("2100123", read.Key);
            Assert.Equal(1.25m, read.Qty);
        }

        [Fact]
        public void Gs1DataMatrix_CarriesGtinBatchAndExpiry()
        {
            var read = BarcodeCalc.Read("0106221031490019" + "17271231" + "10LOT42", Scale);
            Assert.Equal("6221031490019", read.Key);
            Assert.Equal("LOT42", read.Batch);
            Assert.Equal(new DateTime(2027, 12, 31), read.Expiry);
        }

        [Fact]
        public void LocalBarcode_IsReadAsIs() => Assert.Equal("SHOP-77", BarcodeCalc.Read(" SHOP-77 ", Scale).Key);
    }
}
