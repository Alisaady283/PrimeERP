using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Composition.Definitions;
using PrimeERP.Platform.Localization;
using PrimeERP.UI.Components.Display;
using PrimeERP.UI.Components.Layout;
using PrimeERP.UI.Components.Tree;

namespace PrimeERP.Composition.Renderers
{
    /// <summary>
    /// يبني صفحة شجرة (Tree) أو شجرة+تفاصيل (TreeSplit) كاملة من ModuleDefinition واحدة — نفس فلسفة
    /// CrudPageRenderer (تجميع قطع جاهزة: PageHeader/FilterBar/AppTreeView/AppCard، صفر XAML جديد). الفرق
    /// الجوهري: الشجرة تُحمَّل كاملة مرة واحدة (لا ترقيم من طرف الخادم — لا معنى لصفحة واحدة من هرم)، والبحث
    /// فلترة إخفاء حقيقية جانب العميل عبر AppTreeView.SearchText/TreeFilterEngine، لا استعلام خادم لكل حرف.
    /// TreeBuilder يحوّل Items المسطَّحة لشجرة عبر Reflection على ModuleDefinition.TreeOptions — يُستدعى من
    /// هنا بعد LoadAsync مباشرة، لا من داخل TreeViewModelBase (6.UI): TreeLayoutOptions الفعلية معروفة فقط
    /// عند التسجيل هنا في ModuleDefinition، والوارث (AccountsViewModel) يبقى بلا أي منطق شجرة على الإطلاق.
    /// </summary>
    public static class TreeRenderer
    {
        public static FrameworkElement Render(ModuleDefinition definition, IServiceProvider services)
        {
            if (definition.TreeOptions == null)
                throw new InvalidOperationException($"الوحدة '{definition.Key}' بتخطيط {definition.LayoutKind} بلا TreeOptions.");

            dynamic vm = services.GetRequiredService(definition.ViewModelType);
            var options = definition.TreeOptions;

            var header = new PageHeader();
            var btnExpand = new Button { Content = LocalizationService.Get("Str.ExpandAll") };
            BindingOperations.SetBinding(btnExpand, ButtonBase.CommandProperty, new Binding("ExpandAllCommand"));
            var btnCollapse = new Button { Content = LocalizationService.Get("Str.CollapseAll"), Margin = new Thickness(8, 0, 0, 0) };
            BindingOperations.SetBinding(btnCollapse, ButtonBase.CommandProperty, new Binding("CollapseAllCommand"));
            header.ActionsContent = new StackPanel { Orientation = Orientation.Horizontal, Children = { btnExpand, btnCollapse } };

            var filterBar = new FilterBar { SearchPlaceholder = LocalizationService.Get("Str.Search") };
            filterBar.Search += (_, text) => vm.SearchText = text;

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

            root.Loaded += async (_, __) =>
            {
                await (Task)vm.LoadAsync();

                IEnumerable items = vm.Items;
                var built = TreeBuilder.Build(items, options);

                ObservableCollection<TreeNodeViewModel> rootNodes = vm.RootNodes;
                rootNodes.Clear();
                foreach (var node in built)
                    rootNodes.Add(node);
            };

            return root;
        }

        private static AppCard BuildDetailsPanel(System.Collections.Generic.List<GridColumn> columns)
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
