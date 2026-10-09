using PrimeERP.Application.PageServices.Admin;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Platform;
using PrimeERP.Platform.Localization;

namespace PrimeERP.UI.Services
{
    /// <summary>البحث عن تحديث وتنزيله</summary>
    public static class UpdateFlow
    {
        public static async Task RunAsync(IServiceProvider services)
        {
            var updates = services.GetRequiredService<IUpdateService>();
            var dialogs = services.GetRequiredService<IDialogService>();
            var toast = services.GetRequiredService<IToastService>();

            var check = await updates.CheckAsync();
            if (check.IsFailure) { toast.Error(check.ErrorMessage); return; }
            if (check.Value.PagesChanged) toast.Info(LocalizationService.Get("Str.Settings.PagesSynced"));

            if (!check.Value.Available)
            {
                toast.Info(LocalizationService.Get("Str.Settings.UpToDate", AppInfo.Version));
                return;
            }

            var title = LocalizationService.Get("Str.Settings.Update");
            if (!await dialogs.ConfirmAsync(title, LocalizationService.Get("Str.Settings.UpdateAvailable", check.Value.Version))) return;

            using var handle = dialogs.ShowProgress(title, LocalizationService.Get("Str.Settings.Downloading", check.Value.Version));
            var stage = LocalizationService.Get("Str.Settings.DownloadStage");
            var progress = new Progress<double>(percent => handle.Report(percent, stage));

            var file = await updates.DownloadAsync(check.Value, progress);
            if (file.IsFailure) { toast.Error(file.ErrorMessage); return; }

            if (!await dialogs.ConfirmAsync(title, LocalizationService.Get("Str.Settings.InstallNow", check.Value.Version))) return;

            try
            {
                var folder = AppContext.BaseDirectory.TrimEnd('\\');
                var script = $"Wait-Process -Id {Environment.ProcessId} -ErrorAction SilentlyContinue; " +
                             $"Expand-Archive -LiteralPath '{file.Value}' -DestinationPath '{folder}' -Force; Start-Process '{Environment.ProcessPath}'";
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("powershell", $"-NoProfile -WindowStyle Hidden -Command \"{script}\"")
                    { UseShellExecute = true, Verb = "runas" });
                System.Windows.Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                toast.Error(ex.Message);
            }
        }

        public static async Task SyncAsync(IServiceProvider services)
        {
            var check = await services.GetRequiredService<IUpdateService>().CheckAsync();
            if (check.IsFailure) return;

            var toast = services.GetRequiredService<IToastService>();
            if (check.Value.PagesChanged) toast.Info(LocalizationService.Get("Str.Settings.PagesSynced"));
            if (check.Value.Available) toast.Info(LocalizationService.Get("Str.Settings.UpdateAvailable", check.Value.Version));
        }
    }
}
