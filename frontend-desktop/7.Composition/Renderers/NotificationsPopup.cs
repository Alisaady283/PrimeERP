using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Application.PageServices.Admin;
using PrimeERP.Platform.Localization;

namespace PrimeERP.Composition.Renderers
{
    /// <summary>
    /// قائمة التنبيهات تحت الجرس — كقائمة الويب: سطح البطاقة وحدّها وظلّ المنبثق، والأحدث أولاً،
    /// وغير المقروء بنقطة، والنقر يفتح صفحة التنبيه ويعلّمه مقروءاً.
    /// </summary>
    public static class NotificationsPopup
    {
        public static void Show(FrameworkElement anchor, IServiceProvider services, Action<string> navigate, Action changed)
        {
            var notifications = services.GetRequiredService<INotificationService>();
            var popup = new Popup { PlacementTarget = anchor, Placement = PlacementMode.Bottom, StaysOpen = false, AllowsTransparency = true };

            var list = new StackPanel();
            var card = new Border { Width = 360, BorderThickness = new Thickness(1), Margin = new Thickness(0, 6, 0, 0) };
            card.SetResourceReference(Border.BackgroundProperty, "SurfaceDefault");
            card.SetResourceReference(Border.BorderBrushProperty, "OutlineDefault");
            card.SetResourceReference(Border.CornerRadiusProperty, "P.Radius.Lg");
            card.SetResourceReference(UIElement.EffectProperty, "ShadowMd");

            var title = Text(LocalizationService.Get("Str.Notifications.Title"), "TextPrimary", bold: true);
            var markAll = Text(LocalizationService.Get("Str.Notifications.MarkAll"), "BrandDefault");
            markAll.Cursor = Cursors.Hand;
            var header = new DockPanel { Margin = new Thickness(14, 12, 14, 8) };
            DockPanel.SetDock(markAll, Dock.Right);
            header.Children.Add(markAll);
            header.Children.Add(title);

            card.Child = new StackPanel
            {
                Children = { header, new ScrollViewer { MaxHeight = 420, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Content = list } }
            };
            popup.Child = card;

            void Fill()
            {
                list.Children.Clear();
                var items = notifications.Mine().Value ?? new();
                if (items.Count == 0) list.Children.Add(new Border { Padding = new Thickness(14, 18, 14, 18), Child = Text(LocalizationService.Get("Str.Notifications.Empty"), "TextMuted") });

                foreach (var item in items)
                {
                    var dot = new Border { Width = 8, Height = 8, CornerRadius = new CornerRadius(4), Margin = new Thickness(0, 6, 10, 0), VerticalAlignment = VerticalAlignment.Top };
                    dot.SetResourceReference(Border.BackgroundProperty, item.IsRead ? "OutlineSubtle" : item.Severity == "danger" ? "Danger" : item.Severity == "warning" ? "Warning" : "BrandDefault");

                    var text = new StackPanel
                    {
                        Children =
                        {
                            Text(item.Title, "TextPrimary", bold: !item.IsRead),
                            Text(item.Body, "TextSecondary"),
                            Text(item.CreatedAt.ToString("yyyy-MM-dd HH:mm"), "TextMuted", small: true)
                        }
                    };
                    var row = new DockPanel { Margin = new Thickness(14, 8, 14, 8) };
                    DockPanel.SetDock(dot, Dock.Left);
                    row.Children.Add(dot);
                    row.Children.Add(text);

                    var hit = new Border { Child = row, Background = Brushes.Transparent, Cursor = Cursors.Hand };
                    hit.MouseEnter += (_, __) => hit.SetResourceReference(Border.BackgroundProperty, "TableRowHover");
                    hit.MouseLeave += (_, __) => hit.Background = Brushes.Transparent;
                    hit.MouseLeftButtonUp += (_, __) =>
                    {
                        notifications.MarkRead(new[] { item.Id });
                        popup.IsOpen = false;
                        changed();
                        if (!string.IsNullOrEmpty(item.PageKey)) navigate(item.PageKey);
                    };
                    list.Children.Add(hit);
                }
            }

            markAll.MouseLeftButtonUp += (_, __) =>
            {
                notifications.MarkRead(notifications.Mine().Value?.Where(n => !n.IsRead).Select(n => n.Id) ?? Enumerable.Empty<int>());
                Fill();
                changed();
            };

            Fill();
            popup.IsOpen = true;
        }

        private static TextBlock Text(string text, string brush, bool bold = false, bool small = false)
        {
            var block = new TextBlock { Text = text, TextWrapping = TextWrapping.Wrap, FontWeight = bold ? FontWeights.SemiBold : FontWeights.Normal };
            block.SetResourceReference(TextBlock.ForegroundProperty, brush);
            block.SetResourceReference(TextBlock.FontFamilyProperty, "P.Font.Family.Primary");
            block.SetResourceReference(TextBlock.FontSizeProperty, small ? "P.Font.Size.100" : "P.Font.Size.200");
            return block;
        }
    }
}
