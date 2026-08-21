using System.Collections.Generic;

namespace PrimeERP.Views.Controls.Shell
{
    /// <summary>عنصر تنقّل واحد في AppSidebar — يبنيه المستهلك (Gallery أو AppShell لاحقاً)، لا قاعدة بيانات هنا.</summary>
    public class NavItem
    {
        public string Key           { get; set; }
        public string Text          { get; set; }
        public string IconKey       { get; set; }
        public string PermissionKey { get; set; }

        /// <summary>نص شارة عددية (مثل عدد الإشعارات لهذا القسم) — فارغ = لا شارة.</summary>
        public string Badge { get; set; }

        public bool IsSeparator { get; set; }

        public List<NavItem> Children { get; set; } = new();
    }
}
