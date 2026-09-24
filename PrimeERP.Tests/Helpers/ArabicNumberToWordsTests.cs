using PrimeERP.Domain.Helpers;
using Xunit;

namespace PrimeERP.Tests.Helpers
{
    /// <summary>تفقيط الأرقام بالعربية</summary>
    public class ArabicNumberToWordsTests
    {
        [Theory]
        [InlineData(0, "صفر جنيه فقط")]
        [InlineData(1, "واحد جنيه فقط")]
        [InlineData(2, "جنيهان فقط")]
        [InlineData(3, "ثلاثة جنيهات فقط")]
        [InlineData(11, "أحد عشر جنيهاً فقط")]
        [InlineData(100, "مائة جنيه فقط")]
        [InlineData(200, "مائتا جنيه فقط")]
        [InlineData(1000, "ألف جنيه فقط")]
        [InlineData(2000, "ألفا جنيه فقط")]
        [InlineData(1000000, "مليون جنيه فقط")]
        [InlineData(2000000, "مليونا جنيه فقط")]
        public void ConvertsWholeAmounts(decimal amount, string expected) =>
            Assert.Equal(expected, ArabicNumberToWords.Convert(amount));

        [Fact]
        public void ConvertsTheMandatedMixedAmount() =>
            Assert.Equal("ألف وخمسمائة وثلاثة وعشرون جنيهاً وخمسة وسبعون قرشاً فقط",
                ArabicNumberToWords.Convert(1523.75m));

        [Fact]
        public void UsesIrregularPluralsForSubUnits() =>
            Assert.Equal("واحد جنيه وخمسة قروش فقط", ArabicNumberToWords.Convert(1.05m));

        [Fact]
        public void PrefixesNegativeAmounts() =>
            Assert.StartsWith("سالب", ArabicNumberToWords.Convert(-5m));
    }
}
