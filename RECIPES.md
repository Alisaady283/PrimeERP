# RECIPES.md — خطوات كل طلب

ما أفعله حين يُطلَب شيءٌ بعينه: ماذا أستورد، وأي ملفات ألمس، وبأي ترتيب. القواعد في [RULES.md](RULES.md) والمواضع في [ARCHITECTURE.md](ARCHITECTURE.md).

## الدورة — على كل طلب بلا استثناء

```
١ فحص      →  ابحث بالمفهوم لا بالنصّ: هل يوجد مثيله؟ أي مصنع يبنيه؟ أي أساس يرثه؟
٢ استيراد  →  استعمل ما وجدتَ. لم تجد وسيتكرّر؟ ابنِ المصدر المشترك أولاً ثم استورده مرّتين
٣ إعلان    →  الجديد وصفٌ في 8.Modules لا كودٌ: صفر XAML، صفر code-behind
٤ إكمال    →  الشروط الثلاثة (RULES.md § إضافة شاشة)
٥ تحقق     →  الثلاثة خضراء أو لم ينتهِ العمل (CLAUDE.md § التحقق)
```

---

## ٠) مثالٌ حقيقي — شاشة «الخزائن والبنوك» من أولها لآخرها

سبعة مواضع، كلها موجودة في المستودع الآن. من يفهم هذه الشاشة يفهم كل شاشات النظام.

| # | الموضع | الملف |
|---|---|---|
| ١ | الكيان | `3.Domain/Entities/Treasury.cs` — خصائص فقط |
| ٢ | المستودع | `2.Data/Repositories/TreasuryRepository.cs` — يرث `RepositoryBase<T>` بـLINQ |
| ٣ | البيانات | `4.Application/DTOs/Treasury/TreasuryDto.cs` — `Dto`/`Create`/`Update`/`Filter` |
| ٤ | الخدمة | `4.Application/Legacy/Treasury/TreasuryService.cs` خدمة الصفحة، ومنطقها في `4.Application/Services/…` |
| ٥ | نموذج العرض | `6.UI/ViewModels/TreasuriesViewModel.cs` — يرث `CrudViewModelBase` ويُعلن `PermissionPrefix` |
| ٦ | الإعلان | `8.Modules/TreasuryRegistrations.cs:21` |
| ٧ | الشروط الثلاثة | `NavigationMap.cs:19` · `Strings.ar.xaml:217` · `DependencyInjection.cs:162` |

الإعلان نفسه — هذا ما يصير شاشةً كاملة، بلا XAML وبلا code-behind:

```csharp
registry.Register(new ModuleDefinition
{
    Key = "Treasuries", TitleKey = "Str.Module.Treasuries", PermissionPrefix = "Treasuries",
    ViewModelType = typeof(TreasuriesViewModel),
    Columns = new()
    {
        new() { Header = "الكود", Binding = nameof(TreasuryDto.Code), Width = 100, Align = ColumnAlign.Center },
        new() { Header = "الرصيد", Binding = nameof(TreasuryDto.Balance), Width = 130,
                Format = "N2", Footer = FooterAggregate.Sum },
    },
    Dialog = new DialogDefinition
    {
        TitleKey = "Str.Treasuries.Add", TitleEditKey = "Str.Treasuries.Edit", GridColumns = 2,
        ServiceType = typeof(ITreasuryService),
        CreateDtoType = typeof(CreateTreasuryDto), UpdateDtoType = typeof(UpdateTreasuryDto),
        Fields = new()
        {
            new() { Key = nameof(CreateTreasuryDto.Kind), LabelKey = "النوع", Kind = FieldKind.Picker,
                    PickerType = "TreasuryKind", IsRequired = true, IsReadOnlyOnEdit = true },
            new() { Key = nameof(CreateTreasuryDto.Name), LabelKey = "الاسم", Kind = FieldKind.Text, IsRequired = true },
        }
    }
});
```

وعليها تُجرى التعديلات الأربعة الشائعة — سطرٌ واحد لكلٍّ منها:

| المطلوب | أين | كيف |
|---|---|---|
| عمود جديد | `Columns` | `new() { Header = "…", Binding = nameof(Dto.X), Width = 120 }` — والحقل موجودٌ في الـDto أولاً |
| زرّ | `EnabledActions` | من كتالوج `ToolbarAction` — لا يُبنى زرّ جديد |
| فلتر | `Filters` | ولا يعمل إلا إذا قرأه المستودع من `XFilter` |
| القسم الذي تظهر فيه | `NavigationMap.Coded` | مفتاح الصفحة داخل مصفوفة قسمها |

والمصنع يسبق الإعلان اليدوي: قائمةٌ بسيطة (كود/اسم/نشط) تُسجَّل بسطر واحد — `ModuleRegistrations.cs:249`:

```csharp
RegisterLookup(registry, "Brands", "Str.Module.Brands", "Brands.Add", "Brands.Edit", typeof(BrandsViewModel));
```

---

## ١) تقرير جديد

التقرير ليس شاشة. هو **دالةٌ في خدمة** تُرجع `Result<ReportData>`، و**إعلانٌ** يصف من أين تُقرأ وبأي أعمدة تُعرَض. `ReportRenderer` يستدعيها بالانعكاس — بلا شاشة وبلا نموذج عرض.

| # | الملف | ماذا |
|---|---|---|
| ١ | `4.Application/Reporting/ReportRows.cs` | سطر النتيجة: خصائص فقط |
| ٢ | `4.Application/Reporting/<الاسم>ReportService.cs` | واجهة + صنف يقرأ من **خدمة** الكيان (لا مستودع)، ويُرجع `ReportData { Rows, Totals }` |
| ٣ | `App/Bootstrap/DependencyInjection.cs` | `AddSingleton<I…, …>()` |
| ٤ | `8.Modules/ReportRegistrations.cs` | استدعاء `Register(...)` واحد + دالّة أعمدة |
| ٥ | `NavigationMap.Coded` | المفتاح في قسم `Reports` |
| ٦ | `Strings.{ar,en}.xaml` | `Str.Module.<المفتاح>` + عناوين الأعمدة |

**يُستورَد**: `StandardFields.DateRange()` للفترة · `Money(header, binding)` لعمود مالي · `Party`/`Account`/`Warehouse` للبارامترات المتكرّرة · `FinancialStatementFactory.RowKind` للقوائم المالية.

**يُمنَع**: أي حساب في `ReportRegistrations` (يفحصه `check.sh` §5.5) · شكل نتيجة ثانٍ غير `ReportData` · استعلام SQL في خدمة التقرير.

**انتبه**: أعمدة التقرير وبارامتراته تسكن `ReportDefinition.Columns` و `Parameters` — لا `ModuleDefinition.Columns` و `Filters`. `ReportRenderer` يقرأ الأولى وحدها.

---

## ٢) شاشة سجلّ (كيان له قائمة وحوار)

| # | الملف | ماذا |
|---|---|---|
| ١ | `3.Domain/Entities/` | كيان: خصائص فقط |
| ٢ | `2.Data/Repositories/` | يرث `RepositoryBase<T>` · `Shape` شرطاً · `By`/`DocumentOrder` ترتيباً — LINQ فقط |
| ٣ | `4.Application/DTOs/` | `XDto` · `CreateXDto` · `UpdateXDto` · `XFilter` |
| ٤ | `4.Application/Validation/` | لا ملفّ: شروط `Field<T>` معاملاتٌ إلى `Check.Valid` |
| ٥ | `4.Application/Legacy/<القسم>/` خدمة الصفحة، ومنطقها حالة استخدام في `4.Application/Services/` | يرث `ServiceBase` ويُعلن الثلاثة |
| ٦ | `6.UI/ViewModels/` | يرث `CrudViewModelBase<TDto,TFilter>` ويُعلن `PermissionPrefix` فقط |
| ٧ | `8.Modules/ModuleRegistrations.cs` | `ModuleDefinition` بأعمدتها وحوارها |
| ٨ | الشروط الثلاثة | الخريطة + القاموسان + التسجيل |

المصانع وما تأتي به الوراثة: `RULES.md § إضافة خدمة` و `§ إضافة شاشة`.

---

## ٣) زرّ على شاشة

لا يُبنى زرّ: الكتالوج `ToolbarAction.Catalogue` فيه الاثنا عشر (جديد · تعديل · حذف · حفظ · إلغاء · طباعة · تصدير · تحديث · ترحيل · إلغاء ترحيل · توسيع · طيّ).

- **إظهار وإخفاء**: `EnabledActions` على الإعلان — بلا إعلان تظهر كلها. يُطبَّق في `ToolbarActions.Enabled` الذي تستورده القائمة والشجرة.
- **زرّ بمنطق خاص على الصفّ**: `RowActions` على الإعلان.
- **من وحدة البناء**: صفٌّ في `BuilderActions` — يظهر أثره في الصفحة الحقيقية بعد إعادة التشغيل.
- **صلاحيته**: `PermissionKey` على الزرّ؛ `ActionToolbar` يُخفي ما لا صلاحية له.

**يُمنَع**: زرّ مكتوب في مُصيِّر واحد — يسري في شاشة ويسقط في أخرى.

---

## ٤) فلتر على شاشة

`Filters` على `ModuleDefinition`. القطعة والقائمة والقيمة كلها من مصنع حقول الحوار عبر `FilterControls`، تستورده صفحة القائمة وصفحة الشجرة معاً.

- **قائمة**: `PickerType` — نفس أنواع الحقول (`Account`, `Category` + `PickerCategoryModuleKey`, `Product`, `Warehouse`, تعدادات النظام، `Table:<وحدة>:<عمود>` لجدولٍ مبنيّ).
- **تبديلي**: `Kind = FilterKind.Toggle` فيصير مربّع تأشير.
- **فلترٌ يحكم فلتراً**: `PickerFilterField` (القسم ← صفحاته).
- **من وحدة البناء**: صفٌّ في `BuilderFilters`؛ ولو طابق مفتاحُه حقلاً مُعلَناً في الشاشة أخذ قائمته وعنوانه منه.

**ولا يعمل الفلتر إلا إذا كان لمفتاحه خاصيةٌ في `XFilter` يقرؤها المستودع.**

---

## ٥) حقل جديد على كيان قائم

السلسلة كاملة وإلا ظهر الحقل ولم يُحفظ:

```
الكيان (خاصية) → المستودع إن لزم شرطٌ جديد
       → DTOs (Dto/Create/Update) → الخدمة: بناء الكيان + ToDto
       → المتحقّق إن لزم → حقل في الحوار → عمود في الشبكة → نصّان
```

خاصيةٌ على الكيان تكفي: `PrimeDbContext` يُولَّد من الجداول القائمة، و`SchemaSync` يُلحق العمود الناقص عند الإقلاع — فلا مهاجرة يدوية ولا `INSERT`/`UPDATE` تُكتب.

---

## ٦) عمود على شبكة قائمة

- **دائم**: `Columns` في الإعلان.
- **من وحدة البناء**: صفٌّ في `BuilderColumns` — يعلو على الإعلان: تعديله أو إضافته أو حذفه يظهر في الشاشة الحقيقية وفي أي نسخة، والعمود يحتفظ بما لا يصفه الوصف (قالب الخلية، المحاذاة).
- **عرضه نسبةً**: `WidthPercent` — نسبةٌ واحدة تجعل الجدول نجميّاً. الحساب في `LayoutCalc.Shares` تقرؤه الشاشة والمُحمِّل معاً.

---

## ٧) قسم أو صفحة جديدة من وحدة البناء، بلا كود

**أقسام**: كود وعنوان وأيقونة وترتيب.
**صفحات**: النوع أولاً (سجل · حركة · تقرير) فهو يحكم ما بعده، ثم القسم، ثم المفتاح والجدول. ثم **الأعمدة** (نوع كل عمود، ومرجعه إن كان «من جدول»، ومحسوباً إن كان تجميعاً)، ثم **الأزرار** من الكتالوج، ثم **الفلاتر**.

الجدول يُنشأ عند الإقلاع (`EnsureBuiltTable`)، والصلاحيات تُولَّد (`PermissionKeys.RegisterBuilt`)، والشاشة تمرّ من `PageRenderer` كأي شاشة مكتوبة.

---

## ٨) خدمة جديدة

راجع `RULES.md § إضافة خدمة` — الخطوات الستّ، وما تأتي به الوراثة، وما يُمنَع داخلها.

---

## ٩) عملية طويلة (نسخ، نسخة احتياطية، تصدير ضخم)

راجع `ARCHITECTURE.md § العمليات الطويلة`. حوارٌ بدائرةٍ بلا نهاية مرفوض.

---

## ١٠) تغيير مظهر أو نصّ

راجع جدول `RULES.md § أين يُصلَح كل نوع من الخطأ`. وصفر لون أو مقاس مكتوب في قطعة مرئية — كلها `DynamicResource`.

---

## ١١) تعديل موجود

1. افحص الآلية الصحيحة لهذا النوع أولاً.
2. بناءٌ في غير موضعه أو نسخةٌ يدوية من مشترك: يُحذَف ويُعاد بناؤه سليماً.
3. اضطررتَ لتعديل أكثر من موضع لتغييرٍ واحد؟ المصدر المشترك غائب — ابنِه ثم استورده.
4. المؤقّت يُحذَف فور اكتشافه.
