# ARCHITECTURE.md — أين يسكن كل شيء

## الشجرة

```
PrimeERP/
├── App.xaml(.cs)      يبقيان في الجذر — قيد مُصرِّف XAML
├── 1.Platform/        Security/ Permissions/ Audit/ Localization/ Settings/ Net/ Design/
├── 2.Data/            Core/ Repositories/ Seeders/
├── 3.Domain/          Entities/ Enums/ Calculations/ Results/ Contracts/ Helpers/
├── 4.Application/     Services/ (المنطق العامّ) Legacy/ (الصفحات تستدعيه) Validation/ (Check) DTOs/ Reporting/
├── 5.Design/          Colors.xaml Sizes.xaml Theme.xaml + Styles/ Icons/ Strings/ Surfaces/
├── 6.UI/              Components/ Converters/ Services/ ViewModels/
├── 7.Composition/     Definitions/ Renderers/ Registry/ Pull/ Print/
├── 8.Modules/         تسجيل الوحدات والتقارير + مصانعها
├── App/               Bootstrap/DependencyInjection.cs، MainWindow (Shell)
├── PrimeERP.Tests/    الاختبارات
├── PrimeERP.Setup/    المنصِّب المستقل
├── Tools/             فحص المعمارية وتوليد الوثائق والرموز
├── docs/              الفهرس والمخططات — مولَّدة
└── server/            خادم التراخيص والتحديث (Python)
```

## انتماء الطبقات

| الطبقة | تحتوي | يُمنع أن تحتوي |
|---|---|---|
| **1.Platform** | بنية تحتية عابرة: صلاحيات، تدقيق، لغة، إعدادات، هوية بصرية | منطق أعمال، أي مرجع لـ 4-8 |
| **2.Data** | مزوّدو القواعد، نموذج EF، المستودعات، أشكال الاستعلام | أي قرار أعمال |
| **3.Domain** | كيانات، تعدادات، صيغ الحساب النقية (`Calculations`)، `Result` | أي استدعاء قاعدة بيانات، أي مرجع لأي طبقة — النقيّة الوحيدة |
| **4.Application** | المنطق العامّ (`Services`)، الصفحات التي تستدعيه (`Legacy`)، التحقق، DTOs، التقارير | أي مرجع لـ 5-8 |
| **5.Design** | XAML فقط: رموز بصرية | أي C# منطقي |
| **6.UI** | القطع المرئية، محوّلات، خدمات واجهة، أسس ViewModel | استدعاء مستودع، منطق أعمال |
| **7.Composition** | التعريفات (بيانات)، المُصيِّرات (تجميع)، السجلّ | منطق أعمال، XAML لصفحة بعينها |
| **8.Modules** | تسجيل كل وحدة عمل إعلاناً | أي منطق أعمال |

**اتجاه الاعتماد**: من أعلى الرقم إلى أدناه فقط. `3.Domain` و `5.Design` لا تعتمدان على شيء. يفحصه `check.sh` آلياً.

**عكس الاعتماد**: ما تحتاجه `1.Platform` من القاعدة تُعلنه عقداً عندها — `ISettingStore` و `IPermissionStore` و `IAuditStore` — وتنفّذه `2.Data/Repositories`، فلا تعرف الطبقة الأولى طبقةً أعلى منها.

---

## دورة الحياة

```
ModuleDefinition  (إعلان في 8.Modules)
        ↓
IModuleRegistry.Register ─── بيان النسخة (UI.Manifest) يرفض هنا، وهنا وحدها
        ↓
   ├── NavigationSource   → القسم في الشريط الجانبي
   ├── PermissionKeys     → مفاتيح الصلاحية (تُولَّد بعد التسجيل لا قبله)
   └── PageRenderer       → التصيير حسب LayoutKind
```

| العنصر | أين يُعلَن | هل يدخل بيان النسخة؟ |
|---|---|---|
| قسم | `NavigationMap.Coded` أو صفّ في `BuilderSections` | لا — يختفي كنتيجة لخروج كل صفحاته |
| صفحة | `ModuleDefinition.Key` | نعم — الوحيد الذي يُحصَر بمفتاحه |
| زرّ | `EnabledActions` على الإعلان | لا — خاصية على الصفحة |
| فلتر | `Filters` على الإعلان | لا — خاصية على الصفحة |
| صلاحية | تُولَّد بعد التسجيل | لا — ما لم يُسجَّل فلا مفتاح له |

**بيان النسخة** (`SettingKeys.UI.Manifest`): مفاتيح الصفحات مفصولة بفاصلة، يقرؤه `RegisterModules` قبل أي تسجيل. فارغٌ يعني النظام كاملاً. النسخة المنشأة تُقلع به فتظهر أقسامها وحدها — حصرٌ بالعرض والصلاحية لا حذفٌ للكود، وملفات النسخة تحوي الوحدات كلها.

**بذر صفحات الكود**: كل صفحة مكتوبة تصير صفّاً في `BuilderModules` بأعمدتها وأزرارها وفلاترها، مؤشَّرةً `IsCoded` — تُعرَض وتُرتَّب وتُحذَف من وحدة البناء، ولا يُنشأ لها جدول. والبذر يقرأ مفاتيح القسم من الخريطة لا من عمود الصفّ، فأي شاشة تُضاف تُبذَر في الإقلاع التالي.

**علوّ الوصف على الصفحة المكتوبة**: `ModuleDefinition` و `GridColumn` سجلّان (`record`)، فتُعاد الصفحة المكتوبة إلى التسجيل بـ `with` وقد أُخذت أعمدتها وأزرارها وفلاترها من صفوف الوصف، بينما يبقى نموذج عرضها وحوارها من الكود. العمود الذي له صفّ يأخذ منه عنوانه وعرضه وإجماليه ويحتفظ بما لا يصفه الوصف (قالب الخلية، المحاذاة)؛ وصفٌّ بلا عمود عمودٌ مُضاف؛ وعمودٌ بلا صفّ محذوف.

**عرض العمود نسبةً**: نسبةٌ واحدة مُدخَلة تجعل الجدول كلَّه نجميّاً (`IsStarWidth`)، فيبقى تناسبه واحداً على أي عرض شاشة وفي أي نسخة. الحساب في `LayoutCalc.Shares` (3.Domain/Calculations) تقرؤه الشاشة والمُحمِّل معاً، فالمعروض هو المطبَّق.

---

## تدفّق البيانات

مسارٌ واحد تقرأ به كل شاشة، ولا مسار ثانٍ:

```
Shape + Order                      (المستودع يصف شرطه وترتيبه بـLINQ)
        ↓
RepositoryBase.Page(...)           ← العدّ والقطع هنا وحدهما
        ↓
CrudServiceBase.GetPaged           ← المدخل الوحيد، ويقطع ما تجاوز الصفحة
        ↓
PagedViewModelBase.FetchPage       ← يملك الصفحة والحجم والبحث والفرز
        ↓
AppDataGrid + AppPagination
```

**`RepositoryBase.Page(page, pageSize, filter, order)`**: العدّ ثم `Skip/Take` بتنفيذٍ واحد لكل المستودعات، و`Id` فاصلٌ أخير يُضاف هنا. المستودع يمرّر فرقه وحده: شرطه دالة `Shape`، وترتيبه `By(key, descending)` أو `DocumentOrder(key, descending, number)`. وعمود فرزٍ غير مُعلَن يسقط على الحالة الافتراضية في `switch` فلا يصل إلى الاستعلام. وشرطٌ يحتاج جدولاً آخر (البحث باسم الموظف مثلاً) يفتح السياق ويمرّره إلى `Page` فيبقى المسار واحداً.

**الصفحة تُحوَّل دفعةً واحدة**: `CrudServiceBase.ToDtos` يحوّل الصفحة كلها، والخدمة التي يحتاج عرضها جدولاً آخر تجلبه للصفحة باستعلامٍ واحد (`NamesOf` · `GetByIds` · `ByCodes` · `AccountsWithLines` · مجاميع السطور) ثم تحوّل. فلا `ToDto` يستعلم لكل صفّ، ولا يُنادى من داخل التحويل خدمةٌ أخرى.

**قطعٌ مضمون في الأساس**: `CrudServiceBase.GetPaged` يقطع ما أرجعته الخدمة إن تجاوز حجم الصفحة، قبل `ToDto`. فخدمةٌ ترقّم في الاستعلام تمرّ كما هي، وخدمةٌ تُرجع القائمة كاملة لا تستطيع إغراق شاشة ولا إرجاع نفس الصفحة عند التنقّل.

**قائمة مرجعية بلا ترقيم**: نموذج العرض الذي يحتاج القائمة كاملةً (وحدات، مخازن، أدوار) يُعلن ذلك بحجم صفحةٍ كبير — لا بتزوير `TotalCount`.

---

## الكيان والـDTO

الكيان هو الصفّ والمُدخل. الـDTO يُكتب حيث يختلف شكل البيانات عن الكيان، لا نسخةً من حقوله.

| القاعدة | موضعها |
|---|---|
| الصفحة التي مُدخلها بعض حقول كيانها تأخذ الكيان نفسه: قراءةً وإنشاءً وتعديلاً، بلا `XDto`/`CreateXDto`/`UpdateXDto` | عقد الخدمة وأساسها (`EntityService<X,X,X,X,XFilter>`)، ونموذج العرض، و`typeof`/`nameof` الكيان في `8.Modules` |
| حقل العرض (اسم مرجع، رصيد، فرق، نصّ نوع) خاصيةٌ على الكيان يتجاهلها EF باسمها في `DERIVED` | الأسماء يملؤها المستودع بضمّةٍ واحدة (`WithNames`/`WithCategoryNames`، وكود المرجع واسمه `WithCodeNames` في كل قراءةٍ تُعرض: الحضور والحركات بموظفها، وحركات الأصل الثلاث بأصلها) أو `ToDtos` (`NamesOf`)، والأرقام صيغٌ من `Calculations` في `ToDtos` |
| حقول النظام لا يكتبها تعديل المستخدم | المستودع: `Modify(entity, db, keep…)` يعلّمها غير معدَّلة (الرصيد، الكود، القيد، المُهلَك). وما يكتبه النظام نفسه يمرّ بدالّةٍ أخرى (`AssetRepository.Update` للإهلاك والتقييم مقابل `Edit` للمستخدم). وحسابات الكيان يعيدها `AddEntityAccount.Keep` من المخزَّن حين يأتي المُدخل بلا حساب |
| الحوار يبدأ التعديل من السجل | `DialogRenderer`: إن أرجع `GetById` نوع التعديل نفسه حُمِّل السجل ثم طُبّقت عليه حقول الحوار، فلا يُصفَّر حقلٌ غير ظاهر |
| الـDTO في ثلاثة مواضع وحدها | المستندات (سطورٌ مكتوبة الأنواع، إدخالٌ بالكود، روابط سحب) · المُدخل المختلف شكلاً (شجرة الحسابات، المستخدم بكلمة مروره، الحضور والحركات بكود الموظف، السند والشيك والقيد والرواتب والترخيص) · إخفاء السرّ (`UserDto`). ومعها المرشِّحات `XFilter` |

---

## العمليات الطويلة

كل عملية تتجاوز ثانية تُنفَّذ خارج خيط الواجهة، بحوار تقدّم، وبنسبة محسوبة لا دائرة بلا نهاية.

الآلية الوحيدة: `IDialogService.ShowProgress(title, message)` تُرجع `IProgressHandle` (فوق `AppProgressDialog`). الخدمة ترفع تقدّمها بـ `IProgress<T>` ولا تعرف الواجهة، والمُصيِّر يُشغّلها بـ `Task.Run` ويعرض الحوار. المرجع العامل: `ProgramEditionService`.

---

## القيم البصرية — ثلاث مناطق

| المنطقة | تُدمَج في `App.Resources`؟ | تخدم |
|---|---|---|
| `5.Design/` (`Colors.xaml` + `Sizes.xaml` ← `Theme.xaml` ← `Styles/`) | نعم | الشاشة |
| `5.Design/Surfaces/PrintTheme.xaml` | لا — يحمّلها `PrintService` وقت البناء | المستندات المطبوعة (`FlowDocument`) |
| `5.Design/Surfaces/ExportTheme.cs` | لا — ثوابت C# لا XAML | ملفات Excel/CSV/PDF المُصدَّرة |

الطباعة والتصدير منفصلتان عن الشاشة: الورق لا يرث أسطحها ولا تباينها. والتصدير ثوابت C# لأن ClosedXML و QuestPDF لا يقرآن `ResourceDictionary`.

**المجموعات الدلالية الست** (`Neutral` · `Info` · `Brand` · `Success` · `Warning` · `Danger`) مُعرَّفة في المناطق الثلاث بنفس القيم الفعلية. لا قيمة تُخترع في منطقة، ولا لون دلالي سابع.

**`StatusVariant` (3.Domain/Results)** هو مفردات الحالة المقفلة، وتصنيفُ كل حالة أعمالٍ إليها مكتوبٌ في التعداد نفسه. توزيع المسؤولية: الخدمة والمستودع والمتحقّق والكيان تُرجع `StatusVariant` ولا تعرف لوناً؛ نموذج العرض يحوّل حالة الأعمال إليه ولا يُرجع `Brush`؛ والتحويل إلى لون في `6.UI/Converters/VariantToBrushConverter.cs` للشاشة، وفي `PrintTheme`/`ExportTheme` للورق والملفات.

---

## الأصول الثابتة — الدورة

```
اقتناء      → مدين «الأصول الثابتة» / دائن الخزينة (نقداً) أو المورد (آجلاً)
إهلاك       → مدين «مصروف الإهلاك» / دائن «مجمّع الإهلاك»
              تشغيلةٌ واحدة تشمل كل الأصول، وتُنتج قيداً لكل أصلٍ عن كل شهرٍ مستحقّ بتاريخ ذلك الشهر
              (أصلٌ تأخّر خمسة أشهر ← خمسة قيود)، فيقرأ كشف الحساب الإهلاك موزّعاً على شهوره لا
              مجمَّعاً في يوم. وسطر التشغيلة يحمل قيده وتاريخ الأصل السابق، فالحذف يعكس الكل بالضبط
إعادة تقييم → زيادة: مدين الأصل / دائن «أرباح رأسمالية»
              نقص:  مدين «خسائر رأسمالية» / دائن الأصل
              ولا تُعاد القيمة إلى صفر: الأصل الذي تسقط قيمته يُستبعد
استبعاد     → مدين الخزينة بالثمن ومجمّع الإهلاك / دائن الأصل، والفرق ربحٌ أو خسارة رأسمالية (DisposalEntry)
حذف         → يعكس قيد الحركة نفسها في معاملة واحدة
تعديل       → عكس الحركة القديمة وإنشاء الجديدة في معاملةٍ واحدة: فشل الجديدة يُبقي القديمة
```

تشغيلة الإهلاك معاملةٌ واحدة: كل الأقساط المستحقّة أو لا شيء.

| القيمة | كيف تُحدَّد |
|---|---|
| تكلفة الشراء | تُدخَل — التكلفة التاريخية، لا تتغيّر أبداً |
| قيمة الخردة · العمر الإنتاجي | تُدخَلان — أساس القسط: (القيمة − الخردة) ÷ (العمر × ١٢) |
| القيمة بعد إعادة التقييم | تتغيّر بإعادة التقييم وحدها، وهي أساس الإهلاك بعدها، وموجبةٌ دائماً (`Str.Asset.RevaluationZero`) |
| مجمّع الإهلاك | محسوب من التشغيلات المُرحَّلة |
| القيمة الدفترية | محسوبة: `AssetCalc.CurrentValue` = أساسها (المُعاد تقييمها، وإلا التكلفة في الصفوف القديمة) − مجمّع الإهلاك. لا تُدخَل ولا تُعدَّل بحقل |

الصيغ النقيّة في `AssetCalc` (`3.Domain/Calculations`) — قسطٌ ودفتريّةٌ وقيمةٌ حاليّة وقابلٌ للإهلاك وفرقُ تقييم — تستوردها خدمةُ الاحتساب وخدمةُ إعادة التقييم والتقارير، فيبقى الرقم واحداً. والترحيل والعكس في خدمة كل حركة تستدعي `Posting` مباشرة: `AssetService` للاقتناء، و`AssetRevaluationService` و`AssetDisposalService` لما بعده، و`AssetDepreciationService` يعكس قسطه ويستدعي `DepreciationCharges` لترحيله وللتشغيلة ولإعادة المُهلَك، وتعطيل الأصل وإعادته بالاستبعاد `IAssetRepository.SetActive`.

**شجرة الحسابات**: `1101` صافي الأصول الثابتة، أبٌ لجذرَين تجميعيَّين — `1101001` (التكلفة) تسكنه الفئات وتحتها أصولها، و `1101002` (مجمّع الإهلاك) تسكنه مرايا الفئات وتحتها مجمّعات أصولها. الجذران مضبوطان في `SettingKeys.Accounts.FixedAssets` و `AccumulatedDepreciation`.

**الأرباح والخسائر الرأسمالية بندان غير تشغيليَّين**: تحت «إيرادات أخرى» (42) و«مصروفات أخرى» (52)، فلا تدخل الربح الإجمالي ولا الربح التشغيلي، وتظهر أسفل قائمة الدخل قبل صافي الربح.

---

## المصائد

سلوكٌ لا يُستنتَج من الكود ولا من التوثيق، ثمنُ تجاهله كسرٌ صامت. **النتيجة وحدها** — لا تاريخ المحاولات.

| المصيدة | القاعدة |
|---|---|
| حوارٌ محجوب | `Window.ShowDialog()` تُعلَّق للأبد في بيئة هذا الجهاز. البديل: `Show()` + حلقة `DispatcherFrame` يدوية. و`ShutdownMode=OnExplicitShutdown` لهذا السبب |
| موارد مركّبة مجمَّدة | مورد يشير لمفتاحٍ متداخل (`DropShadowEffect` مثلاً) يتجمّد على أول قيمة رآها. لذا: `Application.Resources` يُعيَّن قاموساً فارغاً أولاً، وملفات الهوية تُدرَج **قبل** `Theme.xaml` في خطوة بناء `MergedDictionaries` نفسها |
| فرشاةٌ شفافة | `FindResource` على فرشاة `DynamicResource` يُعيد لقطةً تُرسَم شفافة. البديل: `SetResourceReference` |
| لونٌ لا يُشتقّ | WPF لا يُعيد تصدير `Color` من مفتاح `Brush`. لذا `RefreshDerivedColors` يشتقّ `{Name}.Color` من **كل** فرش الشجرة المدموجة، لا من قائمة أسماء يدوية |
| مخطَّط `pack://` | لا يُسجَّل إلا بعد إنشاء `System.Windows.Application`. أي طباعة أو اختبار طباعة يضمن ذلك أولاً |
| قفل SQLite | فتح اتصال جديد داخل معاملة مفتوحة على الخيط نفسه يُعلِّق. القراءة داخل معاملة تستعمل حمل `(conn, tx)` حصراً |
| بناءٌ قديم يمرّ | البرنامج المفتوح يقفل الـDLL فيفشل البناء بصمت ويُختبَر كودٌ قديم. وملفٌّ يُستعاد بطابع وقتٍ أقدم من الـDLL يتخطّاه البناء — يُحدَّث طابعه |
| حقلٌ لا يُنسَخ | `Focusable = false` يمنع النسخ من حقل للقراءة فقط |
| حلقة الربط | إنشاء حساب من خدمة طرف يلزمه `SkipAutoLink = true`، وإلا نادى الحسابُ الطرفَ الذي ناداه |
| نموذجٌ مولَّد | `PrimeDbContext.cs` يولّده `Tools/Docs/context.py`: تعديله وحده يضيع عند التوليد. الاتفاقيات العامّة في `ModelConventions.cs`، وسطر استدعائها في الملف المولَّد وقالبه معاً. وحقل العرض يُعلَن في `DERIVED` باسمه فيُتجاهَل في **كل** الكيانات: لا يُسمّى باسم عمودٍ حقيقي في كيانٍ آخر (`AccountBalance` لا `Balance`)، وسطر `Ignore` في الملف المولَّد بترتيب الخصائص الأبجدي |
| جملةٌ بلا ختم | `ExecuteUpdate`/`ExecuteDelete` لا يمرّان بـ`SaveChanges`: `Set` يختم `UpdatedAt` بنفسه، والحذف الناعم يمرّ بـ`SoftDelete`/`SoftDeleteIn` لا بـ`Set(IsDeleted)` حتى يُختم وقت الحذف ومنفّذه |
| تتبّعٌ باقٍ | سياقٌ مُعار يبقى متتبّعاً ما كتبه، فكتابة الصفّ نفسه مرّتين بنسختين في معاملة واحدة ترفع `InvalidOperationException` يحوّلها `Commit` رسالةً صامتة. لذا `Write` و`Add` يُفرغان التتبّع بعد `SaveChanges` |
| نصٌّ بلا قاموس | `LocalizationService.Get` يُرجع المفتاح نفسه ما لم يُحمَّل `Strings.*.xaml` في `Application.Current`. في الاختبارات يُحمَّل عند أول اختبار WPF ويبقى للعملية كلها، فالرسالة عربيةٌ أو مفتاحٌ بحسب الترتيب. لذا تُقارَن الرسالة بقالبها عبر `Localized.Says` |
| قيمةٌ مخزَّنة لا نصّ | `"يدوي"` في `Entries.ManualSources` مصدر قيودٍ قديمة يُطابَق عليه، لا نصٌّ يُعرض، فيبقى حرفياً. وألفاظ العملة الافتراضية للتفقيط داخل `ArabicNumberToWords` لأنها مدخل نحوٍ عربي لا واجهة |
| نسخٌ بالاسم | `Rows.Copy` تقرأ الخصائص بالانعكاس: عناصر الـtuple ليست خصائص فلا يُنسخ منها شيء، وعناصر WPF تحمل عشرات الخصائص القابلة للكتابة فلا تُنسخ بها. والـDTO لا يعطي الكيان معرّفه — `Id` يُنسخ من كيانٍ لا إليه من DTO — وما يُحسب يُكتب في دالّة `Copy` الثالثة بعد النسخ فلا تطمسه. وكيانٌ إلى كيان ينسخ المعرّف والأختام معه، فلا يُنسخ كيانٌ ليصير صفّاً جديداً |
| بديلٌ بمعرّف قديم | حركة الأصل تُعدَّل عكساً ثم إدراجاً، والمُدخل المحمَّل يحمل معرّف القديم وأختامه: إدراجه كما هو يصطدم بالصفّ المحذوف منطقياً. `AssetMovementServiceBase.Replace` يصفّرها قبل `Write` |
| رؤية المحذوف | `IgnoreQueryFilters()` يسري على الاستعلام كلّه لا على جدولٍ داخله، فضمّةٌ تريد اسم مرجعٍ محذوف تُظهر معه صفوف الجدول الأصلي المحذوفة. يُستعمل حيث يُقصد المحذوف كلّه: مفاتيح البذر (`SectionKeys`/`ModuleKeys`)، ومسح جدولٍ كاملاً |

---

## قرارات مثبتة

سطرٌ لكل قرار: ما هو، ولماذا لا يُعكَس.

| القرار | لماذا |
|---|---|
| القيد يُنشأ مُرحَّلاً | لا زرّ ترحيل ولا قفل يمنع التصحيح — طبقات التحقق تسري عليه من لحظة إنشائه |
| الحارس عند الأبواب | تحقّق الرصيد في مدخل الخدمة (إنشاء/تعديل/حذف) لا في عمق الحذف: الحارس في العمق يرفض بلا أن يسمعه المستند، فيُحذف المستند ويبقى قيده |
| النسخة تُحصر ببيانها | `UI.Manifest` يحصر العرض والصلاحية. لا يُمسح جدولٌ من قاعدة النسخة: المسح يحذف حسابات تصفها البنية، ويُفرّغ العملاء فتفتح بوابة بيانات التجربة |
| الوصف يعلو على الصفحة المكتوبة | صفٌّ بلا عمود = عمودٌ مُضاف، وعمودٌ بلا صفّ = محذوف. فما يبنيه المستخدم يحكم ما كتبه المبرمج |
| العدّ والقطع في موضع واحد | `RepositoryBase.Page` وحده، و`CrudServiceBase.GetPaged` يقطع ما تجاوز الصفحة — فلا ترقيم موازٍ ولا شاشة تُغرَق |
| الكيان المرتبط يكتب الطرفين | خزينةٌ أو عميلٌ يُعدَّل أو يُحذف من صفحته يكتب حسابه في المعاملة نفسها — منطق الشجرة والصفحة واحد، لا اتجاه واحد |
| الصفحة تستدعي الصفحة | مستند الشيكات يستدعي صفحة الشيكات باتجاهه، والأرصدة الافتتاحية تستدعي صفحة القيود بمصدرها، والتقرير يستدعي الصفحة التي تجمع بياناته: استدعاءٌ واحد بدل إعادة استدعاء خدماتها، والمنطق نفسه في `Services` |
| الحذف تعطيل لا إزالة | الصفّ يبقى وكوده محجوز، فلا يُعاد استخدام كودٍ محذوف. وحذف آخر ابنٍ يعيد الأب لحالته: بلا أبناء ⇒ يقبل القيود |
| التمويل شرطٌ في الإنشاء والتعديل | تعديل الأصل يرفض تمويلاً لا يُحلّ حسابه كما يرفضه الإنشاء، فلا يُعاد ترحيل قيد الاقتناء على تمويلٍ قديمٍ غير الذي اختاره المستخدم |
| التعديل استبدالٌ ذرّي | المستند وحركة الأصل يُعدَّلان بعكس القديم وإنشاء الجديد في معاملةٍ واحدة (`DocumentService.Update` · `AssetMovementServiceBase.Replace`)، فلا يضيع المستند إن فشل الجديد |
| القيد بابه الفترة | `Entries.Create(db, …)` يرفض تاريخاً في فترةٍ مقفلة إلا قيد إقفال السنة، و`Entries.CanRemove`/`CanUpdate`/`CanUnpost` ترفض عكسه أو تعديله؛ فكل مستندٍ يمرّ بالباب نفسه لا بنسخته |
| رصيد الطرف رصيد حسابه | `AccountBalances` يكتب رصيد الحساب ورصيد طرفه معاً (`SetBalanceByAccount`) مع كل قيدٍ يُرحَّل أو يُلغى ترحيله أو يُستبدل أو يُحذف (`Entries.Recalculate`) داخل المعاملة نفسها: رقمٌ واحد في موضعٍ واحد، فلا يتأخّر الطرف عن قيدٍ يدوي ولا افتتاحي ولا مستقبلي، ولا تحدّثه صفحة |
| إقفال السنة من حركتها | قيد الإقفال يُبنى من `GetAccountSums` بين بداية السنة ونهايتها، لا من الرصيد التراكمي، فقيدٌ بتاريخ السنة التالية لا يدخله |
| الترتيب قطعيّ | المستند بتاريخه ثم رقمه ثم وقت إنشائه، و`Id` آخر فاصل دائماً — فلا ترتيبٌ يتبدّل بين تشغيلتين. موضعه `RepositoryBase.DocumentOrder`، وفرز المستخدم يسبقه ولا يُلغي فواصله |
| الحوار يبدأ التعديل من السجل | كائن تعديلٍ فارغ تملؤه الحقول الظاهرة وحدها يُصفّر عند الحفظ ما لم يظهر في الحوار. البدء من `GetById` يحفظ الصفّ كلّه، وحقول النظام يحميها المستودع فوق ذلك |
| قيد الاقتناء يُعاد بمدخلاته وحدها | `AcquisitionChanged`: التكلفة والتاريخ وجهة التمويل وطريقة الاقتناء هي مدخلات القيد كلها، فتعديل الاسم لا يمسّ قيداً في فترةٍ مقفلة ولا يغيّر رقمه. وحين يُعاد يُفحص عكسه في `Prepare` قبل `Commit` (`Posting.EnsureReversible`)، لأن الفحص يقرأ باتصالٍ جديد |
| حركة الأصل على أساسها لا على `DocumentService` | `DocumentService.Update` يفحص قبل حذف القديم، وحركة الأصل تفحص بعد عكسه داخل المعاملة (التقييم يقرأ القيمة القديمة بعد العكس، والاستبعاد يفحص «سبق استبعاده»)، فالنقل يكسر التعديل |
| تكلفة المرتجع تتقدّم بما يُسجَّل | `StockMove.GetReturnCosts(…, recordsStock)`: تكلفة كل سطر من الحركات القائمة، وحركة السطر السابق لا تدخل الرصيد إلا حين تُسجَّل (التدفّق المبسّط)، فلا تُحسب تكلفةٌ كأن البضاعة دخلت وهي لم تدخل |
| السحب يُفحص بمجموعه ويُحمَل في التعديل | `DocumentPull.ValidatePulls` يجمع كميات كل سطر مصدر ويقارنها بالمتبقي مستثنياً المستند المُستبدَل (`GetPulledQty(…, exceptTargetType, exceptTargetId)`)، و`CycleDocumentLineDto` يحمل ربطه فيعيده `DocumentRenderer` إلى السطر المحمَّل، فلا يقطع التعديل الربط ولا يتجاوز سطران المتبقي |
| الطباعة في طبقة الواجهة | `PrintService` وقطعه عناصر WPF، فمكانها `6.UI/Services` لا طبقة المنطق؛ ولا `7.Composition` لأن `6.UI` تستعملها |

---

## المبنيّ فعلاً

### 3.Domain
كيانات صرفة، وعقودها المشتركة في `Entities/Common` (`IEntity` · `IProductLine` · `IEmployeeLine`) · تعدادات · `Result`/`PagedResult` · `StatusVariant` · `Calculations/` — الموضع الوحيد لصيغ الحساب، تستدعيها الخدمة بعد الجلب ولا تجلب شيئاً، والصيغة الجديدة تدخل ملفّ نوعها: `LineCalc` (السطر ومجاميعه) · `PayrollCalc` · `AssetCalc` · `InventoryCosting` · `StatementCalc` (القوائم، والرصيد الجاري `Running`، وتجميع الأبناء `Rollup`) · `FiscalPeriodCalc` · `PartyCalc`. الكيان بيانات فقط بلا خصائص محسوبة، وحقل العرض عليه (`DERIVED`) يملؤه المستودع أو `ToDtos`، والتحقق ليس هنا.

### 2.Data
`PrimeDbContext` (نموذج EF لكل الجداول) · `DbContextFactory` (سياقٌ فوق `(conn, tx)` القائمين، وخياراته مخزَّنة لكل محرّكٍ واتصال) · `BuiltTables` (جداول المستخدم ككيس خصائص وقت التشغيل) · `RepositoryBase<T>` بأشكاله المشتركة (`Fetch`/`One`/`Count`/`Any`/`Write`/`Add`/`Edit`/`Modify`/`Set`/`Remove`/`SoftDelete`/`Page`/`By`/`DocumentOrder`/`NamesOf`/`GetByIds`/`ByCodes`/`WithNames`/`WithCodeNames`) · `ModelConventions` (اتفاقيات النموذج: المحذوف منطقياً خارج كل استعلام بمرشِّحٍ عامّ واحد فلا `!IsDeleted` في مستودع، وأختام الإنشاء والتعديل والحذف في `SaveChanges` فلا `CreatedBy`/`UpdatedAt` يدوي في مستودع ولا خدمة)

**الكتابة بجملةٍ واحدة**: `Set(match, setters)` فوق `ExecuteUpdate` و`Remove(match)` فوق `ExecuteDelete` (ونظيراهما `SetIn`/`RemoveIn` لجدولٍ آخر) — لا تحميل صفٍّ لتعديل حقلٍ أو حذفه. وتعديل صفٍّ بعدة حقول من كيانٍ يبقى `Edit`/`Modify` بالتتبّع، و`Modify` يأخذ ما لا يُكتب من حقوله.

**المجاميع في SQL**: رصيد الحساب `IJournalRepository.SumPosted`، ومجاميع الحسابات `GetAccountSums`، وكلاهما فوق استعلام السطور المؤرَّخة الواحد؛ وشبكة أرصدة المخزون `IStockMovementRepository.BalanceGrid`. لا تحميل سطورٍ لجمعها في الذاكرة. · `SchemaSync` (يُلحق بالقاعدة ما نقص من النموذج جدولاً وعموداً) · `DbConfig` (الإعداد وسلسلة الاتصال لكل محرّك: `Sqlite`/`SqlServer`/`PostgreSql`، ويختار EF محرّكه منها).

**عقودٌ عامّة بدل عقدٍ لكل كيان**: `ILookupRepository<T>` (+`LookupRepository<T>` بجدوله) للقوائم · `IPartyRepository<T>` للعملاء والموردين · `IInvoiceRepository<TInvoice,TLine>` و `IReturnRepository<TReturn,TLine>` للمستندات — فالمستودع الخاص لا يُكتب إلا لاستعلامٍ يخصّه.

**أسماء المراجع بضمّةٍ واحدة**: `RepositoryBase.WithNames<T>` يملأ اسم المرجع (فئة، قسم، وظيفة…) للصفحة كلها باستعلامٍ واحد لكل جدول، لا باستعلامٍ لكل صفّ. و`WithCategoryNames` حالته للفئات.

**أساسان مشتركان للمستندات**: `StockAdjustmentRepositoryBase` (رأس+سطور بمخزن وتكلفة) و `CycleDocumentRepositoryBase` (رأس+سطور بطرف وسعر بلا أثر مخزني) — يُمرَّر لكلٍّ اسما جدوليه، فكل مستند جديد وارثٌ بسطر.

### 4.Application
**فصل المنطق عن الصفحة**: كل خدمةٍ تستدعيها صفحة (`8.Modules` أو مُصيِّر أو نموذج عرض) تبقى كاملةً في `Legacy/`. و`Services/` منطقٌ عامّ وحده، يُستدعى بالمعاملات ولا يحمل اسم صفحة. يُنقل المنطق من خدمة الصفحة إلى `Services` وتستدعيه، ولا تُنقل الصفحة نفسها.

| المجلد | ما فيه |
|---|---|
| `Services/Core` | `ServiceBase` · `CrudServiceBase` · `EntityService` · الترقيم |
| `Services/Ledger` | `Entries` قلب القيد: شكله (`Shape`، وعدم التوازن فيه وحده برسالة الفرق قبل قاعدتَي السطور)، وإنشاؤه مُرحَّلاً (`CreatePosted`) وحرّاس تعديله وحذفه وترحيله وإلغائه (`CanUpdate` · `CanDelete` · `CanPost` · `CanUnpost`، وملكية المصدر فيها) · `AccountBalances` أرصدة الحسابات وأطرافها (`Refresh`/`RefreshAll`) · `PeriodGate` · `Posting` · `JournalLines` · `TwoSided.By` الطرفان باتجاهٍ واحد · أجسام القيود: `TradeEntry` البيع والشراء وعكسهما · `DisposalEntry` الاستبعاد · `PayrollEntry` استحقاق الرواتب · `ClosingEntry` إقفال السنة · `OpeningEntry` الافتتاحي · `NewFiscalYear` السنة بفتراتها والأولى حاليّة · `DepreciationCharges` القسط بقيده، والتشغيلة كلها، والمُهلَك والقيمة من الأقساط · `Guards` (له أبناء، له قيود، النقدية، حساب النظام، حسابٌ تديره صفحة) · `PartyByKind` العميل أو المورد واسمه بنوعه · `Statement` كشف الحساب برصيده الجاري · `TrialBalance` ميزان المراجعة |
| `Services/Ledger/Accounts` | `AccountOf` حساب الخزينة أو الطرف أو الإعداد، و`SettingBySign` حساب الإعداد وجهته بالإشارة مرّةً واحدة · `RepairAccounts` الكيان بلا حساب: ورقةٌ باسمه في جذره وإلا جديدة، كلٌّ في معاملته · `SettingAccounts` حساب إعدادٍ يُعتمد أو يُنشأ أو يُصلَح · `AccountCases.Root` جذرٌ موجودٌ يقبل الأبناء · `AddTreeAccount` · `AddEntityAccount` · `AddLinkedAccount` · `AddMirroredAccount` · `EditTreeAccount` · `RenameAccount` · `EditLinkedAccount` · `CloseAccount` · `CloseLinkedAccount`، وإعدادها `AccountSpec<T>`، و`LinkedAccounts` كيان الجذر المرتبط — كل كيانٍ مرتبط يعلن `RootKeys` |
| `Services/Documents` | `DocumentService` · `DocumentPull` السحب (`ValidatePulls` المدخل الوحيد لفحصه) · `StockMove` حركة المخزون، وتكلفة الصرف والمرتجع على هيكلٍ واحد `Costs` بخطوة السطر · `StatusChange` الحالة كترحيل · `TradeLines` · `TradeAccounts` · `ProductLines` سطور المستند بأصنافها |
| `Services/Entities` | `Lookup<T>` القائمة البسيطة بإعدادها `EntitySpec` · `Rows` (الكيان صفّاً، `Copy` نسخ الحقول بالاسم، `State` أول حالةٍ يصدق شرطها، `Active` حالة النشاط) · `Tree` (المطابق وأسلافه، والعقد بأبنائها) · `ByCode` سطورٌ تُحلّ بكود كيانها |

`ServiceBase` — كل خدمة ترثه: `Can` · `FailDenied` · `Audit` · `Tx` · `Commit` · `Msg` · `Settings`.

**الأسس**: `CrudServiceBase` (قراءة) ← `EntityService` (كيانٌ وحساباته: إنشاء وتعديل وحذف بحرّاسها) ← `Lookup<T>` (القوائم) و`PartyServiceBase` (العميل والمورد). و`DocumentService` للمستندات: `Plan` يتحقّق ويُرجع دالة الكتابة، والأساس ينفّذها في `Commit`، والتعديل يستبدل في معاملةٍ واحدة، والحذف يمرّ بحرّاسه (سُحب منه، قيده قابلٌ للعكس، `Guard`). عليه: الدورة الأربعة والأذون الستة والتحويل والفواتير والمرتجعات الأربع والسندات والرواتب. والفواتير والمرتجعات تتقاسم تحضيرها في `TradeLines.Prepare` (السطور ثم السحب ثم `TradeAccounts`)، ويبقى لكلٍّ منها جسم قيده وأثره المخزني. و`AssetMovementServiceBase` لحركات الأصول: `Record`/`Replace`/`Delete` فوق `Write` و`Undo`.

**المنطق بالمعاملات**: صفحة `Legacy` تجمع منطقها باستدعاء `Services` ومعاملاتها. القيد: `Entries` يتحقّق ويرقّم ويُدرج ويُرحّل ويحذف ويعيد الأرصدة ويحرس الفترة والنقدية، و`Posting` يستدعيه بسطورٍ أو بطرفين، و`TradeEntry.Lines(partyDebit, …)` قيد البيع والشراء وعكسهما، و`TwoSided.By(forward, first, second)` الطرفان باتجاهٍ واحد للقبض والصرف والزيادة والنقص وجهة الربح. الحساب: `AddTreeAccount` في الشجرة وحدها، `AddEntityAccount` لكيان صفحة، `AddLinkedAccount` من الشجرة فينشأ كيانه، `AddMirroredAccount` حسابٌ ومجمّعه للأصل والفئة، ومعها `RenameAccount` و`CloseAccount` (يعيد الأب ورقياً). والحرّاس في `Guards`.

`Validation/` دالةٌ واحدة: `Check.Valid(item, Field<T>…)`، والشرط معاملٌ في `Field<T>` لا دالةٌ ولا ملفّ لكل كيان، والشرط المشترك بين كيانين مصفوفةٌ واحدة (`DocumentLines` للسطور، و`EmployeeCode.Rules<T>` للموظف بكوده: مطلوبٌ وموجود، عبر `IEmployeeLine`) · `Reporting/` خدمات التقارير.

**الصفحة استدعاء**: صفحة `Legacy` لا تحسب ولا تبني شجرةً ولا تحمل جدول ربط ولا تنسخ حقلاً حقلاً — كلّها قطعٌ عامّة في `Services` أو `Calculations` تستدعيها بمعاملاتها (`RULES.md § الصفحة والمنطق`).

**أقسام `Legacy`**: Accounting · Admin · Assets · Backup · Builder · Cheques · Common · Documents · HR · Inventory · Parties · Purchasing · Sales · Security · Treasury · Vouchers.

### أين يُصلَح المنطق

| النوع | موضعه | من القائم |
|---|---|---|
| صيغة حساب | `3.Domain/Calculations` | `AssetCalc.CurrentValue` · `FiscalPeriodCalc.PreviousOpen`/`NextClosed` · `InventoryCosting` · `PayrollCalc` |
| تحقّق مُدخل | `Validation` (`Check` · `Field` · `DocumentLines`) | `EmployeeCode.Rules<T>` · `PayrollService.LineFields` (الصافي السالب) |
| عملٌ عامّ | `Services` في مجلده (`Core` · `Documents` · `Entities` · `Ledger/Accounts`) | `StockMove.Costs` · `DocumentPull.ValidatePulls` · `RepairAccounts` · `AccountOf.SettingBySign` |
| عملٌ مخصّص | `Services/Ledger` في الغالب | `DisposalEntry` · `PayrollEntry` · `NewFiscalYear` · `DepreciationCharges` · `TradeEntry` · `ClosingEntry` |
| استعلام · مجموع · أسماء · تعديل حقل | المستودع (`Page` · `WithNames` · `WithCodeNames` · `NamesOf` · `Set`) | `SetBalanceByAccount` · `SetActive` · `IdByCode` · `AnyYear` · `GetPulledQty` |
| الصفحة | `Legacy` | صلاحية · جلب · حارس حالة · استدعاء بمعاملات · تدقيق · نتيجة · إعلانات |

خطاف الأساس في الصفحة (`New` · `Prepare` · `OnSaved` · `CanErase` · `Erase` …) إعلانٌ أو استدعاء، ومنطقٌ داخله منطقٌ في الصفحة. والخطاف الذي لا يُعاد تعريفه يُحذف من أساسه. والمستثنى وحده جسم قيود الفواتير والمرتجعات الأربع (`TradeEntry.Lines` ومعاملاته) وأثرها المخزني.

### 5.Design
ملفّان يحملان كل القيم، وملفٌّ يدمج: **`Colors.xaml`** (٧٧ فرشاة بقيمة Hex صريحة) و**`Sizes.xaml`** (المقاسات: خطّ ومسافة واستدارة وظلّ، ثم مقاس كل مكوّن) ← **`Theme.xaml`** يدمج الأيقونات والأنماط ← **`Styles/`** (أنماط WPF، منها ضمنية بلا `x:Key` تُطبَّق تلقائياً). كلها تُستهلَك بـ`DynamicResource` حصراً.

الأسماء بالدور لا بالصبغة (`BrandDefault` لا `Blue600`)، فالقطعة تأخذ من ملف اللون مباشرة بلا طبقة تسمية وسيطة. **وضعٌ فاتح واحد** — لا نسخة داكنة ولا حزم هوية.

**سلّم `Nav`** للشريط الجانبي وحده، لا يُخلط بسلّم الأسطح العام.

**مالك ترتيب الدمج** (راجع «موارد مركّبة مجمَّدة» في المصائد): `IdentityService.Initialize()`.

**الأيقونات**: مقاسها في `Sizes.xaml` ورسمها في `Icons/Icons.xaml`، وقطعة `AppIcon` مستهلكها الوحيد.

### 6.UI
**Display** — `AppDataGrid` (أعمدة/فرز/ترقيم/صف إجراءات) · `AppPagination` · `AppCard` · `AppEmptyState` · `AppLoadingOverlay`
**Inputs** — `AppTextBox` · `AppTextArea` · `AppNumericBox` · `AppDatePicker` · `AppCheckBox` · `AppComboBox` (بحث+ترشيح) · `AppPasswordBox` · `AppSearchBox`
**Actions** — `AppButton` · `ToolbarAction` (New/Edit/Delete/Refresh/Save/Cancel/Print/Export/Post/Unpost/…) · `ActionToolbar` (يُخفي ما لا صلاحية له تلقائياً)
**Feedback** — `AppDialogWindow` (قاعدة كل الحوارات) · `AppConfirmDialog` · `AppMessageDialog` · `AppProgressDialog` · `AppToast`/`ToastService`
**Layout** — `PageHeader` · `FilterBar` · **Shell** — `AppSidebar`/`NavItem` · `AppTopBar` · **Tree** — `AppTreeView` · `TreeNodeViewModel` · **Pickers** — فوق `PickerGridWindow`/`PickerTreeWindow`
**ViewModels/Base** — `PagedViewModelBase<TDto,TFilter>` · `CrudViewModelBase<TDto,TFilter>` · `TreeViewModelBase` · `PermissionAwareViewModel`
**Services** — `ToastService` · `DialogService` · `ExportService` (CSV/Excel/PDF) · `PrintService` (`FlowDocument` من `PrintTheme`) وقطعه `PaperTheme` · `PaperNodeRenderer` · `ChequePrinter` · `CompanyHeaderComponent` · `Code128` · `ImageData` · `IdentityService` · `NavigationService` · `UIServices` (بوابة code-behind)


### 7.Composition
`PageRenderer` نقطة التوزيع الوحيدة حسب `LayoutKind`:

| النمط | المُصيِّر | يُعلَن بـ |
|---|---|---|
| قائمة + CRUD | `CrudPageRenderer` | `Columns` + `Dialog` |
| مستند رأس+سطور | `DocumentRenderer` (وصفحته `DocumentPageRenderer`) | `DocumentDialog` |
| شجرة | `TreeRenderer` + `TreeBuilder` | `TreeOptions` |
| تقرير | `ReportRenderer` | `Report` |
| إعدادات | `SettingsPageRenderer` | `LayoutKind.Settings` |
| شجرة تأشير | `TreeCheckListRenderer` | `TreeCheckList` |
| لوحة شيكات | `ChequeBoardRenderer` | `LayoutKind.ChequeBoard` |

`DialogRenderer` مصنع قطع الإدخال لكل الشاشات: `BuildField` (القطعة من نوع الحقل) · `LoadPickerItems` (القائمة) · `GetControlValue`/`SetControlValue` (القيمة) · `OnChanged` (حدث التغيّر) · `Coerce` (التحويل إلى نوع الخاصية: قابل للإفراغ، تعداد). يستهلكه `CrudPageRenderer` و `DocumentRenderer` لرأس المستند، ويستورده شريط الفلاتر كما يستورده الحوار — فالفلتر التبديلي مربّع تأشير، والفلتر القائمة تقرأ قائمتها من حقل الصفحة المُعلَن بنفس المفتاح. و `FieldValidation` يفحص الرأس والسطور معاً.

**قطع مشتركة بين المُصيِّرات** — كلٌّ منها موضعٌ واحد يستورده كل مُصيِّر يحتاجه: `FilterControls` (شريط الفلاتر) · `ToolbarActions` (ترشيح أزرار الوحدة) · `NavigationSource` (أقسام الشريط الجانبي) · `ListOutput` (طباعة وتصدير) · `FolderOutput` (اختيار مجلد) · `BuilderPickers` (قوائم تعدادات النظام وكتالوج أزراره).

**قطع تعريف جاهزة**: `StandardFields` · `CategoryDialogFactory` · `TradePaper` (أعمدة وإجماليات وحساب سطر الفواتير).

### 8.Modules
تسجيل كل وحدة إعلاناً، عبر مصانع مشتركة: `StockDocumentFactory` · `CycleDocumentRegistrations` · `CycleVoucherRegistrations` · `TreasuryRegistrations` · `RegisterLookup`، وتقارير الأرصدة بدالّة `Register` في `ReportRegistrations`.

**دورتا الشراء والبيع**: `Documents.SimplifiedFlow` (إعداد) يحكم أي المستندات تظهر عبر `ModuleDefinition.FlowScope` و `IModuleRegistry.VisibleFor`. وتتبّع السحب في جدول واحد `DocumentLinks` عبر `IDocumentPull` لا يعرف نوع مستند بعينه، و `DocumentDialogDefinition.PullSources` يصف من أين يسحب كل مستند.

---

## قرارات بلا رجعة في الشكل والتكرار

| القرار | لماذا |
|---|---|
| خدمات الفواتير والمرتجعات الأربع تتقاسم ~٢٥ كتلة ولا تُدمَج | الدمج يمسّ بناء القيد والضريبة والخصم والأثر المخزني معاً: الربح شكليّ والمخاطرة في قلب المحاسبة |
| `AppTextBox`/`AppPasswordBox`/`AppTextArea` تُعيد تعريف خصائص الاعتماد | وراثة XAML مع إعادة تسجيل خصائص الاعتماد تكسر الربط — قيدٌ في WPF لا خيارٌ لنا |
| قيمةٌ بصرية داخل مكوّنٍ واحد تبقى رقماً | التوكن يُبنى لما يتقاسمه مكوّنان؛ واسمٌ بلا مستهلكٍ ثانٍ تصنيفٌ يطيل الطريق بلا فائدة. و`check.sh` يرفض أي قيمة تتكرر عبر ملفين |
