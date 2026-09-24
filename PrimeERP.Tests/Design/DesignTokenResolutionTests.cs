using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Platform.Design;
using Xunit;

namespace PrimeERP.Tests.Design
{
    /// <summary>رموز التصميم تُحلّ لألوان مرئية</summary>
    [Collection("WpfApplication")]
    public class DesignTokenResolutionTests : System.IDisposable
    {
        private readonly TestDatabaseFixture _db = new();
        public void Dispose() => _db.Dispose();

        [Fact]
        public void EveryToken_ResolvesToAVisibleColor()
        {
            WpfApplicationFixture.Run(() =>
            {
                var identity = _db.Services.GetRequiredService<IIdentityService>();

                identity.Initialize();
                {

                    var transparent = CollectComponentBrushes()
                        .Where(kv => kv.Value.Color.A == 0)
                        .Select(kv => kv.Key)
                        .ToList();

                    Assert.True(transparent.Count == 0,
                        "رموز حُلَّت لشفاف: " + string.Join(", ", transparent));
                }
            });
        }

        [Fact]
        public void GridHeader_HasContrastAgainstRowsAndOwnForeground()
        {
            WpfApplicationFixture.Run(() =>
            {
                var identity = _db.Services.GetRequiredService<IIdentityService>();

                identity.Initialize();
                {

                    var headerBg = Brush("GridHeaderBg");
                    var headerFg = Brush("GridHeaderFg");
                    var rowBg = Brush("SurfaceRaised");

                    Assert.True(headerBg.Color != headerFg.Color, "نص الهيدر بلون خلفيته");
                    Assert.True(headerBg.Color != rowBg.Color, "الهيدر بلون الصفوف");
                }
            });
        }

        [Fact]
        public void EveryComponentToken_IsVisible()
        {
            WpfApplicationFixture.Run(() =>
            {
                var identity = _db.Services.GetRequiredService<IIdentityService>();
                identity.Initialize();

                var transparent = CollectComponentBrushes()
                    .Where(kv => kv.Value.Color.A == 0)
                    .Select(kv => kv.Key)
                    .ToList();

                Assert.True(transparent.Count == 0, "رموز شفافة: " + string.Join(", ", transparent));
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
