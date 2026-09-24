using System;
using System.IO;
using PrimeERP.Application.DTOs.Common;
using PrimeERP.Data.Core;
using PrimeERP.Domain.Contracts;
using PrimeERP.Domain.Rules;

namespace PrimeERP.Application.Validation
{
    /// <summary>تحقّق نسخة البرنامج</summary>
    public class EditionValidator : IValidator<CreateEditionDto>
    {
        private readonly BackupCapability _capability;

        public EditionValidator(BackupCapability capability) => _capability = capability;

        public ValidationResult Validate(CreateEditionDto edition) =>
            Rules.For<CreateEditionDto>()
                .Required(x => x.TargetFolder, "مسار النسخة")
                .Custom((dto, result) =>
                {
                    if (dto.ModuleKeys == null || dto.ModuleKeys.Count == 0)
                        result.AddError(nameof(dto.ModuleKeys), "اختر صفحةً واحدة على الأقل");

                    if (_capability != BackupCapability.FileCopy)
                        result.AddError(nameof(dto.TargetFolder), "إنشاء نسخة يعمل مع قاعدة محلية فقط");

                    if (string.IsNullOrWhiteSpace(dto.TargetFolder)) return;

                    if (Path.TrimEndingDirectorySeparator(Path.GetFullPath(dto.TargetFolder))
                            .StartsWith(Path.TrimEndingDirectorySeparator(AppContext.BaseDirectory),
                                        StringComparison.OrdinalIgnoreCase))
                        result.AddError(nameof(dto.TargetFolder), "المسار داخل مجلد البرنامج نفسه — اختر مجلداً خارجه");
                })
                .Validate(edition);
    }
}
