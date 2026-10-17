using PrimeERP.Application.Services.Core;
using System;
using System.IO;
using PrimeERP.Application.DTOs.Common;
using PrimeERP.Application.Validation;
using PrimeERP.Data.Repositories;
using PrimeERP.Domain.Enums;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Audit;
using PrimeERP.Platform.Localization;
using PrimeERP.Platform.Permissions;
using PrimeERP.Platform.Settings;

namespace PrimeERP.Application.PageServices.Admin
{
    /// <summary>نسخةُ برنامجٍ مستقلّة في مسارٍ</summary>
    public interface IProgramEditionService
    {
        Result Create(CreateEditionDto edition, IProgress<EditionProgress> progress = null);
    }

    public class ProgramEditionService : ServiceBase, IProgramEditionService
    {
        protected override string PermissionPrefix => "BuilderExport";
        protected override string StringPrefix => "Str.Builder";
        protected override string EntityName => "ProgramEdition";

        private const double FilesShare = 96;

        private readonly IEditionRepository _edition;

        public ProgramEditionService(IPermissionService permissions, ISettingsProvider settings,
            ILocalizationService localization, IAuditLogger audit, IEditionRepository edition)
            : base(permissions, settings, localization, audit)
        {
            _edition = edition;
        }

        public Result Create(CreateEditionDto edition, IProgress<EditionProgress> progress = null)
        {
            if (!Can("Create")) return FailDenied();

            var invalid = Check.Valid(edition, EditionFields());
            if (invalid.IsFailure) return invalid;

            var source = Path.TrimEndingDirectorySeparator(AppContext.BaseDirectory);
            var target = Path.TrimEndingDirectorySeparator(Path.GetFullPath(edition.TargetFolder));

            try
            {
                var stage = Msg("CopyingFiles");
                _edition.CopyProgram(source, target, FilesShare, percent => progress?.Report(new EditionProgress(percent, stage)));

                progress?.Report(new EditionProgress(FilesShare, Msg("WritingManifest")));
                _edition.PointAtDatabase(Path.Combine(target, "appsettings.json"), Path.Combine(target, "PrimeERP.db"));
                _edition.WriteJson(Path.Combine(target, "license.json"), new
                {
                    serial = edition.Serial ?? "",
                    customer = edition.Customer ?? "",
                    manifest = string.Join(",", edition.ModuleKeys),
                    simplified = edition.Simplified,
                    english = edition.English,
                    version = edition.Version ?? ""
                });

                Audit.Log(EntityName, 0, AuditAction.Insert,
                    newValue: new { target, edition.Serial, edition.Simplified, pages = edition.ModuleKeys.Count });

                return Result.Ok();
            }
            catch (Exception ex)
            {
                return Result.Fail(ex.Message, ErrorCode.Unexpected);
            }
        }

        /// <summary>شروط النسخة المنشأة</summary>
        private static Field<CreateEditionDto>[] EditionFields() => new Field<CreateEditionDto>[]
        {
            new(x => x.TargetFolder, "Str.Field.EditionPath", Required: true),
            new(x => x.ModuleKeys, "", Required: true, Message: "Str.Builder.PickOnePage"),
            new(x => x.TargetFolder, "", Must: d => string.IsNullOrWhiteSpace(d.TargetFolder) ||
                !Path.TrimEndingDirectorySeparator(Path.GetFullPath(d.TargetFolder))
                    .StartsWith(Path.TrimEndingDirectorySeparator(AppContext.BaseDirectory), StringComparison.OrdinalIgnoreCase),
                Message: "Str.Builder.EditionInsideApp"),
        };
    }
}
