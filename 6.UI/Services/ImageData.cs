using System;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace PrimeERP.UI.Services
{
    /// <summary>تحويل صورة ↔ Base64</summary>
    public static class ImageData
    {
        private const int MaxDimension = 512;

        public static string Encode(string path)
        {
            var source = new BitmapImage();
            source.BeginInit();
            source.UriSource = new Uri(path);
            source.CacheOption = BitmapCacheOption.OnLoad;
            source.EndInit();

            var scale = Math.Min(1.0, MaxDimension / (double)Math.Max(source.PixelWidth, source.PixelHeight));
            BitmapSource scaled = scale < 1.0 ? new TransformedBitmap(source, new ScaleTransform(scale, scale)) : source;

            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(scaled));

            using var stream = new MemoryStream();
            encoder.Save(stream);
            return Convert.ToBase64String(stream.ToArray());
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
