using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Effects;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Platform.Design;
using PrimeERP.UI.Services;
using Xunit;

namespace PrimeERP.Tests.Services.Design
{
    /// <summary>تحميل موارد التصميم</summary>
    [Collection("WpfApplication")]
    public class IdentityServiceTests : IDisposable
    {
        private readonly TestDatabaseFixture _db = new();
        private readonly IIdentityService _service;

        public IdentityServiceTests() => _service = _db.Services.GetRequiredService<IIdentityService>();

        public void Dispose() => _db.Dispose();

        [Fact]
        public void Initialize_BindsEveryDimensionOnARenderedElement()
        {
            WpfApplicationFixture.Run(() =>
            {
                _service.Initialize();

                var border = new Border();
                border.SetResourceReference(Border.BackgroundProperty, "BrandDefault");
                border.SetResourceReference(Border.PaddingProperty, "P.Space.4.Uniform");
                border.SetResourceReference(Border.CornerRadiusProperty, "P.Radius.Md");
                border.SetResourceReference(Border.EffectProperty, "ShadowMd");

                var textStyle = new Style(typeof(TextBlock));
                textStyle.Setters.Add(new Setter(TextBlock.FontFamilyProperty, new DynamicResourceExtension("P.Font.Family.Primary")));
                textStyle.Setters.Add(new Setter(TextBlock.FontSizeProperty, new DynamicResourceExtension("P.Font.Size.500")));

                var text = new TextBlock { Style = textStyle };
                border.Child = text;

                var window = new Window { Content = border, Width = 80, Height = 80, ShowInTaskbar = false, WindowStyle = WindowStyle.None, ShowActivated = false };
                try
                {
                    window.Show();

                    Assert.Equal(Hex("#0F2442"), ((SolidColorBrush)border.Background).Color);
                    Assert.True(border.Padding.Left > 0);
                    Assert.True(border.CornerRadius.TopLeft > 0);
                    Assert.True(((DropShadowEffect)border.Effect).BlurRadius > 0);
                    Assert.False(string.IsNullOrWhiteSpace(text.FontFamily.Source));
                    Assert.True(text.FontSize > 0);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        private static string DesignRoot()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, "5.Design")))
                dir = dir.Parent;
            return Path.Combine(dir!.FullName, "5.Design");
        }


        [Fact]
        public void StyleLayer_ContainsNoLiteralHexColors()
        {
            var files = Directory.GetFiles(Path.Combine(DesignRoot(), "Styles"), "*.xaml");

            var hexPattern = new Regex(@"#[0-9A-Fa-f]{6,8}\b");
            var offenders = new List<string>();

            foreach (var file in files)
            {
                foreach (var line in File.ReadLines(file))
                {
                    if (line.TrimStart().StartsWith("<!--")) continue; // تعليقات توثيقية قد تذكر Hex كمرجع تاريخي
                    if (hexPattern.IsMatch(line))
                        offenders.Add($"{Path.GetFileName(file)}: {line.Trim()}");
                }
            }

            Assert.True(offenders.Count == 0, "قيم Hex حرفية موجودة خارج L1:\n" + string.Join("\n", offenders));
        }

        private static Color Hex(string hex) => (Color)ColorConverter.ConvertFromString(hex);
    }
}
