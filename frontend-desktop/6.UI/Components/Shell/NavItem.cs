using System.Collections.Generic;

namespace PrimeERP.UI.Components.Shell
{
    /// <summary>عنصر تنقّل واحد في AppSidebar</summary>
    public class NavItem
    {
        public string Key           { get; set; }
        public string Text          { get; set; }
        public string IconKey       { get; set; }
        public string PermissionKey { get; set; }

        public string Badge { get; set; }

        public bool IsSeparator { get; set; }

        public List<NavItem> Children { get; set; } = new();
    }
}
