using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
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
            ("SurfaceHeader",     "TextPrimary"),
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

            // نصّ كل زرّ على خلفيته — الملوّن يقرأ TextOnBrand والعادي TextPrimary.
            ("C.Button.Primary.Bg",   "C.Button.Primary.Fg"),
            ("C.Button.Secondary.Bg", "C.Button.Secondary.Fg"),
            ("C.Button.Success.Bg",   "C.Button.Success.Fg"),
            ("C.Button.Warning.Bg",   "C.Button.Warning.Fg"),
            ("C.Button.Danger.Bg",    "C.Button.Danger.Fg"),
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

        /// <summary>
        /// النمط الضمني TargetType="Window" يطابق النوع بالضبط لا المشتقّ منه، فنافذةٌ مشتقّة بلا خلفية
        /// معلنة تقع على أبيض WPF الافتراضي في الوضعين — وهو ما جعل جسم الحوار أبيض تحت الثيم الداكن.
        /// </summary>
        [Fact]
        public void EveryWindow_DeclaresItsOwnBackground()
        {
            var windows = System.IO.Directory
                .GetFiles(RepositoryRoot(), "*.xaml", System.IO.SearchOption.AllDirectories)
                .Where(f => !f.Contains(@"\obj\") && !f.Contains(@"in\"))
                .Select(f => (Path: f, Text: System.IO.File.ReadAllText(f)))
                .Where(x => x.Text.TrimStart().StartsWith("<Window"))
                .ToList();

            Assert.NotEmpty(windows);

            // النافذة الشفافة لا تُلغي أبيض WPF بل تكشف ما خلفها، فأي سطح تدهنه داخلها يجب أن يأتي من
            // الثيم لا من لون حرفي. ونافذة شفافة بلا سطح إطلاقاً (مضيف التنبيهات مثلاً) سليمة كما هي.
            bool Declares(string text)
            {
                if (!text[..Math.Min(text.Length, 1200)].Contains("Background=")) return false;
                if (!text.Contains("AllowsTransparency=\"True\"")) return true;

                return Regex.Matches(text, "Background=\"([^\"]+)\"")
                    .Select(m => m.Groups[1].Value)
                    .Where(value => value != "Transparent")
                    .All(value => value.StartsWith("{DynamicResource "));
            }

            var missing = windows
                .Where(x => !Declares(x.Text))
                .Select(x => System.IO.Path.GetFileName(x.Path))
                .ToList();

            Assert.True(missing.Count == 0,
                "نوافذ بلا خلفية معلنة فتقع على أبيض WPF: " + string.Join("، ", missing));
        }

        /// <summary>
        /// النمط الضمني TargetType="TextBlock" يضبط الخطّ والحجم ولا يضبط اللون، فنصٌّ يُبنى بالكود بلا
        /// Foreground يقع على أسود WPF الافتراضي: يُقرأ في الوضع الفاتح ويختفي في الداكن. الكسر الذي دفع
        /// لكتابته: اسم الصنف في نافذة السحب — ومعه ثلاثة مثله في مُصيِّرات الشيكات والتقارير والإعدادات.
        /// </summary>
        [Fact]
        public void EveryCodeBuiltTextBlock_DeclaresItsForeground()
        {
            var files = System.IO.Directory
                .GetFiles(RepositoryRoot(), "*.cs", System.IO.SearchOption.AllDirectories)
                .Where(f => f.Contains(@"\6.UI\") || f.Contains(@"\7.Composition\"))
                .Where(f => !f.Contains(@"\obj\") && !f.Contains(@"\bin\"))
                // DevTools مستثناة من بناء Release ولا تُعرض لمستخدم — راجع ARCHITECTURE.md § الدين التقني.
                .Where(f => !f.Contains(@"\DevTools\"))
                .ToList();

            Assert.NotEmpty(files);

            var missing = new List<string>();

            foreach (var path in files)
            {
                var text = System.IO.File.ReadAllText(path);

                foreach (Match match in Regex.Matches(text, @"new TextBlock\b"))
                {
                    if (DeclaresForeground(text, match.Index)) continue;

                    var line = text[..match.Index].Count(c => c == '\n') + 1;
                    missing.Add($"{System.IO.Path.GetFileName(path)}:{line}");
                }
            }

            Assert.True(missing.Count == 0,
                "نصوص تُبنى بالكود بلا Foreground فتقع على أسود WPF وتختفي في الوضع الداكن: "
                + string.Join("، ", missing));
        }

        /// <summary>اللون معلنٌ إمّا داخل مُهيّئ الكائن، وإمّا بـ SetResourceReference على المتغيّر الذي
        /// استقبله — وهو الوجه الوحيد الذي يتبدّل مع الوضع حيّاً.</summary>
        private static bool DeclaresForeground(string text, int start)
        {
            var initializer = Initializer(text, start);
            if (initializer.Contains("Foreground")) return true;

            var name = Regex.Match(text[..start], @"(\w+)\s*=\s*$").Groups[1].Value;
            return name.Length > 0 &&
                   text.Contains($"{name}.SetResourceReference(TextBlock.ForegroundProperty");
        }

        /// <summary>نصّ مُهيّئ الكائن بين قوسيه المعقوفين، أو فراغٌ لو لم يكن للكائن مُهيّئ أصلاً.</summary>
        private static string Initializer(string text, int start)
        {
            var open = text.IndexOf('{', start);
            if (open < 0) return "";

            // مُهيّئ الكائن يلي الاسم مباشرة؛ فاصلةٌ منقوطة قبل القوس تعني أن الجملة انتهت بلا مُهيّئ.
            if (text[start..open].Contains(';')) return "";

            var depth = 0;
            for (var i = open; i < text.Length; i++)
            {
                if (text[i] == '{') depth++;
                else if (text[i] == '}' && --depth == 0) return text[open..(i + 1)];
            }

            return "";
        }

        private static string RepositoryRoot()
        {
            var directory = new System.IO.DirectoryInfo(AppContext.BaseDirectory);
            while (directory != null && !System.IO.File.Exists(System.IO.Path.Combine(directory.FullName, "PrimeERP.csproj")))
                directory = directory.Parent;

            return directory?.FullName ?? AppContext.BaseDirectory;
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
