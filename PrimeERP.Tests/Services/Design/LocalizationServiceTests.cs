using System;
using PrimeERP.Platform.Localization;
using Xunit;

namespace PrimeERP.Tests.Services.Design
{
    /// <summary>تبديل قاموس النصوص الحقيقي</summary>
    [Collection("WpfApplication")]
    public class LocalizationServiceTests
    {
        [Fact]
        public void Apply_English_ChangesCurrentLanguageAndResolvesEnglishString()
        {
            WpfApplicationFixture.Run(() =>
            {
                try
                {
                    LocalizationService.Apply(AppLanguage.En);

                    Assert.Equal(AppLanguage.En, LocalizationService.CurrentLanguage);
                    Assert.Equal("Confirm", LocalizationService.Get("Str.Confirm"));
                }
                finally
                {
                    LocalizationService.Apply(AppLanguage.Ar);
                }
            });
        }

        [Fact]
        public void Apply_Arabic_ResolvesArabicString()
        {
            WpfApplicationFixture.Run(() =>
            {
                LocalizationService.Apply(AppLanguage.Ar);

                Assert.Equal(AppLanguage.Ar, LocalizationService.CurrentLanguage);
                Assert.Equal("تأكيد", LocalizationService.Get("Str.Confirm"));
            });
        }

        [Fact]
        public void Toggle_SwitchesBetweenArabicAndEnglish()
        {
            WpfApplicationFixture.Run(() =>
            {
                try
                {
                    LocalizationService.Apply(AppLanguage.Ar);

                    LocalizationService.Toggle();
                    Assert.Equal(AppLanguage.En, LocalizationService.CurrentLanguage);

                    LocalizationService.Toggle();
                    Assert.Equal(AppLanguage.Ar, LocalizationService.CurrentLanguage);
                }
                finally
                {
                    LocalizationService.Apply(AppLanguage.Ar);
                }
            });
        }

        [Fact]
        public void Get_ReturnsKeyItself_WhenKeyNotFound()
        {
            WpfApplicationFixture.Run(() =>
            {
                LocalizationService.Apply(AppLanguage.Ar);
                Assert.Equal("Str.NoSuchKey.Ever", LocalizationService.Get("Str.NoSuchKey.Ever"));
            });
        }

        [Fact]
        public void Apply_RaisesLanguageChanged()
        {
            WpfApplicationFixture.Run(() =>
            {
                try
                {
                    LocalizationService.Apply(AppLanguage.Ar);

                    bool raised = false;
                    EventHandler handler = (s, e) => raised = true;
                    LocalizationService.LanguageChanged += handler;

                    try { LocalizationService.Apply(AppLanguage.En); }
                    finally { LocalizationService.LanguageChanged -= handler; }

                    Assert.True(raised);
                }
                finally
                {
                    LocalizationService.Apply(AppLanguage.Ar);
                }
            });
        }
    }
}
