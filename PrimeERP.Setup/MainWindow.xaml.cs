using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;

namespace PrimeERP.Setup
{
    /// <summary>منصِّبٌ صغير</summary>
    public partial class MainWindow : Window
    {
        private const string Server = "https://primelogic-eg.com/erp";

        private readonly HttpClient _http = new() { Timeout = TimeSpan.FromMinutes(30) };

        public MainWindow()
        {
            InitializeComponent();
            folderBox.Text = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "PrimeERP");

            var beside = Path.Combine(AppContext.BaseDirectory, "serial.txt");
            if (File.Exists(beside)) serialBox.Text = File.ReadAllText(beside).Trim();
        }

        private void Browse_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFolderDialog { Title = "مجلد التثبيت" };
            if (dialog.ShowDialog() == true) folderBox.Text = dialog.FolderName;
        }

        private async void Install_Click(object sender, RoutedEventArgs e)
        {
            var serial = serialBox.Text.Trim();
            var folder = folderBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(serial)) { Say("أدخل السريال"); return; }
            if (string.IsNullOrWhiteSpace(folder)) { Say("اختر مجلد التثبيت"); return; }

            installButton.IsEnabled = false;
            try
            {
                Say("جارٍ التحقق من السريال…");
                var activation = await Activate(serial);
                if (activation == null) return;

                var list = activation.Value.GetProperty("files").GetString();
                var source = $"{Server}{list[..list.LastIndexOf('/')]}";

                Say("جارٍ تنزيل النسخة…");
                Directory.CreateDirectory(folder);
                var listed = Path.Combine(folder, "files.json");
                if (!await Download($"{Server}{list}", listed)) return;

                var files = JsonDocument.Parse(File.ReadAllText(listed)).RootElement.GetProperty("files").EnumerateObject()
                    .Select(file => file.Name).ToList();
                for (var i = 0; i < files.Count; i++)
                {
                    var target = Path.Combine(folder, files[i]);
                    Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                    var url = $"{source}/{string.Join("/", files[i].Split('/').Select(Uri.EscapeDataString))}";
                    if (!await Download(url, target, track: false)) return;
                    progress.Value = (i + 1) * 100d / files.Count;
                }

                WriteLicense(folder, serial, activation.Value);
                Shortcut(folder);

                progress.Value = 100;
                Say("تمّ التثبيت. ستجد اختصار PrimeERP على سطح المكتب.");
                MessageBox.Show("تمّ تثبيت PrimeERP بنجاح.", "PrimeERP", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Say($"تعذّر التثبيت: {ex.Message}");
            }
            finally
            {
                installButton.IsEnabled = true;
            }
        }

        private async Task<JsonElement?> Activate(string serial)
        {
            var response = await _http.PostAsJsonAsync($"{Server}/activate",
                new { serial, machine = PrimeERP.Platform.Net.MachineFingerprint.Value() });

            var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;

            if (response.IsSuccessStatusCode) return body.Clone();

            Say(body.TryGetProperty("error", out var error) ? error.GetString() : "تعذّر التحقق من السريال");
            return null;
        }

        private async Task<bool> Download(string url, string target, bool track = true)
        {
            using var response = await _http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            if (!response.IsSuccessStatusCode) { Say("تعذّر تنزيل الحزمة"); return false; }

            var total = response.Content.Headers.ContentLength ?? 0;
            await using var source = await response.Content.ReadAsStreamAsync();
            await using var file = File.Create(target);

            var buffer = new byte[81920];
            long done = 0;
            int read;

            while ((read = await source.ReadAsync(buffer)) > 0)
            {
                await file.WriteAsync(buffer.AsMemory(0, read));
                done += read;

                if (track && total > 0) progress.Value = done * 100d / total;
            }

            return true;
        }

        private static void WriteLicense(string folder, string serial, JsonElement activation)
        {
            string Read(string name) => activation.TryGetProperty(name, out var value) ? value.GetString() ?? "" : "";

            var license = new
            {
                serial,
                customer = Read("customer"),
                manifest = Read("manifest"),
                version = Read("version"),
                simplified = activation.TryGetProperty("simplified", out var s) && s.GetBoolean()
            };

            File.WriteAllText(Path.Combine(folder, "license.json"), JsonSerializer.Serialize(license));

            var settings = Path.Combine(folder, "appsettings.json");
            if (File.Exists(settings)) return;

            var data = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PrimeERP", serial);
            Directory.CreateDirectory(data);
            File.WriteAllText(settings, JsonSerializer.Serialize(new
            {
                Database = new { Provider = "Sqlite", FilePath = Path.Combine(data, "PrimeERP.db") }
            }));
        }

        private static void Shortcut(string folder)
        {
            var exe = Path.Combine(folder, "PrimeERP.exe");
            if (!File.Exists(exe)) return;

            var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            var link = Path.Combine(desktop, "PrimeERP.url");
            var url = new Uri(exe).AbsoluteUri;

            File.WriteAllLines(link, new[] { "[InternetShortcut]", $"URL={url}", $"IconFile={exe}", "IconIndex=0" });
        }


        private void Say(string message) => statusText.Text = message;
    }
}
