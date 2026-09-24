using System.Windows;
using System.Windows.Media;

namespace PrimeERP.UI.Components
{
    /// <summary>البحث في الشجرة المرئية</summary>
    public static class VisualTree
    {
        public static T FindChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T typed) return typed;

                var found = FindChild<T>(child);
                if (found != null) return found;
            }
            return null;
        }
    }
}
