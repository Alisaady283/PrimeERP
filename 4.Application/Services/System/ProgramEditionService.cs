using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using PrimeERP.Application.DTOs.Common;
using PrimeERP.Application.Services.Backup;
using PrimeERP.Application.Validation;
using PrimeERP.Data.Repositories;
using PrimeERP.Domain.Enums;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Audit;
using PrimeERP.Platform.Localization;
using PrimeERP.Platform.Permissions;
using PrimeERP.Platform.Settings;

namespace PrimeERP.Application.Services
{
    /// <summary>
    /// نسخةُ برنامجٍ مستقلّة في مسارٍ يختاره المستخدم: ملفات التشغيل كما هي، وقاعدةٌ منسوخة بجوارها،
    /// وبيانٌ يحصر أقسامها. لا تُحذف وحدةٌ ولا يُصرَّف شيء — البيان القائم (UI.Manifest) يمنع تسجيل ما
    /// خارجه أصلاً، فالنسخة تُقلع بأقسامها وحدها.
    ///
    /// العملية طويلة بطبعها (آلاف الملفات)، فترفع تقدّمها بـ IProgress وتُستدعى خارج خيط الواجهة.
    /// </summary>
    public interface IProgramEditionService
    {
        Result Create(CreateEditionDto edition, IProgress<EditionProgress> progress = null);
    }

    public class ProgramEditionService : ServiceBase, IProgramEditionService
    {
        protected override string PermissionPrefix => "BuilderExport";
        protected override string StringPrefix => "Str.Builder";
        protected override string EntityName => "ProgramEdition";

        // نسخ الملفات هو الجزء الأطول، فيأخذ معظم الشريط والباقي يوزَّع على خطوتَيه.
        private const double FilesShare = 88;
        private const double DatabaseShare = 8;

        private readonly IBackupService _backup;
        private readonly IEditionRepository _edition;

        public ProgramEditionService(IPermissionService permissions, ISettingsProvider settings,
            ILocalizationService localization, IAuditLogger audit,
            IBackupService backup, IEditionRepository edition)
            : base(permissions, settings, localization, audit)
        {
            _backup = backup;
            _edition = edition;
        }

        public Result Create(CreateEditionDto edition, IProgress<EditionProgress> progress = null)
        {
            if (!Can("Create")) return FailDenied();

            var invalid = Check(new EditionValidator(), edition);
            if (invalid.IsFailure) return invalid;

            var source = Path.TrimEndingDirectorySeparator(AppContext.BaseDirectory);
            var target = Path.TrimEndingDirectorySeparator(Path.GetFullPath(edition.TargetFolder));

            try
            {
                CopyFolder(source, target, progress);

                // القاعدة تُنسخ بآلية النسخ الاحتياطي نفسها — لا نسخ ملفٍ مكتوب هنا بينما الخدمة قائمة.
                progress?.Report(new EditionProgress(FilesShare, Msg("CopyingDatabase")));
                var copied = _backup.Create(target, Msg("EditionNote"), BackupType.Manual);
                if (copied.IsFailure) return Result.Fail(copied.ErrorMessage);

                progress?.Report(new EditionProgress(FilesShare + DatabaseShare, Msg("WritingManifest")));
                WriteEditionSettings(copied.Value.FilePath, edition);
                PointAtDatabase(Path.Combine(target, "appsettings.json"), copied.Value.FilePath);

                Audit.Log(EntityName, 0, AuditAction.Insert,
                    newValue: new { target, edition.Simplified, pages = edition.ModuleKeys.Count });

                return Result.Ok();
            }
            catch (Exception ex)
            {
                return Result.Fail(ex.Message, ErrorCode.Unexpected);
            }
        }

        /// <summary>
        /// إعدادات النسخة في قاعدتها: البيان والوضع. تُكتب بمستودع الإعدادات نفسه على اتصالها هو —
        /// وهو ما يُنشئ الصفّ إن لم يكن موجوداً. جملة UPDATE مكتوبة هنا كانت تُصيب صفر صفوف بلا خطأ،
        /// فتُقلع النسخة ببيانٍ فارغ أي بالنظام كاملاً.
        /// </summary>
        private void WriteEditionSettings(string databasePath, CreateEditionDto edition)
        {
            using var connection = _edition.Open(databasePath);

            foreach (var (key, value) in new Dictionary<string, string>
            {
                [SettingKeys.UI.Manifest] = string.Join(",", edition.ModuleKeys),
                [SettingKeys.Documents.SimplifiedFlow] = edition.Simplified ? "true" : "false"
            })
                SettingRepository.Upsert(connection, null, new SettingRecord
                {
                    Key = key, Value = value, Category = "UI", DataType = "string",
                    IsSystem = true, ModifiedBy = AppSession.Username
                });
        }

        /// <summary>ملفات التشغيل كما هي عدا رموز التنقيح — النسخة تعمل، ولا تُصرَّف.</summary>
        private void CopyFolder(string source, string target, IProgress<EditionProgress> progress)
        {
            Directory.CreateDirectory(target);

            foreach (var directory in Directory.GetDirectories(source, "*", SearchOption.AllDirectories))
                Directory.CreateDirectory(directory.Replace(source, target));

            var files = Directory.GetFiles(source, "*", SearchOption.AllDirectories);
            var stage = Msg("CopyingFiles");
            var reported = -1;

            for (var i = 0; i < files.Length; i++)
            {
                if (!Path.GetExtension(files[i]).Equals(".pdb", StringComparison.OrdinalIgnoreCase))
                    File.Copy(files[i], files[i].Replace(source, target), overwrite: true);

                // إبلاغٌ عند تغيّر النسبة الصحيحة فقط — إبلاغٌ لكل ملف يُغرق خيط الواجهة بآلاف الرسائل.
                var percent = (int)((i + 1) * FilesShare / files.Length);
                if (percent == reported) continue;

                reported = percent;
                progress?.Report(new EditionProgress(percent, stage));
            }
        }

        /// <summary>قاعدة النسخة بجوارها لا في AppData: مسارٌ مطلق يمنع مشاركتها قاعدة البرنامج الأصلي.</summary>
        private static void PointAtDatabase(string settingsFile, string databasePath)
        {
            var settings = File.Exists(settingsFile)
                ? JsonNode.Parse(File.ReadAllText(settingsFile))
                : new JsonObject();

            settings["Database"] ??= new JsonObject();
            settings["Database"]["FilePath"] = databasePath;

            File.WriteAllText(settingsFile, settings.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
        }
    }
}
