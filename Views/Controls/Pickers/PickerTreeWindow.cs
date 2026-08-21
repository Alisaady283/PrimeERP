using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using PrimeERP.Core.Common;
using PrimeERP.Services;
using Btn = PrimeERP.Views.Controls.Actions.AppButton;
using PrimeERP.Views.Controls.Display;
using PrimeERP.Views.Controls.Feedback;
using PrimeERP.Views.Controls.Inputs;
using PrimeERP.Views.Controls.Tree;

namespace PrimeERP.Views.Controls.Pickers
{
    /// <summary>
    /// نافذة اختيار بشجرة مشتركة (تُستخدم بواسطة AccountPicker حالياً) — تُبنى من C# فقط داخل AppDialogWindow.
    /// تعتمد على AppTreeView + TreeFilterEngine للفلترة الحقيقية، ولا تعرف شيئاً عن قاعدة البيانات.
    /// </summary>
    public class PickerTreeWindow : AppDialogWindow
    {
        public TreeNodeViewModel SelectedNode { get; private set; }

        private readonly AppTreeView _tree;
        private readonly string _geometryKey;

        public PickerTreeWindow(string title, IEnumerable<TreeNodeViewModel> roots,
                                Func<TreeNodeViewModel, bool> extraFilter = null,
                                Func<TreeNodeViewModel, bool> isSelectable = null)
        {
            Title = title;
            HeaderTitle = title;
            HeaderVariant = StatusVariant.Brand;
            HeaderIcon = (Geometry)FindResource("IconAccounts");

            ResizeMode = ResizeMode.CanResize;
            SizeToContent = SizeToContent.Manual;
            Width = (double)FindResource("DialogWidthSm");
            Height = 540;

            _geometryKey = "PickerTreeWindow:" + title;
            PickerWindowGeometry.TryRestore(_geometryKey, this);
            Closing += (s, e) => PickerWindowGeometry.Save(_geometryKey, this);

            var rootList = new List<TreeNodeViewModel>(roots ?? Array.Empty<TreeNodeViewModel>());
            if (isSelectable != null)
                ApplySelectable(rootList, isSelectable);

            var search = new AppSearchBox { Placeholder = LocalizationService.Get("Str.SearchPlaceholder"), Margin = new Thickness(0, 0, 0, 10) };

            _tree = new AppTreeView { Height = 360, ExtraFilter = extraFilter };
            _tree.ItemsSource = rootList;
            search.Search += (s, term) => _tree.SearchText = term;

            var btnExpand = new Btn { Text = "توسيع الكل", Variant = "ghost", Size = "sm", Margin = new Thickness(0, 0, 8, 0) };
            btnExpand.Click += (s, e) => _tree.ExpandAll();

            var btnCollapse = new Btn { Text = "طي الكل", Variant = "ghost", Size = "sm" };
            btnCollapse.Click += (s, e) => _tree.CollapseAll();

            var toolsRow = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 10),
                Children = { btnExpand, btnCollapse }
            };

            Body = new StackPanel { Children = { search, toolsRow, _tree } };

            var btnCancel = new Btn { Text = LocalizationService.Get("Str.Cancel"), Variant = "secondary", Size = "sm" };
            btnCancel.Click += (s, e) => { DialogResult = false; Close(); };
            Footer = btnCancel;
        }

        private static void ApplySelectable(IEnumerable<TreeNodeViewModel> nodes, Func<TreeNodeViewModel, bool> predicate)
        {
            foreach (var node in nodes)
            {
                node.IsSelectable = predicate(node);
                ApplySelectable(node.Children, predicate);
            }
        }

        protected override void OnEnterPressed()
        {
            if (_tree.SelectedItem is TreeNodeViewModel node && node.IsSelectable)
            {
                SelectedNode = node;
                DialogResult = true;
                Close();
            }
        }
    }
}
