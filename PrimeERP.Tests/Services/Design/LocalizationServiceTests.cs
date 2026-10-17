using System;
using PrimeERP.Platform.Localization;
using PrimeERP.UI.Services;
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
                    LanguageResources.Apply(AppLanguage.En);

                    Assert.Equal(AppLanguage.En, LocalizationService.CurrentLanguage);
                    Assert.Equal("Confirm", LocalizationService.Get("Str.Confirm"));
                    Assert.Equal("Confirm", System.Windows.Application.Current.Resources["Str.Confirm"]);
                }
                finally
                {
                    LanguageResources.Apply(AppLanguage.Ar);
                }
            });
        }

        [Fact]
        public void Apply_Arabic_ResolvesArabicString()
        {
            WpfApplicationFixture.Run(() =>
            {
                LanguageResources.Apply(AppLanguage.Ar);

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
                    LanguageResources.Apply(AppLanguage.Ar);

                    LanguageResources.Toggle();
                    Assert.Equal(AppLanguage.En, LocalizationService.CurrentLanguage);

                    LanguageResources.Toggle();
                    Assert.Equal(AppLanguage.Ar, LocalizationService.CurrentLanguage);
                }
                finally
                {
                    LanguageResources.Apply(AppLanguage.Ar);
                }
            });
        }

        [Fact]
        public void Get_ReturnsKeyItself_WhenKeyNotFound()
        {
            WpfApplicationFixture.Run(() =>
            {
                LanguageResources.Apply(AppLanguage.Ar);
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
                    LanguageResources.Apply(AppLanguage.Ar);

                    bool raised = false;
                    EventHandler handler = (s, e) => raised = true;
                    LocalizationService.LanguageChanged += handler;

                    try { LanguageResources.Apply(AppLanguage.En); }
                    finally { LocalizationService.LanguageChanged -= handler; }

                    Assert.True(raised);
                }
                finally
                {
                    LanguageResources.Apply(AppLanguage.Ar);
                }
            });
        }
    }
}
