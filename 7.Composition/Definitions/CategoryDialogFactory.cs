using System.Collections.Generic;
using System.Linq;
using PrimeERP.Application.DTOs.Common;
using PrimeERP.Application.Legacy.Common;

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
            CreateDtoType = typeof(CreateCategoryDto),
            UpdateDtoType = typeof(UpdateCategoryDto),
            Fields = new List<FieldDefinition>
            {
                new() { Key = nameof(CreateCategoryDto.Name), LabelKey = "Str.Name", Kind = FieldKind.Text, IsRequired = true, MaxLength = 200 },
                new() { Key = nameof(CreateCategoryDto.Notes), LabelKey = "Str.Notes", Kind = FieldKind.TextArea },
            }.Concat(StandardFields.DialogFields()).ToList(),
            FixedValues = new Dictionary<string, object> { [nameof(CreateCategoryDto.ModuleKey)] = moduleKey }
        };
    }
}
