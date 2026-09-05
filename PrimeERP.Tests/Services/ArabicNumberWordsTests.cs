using PrimeERP.Application.Services.Print;
using Xunit;

namespace PrimeERP.Tests.Services
{
    public class ArabicNumberWordsTests
    {
        [Theory]
        [InlineData(0, "صفر جنيه فقط لا غير")]
        [InlineData(1, "واحد جنيه فقط لا غير")]
        [InlineData(15, "خمسة عشر جنيه فقط لا غير")]
        [InlineData(21, "واحد وعشرون جنيه فقط لا غير")]
        [InlineData(100, "مائة جنيه فقط لا غير")]
        [InlineData(1000, "ألف جنيه فقط لا غير")]
        [InlineData(2000, "ألفان جنيه فقط لا غير")]
        [InlineData(3500, "ثلاثة آلاف وخمسمائة جنيه فقط لا غير")]
        [InlineData(1000000, "مليون جنيه فقط لا غير")]
        public void Converts(decimal amount, string expected) =>
            Assert.Equal(expected, ArabicNumberWords.Convert(amount));

        [Fact]
        public void IncludesTheFraction() =>
            Assert.Equal("خمسة وعشرون جنيه وخمسون قرش فقط لا غير", ArabicNumberWords.Convert(25.50m));
    }
}
