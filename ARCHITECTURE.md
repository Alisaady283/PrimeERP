# ARCHITECTURE.md — PrimeERP

هذا الملف يوثّق البنية المعمارية الثمانية-الطبقات لِـ PrimeERP بعد R1+R2+R3 (إعادة الهيكلة + فرض الحدود + الحقن الحقيقي للاعتماديات). يُستكمل مع كل بند لاحق (R4–R10).

---

## القاعدة الدائمة الأولى — تُفحص قبل إنشاء أي ملف جديد

1. **أي طبقة ينتمي إليها هذا الملف؟** — راجع "قواعد الانتماء" أدناه قبل اختيار المكان.
2. **يوجد أساس مشترك له بالفعل؟** → رِثه، لا تُعد بناءه.
3. **لا يوجد وسيتكرر في أكثر من موضع؟** → ابنِ الأساس المشترك أولاً.
4. **يخرق قاعدة اعتماد (طبقة أعلى تُستدعى من أدنى، أو تخطي أكثر من طبقة)؟** → أعد التفكير في موضع الملف، لا في كسر القاعدة.
5. **يمكن وصفه بتكوين (بيانات) بدل كود؟** → افعل ذلك (ينطبق بشكل رئيسي على 7.Composition/8.Modules).

عند مواجهة كود خاطئ أثناء العمل: **احذف وابنِ سليماً — لا ترصّ كوداً فوق بنية معطوبة.**

## القاعدة الدائمة الثانية — الحذف الفوري، لا استثناء

**أي بناء مؤقت يُحذف فوراً عند اكتشافه — حتى لو كان قابلاً للنقل أو التعديل. لا يُنقل، لا يُعدّل، لا يُبنى عليه.**

السبب المُثبت في هذا المشروع تحديداً: `ServiceLocator` أخفى الاعتماديات فتأخّر كشف الدائرية؛ عقود جزئية (`ISupplierService`) بُنيت عليها خدمات كاملة قبل اكتمالها؛ `_Legacy/` سبَّب تضارب أسماء namespace ظلّ كامناً حتى R1؛ نسختا `DbHelper` القديمتان تعايشتا حتى ظهر deadlock حقيقي؛ `IJournalService` عقد جزئي بُني عليه `FiscalPeriodService` كاملة في F.2.2 قبل اكتمال العقد.

**القاعدة**: عند مواجهة أي مؤقت — احذف، ثم ابنِ البديل الصحيح كاملاً، ثم اربط. **لا تربط شيئاً بعقد ناقص.**

**الاستثناء الوحيد**: إن كان حذفه الآن يكسر البناء وبديله مجدوَل في بند لاحق صريح — يُحذف في ذلك البند إلزامياً، ويُسجَّل الآن بتعليق:
```
// TEMPORARY — يُحذف في R{n}. لا تبنِ عليه.
```
كل حالة استثناء حالية مُرقَّمة ومُسجَّلة في § "الدين التقني" أدناه — لا يوجد مؤقت غير مُرقَّم في الكود بعد R3 (يفحصه `Tools/ArchitectureCheck/check.sh`).

---

## الشجرة الثمانية

```
PrimeERP/
├── App.xaml, App.xaml.cs        ⚠️ استثناء إلزامي — راجع "استثناء App.xaml" أدناه
├── 1.Platform/      Diagnostics/ Security/ Permissions/ Audit/ Localization/ Settings/
├── 2.Data/          Providers/ Core/ Schema/ Query/ Repositories/ Seeders/
├── 3.Domain/        Entities/ Enums/ Rules/ Results/ Contracts/
├── 4.Application/   Pipeline/{Steps,Operations}/ Services/ Validation/ DTOs/    (ServiceLocator.cs محذوف نهائياً — R3)
├── 5.Design/        Identity/{Default,Corporate}/ Semantic/ Styles/ Icons/ Strings/ Surfaces/ Theme.xaml
├── 6.UI/            Components/ Converters/ Behaviors/ Services/ (+ UIServices.cs) ViewModels/ DevTools/
├── 7.Composition/   Definitions/ Renderers/ Registry/          (فارغة بعد — R8)
├── 8.Modules/       Accounting/ Parties/ Inventory/ Sales/ Purchases/ HR/ Reports/ System/  (فارغة بعد — R9)
└── App/             Bootstrap/DependencyInjection.cs (R3)، MainWindow.xaml(.cs)  (Shell الحقيقي — R9)
```

### ⚠️ استثناء App.xaml — قيد أدوات لا قرار معماري

`App.xaml`/`App.xaml.cs` يبقيان في **جذر المشروع**، لا داخل `App/`. **مُثبت تجريبياً** أثناء R1 (أربع محاولات فعلية، راجع سجل الجلسة): وضع `App.xaml` داخل أي مجلد فرعي — حتى مع تصريح `<ApplicationDefinition>` صريح في `.csproj` يستثنيه من `<Page>` ويضيفه بنفسه — يجعل مشروع `wpftmp` المؤقت (الذي يبنيه WPF SDK لتمرير تجميع XAML/Pass 1) يفشل بخطأ `CS5001: Program does not contain a static 'Main' method` بشكل حتمي قابل لإعادة الإنتاج، بصرف النظر عن `-m:1`. نقل الملفين لجذر المشروع فقط (مع إبقاء `MainWindow.xaml(.cs)` تحت `App/`) هو الحل الوحيد الذي أزال الخطأ. الـ namespace يبقى `PrimeERP.App` تماماً كبقية الطبقة — الموقع الفعلي فقط مختلف عن الاصطلاح.

---

## قواعد الانتماء لكل طبقة

| الطبقة | يحتوي | يُمنع أن يحتوي |
|---|---|---|
| **1.Platform** | بنية تحتية عابرة: صلاحيات (تعريف + تخزين)، تدقيق (Audit)، لغة، إعدادات (تخزين + قراءة/كتابة) | منطق أعمال محاسبي، أي مرجع لـ 4.Application/5.Design/6.UI/7.Composition/8.Modules |
| **2.Data** | مزوّدو قواعد بيانات، أدوات SQL خام (DbHelper/SchemaBuilder)، المستودعات (Repository) | أي قرار/حساب/تحقق أعمال، استدعاء Service أو Repository آخر عبر منطق (لا مجرد نوع) |
| **3.Domain** | كيانات بيانات صرفة (Entities)، تعدادات، قواعد محاسبية نقية (دوال حسابية بلا حالة)، أنواع Result/PagedResult/StatusVariant، عقود التحقق (IValidator/ValidationResult/ValidatorBase) | أي استدعاء DB، أي مرجع لأي طبقة أخرى إطلاقاً — الطبقة الوحيدة "نقية" فعلياً بلا استثناء واحد بعد R1 |
| **4.Application** | خدمات الأعمال (Service)، DTOs، مدقّقو الإدخال (Validators)، عقود مشتركة قابلة لإعادة الاستخدام تصعد لـ 3.Domain/Contracts لو احتاجتها طبقة أخرى (مثال `IPrintable`)، لاحقاً: Pipeline/Steps/Operations/ServiceBase | أي مرجع لـ 5.Design/6.UI/7.Composition/8.Modules |
| **5.Design** | XAML فقط — رموز بصرية (Identity→Semantic→Styles)، لا كود C# وراء أي منطق (ExportTheme.cs ثوابت صرفة مسموحة استثناءً موثَّقاً مسبقاً) | أي كود C# منطقي، أي مرجع خارج نفسه |
| **6.UI** | قطع الواجهة (UserControl/Window)، محوّلات (Converters)، خدمات UI-orchestration (Dialog/Toast/Navigation/Export/Identity/Theme)، ViewModel أسس عامة | استدعاء Repository مباشرة، منطق أعمال محاسبي |
| **7.Composition** | تعريفات صفحات/حوارات كبيانات (Definitions)، مُصيِّرات تجمّع القطع (Renderers)، سجلّ الموديولات | أي منطق أعمال، أي XAML مخصص لصفحة واحدة |
| **8.Modules** | تجميع نهائي لكل نطاق عمل (ViewModel متخصص + تعريف Composition + أي ربط خاص) | منطق أعمال (يستدعي 4.Application فقط، لا يُعيد تنفيذه) |
| **App/** | نقطة الإقلاع (MainWindow لاحقاً = AppShell فقط) | أي منطق عدا تجميع/تسجيل |

---

## مصفوفة الاعتماد المسموح (طبقة × طبقة)

`✅` مسموح ومُستهلَك فعلياً · `➖` مسموح، فارغ بعد · `⛔` ممنوع (يفحصه `Tools/ArchitectureCheck/check.sh` تلقائياً)

| من \ إلى | 1.Platform | 2.Data | 3.Domain | 4.Application | 5.Design | 6.UI | 7.Composition | 8.Modules |
|---|---|---|---|---|---|---|---|---|
| **1.Platform** | — | ✅ (Core/Schema فقط) | ✅ | ⛔ | ⛔ | ⛔ | ⛔ | ⛔ |
| **2.Data** | ✅ (Settings) | — | ✅ | ⛔ | ⛔ | ⛔ | ⛔ | ⛔ |
| **3.Domain** | ⛔ | ⛔ | — | ⛔ | ⛔ | ⛔ | ⛔ | ⛔ |
| **4.Application** | ✅ | ✅ | ✅ | — | ⛔ | ⛔ | ⛔ | ⛔ |
| **5.Design** | ⛔ | ⛔ | ⛔ | ⛔ | — | ⛔ | ⛔ | ⛔ |
| **6.UI** | ✅ | ⛔ | ✅ | ➖ | ✅ | — | ⛔ | ⛔ |
| **7.Composition** | ✅ | ⛔ | ✅ | ✅ | ✅ | ✅ | — | ⛔ |
| **8.Modules** | ✅ | ⛔ | ✅ | ✅ | ✅ | ✅ | ✅ | — |
| **App/** | ✅ | ⛔ | ➖ | ✅ | ➖ | ✅ (DevTools، مؤقت) | ➖ | ➖ |

### ⚠️ قيد Platform — انحراف موثَّق عن "لا يعتمد على شيء"

`1.Platform/{Settings,Permissions,Audit}/*` تستدعي `PrimeERP.Data.Core.DbHelper` **و**`PrimeERP.Data.Schema.SchemaBuilder` مباشرة (اتصال SQL خام + تعريف جداولها الخاصة، لا عبر `2.Data/Repositories`). هذا **مقصود لا سهو**: هو بالضبط ما يقطع الدائرية `Database↔Services.Settings` الأصلية — `SettingsService`/`PermissionService`/`AuditLogger` تملك مستودعاتها الخاصة (`SettingRepository`/`PermissionDb`) داخل نفس طبقتها، فلا تحتاج طبقة `2.Data.Repositories` إطلاقاً، بينما `2.Data` (Seeders) يستدعي `1.Platform.Settings` (اتجاه واحد فقط، سليم — يتحقق منه `check.sh` كاستثناء موثَّق لا دائرية حقيقية). الاعتماد الحقيقي المتبقي `1.Platform → 2.Data.Core/Schema` هو اعتماد على "أداة" لا "طبقة أعمال".

### ✅ مخالفة R1 المعروفة — أُصلحت في R2

`PrintService` (4.Application) كان يستدعي `ExportService.Instance` (6.UI) مباشرة — اعتماد Application→UI معكوس. **الحل المُنفَّذ**: عقد `IDocumentExporter` جديد في `3.Domain/Contracts/` (+ نقل `IPrintable`/`PrintSection`/`PrintColumn`/`PrintTotal` معه من 4.Application لنفس المكان — عقد مشترك حقيقي بين الطبقتين، لا ينتمي لإحداهما حصراً). `ExportService` ينفّذ `IDocumentExporter` الآن بجانب `IExportService`. `PrintService.ExportToPdf` يستقبل `IDocumentExporter` عبر حقن حقيقي في المُنشئ منذ R3 (`services.AddSingleton<IDocumentExporter>(sp => sp.GetRequiredService<ExportService>());` في `AddUI()`) — لا مؤقت متبقٍّ هنا. صفر اعتماد Application→UI متبقٍّ (تحقَّق `check.sh`).

---

## الحذف/الدمج المُنفَّذ في R1

- **`_Legacy/`** (10 ملفات، كانت مستبعدة من الترجمة أصلاً) — محذوفة نهائياً.
- **`Core/Transactions/{UnitOfWork,IUnitOfWork}.cs`** — محذوفتان (صفر مستهلك مؤكَّد).
- **`Resources/Icons.xaml`، `Resources/Strings.xaml`** (الجذريتان الفارغتان) — محذوفتان؛ النسختان الحقيقيتان (`Icons/Icons.xaml`, `Strings/Strings.ar|en.xaml`) انتقلتا لـ `5.Design/`.
- **`Views/Windows/LoginWindow.xaml(.cs)`** — محذوفة (Grid فارغ، صفر منطق) — تُبنى حقيقية في R9.
- **8 ViewModels فارغة** (`CustomersViewModel`...) و**14 صفحة/حوار فارغة** (`Views/Pages/*`, `Views/Dialogs/*`) — محذوفة (كلاسات/شاشات فارغة تماماً، صفر مستهلك) — تُبنى عبر 7.Composition/8.Modules في R7–R9.
- **`ISupplierService.cs`** — **لم يُحذف رغم تصنيفه "يُحذف ويُبنى من جديد"**: لا يزال `AccountService.ResolveAutoLink` و`AccountServiceTests` يعتمدان عليه فعلياً؛ حذفه الآن يكسر البناء والاختبارات معاً (يخالف "R1: نقل فقط، صفر تغيير منطق"). **نُقل كما هو** إلى `4.Application/Services/Parties/`، ووُسم بتعليق `// TEMPORARY — يُحذف ويُعاد بناؤه كاملاً في R6` (R2)؛ إعادة بنائه الفعلية ضمن R6 مع `PartyServiceBase` كما ورد صراحة هناك.
- **`ServiceLocator.cs`** — نُقل كما هو إلى `4.Application/` مؤقتاً في R2 (`// TEMPORARY — يُحذف نهائياً في R3`)، ثم **حُذف نهائياً في R3** (`git rm`) — استُبدل بحقن حقيقي عبر `Microsoft.Extensions.DependencyInjection` (تفصيل كامل في § "R3" أدناه).
- **`2.Data/Repositories/{FiscalYearSeeder,NumberSequenceSeeder}.cs`** — نُقلا إلى `2.Data/Seeders/` جديد (R2): كانا يخالفان "Repository لا يستدعي Repository آخر" (كلاهما يقرأ من أكثر من مستودع لتهيئة بيانات أولية) — عزلهما في فئة منفصلة (Bootstrap/Seeding، لا CRUD كيان واحد) يحل التصنيف الخاطئ بلا تغيير منطق.

---

## R2 — فرض الحدود

### الأداة

`Tools/ArchitectureCheck/check.sh` — سكربت Bash (لا مشروع C# منفصل، لسرعة البناء والتكرار مع الأداة المستخدمة طوال الجلسة). يُشغَّل: `bash Tools/ArchitectureCheck/check.sh` من أي مكان. يفحص: حدود الطبقات (اعتماد لأعلى/تخطي طبقتين/دائرية)، حدود المسؤولية (Repository من طبقة عرض، DbHelper من Service، Validator يكتب DB، Entity فيها دالة، ViewModel فيه Brush)، حدود التصميم (Hex حرفي خارج 5.Design، StaticResource للون، StaticResource لأبعاد بنيوية)، التكرار (أسماء كلاسات، مفاتيح موارد مكررة، Light بلا مقابل Dark)، والمؤقت (TEMPORARY بلا رقم بند، TODO بلا رقم). يخرج بكود غير صفري عند أي `FAIL` — `WARN` (دين تقني مسجَّل) لا يوقف البناء.

**تحقق ذاتي أثناء البناء**: النسخة الأولى من الأداة أنتجت 14 نتيجة إيجابية كاذبة (مقارنة إشارات ذاتية داخل نفس الطبقة كأنها اعتماد خارجي، خلط `grep -h` مع فلترة مسار بعده فيُبطلها، استثناء `Data.Schema` المنسي من قيد Platform) — صُحِّحت جميعها بمراجعة كل نتيجة يدوياً قبل قبولها، لا بإسكاتها.

### الخروق الحقيقية المكتشفة وما فُعل بكل واحد

| الخرق | أين | الحل |
|---|---|---|
| Application(4)→UI(6) | `PrintService`→`ExportService` | عقد `IDocumentExporter` في 3.Domain (تفصيل أعلاه) |
| Repository يستدعي Repository | `FiscalYearSeeder`/`NumberSequenceSeeder` | نُقلا إلى `2.Data/Seeders/` (فئة منفصلة عن CRUD) |
| `using` متبقٍّ من التوسيع الآمن في R1 (Domain→Platform) | `Employee.cs`, `Product.cs`, `PurchaseInvoice.cs`, `SalesInvoice.cs`, `StockMovement.cs`, `Supplier.cs` (كلها 3.Domain/Entities) | حُذفت أسطر `using PrimeERP.Platform.Permissions;` غير المُستهلَكة فعلياً (توسيع R1 الآمن لِـ `using PrimeERP.Core;` أضاف مرجعين احتياطاً، أحدهما غير مُستخدَم هنا) |
| `using` متبقٍّ مشابه (Platform→Data.Repositories) | `PermissionService.cs`, `SettingsService.cs` | حُذف `using PrimeERP.Data.Repositories;` غير المُستهلَك — كل من `PermissionDb`/`SettingRepository` أصبح في نفس طبقة المستهلِك أصلاً (وصول ضمني بلا `using`) |
| `StaticResource` للون بدل `DynamicResource` | `AppDropdownButton.xaml`, `AppToast.xaml`, `PickerBaseControl.xaml`, `5.Design/Styles/Inputs.xaml` — الأربعة `Effect="{...Resource ShadowMd}"` | حُوِّلت لـ `DynamicResource` — كانت لن تتحدّث عند تبديل الوضع الداكن (الظلال معطَّلة بالداكن، `Opacity=0`) |

### الدين التقني (مؤقتات مُرقَّمة، لا استثناء بلا رقم)

| العنصر | لماذا مؤقت | بند الحذف/الحل |
|---|---|---|
| ~~`4.Application/ServiceLocator.cs`~~ | ~~حل مؤقت لربط الخدمات قبل DI حقيقي~~ | ✅ **محلول في R3** — حُذف نهائياً، استُبدل بـ DI حقيقي |
| `4.Application/Services/Parties/ISupplierService.cs` (+ عدم تسجيله في `App/Bootstrap/DependencyInjection.cs`) | عقد جزئي بُني ليطابق `ICustomerService` توقيعاً فقط، بلا تنفيذ (`SupplierService`) | **R6** — يُحذف ويُعاد بناؤه كاملاً مع `PartyServiceBase`، ثم يُسجَّل في `AddApplication()` |
| `6.UI/DevTools/{ControlsGalleryPage,MockPickerDataSources}` | أداة تطوير دائمة، لا تُستهلَك من مسار حي | **R9** — تُستثنى من بناء Release (لا حذف، استبعاد) |
| `App/MainWindow.xaml(.cs)` | يعرض Gallery بدل شاشة حقيقية | **R9** — يُستبدل محتواه بـ `AppShell` |
| 32 ملف XAML يستهلك `StaticResource` لأبعاد بنيوية (Height/Radius/FontSize/FontFamily/FontWeight/Space/Icon) بدل `DynamicResource` | `Typography.xaml`/`Metrics.xaml` لم تُدمَجا بعد في سلسلة L1→L2→L3→L4؛ تحويلها الآن بلا الطبقتين L3/L4 عمل جزئي بلا فائدة مُثبَتة | **R4** — يُبنى L3 (Components/Tokens) + L4 (Styles) كاملاً، ثم تتحوَّل الـ32 دفعة واحدة |
| `BackupService.cs` يستدعي `DbHelper` مباشرة (لا Repository) | أوامر `BACKUP`/`RESTORE VERIFYONLY` إدارية على مستوى محرّك القاعدة، لا CRUD كيان — لا يوجد Repository مكافئ منطقياً لها | **لا بند حذف** — استثناء دائم موثَّق، ليس ديناً يُسدَّد |

### التحقق النهائي لـ R2

`Tools/ArchitectureCheck/check.sh` → **صفر FAIL، 3 WARN (كلها دين تقني مُرقَّم أعلاه)**. `dotnet build` (المشروعين) → 0 خطأ. `dotnet test -m:1 --no-build` → **134/134 ناجح، صفر تعديل على أي اختبار**.

---

## R3 — الحقن الحقيقي للاعتماديات (DI)

### الحاوية

`Microsoft.Extensions.DependencyInjection` 10.0.11 (كلا المشروعين). `App/Bootstrap/DependencyInjection.cs` يعرّف 5 دوال توسيع على `IServiceCollection` بترتيب الطبقات: `AddPlatform()` (Permissions/Settings)، `AddData()` (فارغة بعد — لا حالة قابلة للحقن حتى `RepositoryBase` في R5)، `AddApplication()` (كل خدمات 4.Application + `Lazy<IJournalService>`)، `AddUI()` (خدمات 6.UI + تسجيل `ExportService` تحت ثلاثة أنواع: نفسه، `IExportService`، `IDocumentExporter`)، `AddModules()` (فارغة بعد — R9). `App.xaml.cs.OnStartup` يبنيها بالترتيب، يُخرج `IServiceProvider` عبر `App.Services` (static، للقراءة فقط من الخارج)، ويستدعي `UIServices.Initialize(Services)`.

كل خدمة تحوَّلت من `public static readonly XService Instance = new();` + اعتماديات كحقول ذات مُهيِّئ، إلى حقن حقيقي عبر المُنشئ — **بلا استثناء واحد** بين الخدمات الـ14 الحقيقية. `.Instance` لم يعد له وجود في أي خدمة أعمال.

### لماذا `UIServices` ليس `ServiceLocator` معاداً بشكل آخر

`6.UI/Services/UIServices.cs` كلاس static ضيّق مقصود (`Provider`, `Initialize`, خاصية `Permissions` مختصرة) — **الفرق الجوهري عن `ServiceLocator` المحذوف**:

- **مَن يستهلكه**: حصراً كود-خلف View/Control التي يُنشئها WPF عبر مُنشئ بلا بارامترات (XAML) — لا توجد وسيلة لحقن مُنشئ هناك أصلاً. لا خدمة أعمال واحدة (4.Application) تستدعيه أو تعرفه.
- **`ServiceLocator` القديم** كان يُستهلَك **من داخل خدمات الأعمال نفسها** (Service يحل اعتمادية Service آخر عبر Locator بدل المُنشئ) — هذا بالضبط ما أخفى الدائرية الحقيقية `JournalService↔FiscalPeriodService` حتى R3 (موثَّق في القاعدة الدائمة الثانية أعلاه).
- **النطاق**: خاصية واحدة موثَّقة (`Permissions`) لا `TryGet<T>` عام يفتح الباب لأي نوع.

الخلاصة: `UIServices` حدٌّ موثَّق عند نقطة اضطرار تقنية حقيقية (قيد إنشاء WPF)، لا بديل مقنَّع لحقن المُنشئ.

### كسر الدائرية الحقيقية `FiscalPeriodService ↔ JournalService`

دائرية حقيقية ثنائية الاتجاه (لا وهمية): `JournalService.BuildDto` يحتاج `IFiscalPeriodService.GetPeriodFor(entryDate)` مباشرة عند بناء كل DTO؛ `FiscalPeriodService.CloseYear/ClosePeriod/ReopenYear` يحتاج `IJournalService` — لكن فقط **وقت التنفيذ الفعلي لهذه العمليات**، لا وقت الإنشاء. الحل: `FiscalPeriodService` يستقبل `Lazy<IJournalService>` في مُنشئه (يُسجَّل في `AddApplication()` عبر `services.AddSingleton(sp => new Lazy<IJournalService>(() => sp.GetRequiredService<IJournalService>()));`) — لا يُبنى `IJournalService` فعلياً إلا عند أول `.Value`. `JournalService` يستقبل `IFiscalPeriodService` مباشرة بلا `Lazy` (لا حاجة، لا دائرية من هذا الاتجاه وقت الإنشاء).

### `AccountService` والحل الاختياري لخدمات الأطراف

`ResolveAutoLink` يحتاج `ICustomerService`/`ISupplierService` **اختيارياً فقط** (قد تكون `ISupplierService` غير مسجَّلة إطلاقاً — لا تنفيذ لها بعد). استبدل `ServiceLocator.TryGet<T>(out var x)` بحقن `IServiceProvider _services` في المُنشئ + `(ICustomerService)_services.GetService(typeof(ICustomerService))` (يرجع `null` بأمان لو غير مسجَّلة — نفس دلالة `TryGet` تماماً، بأداة قياسية بدل أداة مؤقتة).

### حاوية الاختبارات

`TestDatabaseFixture` يبني حاوية DI خاصة به بنفس دوال التسجيل الإنتاجية فعلياً (`Services { get; }` في المُنشئ) + `static BuildServices(Action<IServiceCollection> configureOverrides = null)` تُستخدم من أي فئة اختبار تحتاج Fake/Mock (آخر تسجيل يفوز في `Microsoft.Extensions.DependencyInjection`). فائدة جانبية: كل `TestDatabaseFixture` جديد = حاوية جديدة كلياً = `SettingsService` بذاكرة فارغة دائماً — ألغى تماماً حِيلة `SettingsService.Instance.Reload()` القديمة لمنع تسرّب الإعداد بين الاختبارات (لم تعد ذات معنى أصلاً بعد حذف `.Instance`).

### التحقق النهائي لـ R3

`dotnet build` (المشروعين، بعد إصلاح سباق XAML المعروف بإعادة محاولة واحدة) → **0 تحذير جديد، 0 خطأ**. `Tools/ArchitectureCheck/check.sh` → **صفر FAIL، 3 WARN** (نفس الثلاثة المُرقَّمة أعلاه؛ تعليق `ISupplierService` في `DependencyInjection.cs` رُقِّم `// TEMPORARY — يُحذف في R6` ليطابق القاعدة الدائمة الثانية). `dotnet test -m:1` → **134/134 ناجح، صفر تعديل على منطق أي اختبار** (تحويل بنية الوصول للخدمات فقط، لا تغيير في السيناريوهات المُختبَرة).

---

## اكتشاف فني إضافي أثناء R1 (يستحق التسجيل)

**تسمية الطبقة "Application" تتصادم مع `System.Windows.Application`**: أي ملف تحت شجرة `PrimeERP.*` يستخدم `Application.Current`/`: Application` بلا تأهيل كامل يتعرّض لخطر أن يحلّه المترجم كإشارة لمساحة الاسم `PrimeERP.Application` (طبقة 4) بدل نوع WPF — C# يبحث في مساحات الاسم المحيطة صعوداً قبل استشارة `using`. **الحل المُطبَّق**: كل إشارة WPF لـ `Application` في الكود مؤهَّلة بالكامل الآن (`System.Windows.Application`) — 11 ملفاً. أي ملف جديد يستخدم `Application.Current` مستقبلاً **يجب** أن يكتبها مؤهَّلة بالكامل لنفس السبب.

---

## التحقق النهائي لـ R1

`dotnet build PrimeERP.csproj -m:1` → 0 تحذير، 0 خطأ. `dotnet build PrimeERP.Tests/PrimeERP.Tests.csproj -m:1` → 0 خطأ (تحذيرا Nullable سابقان على R1، غير متعلقين به). `dotnet test -m:1 --no-build` → **134/134 ناجح، صفر فشل، صفر تعديل على أي اختبار**.
