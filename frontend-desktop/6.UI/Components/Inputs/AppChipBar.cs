using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace PrimeERP.UI.Components.Inputs
{
    /// <summary>شريحة اختيار: قيمتها ونصّها</summary>
    public sealed record Chip(object Value, string Label);

    /// <summary>
    /// شرائح اختيارٍ واحد تُمرَّر أفقياً — نظير شرائح التصنيفات في نقطة بيع الويب.
    /// المختارة ممتلئة بلون الهوية، والبقية سطحٌ غائر يتلوّن عند المرور.
    /// </summary>
    public class AppChipBar : UserControl
    {
        public static readonly DependencyProperty ItemsProperty =
            DependencyProperty.Register(nameof(Items), typeof(IEnumerable<Chip>), typeof(AppChipBar), new PropertyMetadata(null, (d, _) => ((AppChipBar)d).Rebuild()));

        public static readonly DependencyProperty SelectedValueProperty =
            DependencyProperty.Register(nameof(SelectedValue), typeof(object), typeof(AppChipBar), new PropertyMetadata(null, (d, _) => ((AppChipBar)d).Paint()));

        public IEnumerable<Chip> Items { get => (IEnumerable<Chip>)GetValue(ItemsProperty); set => SetValue(ItemsProperty, value); }
        public object SelectedValue { get => GetValue(SelectedValueProperty); set => SetValue(SelectedValueProperty, value); }

        /// <summary>اختيرت شريحة</summary>
        public event EventHandler SelectionChanged;

        private readonly StackPanel _row = new() { Orientation = Orientation.Horizontal };
        private readonly List<(Chip Chip, Border Box, TextBlock Text)> _chips = new();

        public AppChipBar()
        {
            Content = new ScrollViewer { Content = _row, HorizontalScrollBarVisibility = ScrollBarVisibility.Auto, VerticalScrollBarVisibility = ScrollBarVisibility.Disabled };
        }

        private void Rebuild()
        {
            _row.Children.Clear();
            _chips.Clear();
            foreach (var chip in Items ?? Enumerable.Empty<Chip>())
            {
                var text = new TextBlock { Text = chip.Label, VerticalAlignment = VerticalAlignment.Center };
                text.SetResourceReference(TextBlock.FontSizeProperty, "P.Font.Size.100");
                var box = new Border { Child = text, Height = 28, Padding = new Thickness(12, 0, 12, 0), Margin = new Thickness(0, 0, 6, 0), Cursor = Cursors.Hand };
                box.SetResourceReference(Border.CornerRadiusProperty, "P.Radius.Full");
                box.MouseEnter += (_, __) => { if (!Equals(chip.Value, SelectedValue)) box.SetResourceReference(Border.BackgroundProperty, "BrandSoft"); };
                box.MouseLeave += (_, __) => Paint();
                box.MouseLeftButtonUp += (_, __) =>
                {
                    SelectedValue = chip.Value;
                    SelectionChanged?.Invoke(this, EventArgs.Empty);
                };
                _chips.Add((chip, box, text));
                _row.Children.Add(box);
            }
            Paint();
        }

        private void Paint()
        {
            foreach (var (chip, box, text) in _chips)
            {
                var on = Equals(chip.Value, SelectedValue);
                box.SetResourceReference(Border.BackgroundProperty, on ? "BrandDefault" : "SurfaceSunken");
                text.SetResourceReference(TextBlock.ForegroundProperty, on ? "TextOnBrand" : "TextSecondary");
                text.SetResourceReference(TextBlock.FontWeightProperty, on ? "P.Font.Weight.Medium" : "P.Font.Weight.Normal");
            }
        }
    }
}
