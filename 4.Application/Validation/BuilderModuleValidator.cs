using System.Collections.Generic;
using System.Linq;
using PrimeERP.Domain.Contracts;
using PrimeERP.Domain.Entities;
using PrimeERP.Domain.Enums;
using PrimeERP.Domain.Rules;

namespace PrimeERP.Application.Validation
{
    /// <summary>قواعد وصف الصفحة المبنيّة</summary>
    public class BuilderModuleValidator : IValidator<BuilderModule>
    {
        private readonly List<BuilderModule> _existing;

        public BuilderModuleValidator(List<BuilderModule> existing) => _existing = existing;

        public ValidationResult Validate(BuilderModule module) =>
            Rules.For<BuilderModule>()
                .Required(m => m.Key, "مفتاح الصفحة")
                .Required(m => m.Title, "اسم الصفحة")
                .Custom((m, result) =>
                {
                    if (m.SectionId <= 0) result.AddError("SectionId", "القسم مطلوب");

                    if (_existing.Any(e => e.Id != m.Id && e.Key == m.Key))
                        result.AddError("Key", "مفتاح الصفحة مكرَّر");

                    if (m.Kind == BuilderKind.Report) return;

                    if (string.IsNullOrWhiteSpace(m.TableName))
                        result.AddError("TableName", "الجدول مطلوب لصفحةٍ تحفظ سجلات");
                    else if (_existing.Any(e => e.Id != m.Id && e.TableName == m.TableName))
                        result.AddError("TableName", "الجدول مستعمَل في صفحةٍ أخرى");
                })
                .Validate(module);
    }
}
