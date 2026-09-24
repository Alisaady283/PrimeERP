using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Application.Services;
using Xunit;

namespace PrimeERP.Tests.Services
{
    /// <summary>الترقيم التسلسلي</summary>
    [Collection("Database")]
    public class NumberSequenceServiceTests
    {
        private readonly INumberSequenceService _service;

        public NumberSequenceServiceTests(TestDatabaseFixture db) => _service = db.Services.GetRequiredService<INumberSequenceService>();

        [Fact]
        public void Next_FirstCall_StartsAtOneWithFiveDigitPadding()
        {
            var number = _service.Next("Test.Seq.First");
            var year = System.DateTime.Now.Year;

            Assert.Equal($"Test.Seq.First-{year}-00001", number);
        }

        [Fact]
        public void Next_SecondCall_IncrementsBySequentialKey()
        {
            _service.Next("Test.Seq.Increment");
            var second = _service.Next("Test.Seq.Increment");
            var year = System.DateTime.Now.Year;

            Assert.Equal($"Test.Seq.Increment-{year}-00002", second);
        }

        [Fact]
        public void Peek_DoesNotConsumeTheNumber()
        {
            var peeked = _service.Peek("Test.Seq.Peek");
            var next = _service.Next("Test.Seq.Peek");

            Assert.Equal(peeked, next);

            var peekedAfter = _service.Peek("Test.Seq.Peek");
            Assert.NotEqual(next, peekedAfter);
        }

        [Fact]
        public void Next_DifferentKeys_AreIndependentSequences()
        {
            var a1 = _service.Next("Test.Seq.KeyA");
            var b1 = _service.Next("Test.Seq.KeyB");
            var a2 = _service.Next("Test.Seq.KeyA");

            Assert.StartsWith("Test.Seq.KeyA", a1);
            Assert.StartsWith("Test.Seq.KeyB", b1);
            Assert.EndsWith("00002", a2);
        }
    }
}
