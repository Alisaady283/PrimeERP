using System.Collections.Generic;
using System.Linq;
using PrimeERP.Platform.Permissions;
using PrimeERP.Domain.Enums;
using PrimeERP.Domain.Entities;
using PrimeERP.UI.Components.Tree;

namespace PrimeERP.UI.Components.Pickers
{
    /// <summary>اختيار حساب من شجرة الحسابات</summary>
    public class AccountPicker : PickerBase<Account>
    {
        public bool         LeafOnly   { get; set; } = true;
        public AccountType? TypeFilter { get; set; }
        public List<int>    ExcludeIds { get; set; } = new();

        protected override async void OpenSelectionWindow()
        {
            if (DataSource == null) return;

            var all = (await DataSource.SearchAsync("", 5000)).ToList();
            var roots = BuildTree(all);

            var window = new PickerTreeWindow("اختيار حساب", roots, extraFilter: null, isSelectable: IsNodeSelectable);

            if (window.ShowDialog() == true && window.SelectedNode?.Data is Account selected)
                CommitSelection(selected);
        }

        private bool IsNodeSelectable(TreeNodeViewModel node)
        {
            if (node.Data is not Account account) return false;
            if (LeafOnly && !account.IsLeaf) return false;
            if (TypeFilter.HasValue && account.Type != (int)TypeFilter.Value) return false;
            if (ExcludeIds.Contains(account.Id)) return false;
            return true;
        }

        private static List<TreeNodeViewModel> BuildTree(List<Account> accounts)
        {
            var nodeByCode = accounts.ToDictionary(a => a.Code, a => new TreeNodeViewModel
            {
                Id          = a.Id.ToString(),
                Code        = a.Code,
                Name        = a.Name,
                DisplayText = $"{a.Code} - {a.Name} ({a.Balance:N2})",
                IsLeaf      = a.IsLeaf,
                Data        = a
            });

            var roots = new List<TreeNodeViewModel>();

            foreach (var account in accounts)
            {
                var node = nodeByCode[account.Code];

                if (!string.IsNullOrEmpty(account.ParentCode) && nodeByCode.TryGetValue(account.ParentCode, out var parent))
                    parent.AddChild(node);
                else
                    roots.Add(node);
            }

            return roots;
        }
    }
}
