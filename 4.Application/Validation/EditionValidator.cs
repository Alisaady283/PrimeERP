using System;
using System.IO;
using PrimeERP.Application.DTOs.Common;
using PrimeERP.Data.Core;
using PrimeERP.Domain.Contracts;
using PrimeERP.Domain.Rules;

namespace PrimeERP.Application.Validation
{
    public class EditionValidator : IValidator<CreateEditionDto>
    {
        public ValidationResult Validate(CreateEditionDto edition) =>
            Rules.For<CreateEditionDto>()
                .Required(x => x.TargetFolder, "مسار النسخة")
                .Custom((dto, result) =>
                {
                    if (dto.ModuleKeys == null || dto.ModuleKeys.Count == 0)
                        result.AddError(nameof(dto.ModuleKeys), "اختر صفحةً واحدة على الأقل");

                    // نسخ القاعدة ملفاً سلوكُ SQLite وحده — الخادم يبقى خادماً، فلا تُنسخ قاعدته في مجلد.
                    if (DbFactory.Current.GetBackupCapability() != BackupCapability.FileCopy)
                        result.AddError(nameof(dto.TargetFolder), "إنشاء نسخة يعمل مع قاعدة محلية فقط");

                    if (string.IsNullOrWhiteSpace(dto.TargetFolder)) return;

                    // نسخة داخل البرنامج تنسخ نفسها وهي تُنسخ — مسارٌ خارجه شرطُ اكتمالها.
                    if (Path.TrimEndingDirectorySeparator(Path.GetFullPath(dto.TargetFolder))
                            .StartsWith(Path.TrimEndingDirectorySeparator(AppContext.BaseDirectory),
                                        StringComparison.OrdinalIgnoreCase))
                        result.AddError(nameof(dto.TargetFolder), "المسار داخل مجلد البرنامج نفسه — اختر مجلداً خارجه");
                })
                .Validate(edition);
    }
}
