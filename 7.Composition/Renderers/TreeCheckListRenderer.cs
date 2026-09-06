using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Composition.Definitions;
using PrimeERP.Platform.Localization;
using PrimeERP.UI.Components.Display;
using PrimeERP.UI.Components.Inputs;
using PrimeERP.UI.Components.Layout;
using PrimeERP.UI.Components.Tree;
using PrimeERP.UI.Services;
using Btn = PrimeERP.UI.Components.Actions.AppButton;

namespace PrimeERP.Composition.Renderers
{
    // شجرة قابلة للتأشير مدفوعة بمصدر — تجميع PageHeader/FilterBar/AppTreeView/AppComboBox، صفر عنصر خام.
    public static class TreeCheckListRenderer
    {
        public static FrameworkElement Render(ModuleDefinition definition, IServiceProvider services)
        {
            var def = definition.TreeCheckList
                ?? throw new InvalidOperationException($"الوحدة '{definition.Key}' بتخطيط TreeCheckList بلا TreeCheckListDefinition.");

            var toast = services.GetRequiredService<IToastService>();
            var nodes = new List<TreeNodeViewModel>();
            var tree = new AppTreeView { CheckMode = def.Mode };

            var sourcePicker = new AppComboBox
            {
                Label = LocalizationService.Get(def.SourceLabelKey),
                DisplayMemberPath = "Display",
                SelectedValuePath = "Id",
                Width = 260
            };
            sourcePicker.ItemsSource = def.SourceItems(services);

            var summary = new TextBlock { Margin = new Thickness(0, 8, 0, 0) };
            summary.SetResourceReference(TextBlock.ForegroundProperty, "TextSecondary");

            void RefreshSummary()
            {
                var keyNodes = nodes.SelectMany(n => n.Children).ToList();
                var granted = keyNodes.Count(n => n.CheckState is NodeCheckState.Checked or NodeCheckState.Granted);
                summary.Text = $"الممنوح {granted} من {keyNodes.Count}";
            }

            int SelectedSourceId() => sourcePicker.SelectedValue is int id ? id : 0;

            void LoadTree()
            {
                var sourceId = SelectedSourceId();
                nodes.Clear();
                if (sourceId > 0) nodes.AddRange(def.BuildTree(services, sourceId));
                tree.ItemsSource = nodes;
                RefreshSummary();
            }

            sourcePicker.SelectionChanged += (_, __) => LoadTree();
            tree.CheckStateChanged += (_, node) =>
            {
                def.OnCheckChanged?.Invoke(node, nodes);
                RefreshSummary();
            };

            var header = new PageHeader();
            var actions = new StackPanel { Orientation = Orientation.Horizontal };

            foreach (var action in def.Actions)
            {
                var button = new Btn { Text = LocalizationService.Get(action.TextKey), Variant = action.Variant, Size = "sm", Margin = new Thickness(8, 0, 0, 0) };
                button.Click += (_, __) =>
                {
                    if (SelectedSourceId() == 0) return;
                    action.Run(services, SelectedSourceId(), nodes);
                    RefreshSummary();
                };
                actions.Children.Add(button);
            }

            var saveButton = new Btn { Text = LocalizationService.Get("Str.Save"), Variant = "primary", Size = "sm", Margin = new Thickness(8, 0, 0, 0) };
            saveButton.Click += (_, __) =>
            {
                if (SelectedSourceId() == 0) return;
                var result = def.Save(services, SelectedSourceId(), nodes);
                if (result.IsSuccess) toast.Success(LocalizationService.Get("Str.Success"));
                else toast.Error(result.ErrorMessage);
            };
            actions.Children.Add(saveButton);
            header.ActionsContent = actions;

            var filterBar = new FilterBar { SearchPlaceholder = LocalizationService.Get("Str.Search") };
            filterBar.Search += (_, text) => tree.SearchText = text;

            var top = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(24, 8, 24, 0) };
            top.Children.Add(sourcePicker);

            var root = new Grid();
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            Grid.SetRow(header, 0);
            Grid.SetRow(top, 1);
            Grid.SetRow(filterBar, 2);
            Grid.SetRow(tree, 3);
            Grid.SetRow(summary, 4);
            root.Children.Add(header);
            root.Children.Add(top);
            root.Children.Add(filterBar);
            root.Children.Add(tree);
            root.Children.Add(summary);

            root.Loaded += (_, __) =>
            {
                var first = (def.SourceItems(services)).FirstOrDefault();
                if (first != null)
                {
                    sourcePicker.SelectedItem = ((List<SourceOption>)sourcePicker.ItemsSource).FirstOrDefault(o => o.Id == first.Id);
                    LoadTree();
                }
            };

            return root;
        }
    }
}
