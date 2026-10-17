using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace PrimeERP.UI.Services
{
    /// <summary>صورة PNG لعنصرٍ معروض كما يراه المستخدم، على خلفيةٍ بيضاء أو خلفية صفحته</summary>
    public static class VisualSnapshot
    {
        private const double Scale = 2;

        public static string Png(FrameworkElement element, Brush background = null) =>
            element == null || element.ActualWidth < 1 || element.ActualHeight < 1 ? null
                : Render(element, background, new Rect(0, 0, element.ActualWidth, element.ActualHeight));

        private static string Render(FrameworkElement element, Brush background, Rect bounds)
        {
            var visual = new DrawingVisual();
            using (var context = visual.RenderOpen())
            {
                context.DrawRectangle(background ?? Brushes.White, null, bounds);
                var brush = new VisualBrush(element) { Stretch = Stretch.None, AlignmentX = AlignmentX.Left, AlignmentY = AlignmentY.Top };
                if (element.FlowDirection == FlowDirection.RightToLeft && VisualTreeHelper.GetParent(element) != null) brush.Transform = new MatrixTransform(-1, 0, 0, 1, bounds.Width, 0);
                context.DrawRectangle(brush, null, bounds);
            }

            var bitmap = new RenderTargetBitmap((int)(bounds.Width * Scale), (int)(bounds.Height * Scale), 96 * Scale, 96 * Scale, PixelFormats.Pbgra32);
            bitmap.Render(visual);

            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using var stream = new MemoryStream();
            encoder.Save(stream);
            return Convert.ToBase64String(stream.ToArray());
        }
    }
}
