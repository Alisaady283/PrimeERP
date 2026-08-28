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
| ~~32 ملف XAML يستهلك `StaticResource` لأبعاد بنيوية بدل `DynamicResource`~~ | ~~`Typography.xaml`/`Metrics.xaml` لم تُدمَجا بعد في سلسلة L1→L2→L3→L4~~ | ✅ **محلول في R4** — L1→L4 كاملة، صفر مفتاح P./S./C. عبر StaticResource خارج الاستثناءات البنيوية الموثَّقة |
| `BackupService.cs` يستدعي `DbHelper` مباشرة (لا Repository) | أوامر `BACKUP`/`RESTORE VERIFYONLY` إدارية على مستوى محرّك القاعدة، لا CRUD كيان — لا يوجد Repository مكافئ منطقياً لها | **لا بند حذف** — استثناء دائم موثَّق، ليس ديناً يُسدَّد |
| `UIServices` (بوابة الوصول الوحيدة لـ code-behind بلا حقن) | بوابة عودة محتملة لنمط `ServiceLocator` لو تُرك استهلاكها يتّسع | **يتقلَّص تدريجياً في R8** كلما تحوَّلت الصفحات لـ Composition (Definitions/Renderers)؛ عند اكتمال R8: مراجعة ما تبقّى وحذف ما أمكن. `check.sh` يفرض الحد الآن: صفر استهلاك خارج `6.UI/**/*.xaml.cs` أو `App/**/*.xaml.cs` |

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

## R4 — طبقة التصميم كاملة (L1 → L4)

### السلسلة

```
L1  5.Design/Identity/{Default,Corporate}/Primitives.{Color,Type,Space,Shape,Motion,Elevation}.xaml
L2  5.Design/Semantic/{Semantic.Light,Semantic.Dark,Semantic.Type}.xaml
L3  5.Design/Components/Tokens.{Button,Input,Dialog,Grid,Nav,Card,Badge,Toolbar,Pagination,Tree,Toast,Document}.xaml
L4  5.Design/Styles/{Implicit,ScrollBars,Style.Button,Style.Input,Style.Dialog}.xaml
```

كل حزمة هوية تختلف فعلياً في الأبعاد الخمسة (إثبات لا شكلي): Brand بنفسجي بدل أزرق، Calibri/Georgia بدل
Segoe UI (+مقياس خط أكبر بنقطة واحدة عبر كل الدرجات)، مسافات ×1.25، زوايا أحدّ (تقريباً النصف)، ظلال أقوى
(Opacity/Blur أعلى وضوحاً).

### ⚠️ توقف 4 — قيد WPF: لا يمكن استعارة قيمة نوع-قيمة (double/Thickness/CornerRadius) تحت اسم L2/L3 جديد

`DynamicResourceExtension` يحتاج `DependencyProperty` حقيقياً على `DependencyObject` ليتعلَّق به — `SolidColorBrush.Color`
أو `DropShadowEffect.BlurRadius` صالحان (الكائن المضيف `Freezable`/`DependencyObject`)، لكن لا يوجد "كائن مضيف"
لقيمة `double`/`Thickness`/`CornerRadius` خام يمكن إعادة تصديرها تحت مفتاح جديد مع بقاء التبديل الحي. **الأثر**:
L2 Semantic.Space/Shape/Motion لا تُبنى كملفات مستقلة (كانت ستكون فارغة من أي مورد حقيقي)؛ الألوان/الظلال/
الطباعة (مغلَّفة في Brush/Effect/Style) تمر عبر L2 كالمخطَّط بالضبط. المسافة/الشكل: L3/L4 تشير لـ L1
(`P.Space.*`/`P.Radius.*`) **مباشرة**، بتعليق موثِّق عند كل استخدام — استثناء صحيح معمارياً لا انحراف عنه،
مفروض بقيد WPF نفسه لا كسلاً. `C.Button.Height.*`/`C.Input.Height`/إلخ (أبعاد مكوّن مستقلة لا تقابل خطوة
واحدة في `P.Space.*`) قيم L3 أصيلة، ثابتة بين الهويات عمداً (ليست من الأبعاد الخمسة المُختبَرة).

### ⚠️ توقف 5 — الاكتشاف الحاسم: `ResourceDictionary` تُجمِّد موارد متداخلة عند أول تحميل

هذا ما فشلت به الآلية الأصلية للتبديل (الموروثة من R1/توقف 3) فعلياً عند اختبار الأبعاد الخمسة كاملة — لم تكن
مشكلة في الاختبار بل في التصميم نفسه:

1. **التاريخ**: ثلاث محاولات مُختبَرة تجريبياً — (أ) تعديل قاموس متداخل داخل Theme.xaml مباشرة، (ب) استبدال
   `Application.Resources` بالكامل بشجرة جاهزة مسبقاً دفعة واحدة، (ج) — الآلية المُستخدَمة منذ R1: تعيين
   `Application.Resources` لقاموس **مصدره Theme.xaml** جديد أولاً، ثم إزالة/إدراج ملفات الهوية الستة كخطوة
   لاحقة منفصلة. الثلاثة كانت تُحدِّث `Border.Background`/`Padding`/`CornerRadius` (خصائص مباشرة على
   `FrameworkElement`) بشكل صحيح — لكن (ج) فشلت لأي مورد **متداخل**: `DropShadowEffect` باسم `ShadowMd`
   (`BlurRadius="{DynamicResource P.Shadow.Md.Blur}"`) ظل عالقاً على قيمة الهوية القديمة للأبد، مهما استُدعي
   `InvalidateProperty`/`ClearValue`/`SetResourceReference` لاحقاً على العنصر أو حتى على الكائن نفسه.
2. **السبب الجذري (مُثبَت بتشخيص مباشر)**: `Application.Current.TryFindResource("ShadowMd")` **طازجاً بلا أي
   علاقة بعنصر حيّ** أعاد Blur الهوية القديمة أيضاً — أي أن العطل ليس في مسار إبطال خاصية عنصر، بل في
   `ResourceDictionary` نفسها: أول مرة يُطلَب فيها مورد مُركَّب (هنا `ShadowMd`، الذي يعيش في `Semantic.Light.xaml`
   ويُدمَج **بعد** ملفات الهوية) تُقيَّم إشارته المتداخلة (`P.Shadow.Md.Blur`) نسبةً لحالة الشجرة **في تلك
   اللحظة بالضبط**، ثم تُجمَّد النتيجة داخل الكائن ضمن تلك النسخة من `ResourceDictionary` — لا تُعاد تقييمها
   لاحقاً مهما تغيّر ما تشير إليه. بما أن `Theme.xaml` (وبالتالي `Semantic.Light.xaml`) كان يُحمَّل **قبل**
   إزالة/إدراج ملفات الهوية الفعلية (الترتيب ج)، ولو للحظة واحدة فقط، تتجمَّد `ShadowMd` على الهوية **القديمة**
   للأبد — بخلاف خاصية محلية مباشرة على `FrameworkElement` حيّ (`Border.Background`)، التي **تُعاد** تقييمها
   بصورة صحيحة عند أي تغيّر لاحق في سلسلة الموارد بفضل اشتراكها الحيّ في شجرة العرض.
3. **الحل**: عكس ترتيب البناء — الهوية **أولاً**، ثم `Theme.xaml`، لا العكس أبداً. `Theme.xaml` نفسه عُدِّل
   ليخلو من أي مرجع لملفات الهوية (لم يعد صالحاً للدمج المباشر وحده). `IIdentityService.Apply` يبني الشجرة
   يدوياً: قاموس فارغ جديد حيّ ← الستة ملفات `Identity/{key}/Primitives.*.xaml` ← `Theme.xaml` (بلا هوية
   مُضمَّنة) ← القواميس المحفوظة (نصوص اللغة، تراكب الوضع الداكن). بهذا الترتيب، لحظة تحميل `Semantic.Light.xaml`
   لأول مرة، الهوية الصحيحة **موجودة بالفعل** في السلسلة — فتُقيَّم `ShadowMd` (وأي مورد متداخل مشابه) بشكل
   صحيح من أول مرة، بلا نافذة "هوية خاطئة" ولو للحظة.
4. **⚠️ فخ مصاحب اكتُشف أثناء الإصلاح**: القواميس "المحفوظة" (preserved، أي ما ليس Theme.xaml) يجب أن
   تستبعد ملفات الهوية القديمة أيضاً صراحة، لا `Theme.xaml` فقط — وإلا تُعاد إضافتها هي نفسها في نهاية القائمة
   (أعلى أولوية في `MergedDictionaries`، آخر تعريف يفوز) فتطغى على الحزمة الجديدة بالكامل بصمت.

**الدرس العام**: أي مورد WPF **مُركَّب** (Effect، وبالقياس أي `Freezable` آخر غير Brush) يشير بدوره لمورد آخر
عبر `DynamicResource` **يجب أن يُحمَّل لأول مرة بعد** استقرار كل ما يعتمد عليه، لا قبله ولو مؤقتاً — خلافاً
لخاصية محلية مباشرة على عنصر حيّ في شجرة العرض، التي تتسامح مع الترتيب لأنها تُعاد تقييمها دوماً بصورة صحيحة.

### الاختبار الحاسم

`IdentityServiceTests.Apply_LiveSwap_ChangesAllFiveDimensionsOnAnAlreadyRenderedElement` — عنصر `Border`
واحد (+ `TextBlock` ابن بنمط `Style` حقيقي) يُنشأ **مرة واحدة**، يُعرض في نافذة فعلية، تُقرأ قيمه الخمس
(`Background`/`Padding`/`CornerRadius`/`Effect`/`FontFamily`+`FontSize`) قبل `Apply("Corporate")` وبعده — بلا
إعادة إنشاء العنصر وبلا تعديل ملف قطعة واحد. + `SemanticDark_RedefinesEveryKeyInSemanticLight` (تكافؤ فاتح/داكن
مفتاحاً بمفتاح، فحص نصي) + `ComponentAndStyleLayers_ContainNoLiteralHexColors` (صفر Hex حرفي خارج L1، فحص نصي
عبر L2/L3/L4 كاملة).

### فحص `check.sh` المُضاف

`StaticResource` لمفتاح يبدأ بـ `P.`/`S.` = FAIL دائماً بلا استثناء. لمفتاح `C.*` = FAIL إلا للقائمة المسموحة
صراحة (`C.Nav.Icon.ColumnWidth`، `C.Grid.RowHeader.Width` — ثوابت بنيوية موثَّقة في Tokens نفسها). استهلاك
`UIServices.` خارج `6.UI/**/*.xaml.cs` أو `App/**/*.xaml.cs` = FAIL (القيد المُلزَم من تأكيد المستخدم قبل R4)
— كشف خرقين حقيقيين فعلاً عند أول تشغيل: `NavItemViewModel`/`PermissionAwareViewModel` كانا يستهلكانها من
داخل ViewModel لا code-behind؛ أُصلحا بحقن `IPermissionService` عبر المُنشئ من المستدعي الأول (`AppSidebar.xaml.cs`)
بدل الوصول المباشر.

### اكتشاف جانبي جسيم: مكوّنات كاملة كانت تشير لموارد ميتة أصلاً

فحص شامل (كل `x:Key` مُعرَّف مقابل كل `StaticResource`/`DynamicResource`/`FindResource` مُستخدَم، XAML وC# معاً)
كشف أن **~24 ملفاً** (كل مكوّنات `Display`/`Documents`/`Feedback`/`Shell`/`Layout`/`Pickers` تقريباً، عدا
Button/Input/Dialog المُهاجَرة سابقاً) تستهلك 27 مفتاحاً غير موجود إطلاقاً في أي قاموس حالي (`PanelBrush`،
`OutlineBrush`، `BodyTextBrush`، `FontWeightSemibold`، `OkBrush`، ...) — بقايا تسمية قديمة (`Colors.xaml` قبل
`Semantic.Light/Dark.xaml`) لم تُهاجَر قط عند ذلك التحوُّل السابق. **الأثر الفعلي**: كل هذه المكوّنات كانت
ستُطلق `XamlParseException` (مورد غير موجود) في أول لحظة تُعرَض فيها فعلياً — لم يظهر هذا في `dotnet build`
(لا يتحقَّق من مراجع Application-level وقت الترجمة) ولا في الاختبارات (لا اختبار حالي يُصيِّر هذه المكوّنات).
أُصلحت جميعها إلى مفاتيح L2/L3 حقيقية (جدول تحويل كامل، أُطبِّق عبر `sed` منهجي ثم تحقُّق نصي صفري النتيجة).

### التحقق النهائي لـ R4

`dotnet build` (المشروعين) → **0 تحذير جديد، 0 خطأ**. `Tools/ArchitectureCheck/check.sh` → **صفر FAIL، 2 WARN**
(كلاهما دين تقني سابق غير متعلِّق بـ R4). `dotnet test -m:1` → **136/136 ناجح** (134 سابقة + اختباران جديدان:
الحاسم + تكافؤ Light/Dark؛ اختبار "صفر Hex حرفي" الثالث ضمن نفس ملف الاختبار). صفر مفتاح ميت متبقٍّ (تحقُّق
شامل XAML+C# قبل الإغلاق).

---

## R5 — WhereBuilder + RepositoryBase + إعادة بناء المستودعات

المستودعات الستة تحوَّلت من `static class` إلى instance classes ترث `RepositoryBase<T>` (`2.Data/Repositories/Base`)
وتنفّذ واجهة `I*Repository` مسجَّلة في `AddData()`، تُحقن عبر المُنشئ في كل خدمة/Seeder/اختبار مستهلِك (~15 ملفاً).
سبب القرار: استدعاء ساكن لمستودع هو نفس عيب `ServiceLocator` المحذوف في R3 (اعتمادية مخفية) — راجع القاعدة
العامة "الصحيح لا الأسرع" التي أكّدها المستخدم صراحة عند هذا الفرق.

`RepositoryBase<T>` يمتص تكرار (conn,tx) الحقيقي: كل دالة قراءة/كتابة كانت تُكتب مرتين (نسخة عادية + نسخة
`(DbConnection, DbTransaction)`) أصبحت دالة واحدة ببارامترين اختياريين. `WhereBuilder` (`2.Data/Query`) يبني
WHERE شرطياً بدل تكرار `var where = new List<string>()` يدوياً في كل استعلام صفحات/بحث.

المستودعات ذات الكيان الواحد (Account/Customer/Backup/NumberSequence) قريبة أو تحت هدف الـ60 سطراً.
Journal وFiscalPeriod يديران كيانين مرتبطين كلٌّ (Entry+Line، Year+Period) عبر `QueryAs<TOther>` — أكبر
حجماً (246 و159 سطراً) بحكم تعدد الكيان الفعلي، لا تكراراً غير مبرَّر.

فحصان جديدان في `check.sh`: صفر استدعاء ساكن لأي مستودع في المشروع كله؛ (سبق التنفيذ ولم يتغيّر).

136/136 اختباراً ناجح (بلا تغيير في العدد — فقط طريقة حصول الاختبارات على المستودع تغيّرت من استدعاء ساكن
إلى حقن، لا السلوك المُختبَر). صفر FAIL في check.sh. أثناء التحويل ظهر خطآن حقيقيان سابقان (`CustomerRepository.
GetAll` كان يبني WHERE عبر WhereBuilder بلا تمرير بارامتراتها الفعلية؛ `NumberSequenceRepository.EnsureRowCore`
كان يمرّر استعلام تحقق وجود (`SELECT 1`) عبر دالة الـ Map الخاصة بالكيان الكامل) — أصلحهما الاختبار نفسه.

---

## R6 — Pipeline/Steps/Operations + ServiceBase/CrudServiceBase/PartyServiceBase + إعادة بناء الخدمات

### البنية التحتية

`4.Application/Pipeline/`: `PipelineContext` (Conn/Tx/Input/Output/Items/Log)، `IStep`، `Steps/` (Permission
/Validation/Transaction/Audit/Sequence/Setting/Func)، `Pipeline<TOut>` (مؤلِّف مسطّح: `.Permission().Validate()
.Rule().InTransaction(tx => tx.Before().Save().After()).Audit().Execute(input)`)، و`Operations/WriteOperation
<TDto>` (لعمليات Create/Update/Delete/Post/Unpost: صلاحية+تحقق تُلحَقان فوراً، Rule تُلحَق مباشرة قبل المعاملة،
Before/After تُخزَّن وتُنسَج داخل معاملة واحدة عند `.Run()` — هذا الترتيب المؤجَّل هو ما يجعل شكل الاستدعاء
النهائي `CreateOp(dto).Rule(x).Before(y).After(z).Run()` ينتج تسلسلاً صحيحاً رغم أن الاستدعاء نفسه مسطّح).
`TransactionStep` يعيد استخدام معاملة خارجية مفتوحة بدل فتح اتصال جديد لو `ctx.Conn != null` (نفس قيد تفادي
الـ deadlock الموثَّق في `DbHelper.Query`). اختُبرت البنية بكيان وهمي (`TestEntity`, 11 اختباراً) قبل أي استخدام
حقيقي، إلزامياً حسب أمر R6.

`ServiceBase` (`4.Application/Services/`): `Require/Can/Fail/Ok/Msg/Setting<T>/Check<T>/Tx` — تمتص فحص الصلاحية
(`Can(action)` يبني `"{PermissionPrefix}.{action}"`)، الرسالة المترجمة (`Msg(key)` يبني `"{StringPrefix}.{key}"`)،
والمعاملة المخصّصة (`Tx`: body يرجع `Result.Fail` بدل رمي استثناء، فالتراجع يحدث عبر استثناء تحكّم داخلي `Tran
sactionAbortedException` بلا كسر تدفّق الاستثناءات العادي). `CrudServiceBase<TEntity,TDto,TFilter>` يعمّم القراءة
فقط (GetById/GetPaged/Search) — الإنشاء/التعديل/الحذف بقيا خارج القالب العام عمداً: منطق الأعمال الفعلي (ربط
حساب، تحذيرات تكرار، فحص حد ائتماني) مختلف بدرجة تجعل قالباً واحداً يُخفي المنطق بدل أن يلخّصه، فتُبنى مباشرة في
كل خدمة مستخدمةً دوال `ServiceBase` الجاهزة. `PartyServiceBase<TEntity,TDto,TFilter> : CrudServiceBase` يحمل
المنطق المشترك الحقيقي بين العملاء والموردين: ربط حساب فرعي (`GetParentAccount`/`CreateLinkedAccount`)،
الاتجاه المعاكس (`CreateFromAccount`/`DeleteByAccountCode`/`UpdateNameFromAccount` يستدعيها `AccountService`)،
`RecalculateBalance`/`GetStatement` من `IAccountService` دائماً (لا حساب مزدوج).

`AuditLogger` تحوَّل من `static` إلى `IAuditLogger` (كل مستدعيه الستة كانوا فعلاً داخل نطاق R6). `Localization
Service` بقي `static` كما هو (7 مستهلكين في 6.UI لتبديل لغة الواجهة الحيّة لا معنى لحقنهم) وأُضيف `ILocalization
Service`/`LocalizationAdapter` كجسر قابل للحقن لطبقة الخدمات فقط.

### مزوّد (Provider) مقابل خدمة (Service) — قاعدة معمارية عامة اكتُشفت عبر SettingsService

**المشكلة الأولى**: `SettingsService` (كانت في `1.Platform/Settings/`) لا يمكن أن ترث `ServiceBase` لأن
`ServiceBase` يحقن `ISettingsService` — وهي نفسها. مُيّز هذا أول الأمر خطأً كـ"استثناء معماري موثَّق" (يمرّر
`null` في المُنشئ)، فحقّق تخفيضاً 155→123 سطراً (~21%) فقط — دون شرط الـ50% الذي حدَّده R6.

**التشخيص الصحيح** (صحّحه المستخدم صراحة): السبب لم يكن اعتماداً ذاتياً دائرياً بل **موضع خاطئ**. `1.Platform`
لا يجوز أن يحوي "خدمة" بمعنى `ServiceBase` (صلاحية+audit+رسائل+معاملة أعمال) — الطبقات الدنيا تحوي **مزوّدات**
(عمليات تقنية بحتة بلا أيٍّ من ذلك) فقط. القيد ليس خارجياً (منصة/WPF/.NET) بل ناتج عن وضع الملف في الطبقة
الخطأ — فيُصلَح بالفصل، لا بالتوثيق كاستثناء.

**الفصل المُنفَّذ**:
- `1.Platform/Settings/ISettingsProvider.cs` + `SettingsProvider.cs` — قراءة/كتابة خام + Cache فقط، بلا صلاحية
  ولا audit ولا رسائل، يعتمد على `SettingRepository` مباشرة. تستهلكه الطبقات الدنيا (`ServiceBase.Setting<T>`
  نفسها تقرأ منه الآن) و`SettingStep` في الـ Pipeline (قراءة تقنية بحتة، لا تحتاج بوابة صلاحية).
- `4.Application/Services/System/ISettingsService.cs` + `SettingsService.cs` (مساحة الاسم `PrimeERP.Application.
  Services` وليس `...Services.System` — راجع اكتشاف تصادم الأسماء أدناه) — يرث `ServiceBase` الآن فعلياً، يحقن
  `ISettingsProvider` (لا نفسه)، يضيف الصلاحية والتدقيق فوق القراءة/الكتابة الخام. `SetMany` تُفوَّض كاملة إلى
  `ISettingsProvider.SetManyRaw` (المعاملة نفسها عملية تقنية بحتة، لا قرار أعمال).
- `check.sh`: فحص جديد — أي استدعاء لـ`ISettingsService` من `1.Platform`/`2.Data` = FAIL.

**النتيجة**: `SettingsService.cs` (Application) = 69 سطراً مقابل 155 الأصلية = **55.5% تخفيض**، يحقق الشرط.
147/147 اختباراً ناجح بلا أي تعديل. **القاعدة العامة المستخلصة (تُطبَّق على أي حالة مشابهة لاحقاً — Audit،
Permissions، إلخ)**: أي شيء في `1.Platform` يحتاج `ServiceBase` فهو في الموضع الخطأ ويُقسَّم لا يُستثنى. معيار
التفرقة بين استثناء موثَّق وقيد يُصلَح: هل السبب خارجي (منصة/.NET/WPF لا حل له)؟ استثناء موثَّق. هل السبب داخلي
(تصميمنا نحن — موضع ملف، توزيع مسؤولية، اعتمادية)؟ يُصلَح معمارياً دائماً، لا يُوثَّق كاستثناء.

### اكتشاف تصادم أسماء إضافي (نفس فئة اكتشاف R1: `Application` × `System.Windows.Application`)

تسمية مجلد/مساحة اسم `4.Application/Services/System/` بمساحة اسم `PrimeERP.Application.Services.System` تكسر أي
مرجع غير مؤهَّل لـ`System.*` (مثل `System.Windows.MessageBox`) داخل أي ملف تحت `PrimeERP.Application.Services.*`
— C# يبحث في مساحات الاسم المحيطة صعوداً عن مقطع اسم مطابق (`System`) **قبل** استشارة `using` العام، فيجد
`PrimeERP.Application.Services.System` (مساحة اسم شقيقة حقيقية الآن) بدل `System` العامة. **الحل**: المجلد بقي
`4.Application/Services/System/` كما طُلب، لكن مساحة الاسم أُبقيت مسطّحة `PrimeERP.Application.Services` (لا
`.System` كمقطع أخير) — أي مقطع مساحة اسم جديد يطابق اسم مساحة اسم من BCL/WPF (`System`, `Application`, `Data`,
`Windows`...) يجب تفاديه أو تأهيله بالكامل في كل استخدام.

### خطأ حقيقي وُجد ومُصلِح في ServiceBase قبل أن ينتشر

`ServiceBase.Fail`/`Fail<T>` كانت تتجاهل ErrorCode المُمرَّر من المستدعي وترجع `ErrorCode.Unexpected` دائماً
(الدالة الأصلية كانت `Fail(key, params object[] args) => Result.Fail(Msg(key, args))` — بلا مُعامل code إطلاقاً).
`SettingsService` و`CrudServiceBase` (كلاهما مُلتزَم بالفعل) كانا يستخدمانها بافتراض أن ErrorCode.Unauthorized/
NotFound سيُحفظ — لم يكن. اكتُشف قبل لمس AccountService (التي 3 من اختباراتها تتحقق من `ErrorCode.Unauthorized`
صراحة) — لو أُهمِل هذا لفشلت اختبارات AccountServiceTests/CustomerServiceTests/JournalServiceTests/
FiscalPeriodServiceTests بصمت لاحقاً. أُصلح بإضافة تحميل زائد `Fail(key, code, args)` صريح، وأُعيد فحص/تصحيح كل
استدعاء سابق لـ`Fail`.

**اكتشاف ثانٍ مرتبط**: أربع خدمات (Account/Journal/FiscalPeriod/Customer) تستخدم مفتاح رسالة "رفض الصلاحية" عاماً
مشتركاً `"Str.PermissionDenied"` (لا مفتاحاً خاصاً بكل وحدة كـ`"Str.Settings.PermissionDenied"`) — نمطان
مختلفان موجودان فعلياً في الكود القديم معاً. أُضيفت `FailDenied()`/`FailDenied<T>()` لـServiceBase خصيصاً لهذا
النمط المشترك، منفصلة عن `Fail("PermissionDenied")` العادية (تبني `"{StringPrefix}.PermissionDenied"`).

### جدول التخفيض (قبل/بعد) لكل خدمة

| الخدمة | قبل | بعد | ملاحظة |
|---|---|---|---|
| SettingsService (+ SettingsProvider الجديدة) | 155 | 69 + 88 = 157¹ | فُصلت لمزوّد/خدمة — 69 سطر خدمة فعلية = 55.5% تخفيض عن الأصل المدمَج |
| NumberSequenceService | 61 | 55 | مزوّد تقني بالفعل — لا صلاحية/audit لإزالتها؛ الوحيد المُبسَّط تكرار Next×2 |
| BackupService | 303 | 294 | منطق I/O وتحقق فريد غالباً — لا تكرار عابر للخدمات لإزالته |
| PermissionService | 52 | 40 | مزوّد تقني بالفعل — لا ServiceBase (يفحص هو نفسه الصلاحية، تبسيط DevMode فقط) |
| AccountService | 686 | 669 | منطق أعمال (شجرة، ربط تلقائي) هو الغالب، لا Boilerplate |
| JournalService | 641 | 627 | نفس السبب — تسع خطوات تحقق الترحيل منطق فريد لا يتكرر |
| FiscalPeriodService | 415 | 409 | صلاحيات عبر وحدة Settings لا وحدة خاصة؛ Audit جدولين مختلفين |
| CustomerService | 486 | 378 | 22% تخفيض — GetById/GetPaged/Search/GetStatement/CreateFromAccount/DeleteByAccountCode/UpdateNameFromAccount/RecalculateBalance انتقلت لـCrudServiceBase/PartyServiceBase |
| SupplierService (جديدة) | — | 302 | بناء جديد كامل فوق PartyServiceBase — نفس القطع المشتركة مع Customer |

¹ SettingsProvider تُستهلك أيضاً من ServiceBase.Setting&lt;T&gt; والـPipeline (SettingStep) — ليست خاصة بـSettingsService وحدها، فمقارنتها المباشرة بالسطر-إلى-سطر مضلِّلة؛ الرقم الحاسم للبوابة هو 69 سطر خدمة التطبيق.

**ملاحظة صادقة**: التخفيضات الكبيرة (Settings 55.5%، Customer 22%) حصلت حيث كان الـBoilerplate (فحص صلاحية +
رسالة + ErrorCode، أو استعلامات CRUD عامة) فعلاً الجزء الأكبر من الملف. الخدمات الأخرى (Account/Journal/
FiscalPeriod/Backup) تخفيضها متواضع لأن أغلب سطورها منطق أعمال حقيقي غير مكرَّر — عزله محسِّن (صلاحية/تدقيق/
رسائل موحَّدة عبر ServiceBase الآن) لكنه لا يُقلِّص العدد الكلي كثيراً. `SupplierService` (302 سطر) و`CustomerService`
(378 سطر) يتجاوزان هدفي R6 الرقميين (&lt;100 و&lt;150 على الترتيب) — السبب نفسه: `Create` بمعاملتها الذرية
(exception-based rollback)، `Update` بنسخ الحقول، و`CheckCreditLimit` بحسابها — منطق حقيقي غير قابل للاختزال
بأمان عبر قالب Pipeline عام بلا مخاطرة حقيقية على صحة السلوك.

### منطق محاسبي نُقل حرفياً (سطراً بسطر) — للمراجعة المستقبلية

- **AccountService**: توليد كود الابن (أقصى رقم فرعي حالي + 1)، مستوى الحساب من الأب لا من طول الكود، منع إضافة
  ابن تحت حساب Leaf، `IsLeaf` الأب لا تتغيّر تلقائياً، `ResolveAutoLink` عبر `IServiceProvider.GetService` مع
  Fail صريح لو الخدمة غير مسجَّلة، `RecalculateBalance` بإعادة حساب كامل من القيود المرحّلة دائماً (لا تراكمي)،
  `GetStatement` برصيد جارٍ.
- **JournalService**: تسلسل تحقق الترحيل التسعة (`ValidatePostable`) بنفس الترتيب، الأرصدة تُحدَّث فقط عند
  الترحيل، منع سطر بمدين ودائن معاً، منع تعديل/حذف قيد مرحّل، منع Unpost لقيد إقفال سنوي (`ClosingEntrySource`)،
  `PostBatch` معاملة واحدة كل-أو-لا-شيء، ميزان المراجعة بالجانب الطبيعي الصحيح لكل نوع حساب، `GetAccountSums`
  عبر استعلام `GROUP BY` مجمَّع واحد لا حلقة.
- **FiscalPeriodService**: تقسيم الفترات لمدى تواريخ متصلة، قاعدة `IsOpen` ("لا فترة معرَّفة = مفتوحة" مع تجاوز
  `RequireFiscalPeriod`)، منع إقفال فترة قبل إقفال سابقتها، قيد إقفال سنوي متوازن (عكس أرصدة الإيرادات/المصروفات
  + الفرق لحساب الأرباح المحتجزة)، `Lazy<IJournalService>` لكسر الدائرية مع JournalService.
- **CustomerService/SupplierService (PartyServiceBase)**: `SkipAutoLink` إلزامي عند الربط التلقائي (يمنع حلقة
  لا نهائية)، `CreateFromAccount`/`DeleteByAccountCode`/`UpdateNameFromAccount` اتجاه معاكس بلا حلقة ping-pong،
  مزامنة الاسم اتجاه واحد فقط، `CheckCreditLimit` حيث 0 = بلا حد، الرصيد دائماً من الحساب المرتبط (لا حساب مزدوج).
- **كل الخدمات الثمانية**: كل توقيع `(conn,tx)` ونمط "القراءة الآمنة" (تفادي deadlock داخل معاملة خارجية عبر
  تمرير نفس الاتصال، لا فتح اتصال جديد) محفوظ حرفياً بلا استثناء.

---

## قبل R7 — أول تشغيل فعلي للتطبيق كشف عن ثغرتين تأسيسيتين

R1–R6 كلها تحقّقت عبر `dotnet build`/`dotnet test` — لا أحد شغّل `PrimeERP.exe` فعلياً طوال المشروع حتى بداية R7.
أول تشغيل حقيقي فشل فوراً، بثغرتين منفصلتين تماماً، كلتاهما لم يكشفها البناء ولا الاختبارات.

### ⚠️ توقف 6 — L3 (Components) يخلط Color بـ Brush في كل ملف تقريباً

**العرض**: `System.InvalidOperationException: '#00FFFFFF' is not a valid value for property 'Color'` عند أول
`Border`/عنصر يستهلك أي مفتاح `C.*`. لاحقاً (بعد إصلاح جزئي): `ResourceReferenceKeyNotFoundException` لمفاتيح
دلالية عادية.

**السبب الجذري**: كل ملفات `5.Design/Components/Tokens.*.xaml` (94 موضعاً عبر 11 ملفاً) تكتب
`<SolidColorBrush x:Key="C.X" Color="{DynamicResource BrandDefault}"/>` — لكن `BrandDefault` (وأمثاله الـ31)
**Brush لا Color**. `DynamicResourceExtension` يحتاج DependencyProperty حقيقياً على DependencyObject
ليتعلَّق به (نفس قيد ⚠️ توقف 4) — `SolidColorBrush.Color` مضيف صالح، لكن فقط لو المصدر Color بالفعل؛ WPF لا
يحوِّل Brush→Color تلقائياً، فيفشل وقت التشغيل حصراً (لا بناء، لا اختبار يُصيِّر هذه الملفات فعلياً).

**محاولتان فاشلتان قبل الحل**، للتوثيق (كلتاهما بُنيتا بنجاح، فشلتا وقت التشغيل فقط — الدرس: نجاح `dotnet build`
لا يعني صحة XAML وقت التشغيل):
1. `<Color x:Key="X.Color">{DynamicResource Y}</Color>` — محتوى عنصر `Color` لا يُفسَّر كـ Markup Extension،
   يُمرَّر كنص حرفي لـ `ColorConverter` فيفشل.
2. `Color="{Binding Color, Source={DynamicResource Y}}"` — WPF يرفض صراحة: *"A 'DynamicResourceExtension' can
   only be set on a DependencyProperty of a DependencyObject"* — `Binding.Source` ليست كذلك.

**الحل**: استخراج Color برمجياً من كل Brush دلالي بعد استقرار الشجرة، لا عبر XAML إطلاقاً —
`IdentityService.RefreshDerivedColors()` تقرأ كل Brush من الـ31 عبر `TryFindResource`، وتحقن
`app.Resources["{Name}.Color"] = brush.Color` (قيمة Color خام مباشرة، بلا أي DynamicResource متداخل، فلا خطر
تجميد). كل الـ94 موضعاً في L3 تحوَّلت لتشير لـ`{DynamicResource X.Color}` بدل `{DynamicResource X}`.

**تبعية بنيوية اكتُشفت أثناء الحل**: `Theme.xaml` كان يدمج L2 (Semantic) وL3 (Components) في خطوة واحدة —
`RefreshDerivedColors()` تحتاج L2 مستقرة *قبل* أول تحميل لـ L3 (نفس درس ⚠️ توقف 5 بالضبط: أي شيء مُركَّب
يعتمد عليه شيء آخر يجب أن يستقر أولاً). **الحل**: فصل `Theme.xaml` إلى `Theme.Semantic.xaml` (L2 فقط) +
`Theme.xaml` (L3+L4). `IIdentityService.Apply` الآن: هوية → `Theme.Semantic.xaml` → `RefreshDerivedColors()` →
`Theme.xaml` → القواميس المحفوظة. نفس الاستثناء المطلوب لملفات الهوية طُبِّق أيضاً على `Theme.Semantic.xaml`
في فلتر "المحفوظة" (وإلا تكرَّر نفس فخ ⚠️ توقف 5 الفرعي).

**⚠️ قيد متبقٍّ موثَّق لا مُصلَح**: `ApplyMode` (تبديل فاتح/داكن التفاعلي، لا عبر إعادة تشغيل) يستدعي
`ThemeService.Apply` الذي يُضيف/يُزيل `Semantic.Dark.xaml` **تراكمياً** على الشجرة الحيّة، لا إعادة بناء كاملة
مثل `Apply` (تبديل الهوية). أي Brush في L3 **يكون قد تجمَّد بالفعل** (عُرض عنصر حيّ يستهلكه) وقت تبديل تفاعلي
لن يتّبع اللون الجديد فوراً — نفس آلية التجميد في توقف 5، تنطبق هنا أيضاً على أي عنصر مُركَّب لم يُعَد بناؤه من
الصفر. غير حرج للإقلاع (تبديل الوضع المحفوظ يحدث في `Initialize()` قبل أي عرض)، لكنه قيد حقيقي على التبديل
التفاعلي بعد الإقلاع يستحق معالجة مستقلة لاحقاً (على الأرجح: توحيد `ApplyMode` مع `Apply` لإعادة بناء كاملة
بدل الإضافة/الحذف التراكمي).

### ⚠️ توقف 7 — التطبيق الفعلي لم يكن يُنشئ مخطَّط قاعدة البيانات إطلاقاً

**السبب**: `App.xaml.cs.OnStartup` يبني حاوية DI ويستدعي `IIdentityService.Initialize()` مباشرة — بلا أي
استدعاء لإنشاء الجداول أو تشغيل الزارعين (Seeders) أو `MigrationRunner.RunPending()`. **فقط**
`TestDatabaseFixture` (بيئة الاختبار) كانت تفعل ذلك — تسلسل حقيقي منذ R3، لم يُستدعَ قط من التطبيق الحقيقي.
النتيجة: `IIdentityService.Initialize()` (وأي قراءة إعداد أخرى) تفشل بصمت بـ
`SqliteException: no such table: AppSettings` — يبتلعها try/catch الموثَّق عمداً في `Initialize()` (مصمَّم
لالتقاط "قاعدة غير جاهزة بعد"، لكنه كان يلتقط فعلياً "قاعدة لم تُبنَ إطلاقاً"). هذا ما أخفى غياب L2 عن
`Application.Resources` الأساسية (توقف 6) — لولا هذا، كانت `Danger` وأمثالها ستُوجَد دائماً عبر Theme.xaml
القديم غير المُقسَّم؛ لكن الثغرتين حقيقيتان مستقلتان، لا إحداهما سبب الأخرى.

**الحل**: `DependencyInjection.EnsureDatabaseReady(IServiceProvider)` — دالة توسيع جديدة، نفس تسلسل
`TestDatabaseFixture` حرفياً (CreateTable لكل مستودع + الزارعون + `MigrationRunner.RunPending()`)، بأمان
الاستدعاء المتكرر (CREATE TABLE IF NOT EXISTS، والزارعون تتحقق داخلياً من عدم التكرار). تُستدعى من
`App.xaml.cs.OnStartup` فوراً بعد بناء الحاوية، قبل `IIdentityService.Initialize()`.

### مبدأ استخراج القطعة (يحكم كل تعميم من R7 فصاعداً)

القطعة تُستخرج حين يكون **العقد موحّداً**، لا حين يتشابه الاسم. اختبار الصحة: هل التجريد سيحتوي
`if`/`switch` لكل كيان؟
- **نعم** → ليس قطعة، بل تكرار مُخفى داخل تجريد كاذب. لا تعمّم.
- **لا** → قطعة صحيحة. عمّم.

تطبيق فعلي (R6/R7): `GetPaged`/`GetById`/`Search`/`Delete` عقودها موحّدة حرفياً عبر كل الخدمات → عُمِّمت
(`CrudServiceBase`، ولاحقاً `PagedViewModelBase`/`CrudViewModelBase`). `Create`/`Update` تختلف DTOs شكلاً
بين كيان وآخر (مثال حقيقي: `JournalService.Update` يأخذ `CreateJournalDto` لا `UpdateJournalDto` مستقلة) →
تعميمها كان سيحتاج `if` لكل كيان داخل القاعدة، فبقيت في كل خدمة/ViewModel فعلي.

### R7 — ترحيل مسافات L1 عبر 6.UI/Components (تطبيق مبدأ استخراج القطعة على القيم الحرفية)

فحص فعلي لكل قيم `Margin`/`Padding`/`Width`/`Height` الحرفية عبر 22 ملف قطعة أظهر: **معظمها (~70%) أرقام
ضبط دقيق خاصة بمكانها** (`Margin="2,3,3,3"`، `Width="11"`...) لا تطابق سلّم `P.Space`
(0/4/8/12/16/20/24/32/40/48/64) ولا تتكرر بنمط — نفس اختبار مبدأ استخراج القطعة أعلاه يمنع تعميمها (ستحتاج
رمزاً جديداً لكل قيمة = تجريد كاذب). **رُحِّلت فقط** القيم المطابقة حرفياً للسلّم ضمن شكل بسيط (موحّد أو باتجاه
واحد) — أُضيف ~13 مفتاح `Thickness` جديد لهذه الأشكال المتكررة فعلياً (`P.Space.N.Top/Bottom/Left/Right/
Uniform/Horizontal`) في `Primitives.Space.xaml` (Default وCorporate معاً، بنفس معامل ×1.25). القيم غير
المطابقة بقيت حرفية عمداً — ليست قطعة.

**استُبعد Width/Height على عناصر الأيقونات/التحكم الذاتية الحجم** (Path بحجم أيقونة، أزرار spinner، عمود
DataGrid) رغم تطابق أرقامها أحياناً مع سلّم المسافة — القياس هناك ينتمي لمقياس "حجم" مختلف مفاهيمياً عن
"مسافة"، وربطه بـ`P.Space` قسراً يخلق اقتراناً غير مقصود (تغيير سلّم المسافة سيُحرّك أحجام الأيقونات) رغم
تطابق الأرقام حالياً. الاستثناء الوحيد المُرحَّل من نوع Width: `ColumnDefinition.Width` كعمود فاصل حقيقي بين
محتوى (`DocumentHeader`/`DocumentFooter`) — استخدام مسافة أصيل.

**⚠️ توقف 8 — اكتُشف فعلياً عبر تشغيل حقيقي، لا بناء**: `ColumnDefinition.Width` من نوع `GridLength` لا
`Double` — نفس قيد ⚠️ توقف 4/6 بالضبط (DynamicResource يُسنِد الكائن المُحلَّل مباشرة عبر SetValue بلا محوِّل
نوع، بخلاف تحليل XAML النصي العادي الذي يمرّ عبر TypeConverter). ربط `ColumnDefinition.Width` بمفتاح
`P.Space.4` (Double) بنى بلا خطأ لكن انهار عند أول تشغيل فعلي (`InvalidCastException: Unable to cast ...
Double to ... GridLength`) — التقطه فقط تشغيل `PrimeERP.exe` حقيقي (الأخطاء هذه لا تظهر بـ`dotnet build` ولا
`dotnet test` القياسي). **الحل**: مفتاح `GridLength` مستقل `P.Space.4.Column` (لا Double) بنفس القيمة، بنفس
منطق `P.Space.4.Uniform` (Thickness مستقلة) — درس عام: **كل نوع DependencyProperty هدف يحتاج مورداً من نفس
نوعه حرفياً، لا اشتقاقاً ضمنياً، مهما بدت القيمة الرقمية متطابقة.**

### R7 — AppSidebar بالمواصفة (⚠️ توقف 4/6/8، لا مصفوفة قيم — قطعة واحدة فقط)

فحص `AppSidebar.xaml` مقابل المواصفة الثلاثية:
- **Hover أفتح قليلاً لا أبيض**: مُحقَّقة أصلاً بلا تعديل — `NavHover` تُحلَّل إلى `P.Color.Nav.700` = `#1E2438`
  (أفتح من `NavSurface`=`P.Color.Nav.900`=`#12162A` الأساس، وليس أبيض إطلاقاً). تحقَّق بقراءة القيم الفعلية،
  لا افتراضاً.
- **عمود أيقونة 24 ثابت**: كان `22` — صُحِّح لـ`24` حرفياً (عمود Grid واحد، لا مصفوفة قيم تستحق رمزاً).
- **حشو طبقة واحدة**: `itemsHost` (المضيف الخارجي لكل الصفوف) كان يحمل `Margin="8,4"` — حشو أفقي مكرِّر فوق
  حشو الأعمدة الداخلية لكل صف (عمود الفجوة+عمود الأيقونة يؤديان الحشو الأفقي الفعلي بالفعل). أُزيل الشق
  الأفقي (`Margin="0,4"` — الرأسي فقط يبقى، غير متعلّق بحشو الصف نفسه).
- **إصلاح إضافي مكتشف أثناء المراجعة**: `Foreground="White"` حرفي مرتين (نص الشارة، نص العنصر النشط) — نفس
  قيمة `TextOnBrand` (`P.Color.White`) تماماً، فاستُبدلت بالمرجع الدلالي — صفر فرق بصري، توحيد فقط.

### R7 — إغلاق: CustomersViewModel (أول مستهلك حقيقي، يثبت شرط VM<60 سطر)

شرط إغلاق R7 المُعلَن: "Gallery تعمل، VM نمطي < 60 سطر" — لا يمكن التحقق منه بلا مستهلك حقيقي واحد على
الأقل لـ`CrudViewModelBase`. `6.UI/ViewModels/CustomersViewModel.cs` (32 سطراً شاملة التوثيق) يطبّق
`FetchPage`/`IdOf`/`DeleteItem` فوق `ICustomerService` الحقيقية، مُسجَّل `AddTransient` في DI (حالة عرض واحد،
لا Singleton). `AddNew`/`EditSelected` بلا تنفيذ عمداً وموثَّق — حوار العميل الحقيقي (`CustomerDialog`) لم
يُبنَ بعد (Views/Dialogs حُذفت كـstubs فارغة في R4، تُبنى عبر 7.Composition/8.Modules في R8/R9) — وضع منطق
هناك الآن كان سيعني حواراً وهمياً، ممنوع صراحة. `CustomersViewModelTests.ResolvedFromContainer_LoadsRealCustomers`
يثبت الحل عبر الحاوية الحقيقية (`_db.Services.GetRequiredService<CustomersViewModel>()`، لا `new` يدوي) ضد
بيانات حقيقية مُنشأة عبر `ICustomerService.Create`.

### التحقق

`dotnet build` → 0 خطأ. `dotnet test` → **152/152 ناجح** (148 + 3 PagedViewModelBase/CrudViewModelBase +
1 CustomersViewModel). `check.sh` → **صفر FAIL**. **تشغيل فعلي حقيقي** لـ`PrimeERP.exe` مرتين — الأولى
ضبطت استثناء توقف 8 (لُقِط بتشغيل حقيقي، لا محاكاة)، بعد الإصلاح أُعيد التشغيل وتأكَّد الإقلاع بلا أي
استثناء ولقطة شاشة تُظهر Gallery (شاملاً قسم Documents الذي يحمل الإصلاح) يعمل بصرياً بلا خرق.

**(توثيق أرشيفي لتحقُّق ما قبل R7 — توقف 6/7)**: حينها `dotnet build` → 0 خطأ، `dotnet test` → **148/148
ناجح، صفر تعديل**، `check.sh` → **صفر FAIL** (فحص جديد أُضيف:
`Color="{DynamicResource X}"` حيث X ليس `<Color x:Key>` حقيقياً ولا ينتهي بـ`.Color` = FAIL — تحقَّق فعلياً
بإدخال الخرق الأصلي مؤقتاً والتأكد أن الفحص يلتقطه). **تشغيل فعلي حقيقي** لـ`PrimeERP.exe` (لا محاكاة) — لقطة
شاشة للنافذة الفعلية تُظهر `ControlsGalleryPage` كاملة الأنماط (شارات، مفاتيح تبديل، حدود، ألوان) بلا أي خرق
أو رسالة خطأ.

---

## R8 — 7.Composition: Definitions + Renderers + Registry

**الفكرة**: صفحة قائمة+CRUD واحدة تُعرَّف تصريحياً (`ModuleDefinition` — Key/TitleKey/PermissionPrefix/
ViewModelType/Columns) بدل XAML جديد لكل كيان. `CrudPageRenderer.Render(definition, services)` يبني الصفحة
فعلياً من القطع الجاهزة (`PageHeader`+`FilterBar`+`AppDataGrid`+`AppPagination`، كل قطعة كما هي بلا تعديل)،
يحلّ ViewModel حقيقية من الحاوية عبر `definition.ViewModelType`، ويربط الخصائص بالاسم (`Items`,
`SelectedItem`, `CurrentPage`... إلخ) — لا XAML، كل الربط برمجي عبر `BindingOperations.SetBinding`.
`IModuleRegistry` (Singleton) يخزّن كل `ModuleDefinition` مسجَّلة؛ `8.Modules/ModuleRegistrations.cs` يسجّل
الوحدات الفعلية.

**قيد عام مكتشف أثناء التصميم**: `CrudViewModelBase<TDto,TFilter>` عامة على نوعين يختلفان بين كل وحدة —
لا يمكن لـ`ModuleDefinition.ViewModelType` (نوع واحد `Type`) أن يُعبَّر عنه بقيد عام موحّد هنا. الحل: WPF
`Binding` ينعكس بالاسم على أي كائن وقت التشغيل بصرف النظر عن نوعه المغلَق زمن الترجمة — فلا حاجة لواجهة غير
عامة أصلاً لأي شيء يمرّ عبر Binding. الاستدعاءات المباشرة القليلة التي ليست Binding (`LoadAsync`،
`SearchCommand.Execute` من مستمعي أحداث الفلترة/التصفح) تستخدم `dynamic` — نفس فلسفة Binding نفسها (ربط
بالاسم لا بالنوع)، لا حل مؤقت.

**تقليص UIServices (الشرط المذكور في R8)**: لا يعني حذف استهلاكاتها الست الحالية — كلها قطع XAML بلا
ViewModel خلفها (leaf pieces مثل `AppButton`/`PermissionButton`)، وهذا بالضبط ما بُنيت UIServices من أجله.
"التقليص" قيد تصميمي على ما يُبنى جديداً: `CrudPageRenderer` يحصل على كل خدماته (`IServiceProvider`) عبر
معامل صريح من المُستدعي، لا عبر `UIServices.Provider` — فالصفحات المُركَّبة عبر R8/R9 لا تضيف مستهلكين جدداً
لـUIServices مهما كثر عددها.

**إثبات شرط الإغلاق ("وحدتان بالتكوين، الثانية <50 سطراً")**: `SuppliersViewModel` (33 سطراً) أقصر فعلياً من
`CustomersViewModel` (36 سطراً) لأنها تعيد استخدام نفس العقد الموحّد بالكامل (نفس مبدأ استخراج القطعة) —
سُجِّلت الوحدتان في `ModuleRegistrations.RegisterAll` (46 سطراً لكلتيهما معاً، بيانات تصريحية بحتة).

**فحص جديد في check.sh**: `7.Composition` لا يعتمد على `8.Modules` (نفس نمط فحوصات الطبقات الأخرى — أول مرة
يُفحص هذان المجلدان، كانا فارغين قبل R8).

### ⚠️ توقف 9 — تعليق (Deadlock) حقيقي بين اختبارين يُنشئان System.Windows.Application كلٌّ على خيطه الخاص

اختبار `CrudPageRendererTests` الجديد (يحتاج `Application.Current` حيّة لبناء `FilterBar`/`AppDataGrid`
الحقيقيتين) اتّبع نمط `IdentityServiceTests` القائم: `StaThreadHelper.Run` (خيط STA جديد لكل اختبار، يُغلَق
بـJoin بعد انتهاء الجسم) + `if (Application.Current == null) new Application()`. **بمفرده كل اختبار نجح**؛
**معاً** (نفس عملية الاختبار، أي ترتيب) — تعليق كامل بلا أي إخراج، لا استثناء. السبب: `Application` نفسها
`DispatcherObject` مرتبطة بخيط الإنشاء الأول؛ الاختبار الثاني يجد `Application.Current` غير null (من الأول)
لكنه ينتمي لخيط **انتهى بالفعل** بعد Join — أي وصول له من خيط آخر يُعلَّق (لا رسالة خطأ، لا Dispatcher حيّ
يستجيب). لم يظهر من قبل لأن `IdentityServiceTests` كانت المستهلك الوحيد لهذا النمط طوال المشروع — أول اختبار
WPF-Application ثانٍ فعلياً هو ما كشف الخلل.

**الحل**: `PrimeERP.Tests/WpfApplicationFixture.cs` — خيط STA واحد **دائم** (`IsBackground=true`،
`Dispatcher.Run()`) طوال عملية الاختبار كلها، يُنشئ `Application` مرة واحدة فقط؛ أي اختبار يمرّ عبر
`WpfApplicationFixture.Run(action)` بدل `StaThreadHelper.Run` مباشرة (`_dispatcher.Invoke(action)` على نفس
الخيط الثابت دائماً). `IdentityServiceTests` و`CrudPageRendererTests` كلاهما محوَّلان الآن. `StaThreadHelper`
نفسها بقيت كما هي (لا تزال صحيحة لـ`PrintServiceTests`/`PrintTemplatesTests` — تلك لا تلمس
`Application.Current` إطلاقاً، فقط `FlowDocument` يحتاج STA بلا حاجة لـApplication مشتركة).

**تبعة مباشرة اكتُشفت أثناء الإصلاح**: خيط `Dispatcher.Run()` الدائم يعني `await` داخل مستمعي الأحداث (مثل
`root.Loaded += async (_,__) => await vm.LoadAsync();` في `CrudPageRenderer`) تُجدوَل متابعته على **طابور
نفس الخيط** (`DispatcherSynchronizationContext` حقيقية) — بخلاف `StaThreadHelper` الخام (بلا Dispatcher.Run،
فالمتابعة تذهب لـThreadPool). استطلاع الاختبار بـ`Thread.Sleep` كان سيُعلِّق الخيط عن معالجة طابوره الخاص
(الاختبار نفسه يُنفَّذ **داخل** `Dispatcher.Invoke` على ذلك الخيط) — استُبدل بضخّ إطارات متداخلة
(`DispatcherFrame`/`PushFrame`) في حلقة الاستطلاع، يفسح المجال لمعالجة الطابور بين كل فحص.

**⚠️ خرق حقيقي ثانٍ اكتشفه هذا الاختبار الجديد فور إصلاح التعليق**: `AppPagination.CurrentPageProperty`
مسجَّلة `FrameworkPropertyMetadataOptions.BindsTwoWayByDefault` — ربط `Binding("CurrentPage")` بلا تحديد
Mode صراحة يصبح TwoWay تلقائياً، فيحاول WPF الكتابة العكسية على `PagedViewModelBase.CurrentPage` (خاصية
`private set` عمداً — التنقل الفعلي عبر `PageChanged`→`GoToPageCommand`، لا كتابة مباشرة) فيرمي
`InvalidOperationException: ... cannot work on the read-only property`. الحل: `Mode = BindingMode.OneWay`
صراحة على هذا الربط تحديداً (فُحصت بقية الروابط: `TotalItems`/`IsLoading`/`ItemsSource` بلا هذا الخيار
افتراضياً — `SelectedItem` وحدها TwoWay فعلاً بتصميم، ومطابقة لخاصية VM القابلة للكتابة).

### التحقق

`dotnet build` → 0 خطأ. `check.sh` → **صفر FAIL** (26 فحصاً، فحص جديد لحدود 7.Composition/8.Modules).
`dotnet test` → **153/153 ناجح** (152 + `CrudPageRendererTests`) — الأهم: **يُكمِل التشغيل فعلياً خلال دقيقة
ونصف**، لا تعليقاً؛ التحقُّق الحرج هنا لم يكن نجاح الاختبار الجديد بمفرده (نجح من أول محاولة) بل **تشغيل
مجموعة الاختبارات كاملة معاً** — هذا ما كشف توقف 9 فعلياً. اختبار `CrudPageRendererTests` يبني صفحة حقيقية
لا XAML، يُطلق `Loaded` يدوياً، ويثبت أن `Items` تمتلئ ببيانات حقيقية من `ICustomerService`. **تشغيل فعلي
حقيقي** لـ`PrimeERP.exe` بعد ربط `AddComposition()`/`RegisterModules()` في سلسلة `App.xaml.cs.OnStartup` —
إقلاع سليم بلا استثناء.

---

## R9 — Login حقيقية + تفعيل الصلاحيات + MainWindow = AppShell

**اكتشاف قبل البناء**: طبقة المصادقة/الصلاحيات كانت مبنية بالكامل فعلياً منذ فترة طويلة — `PermissionDb`
(جداول Permissions/Roles/RolePermissions/Users/UserPermissions عبر SchemaBuilder، `SeedDefaults` يزرع دور
SystemAdmin + مستخدم admin)، `PasswordHasher` (PBKDF2 حقيقي، Hash+Verify)، `AppSession.SignIn/SignOut`،
`PermissionService.LoadForUser/GetUserPermissions` — **كلها بلا أي مستهلك حي واحد**، نفس نمط توقف 7 بالضبط
(قطعة كاملة، صفر ربط). لم تُبنَ من الصفر — فقط رُبطت أخيراً.

**الوصل**: `EnsureDatabaseReady` (وTestDatabaseFixture) الآن تستدعي `PermissionDb.CreateTables()/
SeedDefaults()`. `LoginWindow` (جديدة) حقيقية: `PermissionDb.FindByUsername` + `PasswordHasher.Verify`، عند
النجاح `IPermissionService.LoadForUser` (⚠️ أول مسار حي فعلي لها) ثم `AppSession.SignIn`. `MainWindow` أصبحت
`AppShell` فقط (القاعدة المعمارية الثابتة منذ البداية) — `NavItems` من `IModuleRegistry.All()`،
`NavigationRequested` يبني الصفحة عند الطلب عبر `CrudPageRenderer` (لا كل الوحدات دفعة واحدة).

### ⚠️ توقف 10 — `Window.ShowDialog()` تُعلَّق للأبد في بيئة هذا الجهاز تحديداً

`LoginWindow` بُنيت أولاً بـ`ShowDialog()` (النمط القياسي لحوار دخول). النتيجة الفعلية: العملية تبقى حيّة
(الذاكرة تنمو) لكن **صفر نافذة مرئية إطلاقاً** — لا استثناء، لا سجلّ. عُزل السبب تجريبياً بمنهجية "قلّص حتى
يظهر الخلل": XAML مبسَّط لأقصى درجة (زر واحد فقط) أظهر نفس التعليق، بينما **`Show()` (غير مِودال) لنفس
النافذة تماماً عملت فوراً** — إثبات قاطع أن المشكلة في آلية `ShowDialog()` الداخلية لهذه البيئة تحديداً، لا في
محتوى النافذة. **الحل**: `Show()` + حلقة `DispatcherFrame` يدوية (`Closed` يُنهي الحلقة) تُحاكي حجب
`ShowDialog` المطلوب بلا استخدام آليته المُعطَّلة — `App.xaml` تحوَّلت لـ`ShutdownMode="OnExplicitShutdown"`
(بدل الاعتماد على أي نافذة "MainWindow" تلقائية). `LoginWindow` لم تعد تستخدم `DialogResult` (يتطلب
`ShowDialog` صراحة) — خاصية `LoginSucceeded` عامة بدلاً منه.

### ⚠️ تكرار بصري حقيقي اكتُشف بالمراجعة اليدوية بعد أول تسجيل دخول فعلي

أول تشغيل فعلي كامل (Login→AppShell→CrudPageRenderer) كشف تكرارين بصريين، كلاهما "يعملان بلا خطأ" (لا
استثناء، لا فشل اختبار) — لا يُكتشفان إلا بالعين على تشغيل حقيقي:
1. **صف ترقيم مكرر**: `AppDataGrid` تحمل `AppPagination` داخلية خاصة بها (ترقيم **جانب العميل** — تُقسِّم
   `ItemsSource` الكاملة محلياً)، بينما `CrudPageRenderer` يضيف `AppPagination` منفصلة (ترقيم **من طرف
   الخادم** — كل صفحة من `GetPaged` فعلياً). القطعتان تتعارضان معمارياً لا بصرياً فقط: الأولى تفترض القائمة
   كاملة في الذاكرة (غير ممكن لجدول عملاء حقيقي بالآلاف)، الثانية تفترض صفحة واحدة فقط — الحل الصحيح ليس
   إخفاء إحداهما بصرياً بل تعطيل آلية الترقيم الداخلية الخاطئة للسياق. أُضيفت `AppDataGrid.ShowPagination`
   (افتراضي `true` — صفر تأثير على أي استهلاك حالٍ بما فيها Gallery)، `CrudPageRenderer` يضبطها `false`.
2. **عنوان الصفحة مكرر حرفياً**: `AppShell.UpdateBreadcrumb()` كانت تضع نفس العنصر الأخير في **كل من**
   `topBar.Breadcrumb` (شريط الأسلاف) **و**`topBar.PageTitle` (العنوان الرئيسي) — مع وحدات مسطّحة بلا تعشيش
   (Customers/Suppliers كلاهما جذري)، يتطابق آخر عنصر في المسار مع العنصر نفسه، فيظهر النص مرتين حرفياً.
   الإصلاح الصحيح عام لأي عمق تعشيش مستقبلي أيضاً: `Breadcrumb` يعرض **الأسلاف فقط** (`path.Take(Count-1)`)
   لا العنصر الحالي نفسه (`PageTitle` يعرضه أصلاً) — عنصر جذري بلا أسلاف = بلا breadcrumb إطلاقاً، وهذا صحيح
   دلالياً (لا معنى لمسار من عنصر واحد).

### ⚠️ توقف 11 — تعليق/فشل متقطّع في مجموعة الاختبارات الكاملة (لا في أي اختبار بمفرده)

بعد إضافة `CrudPageRendererTests`، تشغيل **المجموعة الكاملة** (لا أي مجموعة جزئية) أظهر تعليقاً كاملاً
لمضيف الاختبار أحياناً، وفشل ثلاثة اختبارات طباعة (`فشل بناء مستند الطباعة: The URI prefix is not
recognized`) أحياناً أخرى — **متقطّع بين تشغيلة وأخرى لنفس الكود بالضبط**، وكل اختبار يمرّ منفرداً أو في
مجموعات جزئية صغيرة. تحقُّق حاسم: `git worktree` لعزل commit ما قبل R7 بالكامل (148 اختبار، الكود الأصلي
بلا أي تعديل من هذه الجلسة) — **يمرّ نظيفاً دائماً، صفر تعليق** — يثبت أن السبب فعلياً في تغييرات هذه
الجلسة، لا خللاً بيئياً سابقاً موجوداً أصلاً كما بدا للوهلة الأولى.

سببان حقيقيان منفصلان، كلاهما اكتُشف بالتجربة لا التخمين:
1. **`PermissionDb.SeedDefaults()` في `TestDatabaseFixture`**: ~80 استعلام SQL فردي (فحص وجود + إدراج لكل
   صلاحية من ~40) غير مُجمَّعة بمعاملة واحدة — رخيصة في الإنتاج (مرة واحدة فقط طوال عمر التثبيت، تتخطى كل
   شيء من التشغيلة الثانية) لكن **كل** `TestDatabaseFixture` يبني قاعدة SQLite فارغة جديدة فيُنفَّذ العبء
   كاملاً من الصفر، مضروباً في عشرات فئات الاختبار — تراكم كافٍ لإنهاك مضيف الاختبار فعلياً. الحل:
   `TestDatabaseFixture` تستدعي `CreateTables()` فقط (رخيصة، بنية الجداول لا بياناتها) — لا اختبار حالي
   يحتاج بيانات صلاحيات مزروعة فعلياً (الكل عبر `AppSession.DevMode=true`).
2. **سباق ترتيب تسجيل مخطَّط `pack://`**: يُسجَّل ضمن المُنشئ الساكن لـ`System.Windows.Application` — أول
   لمسة لأي `System.Windows.*` في العملية بأكملها. `PrintService.Theme` (مفرد ثابت، يُبنى مرة واحدة فقط طوال
   التشغيلة) يفترض توفّره دون ضمانه صراحة. ترتيب تشغيل فئات xUnit **غير حتمي** — لو نُفِّذ اختبار طباعة قبل
   أي اختبار يلمس `System.Windows.Application` (كـ`IdentityServiceTests` سابقاً)، يفشل بناء `pack://` بصمت،
   ويبقى المفرد الثابت `null` فيفشل **كل** اختبار طباعة لاحق في نفس التشغيلة (لا تعافي ذاتي). الحل مزدوج:
   (أ) `PrintService.Theme` تضمن `Application.Current` صراحة بنفسها (ضمان حقيقي في مسار الإنتاج أيضاً، لا
   حل اختباري فقط)، (ب) `WpfApplicationFixture.Ensure()` تُستدعى من أول سطر في مُنشئ `TestDatabaseFixture`
   نفسها (تُبنى في كل اختبار تقريباً) — تضمن التسجيل قبل أي منطق اختبار فعلي، بصرف النظر عن ترتيب xUnit.

### التحقق

`dotnet build` → 0 خطأ. `check.sh` → **صفر FAIL** (26 فحصاً). `dotnet test` → **153/153 ناجح، ثلاث تشغيلات
كاملة متتالية متطابقة** (لا تشغيلة واحدة ناجحة قد تكون حظاً — التقطّع السابق يحتاج تكراراً لإثبات الإصلاح
فعلياً، لا فحصاً واحداً). **تشغيل فعلي حقيقي** متكرر لـ`PrimeERP.exe`: تسجيل دخول حقيقي (admin/admin) عبر
محاكاة نقر وكتابة فعلية على النافذة الحيّة (لا استدعاء دالة مباشر)، لقطات شاشة تُثبت AppShell يعمل، التنقل
بين العملاء/الموردين، الشبكة تُحمِّل بيانات حقيقية، ثم لقطات إضافية بعد كل إصلاح تُثبت زوال التكرارين البصريين
كليةً.

### إكمال بقية بنود R9: وحدة الحسابات بالتكوين + استبعاد DevTools من Release

**`AccountsViewModel`** (ثالث مستهلك لـ`CrudViewModelBase`) + تسجيلها في `ModuleRegistrations` بمفتاح
`Accounts` — تثبت أن التكوين يمتد لوحدة **هرمية** (`AccountDto.ParentId/Level`, `AccountTreeFilter`) بلا أي
تعديل في عقد `CrudPageRenderer`/`ModuleDefinition`. الشريط الجانبي أصبح ثلاث وحدات فعلية: العملاء، الموردون،
شجرة الحسابات — مُتحقَّق بلقطة شاشة حقيقية بعد تسجيل دخول فعلي.

**استبعاد `6.UI/DevTools` من بناء Release**: `ItemGroup Condition="'$(Configuration)' == 'Release'"` في
`PrimeERP.csproj` يستبعد `ControlsGalleryPage.xaml(.cs)`/`MockPickerDataSources.cs` من الترجمة (لا مستهلك حيّ
لها أصلاً — أداة مراجعة بصرية للمطوّرين فقط). تحقُّق: `dotnet build -c Release` نظيف، ونوع
`ControlsGalleryPage` **موجود** في IL بناء Debug و**غائب تماماً** من IL بناء Release (فحص مباشر على الـDLL
الناتج، لا افتراض).

### ⚠️ توقف 12 — عودة أعراض توقف 11 بخطأ مختلف تماماً بعد تغييرين لا علاقة لهما ظاهرياً

تشغيل `dotnet test` الكامل بعد إضافة وحدة الحسابات واستبعاد DevTools أظهر فشل **نفس الثلاثة اختبارات طباعة**
بالضبط التي عولجت في توقف 11 — لكن برسالة مختلفة كلياً هذه المرة: `Cannot create more than one
System.Windows.Application instance in the same AppDomain` (لا "URI prefix is not recognized" كما سابقاً).
تحقُّق أول: تشغيل الثلاثة منفردة يمرّ نظيفاً 6/6 — يثبت أنه خلل **ترتيب/تراكم عبر المجموعة الكاملة**، لا خللاً
في الاختبارات نفسها، بنفس طبيعة توقف 11 تماماً وإن كان بعرَض مختلف.

**السبب الجذري**: `IdentityServiceTests` تفتح `Window` حقيقية على الـ`Application` المشتركة الدائمة
(`WpfApplicationFixture`) ثم تُغلقها صراحة (`window.Close()`) في `finally`. `Application.ShutdownMode`
الافتراضي هو `OnLastWindowClose` — إغلاق تلك النافذة (الوحيدة التي أُنشئت على تلك الـ`Application` طوال عمر
التشغيلة) يُطلق `Application.Shutdown()` تلقائياً، الذي يُصفِّر `Application.Current` **للأبد** بعدها (بينما
العلَم الداخلي "أُنشئت Application في هذا الـAppDomain" لا يُصفَّر أبداً طوال عمر العملية). أي حارس لاحق من
نمط `if (Application.Current == null) new Application()` — كحارس `PrintService.Theme` الدفاعي من توقف 11 نفسه
— يرى `Current == null` فيحاول الإنشاء، فيرمي بالضبط استثناء "أكثر من Application واحدة". `Dispatcher.Run()`
الخاص بخيط `WpfApplicationFixture` يبقى حياً رغم ذلك (بدأ يدوياً لا عبر `Application.Run()`، فـ`Shutdown()` لا
يُوقفه) — وهذا بالضبط سبب مرور `CrudPageRendererTests` (تستخدم نفس الـDispatcher بلا مشكلة) بينما اختبارات
الطباعة فقط (تلمس حارس `PrintService.Theme`) تفشل.

**الحل**: `WpfApplicationFixture` تُنشئ الـ`Application` المشتركة بـ`ShutdownMode = ShutdownMode.OnExplicitShutdown`
صراحةً — نفس القرار المُطبَّق أصلاً في `App.xaml.cs` الحقيقي لنفس السبب بالضبط (توقف 10)، لكنه لم يُطبَّق على
الـ`Application` المصطنعة للاختبارات حتى الآن. إغلاق أي نافذة اختبار فردية الآن لا يُنهي الـ`Application`
المشتركة إطلاقاً — بصرف النظر عن عدد النوافذ المفتوحة/المغلقة عبر عمر التشغيلة كاملاً.

**تحقُّق الإصلاح**: `dotnet test` — **ثلاث تشغيلات كاملة متتالية متطابقة، 153/153 في كل مرة** (نفس منهجية
التحقق من توقف 11 بالضبط: نجاح واحد لا يكفي لخلل متقطّع بطبيعته). `check.sh` → صفر FAIL. تشغيل فعلي حقيقي
لـ`PrimeERP.exe` بعد كل هذه التغييرات: تسجيل دخول admin/admin ناجح، AppShell يعرض الوحدات الثلاث بلا أي خلل
بصري.

**الدرس العام**: هذا ثاني خلل من نفس العائلة (توقف 11 ثم 12) — كلاهما ناتج عن نفس الجذر: مُفرَد `Application`
مشترك عبر كامل تشغيلة الاختبار مع افتراضات ضمنية عن دورة حياته (تسجيل `pack://`، `ShutdownMode`) لم تُضبَط
صراحة عند إنشائه أول مرة. أي إضافة مستقبلية تلمس `System.Windows.Application` في بيئة الاختبار يجب أن تُراجَع
تحديداً من زاوية "هل تفترض ضمناً سلوكاً افتراضياً للـApplication لم يُضبَط صراحة هنا؟" لا من زاوية "هل الكود
صحيح بمعزل عن غيره؟" — التقطّع لا يظهر إلا عند التشغيل الكامل تحديداً.

---

## R10 — التغطية الناقصة + اختبار الحدود + اختبار الهوية

تدقيق شامل لتسع مساحات كانت مذكورة صراحة في نطاق R10 الأصلي (NumberSequence, Theme/Identity, Localization,
Dialog, Toast, Navigation, Export, Permission, السبعة Validators, فحص حدود طبقات) — لكل منها بحث فعلي عن
الملف الحقيقي وسطحه العام (لا افتراض)، لا كتابة اختبارات تخمينية. النتيجة: **65 اختباراً جديداً** عبر سبعة
ملفات، صفر تعديل على أي اختبار قائم.

**Theme/Identity**: مُغطاة بالفعل بالكامل عبر `IdentityServiceTests` (موجودة منذ R7/R8) — لا عمل إضافي.

**المُضافة**: `NumberSequenceServiceTests` (تسلسل/تصفير/Peek بلا استهلاك)، `PermissionServiceTests`
(Can/CanAny/CanAll/LoadForUser/GetUserPermissions — دور ∪ منح − سحب، بأدوار/مستخدمين حقيقيين تُزرع مباشرة عبر
DbHelper لا PermissionDb.SeedDefaults الثابتة)، `ValidatorsTests` + `AccountValidatorTests` +
`UserValidatorTests` (**السبعة معاً**: Customer/Supplier/Journal/Account المُستهلَكة فعلياً، وEmployee/
Product/User/Invoice غير المُستهلَكة بعد — هذه الأخيرة **لا تغطية أخرى ممكنة لها حالياً** بلا خدمة تستهلكها،
فالاختبار المباشر هو الوحيد المتاح)، `LocalizationServiceTests`، `NavigationServiceTests`، `ExportServiceTests`
(CSV/Excel/PDF حقيقية عبر ClosedXML/QuestPDF، بما فيها احترام صلاحية العمود)، و`Architecture/
LayerBoundaryTests` (تفصيل أدناه).

**قرار نطاق متعمَّد — Dialog/Toast بلا اختبار xUnit**: كلاهما غلاف رفيع فوق `Window.ShowDialog()`/`.Show()`
حقيقية (`AppConfirmDialog`, `AppMessageDialog`, `ToastHostWindow`) — لا منطق نقي قابل للعزل يستحق اختباراً
(المعاملات تمر مباشرة، يضمنها المترجم). اختبارها الفعلي الوحيد ذو المعنى هو تشغيل حواري حي (نفس فئة
`LoginWindow`/`MainWindow` — كلاهما أيضاً بلا اختبار xUnit، يُتحقَّق منهما فقط عبر تشغيل فعلي حقيقي ولقطات
شاشة، كما في R9). محاولة `ShowDialog()` داخل xUnit تصطدم أصلاً بتوقف 10 (تُعلَّق للأبد في هذه البيئة) — قرار
عدم الاختبار هنا ليس تهرّباً من عائق بل تصنيف صحيح لنوع الكود (سطح تفاعلي حي، لا منطق أعمال).

### ⚠️ باگ حقيقي كشفه أول اختبار فعلي لـ `LocalizationService.Apply`

أول تشغيل لـ`LocalizationServiceTests` رمى `IOException: Cannot locate resource '5.design/strings/
strings.ar.xaml'` — **لم يكن خللاً في الاختبار نفسه**. `LocalizationService.Apply` كانت تبني قاموس النصوص
بـUri **نسبي** (`new Uri("5.Design/Strings/Strings.ar.xaml", UriKind.Relative)`) يعتمد ضمنياً على
`Application.ResourceAssembly` — يُضبط صحيحاً تلقائياً في `PrimeERP.exe` الحقيقي، لكنه هشّ في أي مضيف آخر
(هنا: مضيف اختبار `testhost.exe`، بالضبط نفس السبب الجذري الموثَّق مرتين سابقاً في `IdentityService.Apply`
و`PrintService.Theme`). الكود كان **يعمل بلا خطأ في الإنتاج** فقط لأن لا شيء استدعاه قط في اختبار حتى الآن —
لا استثناء لعزوه لتوقف 11/12 هنا، خلل مستقل مختلف تماماً كُشف لأول مرة بهذا الاختبار تحديداً.

**الحل**: نفس الاصطلاح المُثبَّت مرتين من قبل — Uri مطلق `pack://application:,,,/{asmName};component/
5.Design/Strings/{file}` بدل النسبي. إصلاح إنتاجي حقيقي (لا حل اختباري فقط) — يُصلح نفس الهشاشة الكامنة لأي
مضيف مستقبلي غير `PrimeERP.exe` نفسه.

### `Architecture/LayerBoundaryTests` — نسخة IL من check.sh § 1

`Tools/ArchitectureCheck/check.sh` يفحص نص المصدر (`grep` على `using`) — يفلت منه أي إشارة بنوع مؤهَّل بالكامل
بلا `using` مطابق. `LayerBoundaryTests` الجديد يفحص بدلاً من ذلك **التوقيعات المُصرَّفة فعلياً** عبر انعكاس
(Reflection) على `PrimeERP.dll` الناتجة: لكل طبقة (Data/Application/UI/Composition/Platform/Domain)، يفحص
قاعدة كل نوع + واجهاته + حقوله + خصائصه + توقيعات دواله (بارامترات وقيمة الإرجاع، مع فكّ الأنواع العامة
المتداخلة مثل `Result<PagedResult<AccountDto>>`) عن أي إشارة لمساحة اسم من طبقة أعلى ممنوعة — نفس قواعد
check.sh § 1 بالضبط، منفَّذة بأسلوب مختلف تماماً فتُكمِّله لا تكرره. النتيجة: **صفر انتهاك** — يثبت أن الطبقات
نظيفة فعلياً على مستوى IL، لا فقط على مستوى نمط `using` النصي.

### التحقق

`dotnet build` (Debug وRelease) → 0 خطأ في كلاهما. `check.sh` → **صفر FAIL** (26 فحصاً؛ 2 دين تقني موثَّق
كسابقاً). `dotnet test` → **218/218 ناجح (153 سابقاً + 65 جديداً)، ثلاث تشغيلات كاملة متتالية متطابقة** (نفس
منهجية التحقق من توقف 11/12 — لا تشغيلة واحدة تكفي دليلاً بعد تاريخ التقطّع في هذه المجموعة تحديداً).

---

## R11 — الوحدة البصرية الشاملة + إصلاح حقول تسجيل الدخول + شجرة الحسابات

طلب مستخدم مباشر بثلاثة بنود متتالية: (3) إكمال سلسلة الرموز L1→L4 + كتالوج مولَّد آلياً + بوابة تحقق أوسع في
check.sh، (2) إصلاح تطابق حقلي الدخول (اسم المستخدم/كلمة المرور) عبر رموز C.Input.* بدل قيم حرفية، (1) تجميع
شجرة الحسابات عبر Composition. نُفِّذت بهذا الترتيب (3 يؤسس الرموز التي يعتمد عليها 2، ثم 1 الأكبر) ببوابة
build→test→check.sh→commit بعد كل بند، بلا توقف بيني كما طُلب صراحة.

### البند 3 — تدقيق السلسلة والنتيجة

تدقيق فعلي (لا افتراض) لكل طبقة: **L1** (Identity/Default وCorporate) — الستة ملفات كاملة في كلا الهويتين،
صفر نقص. **L2** (Semantic.Light/Dark) — صفر Hex حرفي (تحقُّق `grep` مباشر). **L3** (5.Design/Components) —
12 ملف Tokens.*.xaml موجودة فعلياً (Badge/Button/Card/Dialog/Document/Grid/Input/Nav/Pagination/Toast/
Toolbar/Tree) تغطي كل عائلة قطعة حقيقية موجودة؛ صفر إشارة لـL1 مباشرة (`grep` تأكيدي). **L4**
(5.Design/Styles) — Implicit.xaml (شبكة أمان لعناصر WPF الخام) + Style.Button/Dialog/Input.xaml + ScrollBars؛
صفر إشارة للون L1 مباشرة. السلسلة كانت نظيفة فعلياً في كل شيء **ما عدا** نقطتين حقيقيتين:

1. **`C.Input.*` ناقصة**: `Height`/`Height.Sm`/`Label.Gap` موجودة، لكن `Radius`/`PaddingX`/`BorderThickness`
   غائبة — `Style.Input.xaml` كانت تشير لـ`P.Radius.Md` (L1) مباشرة و`Padding="10,0"`/`BorderThickness="1"`
   حرفيين، بدل رموز L3 مخصَّصة. **هذا الحل الصحيح تحديداً** (لا كسل توثيقي) لأن `P.Radius.Md` **يختلف قيمةً
   فعلياً بين الهويتين** (Default=6، Corporate=3) — لو بقي مرجعاً حرفياً L1 مباشراً عند حقل الدخول تحديداً، أي
   حقل مستقبلي لا يعرف أن يتبع نفس القياس دون تكرار نفس الإشارة يدوياً في كل مكان.

2. **مشكلة نوع WPF حقيقية اكتُشفت أثناء التنفيذ (⚠️ توقف 13)**: تفصيلها أدناه — أخطر من نقص توثيقي، تسبَّبت
   في تعطُّل فعلي للتطبيق.

**الحل لـ`Radius`/`FontSize`** (كلاهما يختلف فعلياً بين الهويتين: `P.Font.Size.300` = 12 Default / 13
Corporate): WPF **لا يسمح بإعادة تصدير مورد من نوع قيمة** (لا Brush) عبر DynamicResource متسلسل — نفس مشكلة
⚠️ توقف 6 بالضبط (يومها كانت Color، اليوم CornerRadius/double). لا حل XAML بحت. الحل: `IdentityService`
اكتسبت `RefreshDerivedDimensions()` (نفس منهج `RefreshDerivedColors()` المعمول به من توقف 6) — تُستدعى من
`Apply()` بعد دمج ملفات L1 مباشرة، تنسخ القيمة **المُحلولة فعلياً** لـ`P.Radius.Md`/`P.Font.Size.300` تحت
اسم `C.Input.Radius`/`C.Input.FontSize` برمجياً. `PaddingX`/`BorderThickness`/`Label.Gap` **لا تختلف بين
الهويتين إطلاقاً** (لم تكن أصلاً مرتبطة بأي مفتاح L1 هوية — كانت أرقاماً حرّة) فتُعرَّف كرموز L3 ثابتة عادية
في `Tokens.Input.xaml` (نفس نمط `C.Input.Height` الموجود أصلاً) — لا حاجة لآلية الحقن البرمجي لهذه الأربعة.

### ⚠️ توقف 13 — `InvalidOperationException` عند إقلاع فعلي: نوع المورد لا يطابق نوع الخاصية المُستهدفة

أول تشغيل حقيقي بعد الترحيل عطّل التطبيق فوراً عند `LoginWindow.Show()`:
`'1' is not a valid value for property 'BorderThickness'`. السبب: `C.Input.BorderThickness` (وكذلك
`C.Input.Focus.BorderWidth` الموجودة أصلاً) عُرِّفتا كـ`sys:Double` — تعمل بلا مشكلة كقيمة نصية حرفية
(`BorderThickness="1"`، محوَّلة عبر `ThicknessConverter` وقت التحليل)، لكن `{DynamicResource ...}` **لا
يمرّ عبر أي TypeConverter** — يُعيد الكائن المُخزَّن بنوعه الفعلي مباشرة (هنا `double` مصندَق)، و`Border.
BorderThickness` تتوقّع بنية `Thickness` لا `double`، فيرمي WPF استثناءً وقت القياس (Measure) لا وقت
التحليل — **لا خطأ ترجمة، فقط تعطُّل فعلي عند التشغيل**. الحل: تغيير نوع كلا المفتاحين إلى `Thickness` صراحة
(`<Thickness x:Key="C.Input.BorderThickness">1</Thickness>`) — القيمة الفعلية المعروضة لا تتغيّر (`Thickness`
أحادية القيمة "1" تكافئ تماماً الأربعة أضلاع التي كان `ThicknessConverter` يبنيها من النص "1" أصلاً).

**الدرس العام** (يُضاف لدرس توقف 11/12): أي رمز L3/L4 جديد يُستهلَك عبر `DynamicResource` على خاصية WPF ذات
نوع مركّب (`Thickness`/`CornerRadius`/`Brush`... لا `double`/`string` البسيطة) **يجب** أن يُعرَّف بنفس النوع
المركّب فعلياً في ملف الرموز، لا بنوع "مبسَّط" يُفترض أن XAML سيحوّله — لن يفعل، `DynamicResource` ليس تحليل
XAML. تحقُّق سريع قبل أي إضافة مماثلة مستقبلاً: ابحث عن نوع الخاصية المُستهدفة الفعلي (`Border.BorderThickness`
Thickness، `Border.CornerRadius` CornerRadius، `FrameworkElement.Height` double...) وطابق نوع تعريف المورد له
حرفياً — لا تخمين.

### كتالوج الرموز — `Tools/DesignTokens/generate.sh` + `5.Design/DESIGN_TOKENS.md`

مولِّد جديد (bash، بنفس اصطلاح `Tools/ArchitectureCheck/check.sh`) يقرأ كل ملفات L1-L3 فعلياً (لا كتابة يدوية
تتقادم) ويبني جدولاً لكل رمز: المفتاح | القيمة | يشير إلى (`{Dynamic|StaticResource}` مُستخرَج من نفس السطر
إن وُجد) | عدد الملفات المستهلكة (بحث فعلي عبر 5.Design+6.UI+App، مُستبعَداً ملف التعريف نفسه). L4 (Styles)
فهرس فقط (مفتاح/TargetType/ملف) — كتلة `Style` كاملة متعددة الأسطر لا "قيمة" واحدة تُعرَض في جدول بمعنى.
أقسام إضافية: **الرموز غير المستخدمة** (صفر مستهلك خارج ملف التعريف) و**مفاتيح Light/Dark غير المتطابقة**
(تُعيد نفس منطق check.sh لكن كملف مرجعي دائم لا فحصاً عابراً).

⚠️ **درس أداء**: أول تنفيذ استخدم `grep -rl` **واحداً لكل رمز** (~300 رمز × مسح شجرة كاملة لكل واحد) — تجاوز
4 دقائق وأظهر حِمل عمليات فرعية ثقيلاً بوضوح في بيئة Windows/MSYS. أُعيدت الكتابة: مسح واحد فقط لكل إشارات
`(Dynamic|Static)Resource` في الشجرة كاملة، ثم جدول بحث في الذاكرة (bash associative array) — صفر عملية
فرعية جديدة لكل رمز أثناء البحث عن المستهلكين.

### البند 2 — إصلاح حقول الدخول: `AppPasswordBox` قطعة جديدة

**السبب الجذري** (تأكَّد بقراءة `LoginWindow.xaml` مباشرة، لا تخمين): حقل اسم المستخدم يستخدم `AppTextBox`
(قطعة حقيقية — تسمية + حدود مقوَّسة + حالات تركيز/خطأ)، بينما حقل كلمة المرور كان `PasswordBox` **خاماً تماماً**
بلا أي نمط مخصَّص — `5.Design/Styles/Implicit.xaml` (شبكة الأمان لعناصر WPF الخام) لا تُعرِّف نمطاً لـ
`PasswordBox` إطلاقاً، فيظهر بمظهر Windows الافتراضي غير المصمَّم (ارتفاع/حدود/خط مختلفة كلياً) — بالضبط
كما وصف المستخدم.

**القطعة الجديدة**: `AppPasswordBox` (`6.UI/Components/Inputs/`) — نسخة طبق الأصل من بنية `AppTextBox`
(تسمية + Border + حقل داخلي + نص خطأ)، لكن `PasswordBox` داخلياً. `Password` خاصية CLR للقراءة فقط تُفوَّض
مباشرة للعنصر الداخلي (لا `DependencyProperty` قابلة للربط — WPF يمنع ذلك أمنياً على `PasswordBox.Password`
نفسها أصلاً، فلا معنى لمحاولة تجاوزه هنا). `KeyDown` لا يحتاج تمريراً يدوياً — حدث WPF مُوجَّه (Routed) يصعد
تلقائياً من `PasswordBox` الداخلي عبر شجرة العناصر المرئية إلى `AppPasswordBox` نفسها (ترثه من `UIElement`)،
فـ`LoginWindow.xaml.cs` لم يحتَج أي تعديل — `txtPassword.Password` و`KeyDown="txtPassword_KeyDown"` يعملان
بنفس التوقيع تماماً.

أسلوب الحدود (`AppPasswordBox_FieldBorder`) لا يعيد كتابة القيم — `BasedOn="{StaticResource AppTextBox_
FieldBorder}"` مع `Style.Triggers` خاصة به (نفس نمط `AppComboBox_FieldBorder` القائم أصلاً) بحيث `AncestorType`
في كل DataTrigger يطابق نوع القطعة الفعلي (`AppPasswordBox` لا `AppTextBox` — استخدام نمط الأخيرة حرفياً في
قطعة أخرى كان سيُسكت حالات التركيز/التعطيل/الخطأ بصمت، لأن `RelativeSource AncestorType` لن يجد سلفاً مطابقاً).

**أثناء نفس الترحيل**: `AppNumericBox` (قطعة Inputs أخرى موجودة أصلاً) وُجدت بنفس النمط بالضبط — حدودها الخاصة
بها (غير مبنية على `AppTextBox_FieldBorder` أصلاً) تستخدم `BorderThickness="1"` حرفياً و`CornerRadius=
"{DynamicResource P.Radius.Md}"` (L1 مباشرة) و`BorderBrush="{DynamicResource OutlineDefault}"` (يكافئ
`C.Input.Border` دلالياً لكن باسم مختلف) وحقلها الداخلي `FontSize="{DynamicResource P.Font.Size.300}"` (L1
مباشرة) و`Padding="10,0"` حرفياً — رُحِّلت كلها لرموز `C.Input.*` المناظرة (صفر تغيير بصري، القيم متطابقة عددياً).

**بوابة check.sh جديدة مُضافة** (مُتحقَّق أولاً أنها نظيفة على الكود الحالي، لا رمياً أعمى): قياس حرفي
(Height/MinHeight/Padding) على `TextBox`/`PasswordBox` داخل `6.UI/Components/Inputs` تحديداً = FAIL. **مقصورة
عمداً على هذا المجلد لا كل 6.UI**: مُحرِّرات الخلايا المضغوطة في `DocumentLinesGrid.xaml` (شبكات مستندات،
`Padding="6,4"`) وحقل `AppTextArea` (حشو رأسي متعمَّد `10,8` لمساحة نص متعددة الأسطر) قياسات **مختلفة عمداً
عن حقل واحد سطر قياسي** — سياق كثافة/استخدام مختلف كلياً، لا خللاً. فرض `C.Input.*` عليها كان سيُغيّر شكلها
الفعلي، ممنوع صراحة بنص التعليمات ("لا تعدّل أي قطعة بصرياً"). بالمثل أُضيفت فحوصاً لحدود الطبقات (L3←L1
مباشرة، L4←لون L1 مباشرة — مقصورة على اللون تحديداً، لا الاستثناء الموثَّق لـFont/Space/Radius) وHex حرفي
داخل L2/L3/L4، وWARN جديد للون مكرر بقيمة سداسية واحدة تحت اسمين مختلفين ضمن نفس ملف هوية — كلها مُتحقَّقة
صفر FAIL على الكود الحالي قبل تثبيتها.

### التحقق (البندان 3+2)

`dotnet build` → 0 خطأ (تطلَّب `rm -rf obj bin` مرة واحدة بعد إضافة `AppPasswordBox.xaml` — تعارض تخزين
مؤقت معروف لـMSBuild XAML markup-compile عند إضافة ملف XAML جديد وسط بناء تزايدي، لا خللاً في الكود).
`check.sh` → **صفر FAIL** (30 فحصاً، 4 دين تقني: 2 سابقان + 2 WARN لون مكرر جديدان اكتُشفا بالفحص الجديد —
معلوماتيان فقط، لا خرقاً). `dotnet test` → **218/218 ناجح**. **تشغيل فعلي حقيقي**: التقط توقف 13 تحديداً (لم
يظهر بأي اختبار وحدة أو بناء — استثناء وقت تشغيل بحت)، ثم بعد الإصلاح لقطة شاشة حقيقية لنافذة الدخول تُثبت
الحقلين متطابقين تماماً (ارتفاع، حدود، نصف قطر، خط، تسمية بعلامة "*" حمراء).

---

### البند 1 — تجميع شجرة الحسابات عبر Composition

القطع اللازمة كانت جاهزة بالفعل قبل هذا البند: `AppTreeView` (شجرة WPF حقيقية مبنية على `HierarchicalDataTemplate`
+ فلترة إخفاء)، `TreeNodeViewModel`/`TreeFilterEngine` (فلترة حقيقية جانب العميل — الأسلاف المؤدية لمطابقة
تظهر وتتوسّع تلقائياً، غير المطابق يختفي كلياً لا يُعتّم فقط)، و`AccountPicker.BuildTree` (تحويل يدوي من
`List<Account>` مسطّحة لشجرة، نمط مرجعي أُعيد استخدامه). المطلوب هنا: نفس التحويل **معمَّماً عبر Composition**
(تكوين بيانات لا كود مكرر لكل كيان هرمي مستقبلي)، لا XAML مخصَّص لصفحة الحسابات.

**التوسعة المعمارية**: `ModuleDefinition` اكتسبت `LayoutKind` (Grid الافتراضي — صفر تغيير سلوك للوحدات
الحالية) و`TreeOptions` (`TreeLayoutOptions`). `TreeLayoutOptions`/`LayoutKind`/`SelectableRule` تعيش في
**6.UI** (`Components/Tree/`) لا 7.Composition رغم أن `ModuleDefinition` (7.Composition) هي من تستهلكها —
`TreeViewModelBase` (6.UI أيضاً) لا يمكنها الاعتماد على نوع من طبقة أعلى (7.Composition)، بينما العكس
(Composition يعتمد على UI) هو الاتجاه المسموح والمستخدَم أصلاً فعلاً (`ModuleDefinition.Columns` من
`PrimeERP.UI.Components.Display` سابقاً). قرار موضع الملف هذا وحده منع خرقاً حقيقياً لحدود الطبقات كان
سيحدث لو اتُّبع الاسم الحرفي "PageDefinition/TreeLayoutOptions في 7.Composition" دون هذا التدقيق.

**`TreeViewModelBase<TDto,TFilter>`** يرث `PagedViewModelBase` (نفس عقد `FetchPage`) ويضيف فقط حالة عرض
الشجرة (`RootNodes`, `SelectedNode`, `ExpandAllCommand`, `CollapseAllCommand`) — **صفر منطق تحويل شجرة
داخلها**: ذلك يحتاج `TreeLayoutOptions` الفعلية (معروفة فقط عند التسجيل في `ModuleRegistrations`)، فبقاؤه
هناك يجعل `AccountsViewModel` مطابقاً تماماً لبساطة `CustomersViewModel` (21 سطراً، لا فرق بنيوي حقيقي عن
الوحدات المسطّحة — يثبت الشرط: "AccountsViewModel : TreeViewModelBase — صفر منطق شجرة" حرفياً).

**`TreeRenderer`** (7.Composition) يبني PageHeader (أزرار توسيع/طي الكل) → FilterBar (تُعيّن `SearchText` على
الـVM فقط، بلا `SearchCommand` — الشجرة كاملة في الذاكرة أصلاً بعد أول تحميل، فلا معنى لإعادة جلب من الخادم
لكل حرف يُكتب؛ `AppTreeView.SearchText` نفسها OneWay من نفس الخاصية تُشغّل `TreeFilterEngine` تلقائياً) →
`AppTreeView` (+ `AppCard` تفاصيل لـTreeSplit، Bindings بمسار منقّط `"SelectedNode.Data.{Binding}"` — WPF
يحلّها عبر Reflection مباشرة، بلا كود إضافي). بعد `LoadAsync()`، `TreeBuilder` (دالة نقية) يحوّل `vm.Items`
المسطّحة لشجرة عبر Reflection على أسماء حقول `TreeLayoutOptions` (نفس مبدأ "ربط بالاسم لا بالنوع الثابت"
المتَّبع في `CrudPageRenderer`)، ثم يملأ `vm.RootNodes` مباشرة. `PageRenderer` جديد (نقطة توزيع وحيدة حسب
`LayoutKind`) يستبدل استدعاء `CrudPageRenderer.Render` المباشر في `MainWindow.xaml.cs` — موضع القرار الوحيد،
لا فرع شرطي مكرر في كل موضع استدعاء.

### ⚠️ توقف 14 — الصفحة الجديدة كاملة صحيحة، لكن الوحدة لم تُسجَّل في DI إطلاقاً

أول تشغيل فعلي حي (تسجيل دخول admin/admin + نقر "شجرة الحسابات") أغلق التطبيق فوراً بلا أي رسالة مرئية —
**خرج المستخدم من التطبيق بالكامل**، وليس مجرد صفحة فارغة أو خطأ داخل الصفحة (نفس آلية توقف 13: استثناء غير
مُمسوك داخل معالج حدث `Loaded` غير متزامن يُنهي العملية كاملة، لا `Application.DispatcherUnhandledException`
مُسجَّلة). محاولات تكرار الخلل عبر أتمتة نقر/لقطات شاشة حية تعثّرت بتغطية نافذة أخرى للشاشة (بيئة هذا الجلسة
تحديداً) — بدلاً من الاستمرار في محاولات هشّة، أُعيد إنتاج **نفس المسار البرمجي الحقيقي بالضبط**
(`PageRenderer.Render` → DI حقيقي → بيانات حقيقية) عبر اختبار STA جديد (`TreeRendererTests`، نفس أسلوب
`CrudPageRendererTests` القائم) — كشف الاستثناء الحقيقي فوراً بلا حاجة لأي نقر:

```
InvalidOperationException: No service for type 'PrimeERP.UI.ViewModels.AccountsViewModel' has been registered.
```

**السبب**: `AccountsViewModel` لم تُسجَّل قط في `App/Bootstrap/DependencyInjection.cs.AddUI()` —
`services.AddTransient<CustomersViewModel>()`/`<SuppliersViewModel>()` موجودتان، `AccountsViewModel` غائبة
تماماً. هذا **خطأ سابق على هذا البند** (سقط سهواً عند تسجيل وحدة Accounts أول مرة في R9 من نفس الجلسة) — لم
يظهر وقتها لأن التحقق البصري وقتها اكتفى بلقطة شاشة لقائمة الشريط الجانبي (تُبنى من `IModuleRegistry` مباشرة،
لا تحتاج حلّ DI لـViewModel) ولم يشمل نقراً فعلياً على الوحدة (نفس ما تعثّر تكراره الآن). **الدرس**: التحقق
البصري لقائمة تنقّل ≠ التحقق من أن كل عنصر فيها فعلاً قابل للفتح — الفحصان مختلفان، لا يُغني أحدهما عن الآخر.

الحل: سطر واحد — `services.AddTransient<AccountsViewModel>();`. بعد الإصلاح، `TreeRendererTests` يمرّ
نظيفاً: صفحة كاملة تُبنى، بيانات حساب أب+ابن حقيقية تُنشأ عبر `IAccountService.Create` وتُحمَّل، `RootNodes`
تمتلئ بالهرم الصحيح.

### التحقق (البند 1)

`dotnet build` → 0 خطأ (نفس تعارض `rm -rf obj bin` بعد إضافة ملفات جديدة، معروف من قبل). `check.sh` → **صفر
FAIL** (30 فحصاً، 4 دين تقني كسابقاً). `dotnet test` → **219/219 ناجح** (218 سابقاً + `TreeRendererTests`
الجديد — أول اختبار STA حقيقي يبني صفحة شجرة كاملة عبر DI حقيقي ويحمّل هرماً حقيقياً من قاعدة بيانات حقيقية).
**تشغيل فعلي**: توقف 14 اكتُشف وأُصلح عبر تشغيل حقيقي بالضبط كما تقتضي منهجية هذا المشروع؛ التحقق النهائي بعد
الإصلاح تم عبر `TreeRendererTests` (يعيد إنتاج نفس مسار DI/Composition/بيانات الحقيقي الذي يسلكه التطبيق
الفعلي، دون الاعتماد على أتمتة نقر هشّة في بيئة كانت نوافذ أخرى تُغطّي الشاشة فيها).

---

## R11 (تكملة) — الشجرة فارغة وظيفياً + أزرار الإجراءات + حوار الإضافة/التعديل الكامل

بعد التحقق أعلاه تبيّن أن الصفحة "فارغة وظيفياً" رغم أنها تُبنى بنيوياً بلا خطأ — تشخيص المستخدم نفسه حدّد
الاتجاه الصحيح: تحقّق من البيانات أولاً (لا افتراض). فحص مباشر لقاعدة البيانات الفعلية (`PrimeERP.db`) أثبت 21
حساباً حقيقياً بهرمية سليمة تماماً — المشكلة لم تكن بيانات مفقودة إطلاقاً، بل خلل في نقل تلك البيانات لعنصر
الشجرة المرئي فعلياً. أربعة أخطاء حقيقية منفصلة اكتُشفت جميعاً عبر تشغيل حقيقي أو اختبار STA يُعيد نفس المسار
الحقيقي — لا تخمين واحد فيها:

### ⚠️ توقف 15 — AppTreeView.ItemsSource لا تراقب تغيّر المحتوى، فقط تغيّر المرجع

`TreeRenderer` يربط `ItemsSource` بـ`RootNodes` **قبل** اكتمال `LoadAsync` (فارغة وقت الربط)، ثم يملأ نفس
نسخة `RootNodes` لاحقاً (`Clear`+`Add`) بعد وصول البيانات. `AppTreeView.OnItemsSourceChanged` كانت تأخذ **لقطة
واحدة** من المجموعة وقت تغيّر قيمة الخاصية نفسها فقط (`new List<TreeNodeViewModel>(src)`) — لا اشتراك في
`INotifyCollectionChanged`. بما أن مرجع `RootNodes` نفسه لا يتغيّر (تُملأ في مكانها لا تُستبدَل)، WPF لا يعيد
استدعاء معالج التغيّر أبداً، فتبقى الشجرة المرئية فارغة للأبد رغم امتلاء `RootNodes` فعلياً — هذا بالضبط سبب
"الشجرة فارغة وظيفياً". **الحل**: `OnItemsSourceChanged` تشترك الآن في `CollectionChanged` على المصدر
(إلغاء الاشتراك من القديم، اشتراك في الجديد)، فتُحدَّث الشجرة تلقائياً عند أي `Add`/`Clear`/`Remove` لاحق —
نفس سلوك أي `ItemsControl` حقيقي مربوط بـ`ObservableCollection`. مُثبَّت الآن بـ`TreeRendererTests` مُعزَّز
يفحص `AppTreeView.ItemsSource` الفعلية المعروضة، لا حالة الـVM فقط (الفحص السابق كان سيفوّت هذا الخلل تحديداً).

### ⚠️ توقف 16 — ActionToolbar تُكرِّر كل عنصر في قائمة "المزيد"

`ActionToolbar.Rebuild()` تُستدعى مرتين متتاليتين (تغيّر `ButtonsSource` أولاً، ثم حدث `Loaded`)، وكل مرة
تُجدوِل `CacheWidths()` عبر `BeginInvoke`. `CacheWidths()` كانت تُضيف لقائمة `_rendered` بلا تصفيرها هي
نفسها (`Rebuild()` وحدها تُصفِّرها، عند بدايتها) — الاستدعاء الثاني المُجدوَل يجد القائمة ممتلئة بالفعل من
الأول فيضيف فوقها، فكل زر يظهر مرتين في قائمة الفائض. اكتُشف حياً (لقطة شاشة فعلية أظهرت كل إجراء مكرراً).
**الحل**: `CacheWidths()` تُصفِّر `_rendered` من نفسها أولاً — دالة صحيحة بذاتها بصرف النظر عن كم مرة استُدعيت.

### ⚠️ توقف 17 — AppDialogWindow يحاول تعيين نفسه مالكاً لنفسه

`AppDialogWindow`'s constructor: `Owner ??= FindActiveWindow();` حيث `FindActiveWindow()` تبحث عن أول نافذة
`IsActive` في `Application.Windows`، وإلا ترجع `Application.MainWindow`. في بيئة لا توجد فيها نافذة نشطة
حقيقية بعد (مثل اختبار STA معزول، أو أي سياق WPF لم يُثبَّت فيه `MainWindow` بعد) قد ترجع هذه الدالة **النافذة
قيد الإنشاء نفسها**، وWPF يرمي `ArgumentException: Cannot set Owner property to itself`. اكتُشف عبر
`DialogRendererTests` الجديد (أول مستهلك حقيقي لـ`AppDialogWindow` من بيئة اختبار معزولة — كل الحوارات
السابقة AppConfirmDialog/AppMessageDialog كانت تُستهلك فقط من داخل التطبيق الحي حيث `MainWindow` مضبوطة
بالفعل، فلم يظهر هذا الخلل الكامن من قبل). **الحل**: حارس صريح `if (Owner == null && active != this) Owner =
active;` — لا يفترض أبداً أن `FindActiveWindow()` لن ترجع النافذة نفسها.

### ⚠️ توقف 18 — dynamic لا يحلّ استدعاء `AccountService.Create(CreateAccountDto)` رغم تطابق النوع الفعلي تماماً

هذا هو الخلل الذي وصفه المستخدم حياً بدقة: **"الحفظ أو التعديل يؤدي لخروج البرنامج"** — استثناء غير مُمسوك
داخل معالج نقر الزر (نفس آلية توقف 13/14: عملية غير متزامنة تنهي العملية كاملة بلا رسالة). `DialogRenderer.
TrySave` كانت تستدعي `dynamic service = services.GetRequiredService(dialog.ServiceType); service.Create
(createDto);` — `createDto` نوعه الفعلي وقت التشغيل **مطابق تماماً** لـ`CreateAccountDto` (تحقُّق مباشر عبر
`GetType().FullName`، لا افتراض)، ومع ذلك رمى C# Runtime Binder استثناء `RuntimeBinderException: The best
overloaded method match ... has some invalid arguments`. السبب الجذري الدقيق لم يُحسم (على الأرجح تفاعل بين
الحمل الزائد المزدوج لـ`Create` — نسخة بمعامل واحد وأخرى بثلاثة معاملات `DbConnection/DbTransaction/Dto` —
وآلية اختيار الحمل الزائد الديناميكي في هذا السياق تحديداً)، لكن التحقُّق التجريبي حاسم: استدعاء ديناميكي
مطابق حرفياً من داخل اختبار منعزل أعاد إنتاج نفس الاستثناء بالضبط. **الحل العملي الحاسم**: استبدال `dynamic`
بـ`Reflection` صريح (`ServiceType.GetMethod("Create", new[] { CreateDtoType }).Invoke(...)`) — حتمي لا لبس
فيه، يحدّد الحمل الزائد المطلوب بدقة بلا اعتماد على آلية اختيار الحمل الزائد الديناميكي التي أثبتت هشاشتها
هنا تحديداً. **درس عام**: `dynamic` مفيد لربط WPF بالخصائص بالاسم (مُستخدَم بأمان في كل مكان آخر بهذا
المشروع — `CrudPageRenderer`/`TreeRenderer` كلاهما يستدعيان دوال VM أحادية الحمل الزائد بلا مشكلة)، لكنه غير
موثوق بشكل قاطع عند استدعاء **حمل زائد متعدد** على خدمة عمل حقيقية — أي استدعاء `dynamic` جديد لدالة لها أكثر
من نسخة بنفس الاسم يستحق نفس هذا الحذر، أو الانتقال المباشر لـReflection كما هنا.

### تحسين إضافي — تعبئة الأب الافتراضي عند الإضافة من عقدة محدَّدة

طلب المستخدم أثناء الاختبار الحي: عند الضغط على "إضافة" وعقدة مُحدَّدة فعلاً في الشجرة، يجب أن يُقترَح والدها
تلقائياً (قابلاً للتغيير، لا مقفلاً). `AppComboBox.SelectedValue` لا تدعم بحثاً عكسياً من القيمة للعنصر (لا
معالج يربط تغيّرها بإيجاد العنصر المطابق) — أُضيفت `SelectPickerItem` (بحث في `ItemsSource` المُحمَّلة فعلياً
عن العنصر بمطابقة `Id`، ثم تعيين `SelectedItem` مباشرة، الذي بدوره يُحدِّث `SelectedValue` والنص المعروض
تلقائياً عبر آلية `AppComboBox` القائمة). أُعيد ترتيب تحميل عناصر Picker **قبل** محاولة التعبئة المسبقة (كانت
معكوسة سابقاً، عيب كامن آخر كان سيمنع حتى تعبئة التعديل من العمل).

### التحقق النهائي — تشغيل فعلي حقيقي بالكامل

`dotnet build` (Debug + Release) → 0 خطأ. `dotnet test` → **221/221 ناجح** (219 + `DialogRendererTests`
اثنان جديدان: نمط إضافة كامل عبر نقر Save حقيقي محاكى على العنصر الحقيقي، ونمط تعديل كامل). `check.sh` →
**صفر FAIL** (30 فحصاً، 4 دين تقني كسابقاً بلا تغيير). **تشغيل فعلي حي كامل** (تسجيل دخول admin/admin حقيقي،
نقر حقيقي، كتابة حقيقية): الشجرة تعرض الهرم الحقيقي (21 حساباً) → البحث "الصناديق" يفلتر بالإخفاء ويُبقي
مسار الأسلاف فقط → تحديد عقدة يعرض تفاصيلها في اللوحة → شريط الإجراءات الستة يظهر بلا تكرار → زر "جديد" يفتح
حواراً حقيقياً بحقوله الأربعة → البحث داخل منتقي الحساب الأب يعمل → الحفظ **لا يُعلِّق التطبيق** (الإصلاح
الحاسم) → توست نجاح حقيقي → الحساب الجديد "1200001 - حساب اختباري حي" يظهر فوراً متداخلاً بشكل صحيح تحت أبيه
المختار برمز مُولَّد تلقائياً — تسلسل كامل من نقرة "جديد" حتى ظهور النتيجة في الشجرة بلا أي خطأ.

---

## اكتشاف فني إضافي أثناء R1 (يستحق التسجيل)

**تسمية الطبقة "Application" تتصادم مع `System.Windows.Application`**: أي ملف تحت شجرة `PrimeERP.*` يستخدم `Application.Current`/`: Application` بلا تأهيل كامل يتعرّض لخطر أن يحلّه المترجم كإشارة لمساحة الاسم `PrimeERP.Application` (طبقة 4) بدل نوع WPF — C# يبحث في مساحات الاسم المحيطة صعوداً قبل استشارة `using`. **الحل المُطبَّق**: كل إشارة WPF لـ `Application` في الكود مؤهَّلة بالكامل الآن (`System.Windows.Application`) — 11 ملفاً. أي ملف جديد يستخدم `Application.Current` مستقبلاً **يجب** أن يكتبها مؤهَّلة بالكامل لنفس السبب.

---

## التحقق النهائي لـ R1

`dotnet build PrimeERP.csproj -m:1` → 0 تحذير، 0 خطأ. `dotnet build PrimeERP.Tests/PrimeERP.Tests.csproj -m:1` → 0 خطأ (تحذيرا Nullable سابقان على R1، غير متعلقين به). `dotnet test -m:1 --no-build` → **134/134 ناجح، صفر فشل، صفر تعديل على أي اختبار**.

---

## أخطاء اكتُشفت عبر اختبار حي فعلي بعد اكتمال ~35 وحدة (2026-08-28)

### ⚠️ توقف 19 — `ComposedDialogWindow` يرث `OnEscapePressed` الذي يضبط `DialogResult` على نافذة لم تُفتح بـ`ShowDialog()`

`AppDialogWindow.OnEscapePressed()` (الأساس، تخدم زر X بالهيدر ومفتاح Escape) تضبط `DialogResult = false;` قبل
`Close()` — صحيح لـ`AppConfirmDialog`/`AppMessageDialog`/`PickerTreeWindow`/`PickerGridWindow` (تُفتح جميعاً
بـ`ShowDialog()`)، لكن `ComposedDialogWindow` (يخدم كل حوارات `DialogRenderer`/`DocumentRenderer` — كل وحدات
Pattern 2/3) تُفتح بـ`Show()+DispatcherFrame` بسبب توقف 10، وWPF يرمي `InvalidOperationException` عند ضبط
`DialogResult` على نافذة كهذه — فلا يُنفَّذ `Close()` أبداً، وزر X والـEscape يفشلان بصمت (النافذة تبقى مفتوحة).
**الحل**: `ComposedDialogWindow` تُجاوِز `OnEscapePressed()` بـ`Close()` فقط. مُثبَّت بـ`DialogRendererTests.
ShowAndSave_ClickingHeaderCloseButton_ClosesWindow_WithoutThrowing` (فشل بنفس الاستثناء عند إزالة الإصلاح تجريبياً، تأكيد أنه السبب الحقيقي لا افتراض).

### ⚠️ توقف 20 — `AppDataGrid.ItemsSource` لا تراقب تغيّر محتوى `ObservableCollection` ثابتة، فقط تغيّر مرجعها

نفس فئة خلل توقف 15 لكن في `AppDataGrid` هذه المرة: `PagedViewModelBase.Items` مجموعة ثابتة
(`ObservableCollection<TDto> Items { get; } = new();`) تُملأ بـ`Clear()+Add()`، لا تُستبدَل. `AppDataGrid.
OnItemsSourceChanged` كانت تُنفَّذ فقط عند تغيّر **مرجع** الخاصية — يحدث مرة واحدة (عند الربط الأول، والقائمة
لا تزال فارغة قبل أول `LoadAsync`)، ثم لا يتكرر أبداً رغم امتلاء `Items` لاحقاً. النتيجة: كل صفحات القوائم
(العملاء/الموردون/القيود...) تعرض عدّاد نتائج صحيح (`TotalCount` خاصية عادية تُطلِق `PropertyChanged` بشكل
سليم) بينما الجدول نفسه يبقى فارغاً للأبد — بالضبط ما وصفه المستخدم: "يظهر يوجد نتيجة لكن لا يظهر جدول".
**الحل**: `OnItemsSourceChanged` تشترك الآن في `INotifyCollectionChanged.CollectionChanged` على المصدر
(إلغاء/اشتراك عند كل تغيّر مرجع) وتُعيد `LoadItems()` عند أي حدث.

### ⚠️ توقف 21 — `ReportRenderer` عطّل ترقيم `AppDataGrid` نسخاً عن `CrudPageRenderer` بلا إضافة بديل

`CrudPageRenderer` يضبط `ShowPagination = false` عمداً لأنه يضيف `AppPagination` خارجية خاصة به (ترقيم من طرف
الخادم). `ReportRenderer` نسخ نفس السطر رغم أنه **لا** يضيف أي بديل ولا يحتاج له أصلاً — نتيجة التقرير كاملة
في الذاكرة دفعة واحدة (`report.Generate` لا صفحات)، فترقيم `AppDataGrid` الداخلي الافتراضي (`true`) هو المطلوب
تماماً بلا أي إضافة. **الحل**: حذف `ShowPagination = false` من `ReportRenderer` فقط.

### ⚠️ توقف 22 — `PermissionDb.SeedAdminRole` تمنح كل `PermissionKeys.All()` لدور المدير فقط عند إنشاء الدور لأول مرة

`SeedDefaults()` تُستدعى في كل إقلاع تطبيق حقيقي، لكن `SeedAdminRole()` كانت `if (existing != null) return
Convert.ToInt32(existing);` — أي قاعدة بيانات تطوير مستمرة (نفس `PrimeERP.db` عبر جلسات متعددة) تجمّد منح
دور `SystemAdmin` عند أول تشغيل فقط؛ أي `PermissionKey` جديد يُضاف لاحقاً (أي وحدة جديدة — وقد أُضيفت عشرات
هذه الجلسة) لا يُمنَح للمدير أبداً في تلك القاعدة رغم `SeedPermissions()` تسجيله بنجاح في جدول `Permissions`
نفسه. `ActionToolbar.Rebuild()` تفلتر أي زر بصلاحية غير ممنوحة (`UIServices.Permissions.Can`) — فتختفي
الأزرار بصمت بلا أي خطأ أو تحذير، تحديداً ما وصفه المستخدم: "الإضافة تعمل لكن باقي الأزرار غير موجودة".
**الحل**: `SeedAdminRole()` تقرأ منح الدور الحالية دائماً (`GetRolePermissions`) وتُكمِّل أي مفتاح ناقص من
`PermissionKeys.All()`، سواء كان الدور جديداً أو موجوداً من قبل — ذاتية الإصلاح في كل إقلاع تالٍ.

### ملاحظات أصغر من نفس الدفعة

- `PagedViewModelBase.SearchText` موثَّقة كمسؤولية الوارث لدمجها مع `Filter` داخل `FetchPage`، لكن لا أي VM
  فعلها فعلياً — البحث النصي كان بلا أثر لكل الوحدات رغم أن السلسلة كاملة (Service→Repository→SQL) تعمل
  بشكل صحيح لو `Filter.SearchText` وصلها. مُركزَت في `GoToPageAsync` نفسها عبر Reflection (خاصية `SearchText`
  لو وُجدت على `TFilter`) بدل تكرارها يدوياً لكل VM.
- `AppComboBox`'s `chevron` (سهم القائمة) بلا `Fill` (Stroke فقط، منطقة نقر ضئيلة تقريباً) وبلا أي معالج نقر
  إطلاقاً — القائمة كانت تُفتح/تُغلَق فقط عبر GotFocus/LostFocus/`Popup.StaysOpen=False`، فالسهم نفسه بلا أثر
  حقيقي على الإطلاق. أُضيف `Fill="Transparent"` (يوسّع منطقة النقر لكامل الأيقونة) ومعالج نقر يفتح القائمة.
- `AppTextBox.IsReadOnly` كانت تُمرَّر فقط لـ`TextBox.IsReadOnly` (يمنع التعديل) بلا `Focusable=false` — الحقل
  يبقى قابلاً للنقر والتحديد بصرياً وكأنه تفاعلي (حقول `CreatedAt`/`UpdatedAt` تحديداً). أُضيف `Focusable`/
  `Cursor` مرتبطين بنفس الحالة.
- `CategoryDialogFactory.Build` كانت تعرض حقل اختيار "فئة أب" فوق حقل الاسم مباشرة — مربك عند إضافة فئة جديدة
  تماماً (لا معنى لاختيار أب حالي وقت الإنشاء الأول حسب طلب المستخدم الحي). أُزيل الحقل، الفورم الآن اسم+ملاحظات
  فقط. `Str.ParentCategory` ("التصنيف الأب") أُعيد تسميتها استخدامات Customer/Supplier/Product الخاصة بفئة
  العنصر نفسه (لا فئة أب حقيقية) إلى `Str.Category` ("الفئة") للتوحيد.
