using QRCoder;

namespace PrimeERP.UI.Services
{
    /// <summary>رمز QR صورة PNG — للطباعة والتصدير معاً</summary>
    public static class QrImage
    {
        public static byte[] Png(string text, int pixelsPerModule = 6)
        {
            using var generator = new QRCodeGenerator();
            using var data = generator.CreateQrCode(text ?? "", QRCodeGenerator.ECCLevel.M);
            return new PngByteQRCode(data).GetGraphic(pixelsPerModule);
        }
    }
}
