using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Platform.Design;
using Xunit;

namespace PrimeERP.Tests.Design
{
    /// <summary>كل سطح لا بدّ أن يتباين مع نصّه في الوضعين. الكسر الذي دفع لكتابته: توكن سطح بقي بقيمة
    /// الوضع الفاتح بعد اعتماد الوضع الداكن الكامل، فظهرت صفوف نصّها بلون خلفيتها — غير مقروءة تماماً.</summary>
    [Collection("WpfApplication")]
    public class ThemeContrastTests : IDisposable
    {
        private readonly TestDatabaseFixture _db = new();

        public void Dispose() => _db.Dispose();

        // (خلفية، نص) — أزواج يجب أن تُقرأ معاً على الشاشة.
        private static readonly (string Background, string Foreground)[] Pairs =
        {
            ("C.Grid.Row.Bg",     "C.Grid.Cell.Fg"),
            ("C.Grid.Row.AltBg",  "C.Grid.Cell.Fg"),
            ("C.Grid.Header.Bg",  "C.Grid.Header.Fg"),
            ("SurfaceDefault",    "TextPrimary"),
            ("SurfaceRaised",     "TextPrimary"),
            ("SurfaceOverlay",    "TextPrimary"),
            ("C.Input.Bg",        "C.Input.Fg"),
            ("ToolbarBg",         "TextPrimary"),
            ("NavSurface",        "NavText"),
            ("TopBarSurface",     "TopBarText"),
            ("TableRowSelected",  "TableRowSelectedText"),
            ("SurfaceCanvas",     "TextPrimary"),
            ("SurfaceBackground", "TextPrimary"),
            ("PageBg",           "C.Pagination.Info.Fg"),
            ("PageBg",           "TextPrimary"),
            ("SurfaceSunken",     "TextSecondary"),
        };

        [Fact]
        public void EveryPairedSurfaceAndText_StayReadableInBothModes()
        {
            WpfApplicationFixture.Run(() =>
            {
                var identity = _db.Services.GetRequiredService<IIdentityService>();
                var failures = new List<string>();

                foreach (var pack in identity.Available())
                {
                    identity.Apply(pack.Key);

                    foreach (var mode in new[] { ThemeMode.Light, ThemeMode.Dark })
                    {
                        identity.ApplyMode(mode);

                        foreach (var (background, foreground) in Pairs)
                        {
                            var contrast = Contrast(Colour(background), Colour(foreground));
                            if (contrast < 3.0)
                                failures.Add($"{pack.Key}/{mode}: {foreground} على {background} تباين {contrast:N2}");
                        }
                    }
                }

                Assert.True(failures.Count == 0, string.Join(Environment.NewLine, failures));
            });
        }

        private static Color Colour(string key) =>
            ((SolidColorBrush)System.Windows.Application.Current.FindResource(key)).Color;

        // نسبة التباين القياسية (WCAG) — 3.0 هو الحدّ الأدنى للنص الكبير وعناصر الواجهة.
        private static double Contrast(Color a, Color b)
        {
            double L(Color c)
            {
                double Channel(byte v)
                {
                    var s = v / 255.0;
                    return s <= 0.03928 ? s / 12.92 : Math.Pow((s + 0.055) / 1.055, 2.4);
                }
                return 0.2126 * Channel(c.R) + 0.7152 * Channel(c.G) + 0.0722 * Channel(c.B);
            }

            var (high, low) = (Math.Max(L(a), L(b)), Math.Min(L(a), L(b)));
            return (high + 0.05) / (low + 0.05);
        }
    }
}
