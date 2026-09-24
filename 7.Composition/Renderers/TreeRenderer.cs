using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Composition.Definitions;
using PrimeERP.Platform.Localization;
using PrimeERP.UI.Components.Actions;
using PrimeERP.UI.Components.Display;
using PrimeERP.UI.Components.Layout;
using PrimeERP.UI.Components.Tree;
using PrimeERP.UI.Services;

namespace PrimeERP.Composition.Renderers
{
    /// <summary>تصيير الشجرة</summary>
    public static class TreeRenderer
    {
        public static FrameworkElement Render(ModuleDefinition definition, IServiceProvider services)
        {
            if (definition.TreeOptions == null)
                throw new InvalidOperationException($"الوحدة '{definition.Key}' بتخطيط {definition.LayoutKind} بلا TreeOptions.");

            dynamic vm = Resolve.ViewModel(definition, services);
            var options = definition.TreeOptions;

            var header = new PageHeader();

            var actions = new List<ToolbarAction>
            {
                ToolbarAction.New((ICommand)vm.AddCommand, $"{definition.PermissionPrefix}.Create"),
                ToolbarAction.Edit((ICommand)vm.EditCommand, $"{definition.PermissionPrefix}.Edit"),
                ToolbarAction.Delete((ICommand)vm.DeleteCommand, $"{definition.PermissionPrefix}.Delete"),
                ToolbarAction.Refresh((ICommand)vm.RefreshCommand),
                ToolbarAction.SeparatorItem(),
                ToolbarAction.ExpandAll((ICommand)vm.ExpandAllCommand),
                ToolbarAction.CollapseAll((ICommand)vm.CollapseAllCommand),
            };
            header.ActionsContent = new ActionToolbar { ButtonsSource = ToolbarActions.Enabled(definition, actions) };

            var filterBar = new FilterBar { SearchPlaceholder = LocalizationService.Get("Str.Search") };
            filterBar.Search += (_, text) => vm.SearchText = text;

            if (definition.Filters is { Count: > 0 })
                filterBar.FiltersContent = FilterControls.Build(definition.Filters, vm, services);

            var tree = new AppTreeView();
            BindingOperations.SetBinding(tree, AppTreeView.ItemsSourceProperty, new Binding("RootNodes"));
            BindingOperations.SetBinding(tree, AppTreeView.SelectedItemProperty, new Binding("SelectedNode") { Mode = BindingMode.TwoWay });
            BindingOperations.SetBinding(tree, AppTreeView.SearchTextProperty, new Binding("SearchText"));

            var root = new Grid { DataContext = vm };
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            Grid.SetRow(header, 0);
            Grid.SetRow(filterBar, 1);
            root.Children.Add(header);
            root.Children.Add(filterBar);

            if (definition.LayoutKind == LayoutKind.TreeSplit)
            {
                var content = new Grid();
                content.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                content.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(320) });

                var details = BuildDetailsPanel(definition.Columns);
                details.Margin = new Thickness(8, 0, 0, 0);

                Grid.SetColumn(tree, 0);
                Grid.SetColumn(details, 1);
                content.Children.Add(tree);
                content.Children.Add(details);

                Grid.SetRow(content, 2);
                root.Children.Add(content);
            }
            else
            {
                Grid.SetRow(tree, 2);
                root.Children.Add(tree);
            }

            void RebuildTree()
            {
                IEnumerable items = vm.Items;
                var built = TreeBuilder.Build(items, options);
                ObservableCollection<TreeNodeViewModel> rootNodes = vm.RootNodes;
                rootNodes.Clear();
                foreach (var node in built) rootNodes.Add(node);
            }

            ((INotifyCollectionChanged)vm.Items).CollectionChanged += (_, __) => RebuildTree();

            if (definition.Dialog != null)
            {
                var toast = services.GetRequiredService<IToastService>();
                vm.AddRequested += (Action)(() =>
                {
                    int? defaultParent = vm.SelectedNode?.Data?.GetType().GetProperty("Id")?.GetValue(vm.SelectedNode.Data) as int?;
                    if (DialogRenderer.ShowAndSave(definition.Dialog, services, toast, addModeDefaultPickerId: defaultParent))
                        vm.LoadCommand.Execute(null);
                });
                vm.EditRequested += (Action<object>)(item =>
                {
                    if (DialogRenderer.ShowAndSave(definition.Dialog, services, toast, item))
                        vm.LoadCommand.Execute(null);
                });
            }

            root.Loaded += async (_, __) =>
            {
                await (Task)vm.LoadAsync();
                RebuildTree();
            };

            return root;
        }

        private static AppCard BuildDetailsPanel(List<GridColumn> columns)
        {
            var rows = new StackPanel();

            foreach (var col in columns)
            {
                var row = new Grid { Margin = new Thickness(0, 0, 0, 8) };
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                var label = new TextBlock { Text = col.Header, Margin = new Thickness(0, 0, 8, 0) };
                label.SetResourceReference(TextBlock.ForegroundProperty, "TextSecondary");

                var value = new TextBlock { TextWrapping = TextWrapping.Wrap };
                value.SetResourceReference(TextBlock.ForegroundProperty, "TextPrimary");
                var binding = new Binding($"SelectedNode.Data.{col.Binding}");
                if (!string.IsNullOrEmpty(col.Format)) binding.StringFormat = col.Format;
                BindingOperations.SetBinding(value, TextBlock.TextProperty, binding);

                Grid.SetColumn(label, 0);
                Grid.SetColumn(value, 1);
                row.Children.Add(label);
                row.Children.Add(value);
                rows.Children.Add(row);
            }

            return new AppCard { Title = LocalizationService.Get("Str.Details"), Body = rows };
        }
    }
}
