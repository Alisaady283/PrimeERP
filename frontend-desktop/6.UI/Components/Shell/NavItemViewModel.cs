using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using PrimeERP.Platform.Permissions;
using PrimeERP.UI.ViewModels;

namespace PrimeERP.UI.Components.Shell
{
    /// <summary>حالة عنصر التنقّل</summary>
    public class NavItemViewModel : BaseViewModel
    {
        private readonly IPermissionService _permissions;

        public NavItem Model { get; }

        public string Key           => Model.Key;
        public string Text          => Model.Text;
        public string IconKey       => Model.IconKey;
        public string Badge         => Model.Badge;
        public bool   IsSeparator   => Model.IsSeparator;
        public bool   HasChildren   => Children.Count > 0;

        public ObservableCollection<NavItemViewModel> Children { get; } = new();

        private bool _isVisible = true;
        public bool IsVisible { get => _isVisible; set => SetProperty(ref _isVisible, value); }

        private bool _isExpanded;
        public bool IsExpanded { get => _isExpanded; set => SetProperty(ref _isExpanded, value); }

        private bool _isActive;
        public bool IsActive { get => _isActive; set => SetProperty(ref _isActive, value); }

        public NavItemViewModel(NavItem model, IPermissionService permissions)
        {
            Model = model;
            _permissions = permissions;
            foreach (var child in model.Children ?? Enumerable.Empty<NavItem>())
                Children.Add(new NavItemViewModel(child, permissions));
        }

        public NavItemViewModel FindByKey(string key)
        {
            if (Key == key) return this;
            foreach (var child in Children)
            {
                var found = child.FindByKey(key);
                if (found != null) return found;
            }
            return null;
        }

        public bool RecomputeVisibility()
        {
            var anyChildVisible = false;
            foreach (var child in Children)
                anyChildVisible |= child.RecomputeVisibility();

            var selfAllowed = string.IsNullOrEmpty(Model.PermissionKey) || _permissions.Can(Model.PermissionKey);

            IsVisible = HasChildren ? selfAllowed && anyChildVisible : selfAllowed;
            return IsVisible;
        }

        public IEnumerable<NavItemViewModel> Flatten()
        {
            yield return this;
            foreach (var child in Children)
                foreach (var descendant in child.Flatten())
                    yield return descendant;
        }
    }
}
