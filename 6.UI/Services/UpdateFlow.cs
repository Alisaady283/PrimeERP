using PrimeERP.Application.Legacy.Admin;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Platform;

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

            if (!check.Value.Available)
            {
                toast.Info($"نسختك أحدث ما لدينا ({AppInfo.Version})");
                return;
            }

            if (!await dialogs.ConfirmAsync("تحديث", $"يوجد إصدار {check.Value.Version} — هل تريد تنزيله؟")) return;

            using var handle = dialogs.ShowProgress("تحديث", $"تنزيل الإصدار {check.Value.Version}");
            var progress = new Progress<double>(percent => handle.Report(percent, "تنزيل"));

            var file = await updates.DownloadAsync(check.Value, progress);
            if (file.IsFailure) { toast.Error(file.ErrorMessage); return; }

            toast.Success($"نُزِّل الإصدار {check.Value.Version} — أغلق البرنامج وشغّل الحزمة");
        }
    }
}
