# ARCHITECTURE.md — أين يسكن كل شيء

## الشجرة

```
PrimeERP/
├── App.xaml(.cs)      يبقيان في الجذر — قيد مُصرِّف XAML
├── 1.Platform/        Diagnostics/ Security/ Permissions/ Audit/ Localization/ Settings/ Design/
├── 2.Data/            Providers/ Core/ Schema/ Query/ Repositories/ Seeders/ Migrations/
├── 3.Domain/          Entities/ Enums/ Rules/ Results/ Contracts/
├── 4.Application/     Pipeline/ Services/ Validation/ DTOs/ Reporting/
├── 5.Design/          Identity/ Semantic/ Components/ Styles/ Icons/ Strings/ Surfaces/
├── 6.UI/              Components/ Converters/ Behaviors/ Services/ ViewModels/ DevTools/
├── 7.Composition/     Definitions/ Renderers/ Registry/
├── 8.Modules/         تسجيل الوحدات والتقارير + مصانعها
└── App/               Bootstrap/DependencyInjection.cs، MainWindow (Shell)
```

## انتماء الطبقات

| الطبقة | تحتوي | يُمنع أن تحتوي |
|---|---|---|
| **1.Platform** | بنية تحتية عابرة: صلاحيات، تدقيق، لغة، إعدادات، هوية بصرية | منطق أعمال، أي مرجع لـ 4-8 |
| **2.Data** | مزوّدو القواعد، SQL خام، المستودعات، بناة الاستعلام | أي قرار أعمال |
| **3.Domain** | كيانات، تعدادات، قواعد محاسبية نقية، `Result`، عقود التحقق | أي استدعاء قاعدة بيانات، أي مرجع لأي طبقة — النقيّة الوحيدة |
| **4.Application** | خدمات الأعمال، DTOs، المتحقّقون، Pipeline، التقارير | أي مرجع لـ 5-8 |
| **5.Design** | XAML فقط: رموز بصرية | أي C# منطقي |
| **6.UI** | القطع المرئية، محوّلات، خدمات واجهة، أسس ViewModel | استدعاء مستودع، منطق أعمال |
| **7.Composition** | التعريفات (بيانات)، المُصيِّرات (تجميع)، السجلّ | منطق أعمال، XAML لصفحة بعينها |
| **8.Modules** | تسجيل كل وحدة عمل إعلاناً | أي منطق أعمال |

**اتجاه الاعتماد**: من أعلى الرقم إلى أدناه فقط. `3.Domain` و `5.Design` لا تعتمدان على شيء. يفحصه `check.sh` آلياً.

**انحرافان موثَّقان**: `1.Platform/{Settings,Permissions,Audit}` تستدعي `2.Data.Core.DbHelper` (أداة اتصال لا طبقة أعمال) · `BackupService` يستدعي `DbHelper` (أوامر إدارية لا CRUD).

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

**بذر صفحات الكود**: كل صفحة مكتوبة تصير صفّاً في `BuilderModules` بأعمدتها وأزرارها وفلاترها، مؤشَّرةً `IsCoded` — تُعرَض وتُرتَّب وتُحذَف من وحدة البناء، ولا يُنشأ لها جدول. والبذر يقرأ مفاتيح القسم من الخريطة لا من عمود الصفّ المبذور سابقاً، فأي شاشة تُضاف بعد أول بذر تُبذَر في الإقلاع التالي.

**علوّ الوصف على الصفحة المكتوبة**: `ModuleDefinition` و `GridColumn` سجلّان (`record`)، فتُعاد الصفحة المكتوبة إلى التسجيل بـ `with` وقد أُخذت أعمدتها وأزرارها وفلاترها من صفوف الوصف، بينما يبقى نموذج عرضها وحوارها من الكود. العمود الذي له صفّ يأخذ منه عنوانه وعرضه وإجماليه ويحتفظ بما لا يصفه الوصف (قالب الخلية، المحاذاة)؛ وصفٌّ بلا عمود عمودٌ مُضاف؛ وعمودٌ بلا صفّ محذوف.

**عرض العمود نسبةً**: نسبةٌ واحدة مُدخَلة تجعل الجدول كلَّه نجميّاً (`IsStarWidth`)، فيبقى تناسبه واحداً على أي عرض شاشة وفي أي نسخة. الحساب في `ColumnWidths` (4.Application) تقرؤه الشاشة والمُحمِّل معاً، فالمعروض هو المطبَّق.

---

## تدفّق البيانات

مسارٌ واحد تقرأ به كل شاشة، ولا مسار ثانٍ:

```
WhereBuilder + OrderBuilder        (المستودع يصف شرطه وترتيبه)
        ↓
RepositoryBase.Page(...)           ← العدّ والقطع هنا وحدهما
        ↓
CrudServiceBase.GetPaged           ← المدخل الوحيد، ويقطع ما تجاوز الصفحة
        ↓
PagedViewModelBase.FetchPage       ← يملك الصفحة والحجم والبحث والفرز
        ↓
AppDataGrid + AppPagination
```

**`RepositoryBase.Page(where, page, pageSize, order, from, select)`**: `SELECT COUNT` ثم `LIMIT/OFFSET` بتنفيذٍ واحد لكل المستودعات. المستودع يمرّر فرقه وحده: شرطه (`WhereBuilder`)، وترتيبه (`OrderBuilder.By`)، و`from`/`select` إن كان يقرأ من ضمّ (`JOIN`) لا من جدوله وحده. و`Top(where, order, max)` لبحثٍ سريع بلا عدّ. و`SortOf(requested, fallback, allowed)` يمنع وصول عمود فرزٍ غير مُعلَن إلى SQL.

**قطعٌ مضمون في الأساس**: `CrudServiceBase.GetPaged` يقطع ما أرجعته الخدمة إن تجاوز حجم الصفحة، قبل `ToDto`. فخدمةٌ ترقّم في SQL تمرّ كما هي، وخدمةٌ تُرجع القائمة كاملة لا تستطيع إغراق شاشة ولا إرجاع نفس الصفحة عند التنقّل.

**قائمة مرجعية بلا ترقيم**: نموذج العرض الذي يحتاج القائمة كاملةً (وحدات، مخازن، أدوار) يُعلن ذلك بحجم صفحةٍ كبير — لا بتزوير `TotalCount`.

---

## العمليات الطويلة

كل عملية تتجاوز ثانية تُنفَّذ خارج خيط الواجهة، بحوار تقدّم، وبنسبة محسوبة لا دائرة بلا نهاية.

الآلية الوحيدة: `IDialogService.ShowProgress(title, message)` تُرجع `IProgressHandle` (فوق `AppProgressDialog`). الخدمة ترفع تقدّمها بـ `IProgress<T>` ولا تعرف الواجهة، والمُصيِّر يُشغّلها بـ `Task.Run` ويعرض الحوار. المرجع العامل: `ProgramEditionService`.

---

## القيم البصرية — ثلاث مناطق

| المنطقة | تتبدّل فاتح/داكن؟ | تُدمَج في `App.Resources`؟ | تخدم |
|---|---|---|---|
| `5.Design/` (Identity → Semantic → Components → Styles) | نعم | نعم | الشاشة |
| `5.Design/Surfaces/PrintTheme.xaml` | لا | لا — يحمّلها `PrintService` وقت البناء | المستندات المطبوعة (`FlowDocument`) |
| `5.Design/Surfaces/ExportTheme.cs` | لا | لا — ثوابت C# لا XAML | ملفات Excel/CSV/PDF المُصدَّرة |

الطباعة والتصدير منفصلتان لأن الورق لا يتبدّل مع ثيم الشاشة: قالبٌ يقرأ `Semantic.Light` مباشرةً يطبع خلفيةً سوداء بمجرد تفعيل الوضع الداكن. والتصدير ثوابت C# لأن ClosedXML و QuestPDF لا يقرآن `ResourceDictionary`.

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

القواعد النقيّة في `DepreciationRules` (3.Domain) — قسطٌ ودفتريّةٌ وقابلٌ للإهلاك وفرقُ تقييم — تستوردها خدمةُ الاحتساب وخدمةُ إعادة التقييم والتقارير، فيبقى الرقم واحداً. والترحيل والعكس في `AssetPosting` وحده.

**شجرة الحسابات**: `1101` صافي الأصول الثابتة، أبٌ لجذرَين تجميعيَّين — `1101001` (التكلفة) تسكنه الفئات وتحتها أصولها، و `1101002` (مجمّع الإهلاك) تسكنه مرايا الفئات وتحتها مجمّعات أصولها. الجذران مضبوطان في `SettingKeys.Accounts.FixedAssets` و `AccumulatedDepreciation`.

**الأرباح والخسائر الرأسمالية بندان غير تشغيليَّين**: تحت «إيرادات أخرى» (42) و«مصروفات أخرى» (52)، فلا تدخل الربح الإجمالي ولا الربح التشغيلي، وتظهر أسفل قائمة الدخل قبل صافي الربح.

---

## المبنيّ فعلاً

### 3.Domain
كيانات صرفة · تعدادات · `Result`/`PagedResult` · `Rules.For<T>()` · `IValidator<T>` · `ValidationResult` · `StatusVariant` · `DepreciationRules` و `AccountingRules`.

### 2.Data
`RepositoryBase<T>` · `WhereBuilder` (شرط SQL آمن) · `OrderBuilder` (قاعدة الترتيب الواحدة) · `SchemaBuilder` · `MigrationRunner` · `DbFactory`/`IDbProvider` بثلاثة مزوّدين (`Sqlite`/`SqlServer`/`PostgreSql`) قابلة التبديل عبر `DbConfig.Provider`.

**أساسان مشتركان للمستندات**: `StockAdjustmentRepositoryBase` (رأس+سطور بمخزن وتكلفة) و `CycleDocumentRepositoryBase` (رأس+سطور بطرف وسعر بلا أثر مخزني) — يُمرَّر لكلٍّ اسما جدوليه، فكل مستند جديد وارثٌ بسطر.

**قاعدة الترتيب** (`OrderBuilder`): المستند بتاريخه تنازلياً ثم برقمه ثم بوقت إنشائه، لأن الرقم وحده لا يعكس الترتيب حين تُضاف السجلات من أكثر من شاشة بسلاسل مختلفة. والبيانات الأساسية بكودها. و `Id` آخر فاصل دائماً فالترتيب قطعيّ. وفرز المستخدم من رأس الجدول يسبق ذلك ولا يُلغي الفواصل بعده.

**قراءةٌ من داخل معاملة قائمة** تستعمل حمل `(conn, tx)` حصراً: فتح اتصال جديد على SQLite من داخل معاملة مفتوحة على الخيط نفسه يُعلِّق، لأن القفل الكتابي لا يُحرَّر حتى يعود الاستدعاء المتزامن.

### 4.Application
`ServiceBase` — كل خدمة ترثه: `Can` · `FailDenied` · `Check` · `Audit` · `Tx` · `Msg` · `Settings`.
`CrudServiceBase` · `PartyServiceBase` · `CycleDocumentServiceBase` · `StockAdjustmentServiceBase` · `ChequeDocumentServiceBase` · `VoucherServiceBase`.
`Pipeline/{Steps,Operations}` لتركيب العمليات متعددة الخطوات · `Validation/` متحقّق لكل كيان · `Reporting/` خدمات التقارير.

**الأقسام**: Accounting · Parties · Inventory · Sales · Purchasing · Documents · Cheques · Vouchers · HR · Assets · Treasury · Security · Backup · Print.

### 5.Design
أربع طبقات موارد تُستهلَك بـ `DynamicResource` حصراً (تتبدّل حيّاً بلا إعادة تحميل): **Identity** (قيم خام — ثلاث حزم: `Signature`/`Default`/`Corporate`) ← **Semantic** (معنى — فاتح وداكن منفصلان) ← **Components** (رموز المكوّن) ← **Styles** (أنماط WPF، منها ضمنية بلا `x:Key` تُطبَّق تلقائياً).

الأسماء في Identity بالدور لا بالصبغة (`P.Color.Brand.*` لا `P.Color.Blue.*`)، فتُعيد كل حزمةٍ تعريفَ المفاتيح نفسها بقيم مختلفة بلا تغيير سطرٍ في Semantic.

**سلالم الوضع الداكن**: `NeutralDark` للأسطح (الإضاءة تزيد مع الارتفاع) · `Nav` للشريط الجانبي وحده. لا تُخلطان.

**ثلاث قواعد تكسر الألوان صامتاً بلا خطأ بناء**:

1. رموز الطبقة الثالثة تقرأ ألوان الثانية عبر مفاتيح `{Name}.Color` يشتقّها `IdentityService.RefreshDerivedColors` من **كل** فرش الشجرة المدموجة. لا يُعاد إلى قائمة أسماء يدوية: أي فرشاة خارج القائمة تُحلّ شفافة.
2. `Application.Resources` يُعيَّن قاموساً فارغاً أولاً ثم تُبنى `MergedDictionaries` داخله، وملفات الهوية تُدرَج **قبل** `Theme.xaml` في الخطوة نفسها. بعده تتجمّد الموارد المركّبة (`DropShadowEffect` مثلاً) على أول قيمة رأتها ولا يُعاد تقييمها.
3. `UI.Identity` مخزَّن في القاعدة و `SettingSeeder` لا يستبدل قيمة قائمة — إطلاق حزمة جديدة يتطلّب رفع `IdentityService.IdentityBaseline`.

**الأيقونات**: مواصفة واحدة في `Tokens.Icon.xaml` وقطعة `AppIcon` مستهلكها الوحيد. جدول الرموز الكامل في `5.Design/DESIGN_TOKENS.md`.

### 6.UI
**Display** — `AppDataGrid` (أعمدة/فرز/ترقيم/صف إجراءات) · `AppPagination` · `AppCard` · `AppEmptyState` · `AppLoadingOverlay`
**Inputs** — `AppTextBox` · `AppTextArea` · `AppNumericBox` · `AppDatePicker` · `AppCheckBox` · `AppComboBox` (بحث+ترشيح) · `AppPasswordBox` · `AppSearchBox`
**Actions** — `AppButton` · `ToolbarAction` (New/Edit/Delete/Refresh/Save/Cancel/Print/Export/Post/Unpost/…) · `ActionToolbar` (يُخفي ما لا صلاحية له تلقائياً)
**Feedback** — `AppDialogWindow` (قاعدة كل الحوارات) · `AppConfirmDialog` · `AppMessageDialog` · `AppProgressDialog` · `AppToast`/`ToastService`
**Layout** — `PageHeader` · `FilterBar` · **Shell** — `AppSidebar`/`NavItem` · `AppTopBar` · **Tree** — `AppTreeView` · `TreeNodeViewModel` · **Pickers** — فوق `PickerGridWindow`/`PickerTreeWindow`
**ViewModels/Base** — `PagedViewModelBase<TDto,TFilter>` · `CrudViewModelBase<TDto,TFilter>` · `TreeViewModelBase` · `PermissionAwareViewModel`
**Services** — `ToastService` · `DialogService` · `ExportService` (CSV/Excel/PDF) · `IdentityService` · `ThemeService` · `NavigationService` · `UIServices` (بوابة code-behind)

`6.UI/DevTools` مُستثناة من بناء Release.

### 7.Composition
`PageRenderer` نقطة التوزيع الوحيدة حسب `LayoutKind`:

| النمط | المُصيِّر | يُعلَن بـ |
|---|---|---|
| قائمة + CRUD | `CrudPageRenderer` | `Columns` + `Dialog` |
| مستند رأس+سطور | `DocumentRenderer` | `DocumentDialog` |
| شجرة | `TreeRenderer` + `TreeBuilder` | `TreeOptions` |
| تقرير | `ReportRenderer` | `Report` |
| إعدادات | `SettingsPageRenderer` | `LayoutKind.Settings` |
| شجرة تأشير | `TreeCheckListRenderer` | `TreeCheckList` |
| لوحة شيكات | `ChequeBoardRenderer` | `LayoutKind.ChequeBoard` |

`DialogRenderer` مصنع قطع الإدخال لكل الشاشات: `BuildField` (القطعة من نوع الحقل) · `LoadPickerItems` (القائمة) · `GetControlValue`/`SetControlValue` (القيمة) · `OnChanged` (حدث التغيّر) · `Coerce` (التحويل إلى نوع الخاصية: قابل للإفراغ، تعداد). يستهلكه `CrudPageRenderer` و `DocumentRenderer` لرأس المستند، ويستورده شريط الفلاتر كما يستورده الحوار — فالفلتر التبديلي مربّع تأشير، والفلتر القائمة تقرأ قائمتها من حقل الصفحة المُعلَن بنفس المفتاح. و `FieldValidation` يفحص الرأس والسطور معاً.

**قطع مشتركة بين المُصيِّرات** — كلٌّ منها موضعٌ واحد يستورده كل مُصيِّر يحتاجه: `FilterControls` (شريط الفلاتر) · `ToolbarActions` (ترشيح أزرار الوحدة) · `NavigationSource` (أقسام الشريط الجانبي) · `ListOutput` (طباعة وتصدير) · `FolderOutput` (اختيار مجلد) · `BuilderPickers` (قوائم تعدادات النظام وكتالوج أزراره).

**قطع تعريف جاهزة**: `StandardFields` · `CategoryDialogFactory` · `TradePaper` (أعمدة وإجماليات وحساب سطر الفواتير).

### 8.Modules
تسجيل كل وحدة إعلاناً، عبر مصانع مشتركة: `StockDocumentFactory` · `CycleDocumentRegistrations` · `CycleVoucherRegistrations` · `TreasuryRegistrations` · `BalanceReportFactory` · `RegisterLookup`.

**دورتا الشراء والبيع**: `Documents.SimplifiedFlow` (إعداد) يحكم أي المستندات تظهر عبر `ModuleDefinition.FlowScope` و `IModuleRegistry.VisibleFor`. وتتبّع السحب في جدول واحد `DocumentLinks` عبر `IDocumentLinkService` لا يعرف نوع مستند بعينه، و `DocumentDialogDefinition.PullSources` يصف من أين يسحب كل مستند.

---

## الدين التقني المسجَّل

| العنصر | لماذا | الحالة |
|---|---|---|
| `BackupService` يستدعي `DbHelper` | أوامر BACKUP/RESTORE إدارية لا CRUD | استثناء دائم |
| قيم Hex مكرّرة في حزمتَي هوية | تدرّج متقارب عمداً | مقبول، مُراقَب |
| `BackupService` يعمل على خيط الواجهة | سابق لقاعدة العمليات الطويلة | يُنقل إلى `ShowProgress` كما فعل `ProgramEditionService` |
| نصوص `ToolbarAction.Catalogue` عربية في الكتالوج لا في `Strings.xaml` | نقلها يمسّ كل زرّ في النظام | مسجَّل |
| `CustomerPicker` يعيد حساب تجاوز حدّ الائتمان محلياً | مصدر الاختيار يُرجع الكيان لا الـDTO، والخدمة تحسبه في `CustomerDto.IsOverCreditLimit` | يزول بجعل المصدر يُرجع الـDTO — **منطق أعمال في 6.UI اليوم** |
| `ControlsGalleryPage` و `MockPickerDataSources` | أدوات تطوير دائمة، مستثناة من بناء Release ولا تُستهلَك من مسار حيّ | استثناء دائم |
