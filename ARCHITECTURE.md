# ARCHITECTURE.md — أين يسكن كل شيء

## الشجرة

```
PrimeERP/
├── App.xaml(.cs)      يبقيان في الجذر — قيد مُصرِّف XAML
├── 1.Platform/        Security/ Permissions/ Audit/ Localization/ Settings/ Net/ Design/
├── 2.Data/            Core/ Repositories/ Seeders/
├── 3.Domain/          Entities/ Enums/ Rules/ Results/ Contracts/ Helpers/
├── 4.Application/     Pipeline/ Services/ Validation/ DTOs/ Reporting/
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
| **3.Domain** | كيانات، تعدادات، قواعد محاسبية نقية، `Result`، عقود التحقق | أي استدعاء قاعدة بيانات، أي مرجع لأي طبقة — النقيّة الوحيدة |
| **4.Application** | خدمات الأعمال، DTOs، المتحقّقون، Pipeline، التقارير | أي مرجع لـ 5-8 |
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

**عرض العمود نسبةً**: نسبةٌ واحدة مُدخَلة تجعل الجدول كلَّه نجميّاً (`IsStarWidth`)، فيبقى تناسبه واحداً على أي عرض شاشة وفي أي نسخة. الحساب في `ColumnWidths` (4.Application) تقرؤه الشاشة والمُحمِّل معاً، فالمعروض هو المطبَّق.

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

**`RepositoryBase.Page(page, pageSize, filter, order)`**: العدّ ثم `Skip/Take` بتنفيذٍ واحد لكل المستودعات، و`Id` فاصلٌ أخير يُضاف هنا. المستودع يمرّر فرقه وحده: شرطه دالة `Shape`، وترتيبه `By(key, descending)` أو `DocumentOrder(key, descending, number)`. وعمود فرزٍ غير مُعلَن يسقط على الحالة الافتراضية في `switch` فلا يصل إلى الاستعلام.

**قطعٌ مضمون في الأساس**: `CrudServiceBase.GetPaged` يقطع ما أرجعته الخدمة إن تجاوز حجم الصفحة، قبل `ToDto`. فخدمةٌ ترقّم في الاستعلام تمرّ كما هي، وخدمةٌ تُرجع القائمة كاملة لا تستطيع إغراق شاشة ولا إرجاع نفس الصفحة عند التنقّل.

**قائمة مرجعية بلا ترقيم**: نموذج العرض الذي يحتاج القائمة كاملةً (وحدات، مخازن، أدوار) يُعلن ذلك بحجم صفحةٍ كبير — لا بتزوير `TotalCount`.

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
حذف         → يعكس قيد الحركة نفسها في معاملة واحدة
```

| القيمة | كيف تُحدَّد |
|---|---|
| تكلفة الشراء | تُدخَل — التكلفة التاريخية، لا تتغيّر أبداً |
| قيمة الخردة · العمر الإنتاجي | تُدخَلان — أساس القسط: (القيمة − الخردة) ÷ (العمر × ١٢) |
| القيمة بعد إعادة التقييم | تتغيّر بإعادة التقييم وحدها، وهي أساس الإهلاك بعدها |
| مجمّع الإهلاك | محسوب من التشغيلات المُرحَّلة |
| القيمة الدفترية | محسوبة: المُعاد تقييمها − مجمّع الإهلاك. لا تُدخَل ولا تُعدَّل بحقل |

القواعد النقيّة في `DepreciationRules` (3.Domain) — قسطٌ ودفتريّةٌ وقابلٌ للإهلاك وفرقُ تقييم — تستوردها خدمةُ الاحتساب وخدمةُ إعادة التقييم والتقارير، فيبقى الرقم واحداً. والترحيل والعكس في خدمة كل حركة: `AssetService` للاقتناء، و`AssetDepreciationService` و`AssetRevaluationService` و`AssetDisposalService` لما بعده.

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
| الحذف تعطيل لا إزالة | الصفّ يبقى وكوده محجوز، فلا يُعاد استخدام كودٍ محذوف. وحذف آخر ابنٍ يعيد الأب لحالته: بلا أبناء ⇒ يقبل القيود |
| الترتيب قطعيّ | المستند بتاريخه ثم رقمه ثم وقت إنشائه، و`Id` آخر فاصل دائماً — فلا ترتيبٌ يتبدّل بين تشغيلتين. موضعه `RepositoryBase.DocumentOrder`، وفرز المستخدم يسبقه ولا يُلغي فواصله |

---

## المبنيّ فعلاً

### 3.Domain
كيانات صرفة · تعدادات · `Result`/`PagedResult` · `Rules.For<T>()` · `IValidator<T>` · `ValidationResult` · `StatusVariant` · `DepreciationRules` و `AccountingRules`.

### 2.Data
`PrimeDbContext` (نموذج EF لكل الجداول) · `DbContextFactory` (سياقٌ فوق `(conn, tx)` القائمين) · `BuiltTables` (جداول المستخدم ككيس خصائص وقت التشغيل) · `RepositoryBase<T>` بأشكاله المشتركة (`Fetch`/`One`/`Count`/`Live`/`Write`/`Add`/`Edit`/`Modify`/`SoftDelete`/`Page`/`By`/`DocumentOrder`) · `SchemaSync` (يُلحق بالقاعدة ما نقص من النموذج جدولاً وعموداً) · `DbConfig` (الإعداد وسلسلة الاتصال لكل محرّك: `Sqlite`/`SqlServer`/`PostgreSql`، ويختار EF محرّكه منها).

**عقودٌ عامّة بدل عقدٍ لكل كيان**: `ILookupRepository<T>` (+`LookupRepository<T>` بجدوله) للقوائم · `IPartyRepository<T>` للعملاء والموردين · `IInvoiceRepository<TInvoice,TLine>` و `IReturnRepository<TReturn,TLine>` للمستندات — فالمستودع الخاص لا يُكتب إلا لاستعلامٍ يخصّه.

**أسماء المراجع بضمّةٍ واحدة**: `RepositoryBase.WithCategoryNames` يملأ اسم الفئة للصفحة كلها باستعلامٍ واحد، لا باستعلامٍ لكل صفّ.

**أساسان مشتركان للمستندات**: `StockAdjustmentRepositoryBase` (رأس+سطور بمخزن وتكلفة) و `CycleDocumentRepositoryBase` (رأس+سطور بطرف وسعر بلا أثر مخزني) — يُمرَّر لكلٍّ اسما جدوليه، فكل مستند جديد وارثٌ بسطر.

### 4.Application
`ServiceBase` — كل خدمة ترثه: `Can` · `FailDenied` · `Check` · `Audit` · `Tx` · `Msg` · `Settings`.
`CrudServiceBase` · `PartyServiceBase` · `CycleDocumentServiceBase` · `StockAdjustmentServiceBase` · `ChequeDocumentServiceBase` · `VoucherServiceBase`.
`Pipeline/{Steps,Operations}` لتركيب العمليات متعددة الخطوات · `Validation/` متحقّق لكل كيان · `Reporting/` خدمات التقارير.

**الأقسام**: Accounting · Parties · Inventory · Sales · Purchasing · Documents · Cheques · Vouchers · HR · Assets · Treasury · Security · Backup · Print.

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
**Services** — `ToastService` · `DialogService` · `ExportService` (CSV/Excel/PDF) · `IdentityService` · `NavigationService` · `UIServices` (بوابة code-behind)


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

**دورتا الشراء والبيع**: `Documents.SimplifiedFlow` (إعداد) يحكم أي المستندات تظهر عبر `ModuleDefinition.FlowScope` و `IModuleRegistry.VisibleFor`. وتتبّع السحب في جدول واحد `DocumentLinks` عبر `IDocumentLinkService` لا يعرف نوع مستند بعينه، و `DocumentDialogDefinition.PullSources` يصف من أين يسحب كل مستند.

---

## قرارات بلا رجعة في الشكل والتكرار

| القرار | لماذا |
|---|---|
| خدمات الفواتير والمرتجعات الأربع تتقاسم ~٢٥ كتلة ولا تُدمَج | الدمج يمسّ بناء القيد والضريبة والخصم والأثر المخزني معاً: الربح شكليّ والمخاطرة في قلب المحاسبة |
| `AppTextBox`/`AppPasswordBox`/`AppTextArea` تُعيد تعريف خصائص الاعتماد | وراثة XAML مع إعادة تسجيل خصائص الاعتماد تكسر الربط — قيدٌ في WPF لا خيارٌ لنا |
| قيمةٌ بصرية داخل مكوّنٍ واحد تبقى رقماً | التوكن يُبنى لما يتقاسمه مكوّنان؛ واسمٌ بلا مستهلكٍ ثانٍ تصنيفٌ يطيل الطريق بلا فائدة. و`check.sh` يرفض أي قيمة تتكرر عبر ملفين |
