using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Application.Services;
using PrimeERP.Platform.Settings;
using Xunit;
using PrimeERP.Application.Services.Admin;

namespace PrimeERP.Tests.Services
{
    /// <summary>الإعدادات</summary>
    [Collection("Database")]
    public class SettingsServiceTests
    {
        private readonly ISettingsService _service;

        public SettingsServiceTests(TestDatabaseFixture db) => _service = db.Services.GetRequiredService<ISettingsService>();

        [Fact]
        public void Get_ReturnsDefault_WhenKeyMissing()
        {
            var value = _service.Get("NoSuchKey.Ever", "fallback");
            Assert.Equal("fallback", value);
        }

        [Fact]
        public void Get_ReturnsDefault_ForMissingIntKey()
        {
            var value = _service.Get("NoSuchKey.Int", 42);
            Assert.Equal(42, value);
        }

        [Fact]
        public void SetThenGet_ReturnsStoredStringValue()
        {
            var result = _service.Set("Test.String.Key", "hello");
            Assert.True(result.IsSuccess);

            var value = _service.Get("Test.String.Key", "");
            Assert.Equal("hello", value);
        }

        [Fact]
        public void SetThenGet_RoundTripsBool()
        {
            _service.Set("Test.Bool.Key", true);
            Assert.True(_service.Get("Test.Bool.Key", false));

            _service.Set("Test.Bool.Key", false);
            Assert.False(_service.Get("Test.Bool.Key", true));
        }

        [Fact]
        public void SetThenGet_RoundTripsInt()
        {
            _service.Set("Test.Int.Key", 123);
            Assert.Equal(123, _service.Get("Test.Int.Key", 0));
        }

        [Fact]
        public void Cache_ReflectsSetWithoutManualReload()
        {
            _service.Set("Test.Cache.Key", "first");
            Assert.Equal("first", _service.Get("Test.Cache.Key", ""));

            _service.Set("Test.Cache.Key", "second");
            Assert.Equal("second", _service.Get("Test.Cache.Key", ""));
        }

        [Fact]
        public void Reload_ForcesFreshReadOnNextGet()
        {
            _service.Set("Test.Reload.Key", "before");
            Assert.Equal("before", _service.Get("Test.Reload.Key", ""));

            _service.Reload();

            Assert.Equal("before", _service.Get("Test.Reload.Key", ""));
        }

        [Fact]
        public void SetMany_PersistsAllValuesInOneCall()
        {
            var result = _service.SetMany(new()
            {
                ["Test.Many.A"] = "a-value",
                ["Test.Many.B"] = 7
            });

            Assert.True(result.IsSuccess);
            Assert.Equal("a-value", _service.Get("Test.Many.A", ""));
            Assert.Equal(7, _service.Get("Test.Many.B", 0));
        }

        [Fact]
        public void SettingChanged_FiresWithChangedKey()
        {
            string? changedKey = null;
            _service.SettingChanged += key => changedKey = key;

            _service.Set("Test.Event.Key", "x");

            Assert.Equal("Test.Event.Key", changedKey);
        }
    }
}
