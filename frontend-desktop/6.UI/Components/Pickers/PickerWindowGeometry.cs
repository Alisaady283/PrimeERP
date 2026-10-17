using System.Collections.Generic;
using System.Windows;

namespace PrimeERP.UI.Components.Pickers
{
    /// <summary>يتذكر آخر حجم/موضع لكل نافذة</summary>
    internal static class PickerWindowGeometry
    {
        private static readonly Dictionary<string, Rect> _cache = new();

        public static void Save(string key, Window window) =>
            _cache[key] = new Rect(window.Left, window.Top, window.Width, window.Height);

        public static bool TryRestore(string key, Window window)
        {
            if (!_cache.TryGetValue(key, out var rect)) return false;

            window.WindowStartupLocation = WindowStartupLocation.Manual;
            window.Left   = rect.X;
            window.Top    = rect.Y;
            window.Width  = rect.Width;
            window.Height = rect.Height;
            return true;
        }
    }
}
