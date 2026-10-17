using System;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace PrimeERP.UI.Services
{
    /// <summary>تحويل صورة ↔ Base64</summary>
    public static class ImageData
    {
        /// <summary>الصورة بمقاسها وصيغتها</summary>
        public static string Encode(string path, int maxDimension = 512, bool jpeg = false)
        {
            var source = new BitmapImage();
            source.BeginInit();
            source.UriSource = new Uri(path);
            source.CacheOption = BitmapCacheOption.OnLoad;
            source.EndInit();

            var scale = Math.Min(1.0, maxDimension / (double)Math.Max(source.PixelWidth, source.PixelHeight));
            BitmapSource scaled = scale < 1.0 ? new TransformedBitmap(source, new ScaleTransform(scale, scale)) : source;

            BitmapEncoder encoder = jpeg ? new JpegBitmapEncoder { QualityLevel = 85 } : new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(jpeg ? Flatten(scaled) : scaled));

            using var stream = new MemoryStream();
            encoder.Save(stream);
            return Convert.ToBase64String(stream.ToArray());
        }

        /// <summary>الشفاف على أبيض</summary>
        private static BitmapSource Flatten(BitmapSource image)
        {
            var visual = new DrawingVisual();
            using (var context = visual.RenderOpen())
            {
                var area = new System.Windows.Rect(0, 0, image.PixelWidth, image.PixelHeight);
                context.DrawRectangle(Brushes.White, null, area);
                context.DrawImage(image, area);
            }
            var flat = new RenderTargetBitmap(image.PixelWidth, image.PixelHeight, 96, 96, PixelFormats.Pbgra32);
            flat.Render(visual);
            return flat;
        }

        public static BitmapImage Decode(string base64)
        {
            if (string.IsNullOrWhiteSpace(base64)) return null;

            try
            {
                var image = new BitmapImage();
                image.BeginInit();
                image.StreamSource = new MemoryStream(Convert.FromBase64String(base64));
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.EndInit();
                image.Freeze();
                return image;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
