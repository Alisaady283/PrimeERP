using System;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Composition.Definitions;
using PrimeERP.Platform.Localization;
using PrimeERP.UI.Components.Layout;
using PrimeERP.UI.Services;
using Btn = PrimeERP.UI.Components.Actions.AppButton;

namespace PrimeERP.Composition.Renderers
{
    /// <summary>تصيير صفحة المستند</summary>
    public static class DocumentPageRenderer
    {
        public static FrameworkElement Render(ModuleDefinition definition, IServiceProvider services)
        {
            var def = definition.DocumentDialog
                ?? throw new InvalidOperationException($"الوحدة '{definition.Key}' بتخطيط DocumentPage بلا DocumentDialogDefinition.");

            var toast = services.GetRequiredService<IToastService>();
            var root = new Grid();
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var header = new PageHeader();
            var actions = new StackPanel { Orientation = Orientation.Horizontal };
            var status = new TextBlock { Margin = new Thickness(24, 10, 24, 14), VerticalAlignment = VerticalAlignment.Center };
            status.SetResourceReference(TextBlock.ForegroundProperty, "TextMuted");

            var scroll = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                Padding = new Thickness(24, 8, 24, 8)
            };

            DocumentRenderer.DocumentEditor editor = null;

            void Load(object editItem)
            {
                editor = DocumentRenderer.BuildEditor(def, services, toast, editItem);
                if (editor == null) return;

                scroll.Content = editor.Body;
                status.Text = editor.IsEdit
                    ? LocalizationService.Get(def.TitleEditKey)
                    : LocalizationService.Get(def.TitleKey);
            }

            var btnNew = new Btn { Text = LocalizationService.Get("Str.Add"), Variant = "secondary", Size = "sm", Margin = new Thickness(8, 0, 0, 0) };
            btnNew.Click += (_, __) => Load(null);

            var btnSave = new Btn { Text = LocalizationService.Get("Str.Save"), Variant = "primary", Size = "sm", Margin = new Thickness(8, 0, 0, 0) };
            btnSave.Click += (_, __) =>
            {
                if (editor == null) return;
                if (!DocumentRenderer.TrySaveEditor(def, services, toast, editor)) return;
                Load(null);
            };

            actions.Children.Add(btnNew);
            foreach (var pullButton in DocumentRenderer.BuildPullButtons(def, services, () => editor)) actions.Children.Add(pullButton);
            var btnPrint = new Btn { Text = LocalizationService.Get("Str.Print"), Variant = "secondary", Size = "sm", Margin = new Thickness(8, 0, 0, 0) };
            btnPrint.Click += (_, __) => DocumentPrinter.PrintSelected(definition, services, editor?.EditItem);

            actions.Children.Add(btnPrint);
            actions.Children.Add(btnSave);
            header.ActionsContent = actions;

            Grid.SetRow(header, 0);
            Grid.SetRow(scroll, 1);
            Grid.SetRow(status, 2);
            root.Children.Add(header);
            root.Children.Add(scroll);
            root.Children.Add(status);

            root.Loaded += (_, __) => { if (editor == null) Load(null); };

            return root;
        }
    }
}
