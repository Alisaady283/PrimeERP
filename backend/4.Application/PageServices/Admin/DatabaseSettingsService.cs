using System.IO;
using PrimeERP.Application.DTOs.Common;
using PrimeERP.Application.Services.Core;
using PrimeERP.Data.Core;
using PrimeERP.Domain.Enums;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Audit;
using PrimeERP.Platform.Localization;
using PrimeERP.Platform.Permissions;
using PrimeERP.Platform.Settings;

namespace PrimeERP.Application.PageServices.Admin
{
    /// <summary>قاعدة البيانات في ملف الإعداد: تُقرأ وتُكتب، والتغيير يسري بعد إعادة التشغيل</summary>
    public interface IDatabaseSettingsService
    {
        Result<DatabaseSettingsDto> Get();
        Result Save(DatabaseSettingsDto dto);
    }

    public class DatabaseSettingsService : ServiceBase, IDatabaseSettingsService
    {
        private const string SqliteExtension = ".db";

        public DatabaseSettingsService(IPermissionService permissions, ISettingsProvider settings, ILocalizationService localization, IAuditLogger audit)
            : base(permissions, settings, localization, audit) { }

        protected override string PermissionPrefix => "Settings";
        protected override string StringPrefix => "Str.Database";
        protected override string EntityName => "DatabaseSettings";

        public Result<DatabaseSettingsDto> Get()
        {
            if (!Can("System")) return FailDenied<DatabaseSettingsDto>();
            var current = DbConfig.Load();
            return Result.Ok(new DatabaseSettingsDto
            {
                Provider = (int)current.Provider,
                Folder = current.Provider == DatabaseProvider.Sqlite ? Path.GetDirectoryName(current.FilePath) : PrimeERP.Platform.AppInfo.DataFolder,
                FileName = Path.GetFileName(current.FilePath),
                Host = current.Host, Port = current.Port, Database = current.Database,
                Username = current.Username, UseIntegratedSecurity = current.UseIntegratedSecurity
            });
        }

        public Result Save(DatabaseSettingsDto dto)
        {
            if (!Can("System")) return FailDenied();

            var provider = (DatabaseProvider)dto.Provider;
            var config = DbConfig.Read(DbConfig.SettingsFile);
            config.Provider = provider;

            if (provider == DatabaseProvider.Sqlite)
            {
                if (string.IsNullOrWhiteSpace(dto.Folder)) return Fail("FolderRequired", ErrorCode.ValidationFailed);
                if (string.IsNullOrWhiteSpace(dto.FileName)) return Fail("FileNameRequired", ErrorCode.ValidationFailed);
                if (dto.FileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0) return Fail("FileNameInvalid", ErrorCode.ValidationFailed);
                var name = Path.HasExtension(dto.FileName) ? dto.FileName.Trim() : dto.FileName.Trim() + SqliteExtension;
                config.FilePath = Path.Combine(dto.Folder.Trim(), name);
            }
            else
            {
                if (string.IsNullOrWhiteSpace(dto.Host)) return Fail("HostRequired", ErrorCode.ValidationFailed);
                if (string.IsNullOrWhiteSpace(dto.Database)) return Fail("DatabaseRequired", ErrorCode.ValidationFailed);
                config.Host = dto.Host.Trim();
                config.Port = dto.Port;
                config.Database = dto.Database.Trim();
                config.UseIntegratedSecurity = dto.UseIntegratedSecurity;
                config.Username = dto.UseIntegratedSecurity ? "" : dto.Username ?? "";
                if (dto.UseIntegratedSecurity) config.Password = "";
                else if (!string.IsNullOrEmpty(dto.Password)) config.Password = dto.Password;
            }

            try { config.Save(DbConfig.SettingsFile); }
            catch (IOException ex) { return Fail("SaveFailed", ErrorCode.ValidationFailed, ex.Message); }
            catch (System.UnauthorizedAccessException ex) { return Fail("SaveFailed", ErrorCode.ValidationFailed, ex.Message); }

            Audit.Log(EntityName, 0, AuditAction.Update,
                newValue: new { provider = provider.ToString(), config.FilePath, config.Host, config.Port, config.Database }, details: Msg("Changed"));
            return Result.Ok();
        }
    }
}
