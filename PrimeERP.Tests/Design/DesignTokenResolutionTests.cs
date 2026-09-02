using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Platform.Design;
using ThemeMode = PrimeERP.Platform.Design.ThemeMode;
using Xunit;

namespace PrimeERP.Tests.Design
{
    /// <summary>رموز L3 (C.*) تُبنى من Color خام مُشتق برمجياً من فرش L2 — أي مفتاح L2 لا يُشتق يُحلّ لشفاف
    /// صامت بلا خطأ بناء (هيدر الجدول ظهر أبيض على أبيض بهذا السبب).</summary>
    [Collection("WpfApplication")]
    public class DesignTokenResolutionTests : System.IDisposable
    {
        private readonly TestDatabaseFixture _db = new();
        public void Dispose() => _db.Dispose();

        /// <summary>كل حزمة هوية مسجَّلة تُفحص — حزمة ناقصة مفتاحاً واحداً تُنتج لوناً شفافاً بلا خطأ بناء.</summary>
        [Fact]
        public void EveryIdentityPack_ResolvesAllComponentTokensToVisibleColors()
        {
            WpfApplicationFixture.Run(() =>
            {
                var identity = _db.Services.GetRequiredService<IIdentityService>();

                foreach (var pack in identity.Available())
                {
                    identity.Apply(pack.Key);

                    var transparent = CollectComponentBrushes()
                        .Where(kv => kv.Value.Color.A == 0)
                        .Select(kv => kv.Key)
                        .ToList();

                    Assert.True(transparent.Count == 0,
                        $"حزمة {pack.Key}: رموز C.* حُلَّت لشفاف (مفتاح L2 غير مُشتق): " + string.Join(", ", transparent));
                }
            });
        }

        [Fact]
        public void GridHeader_HasContrastAgainstRowsAndOwnForeground_InEveryPack()
        {
            WpfApplicationFixture.Run(() =>
            {
                var identity = _db.Services.GetRequiredService<IIdentityService>();

                foreach (var pack in identity.Available())
                {
                    identity.Apply(pack.Key);

                    var headerBg = Brush("C.Grid.Header.Bg");
                    var headerFg = Brush("C.Grid.Header.Fg");
                    var rowBg = Brush("C.Grid.Row.Bg");

                    Assert.True(headerBg.Color != headerFg.Color, $"حزمة {pack.Key}: نص الهيدر بلون خلفيته");
                    Assert.True(headerBg.Color != rowBg.Color, $"حزمة {pack.Key}: الهيدر بلون الصفوف");
                }
            });
        }

        /// <summary>الوضع الداكن كان يُضاف فوق L3 فتبقى رموز C.* على قيم الفاتح — أي "بلا أثر".</summary>
        [Fact]
        public void DarkMode_ActuallyChangesComponentTokens_AndLightRestoresThem()
        {
            WpfApplicationFixture.Run(() =>
            {
                var identity = _db.Services.GetRequiredService<IIdentityService>();
                identity.Apply("Signature");

                identity.ApplyMode(ThemeMode.Light);
                var lightCanvas = Brush("C.Grid.Row.Bg").Color;
                var lightNav = Brush("C.Nav.Surface").Color;
                var lightText = Brush("C.Grid.Cell.Fg").Color;

                identity.ApplyMode(ThemeMode.Dark);
                Assert.NotEqual(lightCanvas, Brush("C.Grid.Row.Bg").Color);
                Assert.NotEqual(lightNav, Brush("C.Nav.Surface").Color);
                Assert.NotEqual(lightText, Brush("C.Grid.Cell.Fg").Color);

                identity.ApplyMode(ThemeMode.Light);
                Assert.Equal(lightCanvas, Brush("C.Grid.Row.Bg").Color);
                Assert.Equal(lightNav, Brush("C.Nav.Surface").Color);
            });
        }

        [Fact]
        public void BothModes_KeepEveryComponentTokenVisible()
        {
            WpfApplicationFixture.Run(() =>
            {
                var identity = _db.Services.GetRequiredService<IIdentityService>();
                identity.Apply("Signature");

                foreach (var mode in new[] { ThemeMode.Light, ThemeMode.Dark })
                {
                    identity.ApplyMode(mode);

                    var transparent = CollectComponentBrushes()
                        .Where(kv => kv.Value.Color.A == 0)
                        .Select(kv => kv.Key)
                        .ToList();

                    Assert.True(transparent.Count == 0, $"{mode}: رموز شفافة: " + string.Join(", ", transparent));
                }

                identity.ApplyMode(ThemeMode.Light);
            });
        }

        private static SolidColorBrush Brush(string key) =>
            (SolidColorBrush)System.Windows.Application.Current.FindResource(key);

        private static Dictionary<string, SolidColorBrush> CollectComponentBrushes()
        {
            var found = new Dictionary<string, SolidColorBrush>();
            Walk(System.Windows.Application.Current.Resources, found);
            return found;
        }

        private static void Walk(ResourceDictionary dict, Dictionary<string, SolidColorBrush> into)
        {
            foreach (var merged in dict.MergedDictionaries)
                Walk(merged, into);

            foreach (var key in dict.Keys)
            {
                if (key is not string name || !name.StartsWith("C.")) continue;
                if (dict[key] is SolidColorBrush brush) into[name] = brush;
            }
        }
    }
}
