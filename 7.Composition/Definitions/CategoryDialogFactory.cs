using System.Collections.Generic;
using System.Linq;
using PrimeERP.Application.DTOs.Common;
using PrimeERP.Application.Services.Common;

namespace PrimeERP.Composition.Definitions
{
    // حوار إضافة/تعديل فئة موحّد — يُستدعى لكل وحدة تحتاجه (Products اليوم، غيرها لاحقاً) بلا تكرار كود.
    // ModuleKey تُثبَّت عبر FixedValues، بلا أي عنصر مرئي للمستخدم.
    public static class CategoryDialogFactory
    {
        public static DialogDefinition Build(string moduleKey) => new()
        {
            TitleKey = "Str.Category.Add",
            TitleEditKey = "Str.Category.Edit",
            GridColumns = 1,
            ServiceType = typeof(ICategoryService),
            CreateDtoType = typeof(CreateCategoryDto),
            UpdateDtoType = typeof(UpdateCategoryDto),
            Fields = new List<FieldDefinition>
            {
                new() { Key = nameof(CreateCategoryDto.ParentId), LabelKey = "Str.ParentCategory", Kind = FieldKind.Picker, IsReadOnlyOnEdit = true, PickerType = "Category", PickerCategoryModuleKey = moduleKey },
                new() { Key = nameof(CreateCategoryDto.Name), LabelKey = "Str.Name", Kind = FieldKind.Text, IsRequired = true, MaxLength = 200 },
                new() { Key = nameof(CreateCategoryDto.Notes), LabelKey = "Str.Notes", Kind = FieldKind.TextArea },
            }.Concat(StandardFields.DialogFields()).ToList(),
            FixedValues = new Dictionary<string, object> { [nameof(CreateCategoryDto.ModuleKey)] = moduleKey }
        };
    }
}
