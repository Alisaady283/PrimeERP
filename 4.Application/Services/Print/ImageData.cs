using System;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace PrimeERP.Application.Services.Print
{
    /// <summary>تحويل صورة ↔ Base64 — مشترك بين حقل الصورة في الواجهة ومستند الطباعة، فالترميز واحد لا نسختان.</summary>
    public static class ImageData
    {
        private const int MaxDimension = 512;

        /// <summary>يقرأ ملفاً، يصغّره لأقصى بُعد ثابت، ويعيده PNG بترميز Base64 — الحجم المحفوظ صغير أياً كان الأصل.</summary>
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

        /// <summary>الصورة تُجمَّد قبل إعادتها — بناء مستند الطباعة يجري على خيط STA غير خيط الإنشاء، وبلا
        /// تجميد يرمي "belongs to a different thread" (نفس سبب تجميد فُرَش PrintTheme).</summary>
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
