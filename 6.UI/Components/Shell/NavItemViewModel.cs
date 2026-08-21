using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using PrimeERP.Platform.Permissions;
using PrimeERP.UI.ViewModels;

namespace PrimeERP.UI.Components.Shell
{
    /// <summary>
    /// غلاف حالة عرض حول NavItem (IsVisible/IsExpanded/IsActive) — يُبنى مرة عند ItemsSource ويُعاد حساب
    /// IsVisible منه فقط عند AppSession.PermissionsChanged، بلا إعادة بناء الشجرة كاملة.
    /// IPermissionService يُمرَّر بارامتر بناء من المستدعي الأول (AppSidebar.xaml.cs، code-behind يملك
    /// UIServices) — لا UIServices هنا مباشرة، لأن NavItemViewModel نفسها ViewModel لا code-behind
    /// (راجع القيد الملزم في ARCHITECTURE.md § UIServices: صفر استهلاك من Service/VM).
    /// </summary>
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

        /// <summary>يبحث عن هذا المفتاح في نفسه أو أي عنصر تحته (بحث عميق).</summary>
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

        /// <summary>
        /// يعيد حساب IsVisible تصاعدياً: عنصر بلا أبناء يظهر لو مسموح له فقط؛ عنصر له أبناء يظهر لو مسموح له
        /// (أو بلا PermissionKey) وله ابن ظاهر واحد على الأقل — فأب كل أبنائه محجوبون يختفي تلقائياً.
        /// </summary>
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
