# ARCHITECTURE.md — PrimeERP

هذا الملف يوثّق البنية المعمارية الثمانية-الطبقات لِـ PrimeERP بعد R1 (إعادة الهيكلة). يُستكمل مع كل بند لاحق (R2–R10).

---

## القاعدة الدائمة — تُفحص قبل إنشاء أي ملف جديد

1. **أي طبقة ينتمي إليها هذا الملف؟** — راجع "قواعد الانتماء" أدناه قبل اختيار المكان.
2. **يوجد أساس مشترك له بالفعل؟** → رِثه، لا تُعد بناءه.
3. **لا يوجد وسيتكرر في أكثر من موضع؟** → ابنِ الأساس المشترك أولاً.
4. **يخرق قاعدة اعتماد (طبقة أعلى تُستدعى من أدنى، أو تخطي أكثر من طبقة)؟** → أعد التفكير في موضع الملف، لا في كسر القاعدة.
5. **يمكن وصفه بتكوين (بيانات) بدل كود؟** → افعل ذلك (ينطبق بشكل رئيسي على 7.Composition/8.Modules).

عند مواجهة كود خاطئ أثناء العمل: **احذف وابنِ سليماً — لا ترصّ كوداً فوق بنية معطوبة.**

---

## الشجرة الثمانية

```
PrimeERP/
├── App.xaml, App.xaml.cs        ⚠️ استثناء إلزامي — راجع "استثناء App.xaml" أدناه
├── 1.Platform/      Diagnostics/ Security/ Permissions/ Audit/ Localization/ Settings/
├── 2.Data/          Providers/ Core/ Schema/ Query/ Repositories/
├── 3.Domain/        Entities/ Enums/ Rules/ Results/ Contracts/
├── 4.Application/   ServiceLocator.cs, Pipeline/{Steps,Operations}/ Services/ Validation/ DTOs/
├── 5.Design/        Identity/{Default,Corporate}/ Semantic/ Styles/ Icons/ Strings/ Surfaces/ Theme.xaml
├── 6.UI/            Components/ Converters/ Behaviors/ Services/ ViewModels/ DevTools/
├── 7.Composition/   Definitions/ Renderers/ Registry/          (فارغة بعد — R8)
├── 8.Modules/       Accounting/ Parties/ Inventory/ Sales/ Purchases/ HR/ Reports/ System/  (فارغة بعد — R9)
└── App/             MainWindow.xaml(.cs)                        (Bootstrap/Shell تُستكمل — R9)
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
| **4.Application** | خدمات الأعمال (Service)، DTOs، مدقّقو الإدخال (Validators)، لاحقاً: Pipeline/Steps/Operations/ServiceBase | أي مرجع لـ 5.Design/6.UI/7.Composition/8.Modules — **مخالفة معروفة حالياً**: `PrintService` (Print) يستدعي `ExportService` (6.UI) — راجع "مخالفات معروفة" أدناه |
| **5.Design** | XAML فقط — رموز بصرية (Identity→Semantic→Styles)، لا كود C# وراء أي منطق (ExportTheme.cs ثوابت صرفة مسموحة استثناءً موثَّقاً مسبقاً) | أي كود C# منطقي، أي مرجع خارج نفسه |
| **6.UI** | قطع الواجهة (UserControl/Window)، محوّلات (Converters)، خدمات UI-orchestration (Dialog/Toast/Navigation/Export/Identity/Theme)، ViewModel أسس عامة | استدعاء Repository مباشرة، منطق أعمال محاسبي |
| **7.Composition** | تعريفات صفحات/حوارات كبيانات (Definitions)، مُصيِّرات تجمّع القطع (Renderers)، سجلّ الموديولات | أي منطق أعمال، أي XAML مخصص لصفحة واحدة |
| **8.Modules** | تجميع نهائي لكل نطاق عمل (ViewModel متخصص + تعريف Composition + أي ربط خاص) | منطق أعمال (يستدعي 4.Application فقط، لا يُعيد تنفيذه) |
| **App/** | نقطة الإقلاع (MainWindow لاحقاً = AppShell فقط) | أي منطق عدا تجميع/تسجيل |

---

## جدول الاعتماد الفعلي (بعد R1 — بحث حقيقي، لا افتراض)

| من | يعتمد على (namespaces مستوردة فعلياً) |
|---|---|
| 1.Platform | 2.Data (DbHelper مباشرة — راجع "قيد Platform" أدناه)، 3.Domain (Enums/Results) |
| 2.Data | 1.Platform.Settings (Seeders تقرأ SettingKeys)، 3.Domain |
| 3.Domain | لا شيء — نقية 100% |
| 4.Application | 1.Platform، 2.Data، 3.Domain، 6.UI (**مخالفة واحدة معروفة**، PrintService→ExportService) |
| 5.Design | لا شيء (XAML صرف) |
| 6.UI | 1.Platform (AppSession/Localization)، 3.Domain (StatusVariant/Enums)، 5.Design (ضمنياً عبر DynamicResource) |
| 7.Composition | (فارغة بعد) |
| 8.Modules | (فارغة بعد) |
| App/ | 1.Platform، 4.Application (كل الخدمات، عبر ServiceLocator)، 6.UI.DevTools (مؤقت — ControlsGalleryPage) |

### ⚠️ قيد Platform — انحراف موثَّق عن "لا يعتمد على شيء"

`1.Platform/Settings/*`, `1.Platform/Permissions/PermissionDb.cs`, `1.Platform/Audit/AuditLogger.cs` تستدعي `PrimeERP.Data.Core.DbHelper` مباشرة (اتصال SQL خام، لا عبر `2.Data/Repositories`). هذا **مقصود لا سهو**: هو بالضبط ما يقطع الدائرية `Database↔Services.Settings` التي رصدها التقرير المعماري — `SettingsService` الآن يملك مستودعه الخاص (`SettingRepository`) داخل نفس طبقته، فلا يحتاج طبقة 2.Data للـ Settings إطلاقاً، بينما 2.Data (Seeders) يستدعي `1.Platform.Settings` (اتجاه واحد فقط، سليم). الاعتماد الحقيقي المتبقي هو `1.Platform → 2.Data.Core` (أدوات SQL خام لا مستودعات) — وهذا اعتماد على "أداة" لا "طبقة أعمال"، يُعامل كبنية تحتية مشتركة (نفس منطق أن `3.Domain` وحدها الطبقة الخالية تماماً؛ `1.Platform` تُعامَل عملياً كـ"طبقة صفر ونصف" فوق أدوات DB الخام فقط، تحت كل شيء آخر).

### ⚠️ مخالفة معروفة تنتظر R2

`4.Application/Services/Print/PrintService.cs` يستدعي `PrimeERP.UI.Services.ExportService.Instance.ExportPrintableToPdf(...)` — Application(4) يعتمد على UI(6)، عكس اتجاه الاعتماد المسموح. **لم يُصلَح في R1** (نقل فقط، صفر تغيير منطق) — مسجَّل هنا صراحة لِـ R2 (فحص الحدود) ليلتقطه ويُقرَّر حله (نقل ExportService لـ 4.Application، أو فصل توليد PDF عن التصدير العام).

---

## الحذف/الدمج المُنفَّذ في R1

- **`_Legacy/`** (10 ملفات، كانت مستبعدة من الترجمة أصلاً) — محذوفة نهائياً.
- **`Core/Transactions/{UnitOfWork,IUnitOfWork}.cs`** — محذوفتان (صفر مستهلك مؤكَّد).
- **`Resources/Icons.xaml`، `Resources/Strings.xaml`** (الجذريتان الفارغتان) — محذوفتان؛ النسختان الحقيقيتان (`Icons/Icons.xaml`, `Strings/Strings.ar|en.xaml`) انتقلتا لـ `5.Design/`.
- **`Views/Windows/LoginWindow.xaml(.cs)`** — محذوفة (Grid فارغ، صفر منطق) — تُبنى حقيقية في R9.
- **8 ViewModels فارغة** (`CustomersViewModel`...) و**14 صفحة/حوار فارغة** (`Views/Pages/*`, `Views/Dialogs/*`) — محذوفة (كلاسات/شاشات فارغة تماماً، صفر مستهلك) — تُبنى عبر 7.Composition/8.Modules في R7–R9.
- **`ISupplierService.cs`** — **لم يُحذف رغم تصنيفه "يُحذف ويُبنى من جديد"**: لا يزال `AccountService.ResolveAutoLink` و`AccountServiceTests` يعتمدان عليه فعلياً؛ حذفه الآن يكسر البناء والاختبارات معاً (يخالف "R1: نقل فقط، صفر تغيير منطق"). **نُقل كما هو** إلى `4.Application/Services/Parties/`؛ إعادة بنائه الفعلية ضمن R6 مع `PartyServiceBase` كما ورد صراحة هناك.
- **`ServiceLocator.cs`** — نُقل كما هو إلى `4.Application/` (لم يُحذف — لا يزال العمود الوحيد لربط الخدمات المتقاطعة). يُستبدل بـ DI حقيقي في R3.

---

## اكتشاف فني إضافي أثناء R1 (يستحق التسجيل)

**تسمية الطبقة "Application" تتصادم مع `System.Windows.Application`**: أي ملف تحت شجرة `PrimeERP.*` يستخدم `Application.Current`/`: Application` بلا تأهيل كامل يتعرّض لخطر أن يحلّه المترجم كإشارة لمساحة الاسم `PrimeERP.Application` (طبقة 4) بدل نوع WPF — C# يبحث في مساحات الاسم المحيطة صعوداً قبل استشارة `using`. **الحل المُطبَّق**: كل إشارة WPF لـ `Application` في الكود مؤهَّلة بالكامل الآن (`System.Windows.Application`) — 11 ملفاً. أي ملف جديد يستخدم `Application.Current` مستقبلاً **يجب** أن يكتبها مؤهَّلة بالكامل لنفس السبب.

---

## التحقق النهائي لـ R1

`dotnet build PrimeERP.csproj -m:1` → 0 تحذير، 0 خطأ. `dotnet build PrimeERP.Tests/PrimeERP.Tests.csproj -m:1` → 0 خطأ (تحذيرا Nullable سابقان على R1، غير متعلقين به). `dotnet test -m:1 --no-build` → **134/134 ناجح، صفر فشل، صفر تعديل على أي اختبار**.
