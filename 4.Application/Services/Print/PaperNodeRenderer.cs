using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using PrimeERP.Domain.Contracts;

namespace PrimeERP.Application.Services.Print
{
    /// <summary>
    /// مترجم PaperNode إلى عناصر WPF — عام تماماً، لا يعرف عن الترويسة ولا عن أي قطعة بعينها.
    /// </summary>
    public static class PaperNodeRenderer
    {
        public static FrameworkElement ToElement(PaperNode node, Func<string, object> resource) => node switch
        {
            PaperText text => Text(text, resource),
            PaperImage image => Image(image),
            PaperStack stack => Stack(stack, resource),
            PaperRow row => Row(row, resource),
            PaperRule rule => Rule(rule, resource),
            _ => null
        };

        private static FrameworkElement Text(PaperText node, Func<string, object> resource) => new TextBlock
        {
            Text = node.Text,
            FlowDirection = FlowDirection.RightToLeft,
            HorizontalAlignment = HorizontalAlignment.Right,
            TextWrapping = TextWrapping.NoWrap,
            FontFamily = (FontFamily)resource("FontFamilyPrimary"),
            FontSize = (double)resource(node.Role == PaperTextRole.Name ? "FontSizeXl" : "FontSizeSm"),
            FontWeight = node.Role == PaperTextRole.Name ? (FontWeight)resource("FontWeightBold") : FontWeights.Normal,
            Foreground = (Brush)resource(node.Role == PaperTextRole.Name ? "BrandSolid" : "TextSecondary")
        };

        private static FrameworkElement Image(PaperImage node)
        {
            var source = ImageData.Decode(node.Data);
            if (source == null) return null;

            return new System.Windows.Controls.Image
            {
                Source = source,
                MaxWidth = node.MaxWidth,
                MaxHeight = node.MaxHeight,
                Stretch = Stretch.Uniform,
                VerticalAlignment = VerticalAlignment.Center
            };
        }

        private static FrameworkElement Stack(PaperStack node, Func<string, object> resource)
        {
            var panel = new StackPanel { FlowDirection = FlowDirection.LeftToRight };
            foreach (var child in node.Children)
                if (ToElement(child, resource) is { } element) panel.Children.Add(element);

            return panel;
        }

        /// <summary>
        /// التخطيط LeftToRight بمواضع مطلقة داخل مستند RTL: العمود النجمي يُوضع أخيراً فيقع بعد الانعكاس
        /// على حافة اليمين، وما بحجمه قبله فيلاصق حافة اليسار. الاعتماد على RTL هنا يعكس المحاذاة أيضاً
        /// فيلتصق الطرفان ككتلة واحدة — وهو الخطأ الذي كان يُصلَح في كل مُصيِّر على حدة.
        /// </summary>
        private static FrameworkElement Row(PaperRow node, Func<string, object> resource)
        {
            var grid = new Grid { FlowDirection = FlowDirection.LeftToRight };
            var elements = node.Children.Select(child => ToElement(child, resource)).Where(e => e != null).ToList();
            if (elements.Count == 0) return grid;

            for (var i = 1; i < elements.Count; i++)
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var stretch = elements[0];
            stretch.HorizontalAlignment = HorizontalAlignment.Right;
            stretch.VerticalAlignment = VerticalAlignment.Center;
            Grid.SetColumn(stretch, elements.Count - 1);
            grid.Children.Add(stretch);

            for (var i = 1; i < elements.Count; i++)
            {
                Grid.SetColumn(elements[i], i - 1);
                grid.Children.Add(elements[i]);
            }

            return grid;
        }

        private static FrameworkElement Rule(PaperRule node, Func<string, object> resource) => new Border
        {
            BorderBrush = (Brush)resource("BrandSolid"),
            BorderThickness = new Thickness(0, 0, 0, node.Thickness),
            Margin = new Thickness(0, node.GapAbove, 0, node.GapBelow)
        };
    }
}
