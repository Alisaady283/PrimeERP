using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Data.Sqlite;
using Microsoft.Win32;

namespace PrimeERP.Setup
{
    /// <summary>
    /// منصِّبٌ صغير: يسأل السريال، ويتحقق منه على الخادم بربطه بجهازه، ثم يُنزّل حزمة العميل ويفكّها
    /// ويكتب سرياله في قاعدتها. لا يحمل البرنامج بداخله — ولهذا يبقى ملفاً صغيراً يُنزَّل من الموقع.
    /// </summary>
    public partial class MainWindow : Window
    {
        private const string Server = "https://primelogic-eg.com/erp";

        private readonly HttpClient _http = new() { Timeout = TimeSpan.FromMinutes(30) };

        public MainWindow()
        {
            InitializeComponent();
            folderBox.Text = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "PrimeERP");

            // سريالٌ بجوار المنصِّب (يضعه المطوّر للعميل) يُقرأ فلا يُملى بالهاتف ولا يُخطأ في حرف.
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

                var package = activation.Value.GetProperty("package").GetString();
                var archive = Path.Combine(Path.GetTempPath(), "PrimeERP-package.zip");

                Say("جارٍ تنزيل النسخة…");
                if (!await Download($"{Server}{package}", archive)) return;

                Say("جارٍ فكّ الحزمة…");
                Directory.CreateDirectory(folder);
                ZipFile.ExtractToDirectory(archive, folder, overwriteFiles: true);
                File.Delete(archive);

                WriteLicense(folder, serial, activation.Value.GetProperty("customer").GetString());
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
                new { serial, machine = Fingerprint() });

            var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;

            if (response.IsSuccessStatusCode) return body.Clone();

            Say(body.TryGetProperty("error", out var error) ? error.GetString() : "تعذّر التحقق من السريال");
            return null;
        }

        private async Task<bool> Download(string url, string target)
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

                if (total > 0) progress.Value = done * 100d / total;
            }

            return true;
        }

        /// <summary>السريال يُكتب في قاعدة النسخة — منه يعرف البرنامج نفسه عند طلب التحديث.</summary>
        private static void WriteLicense(string folder, string serial, string customer)
        {
            var database = Directory.GetFiles(folder, "*.db", SearchOption.AllDirectories);
            if (database.Length == 0) return;

            using var connection = new SqliteConnection($"Data Source={database[0]}");
            connection.Open();

            foreach (var (key, value) in new[] { ("License.Serial", serial), ("License.Customer", customer) })
            {
                using var command = connection.CreateCommand();
                command.CommandText = @"INSERT INTO AppSettings ([Key], Value, Category, DataType, IsSystem)
                                        VALUES ($k, $v, 'License', 'string', 1)
                                        ON CONFLICT([Key]) DO UPDATE SET Value = $v";
                command.Parameters.AddWithValue("$k", key);
                command.Parameters.AddWithValue("$v", value ?? "");
                command.ExecuteNonQuery();
            }
        }

        private static void Shortcut(string folder)
        {
            var exe = Path.Combine(folder, "PrimeERP.exe");
            if (!File.Exists(exe)) return;

            var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            var link = Path.Combine(desktop, "PrimeERP.url");
            var url = exe.Replace(Path.DirectorySeparatorChar, '/');

            File.WriteAllLines(link, new[] { "[InternetShortcut]", $"URL=file:///{url}", $"IconFile={exe}", "IconIndex=0" });
        }

        private static string Fingerprint()
        {
            var guid = "";
            try
            {
                using var key = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64)
                    .OpenSubKey(@"SOFTWARE\Microsoft\Cryptography");
                guid = key?.GetValue("MachineGuid")?.ToString() ?? "";
            }
            catch { }

            var parts = string.Join("|", Environment.MachineName, guid);
            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(parts)))[..32];
        }

        private void Say(string message) => statusText.Text = message;
    }
}
