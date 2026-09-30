using System.Collections.Generic;
using System.Linq;
using PrimeERP.Application.Legacy.Common;
using PrimeERP.Domain.Entities;

namespace PrimeERP.Composition.Definitions
{
    /// <summary>حوار الفئة لكل وحدة</summary>
    public static class CategoryDialogFactory
    {
        public static DialogDefinition Build(string moduleKey, string titleKey = "Str.Category.Add", string titleEditKey = "Str.Category.Edit") => new()
        {
            TitleKey = titleKey,
            TitleEditKey = titleEditKey,
            GridColumns = 1,
            ServiceType = typeof(ICategoryService),
            CreateDtoType = typeof(Category),
            UpdateDtoType = typeof(Category),
            Fields = new List<FieldDefinition>
            {
                new() { Key = nameof(Category.Name), LabelKey = "Str.Name", Kind = FieldKind.Text, IsRequired = true, MaxLength = 200 },
                new() { Key = nameof(Category.Notes), LabelKey = "Str.Notes", Kind = FieldKind.TextArea },
            }.Concat(StandardFields.DialogFields()).ToList(),
            FixedValues = new Dictionary<string, object> { [nameof(Category.ModuleKey)] = moduleKey }
        };
    }
}
