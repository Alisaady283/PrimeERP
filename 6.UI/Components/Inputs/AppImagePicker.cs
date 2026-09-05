using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using PrimeERP.Application.Services.Print;

namespace PrimeERP.UI.Components.Inputs
{
    /// <summary>حقل صورة قيمته نص Base64 لا مسار ملف — الصورة تُصغَّر وتُحفَظ داخل البيانات نفسها، فتنجو مع
    /// النسخة الاحتياطية وتنتقل مع النظام، بخلاف ملف خارجي يختفي بصمت وقت الطباعة.</summary>
    public class AppImagePicker : UserControl
    {
        private readonly Image _preview = new() { Stretch = Stretch.Uniform, Height = 96, Margin = new Thickness(0, 0, 12, 0) };
        private readonly TextBlock _empty = new() { Text = "لا صورة", VerticalAlignment = VerticalAlignment.Center };
        private string _base64;
        private string _error;

        public AppImagePicker(string label)
        {
            var pick = new Actions.AppButton { Text = "اختيار صورة", Variant = "secondary", Size = "sm", Margin = new Thickness(0, 0, 0, 6) };
            var clear = new Actions.AppButton { Text = "إزالة", Variant = "ghost", Size = "sm" };

            pick.Click += (_, __) => PickFile();
            clear.Click += (_, __) => Value = null;

            _empty.SetResourceReference(TextBlock.ForegroundProperty, "TextMuted");

            var buttons = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Children = { pick, clear } };
            var row = new StackPanel { Orientation = Orientation.Horizontal, Children = { _preview, _empty, buttons } };

            var caption = new TextBlock { Text = label, Margin = new Thickness(0, 0, 0, 6) };
            caption.SetResourceReference(TextBlock.ForegroundProperty, "TextSecondary");

            Content = new StackPanel { Children = { caption, row } };
            Render();
        }

        public string Value
        {
            get => _base64;
            set { _base64 = string.IsNullOrWhiteSpace(value) ? null : value; Render(); }
        }

        private void PickFile()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "اختر صورة الشعار",
                Filter = "الصور|*.png;*.jpg;*.jpeg;*.bmp|كل الملفات|*.*"
            };
            if (dialog.ShowDialog() != true) return;

            // الابتلاع الصامت كان يترك المستخدم يظن أن الشعار حُفظ بينما لم يُقرأ الملف أصلاً — الرسالة
            // تظهر داخل الحقل نفسه (المكوّن لا يعرف خدمات الإشعارات، ولا يجوز له).
            try { Value = ImageData.Encode(dialog.FileName); _error = null; }
            catch (Exception ex) { Value = null; _error = $"تعذّرت قراءة الصورة: {ex.Message}"; }

            Render();
        }

        private void Render()
        {
            var image = ImageData.Decode(_base64);
            _preview.Source = image;
            _preview.Visibility = image == null ? Visibility.Collapsed : Visibility.Visible;
            _empty.Visibility = image == null ? Visibility.Visible : Visibility.Collapsed;
            _empty.Text = _error ?? "لا صورة";
            _empty.SetResourceReference(TextBlock.ForegroundProperty, _error == null ? "TextMuted" : "Danger");
        }
    }
}
