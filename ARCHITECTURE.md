# ARCHITECTURE.md — PrimeERP

مرجع بنية PrimeERP الثمانية-الطبقات: القواعد الثابتة، القطع المبنية فعلاً القابلة للاستيراد، ونظام التصميم. لا سجل تطوير هنا — تاريخ القرارات في `git log`.

---

## القاعدة الدائمة الأولى — تُفحص قبل إنشاء أي ملف جديد

1. أي طبقة ينتمي إليها هذا الملف؟ راجع "قواعد الانتماء" أدناه.
2. يوجد أساس مشترك له بالفعل؟ → رِثه، لا تُعد بناءه.
3. لا يوجد وسيتكرر في أكثر من موضع؟ → ابنِ الأساس المشترك أولاً.
4. يخرق قاعدة اعتماد (طبقة أعلى تُستدعى من أدنى، أو تخطي أكثر من طبقة)؟ → أعد التفكير في الموضع، لا في كسر القاعدة.
5. يمكن وصفه بتكوين (بيانات) بدل كود؟ → افعل ذلك (ينطبق أساساً على 7.Composition/8.Modules).

عند مواجهة كود خاطئ أثناء العمل: احذف وابنِ سليماً — لا ترصّ كوداً فوق بنية معطوبة.

## القاعدة الدائمة الثانية — الحذف الفوري، لا استثناء

أي بناء مؤقت يُحذف فوراً عند اكتشافه — لا يُنقل، لا يُعدّل، لا يُبنى عليه. الاستثناء الوحيد: حذفه الآن يكسر البناء وبديله مجدوَل صراحة لاحقاً — يُسجَّل بتعليق `// TEMPORARY — السبب` ويُدرَج في "الدين التقني" أدناه. `check.sh` يرفض أي مؤقت غير مُسجَّل.

---

## الشجرة الثمانية

```
PrimeERP/
├── App.xaml, App.xaml.cs        ⚠️ استثناء أدوات WPF — يجب أن يبقيا في جذر المشروع (Pass 1 XAML compiler)
├── 1.Platform/      Diagnostics/ Security/ Permissions/ Audit/ Localization/ Settings/ Design/
├── 2.Data/          Providers/ Core/ Schema/ Query/ Repositories/ Seeders/
├── 3.Domain/        Entities/ Enums/ Rules/ Results/ Contracts/
├── 4.Application/   Pipeline/{Steps,Operations}/ Services/ Validation/ DTOs/
├── 5.Design/        Identity/{Default,Corporate}/ Semantic/ Components/ Styles/ Icons/ Strings/ Surfaces/ Theme.xaml
├── 6.UI/            Components/ Converters/ Behaviors/ Services/ ViewModels/ DevTools/
├── 7.Composition/   Definitions/ Renderers/ Registry/
├── 8.Modules/       ModuleRegistrations.cs، ReportRegistrations.cs، DemoDataSeeder.cs
└── App/             Bootstrap/DependencyInjection.cs، MainWindow.xaml(.cs) — Shell الحقيقي
```

---

## قواعد الانتماء لكل طبقة

| الطبقة | يحتوي | يُمنع أن يحتوي |
|---|---|---|
| **1.Platform** | بنية تحتية عابرة: صلاحيات، تدقيق، لغة، إعدادات، هوية بصرية (تبديل حزمة/وضع) | منطق أعمال محاسبي، أي مرجع لـ 4-8 |
| **2.Data** | مزوّدو قواعد بيانات، أدوات SQL خام، المستودعات (Repository) | أي قرار/حساب أعمال، استدعاء Repository آخر عبر منطق |
| **3.Domain** | كيانات صرفة، تعدادات، قواعد محاسبية نقية، Result/PagedResult، عقود التحقق | أي استدعاء DB، أي مرجع لأي طبقة أخرى — الطبقة الوحيدة النقية بلا استثناء |
| **4.Application** | خدمات الأعمال، DTOs، المدقّقون، Pipeline/Steps/Operations، ServiceBase/CrudServiceBase/PartyServiceBase | أي مرجع لـ 5-8 |
| **5.Design** | XAML فقط — رموز بصرية (Identity→Semantic→Components→Styles) | أي كود C# منطقي، أي مرجع خارج نفسه |
| **6.UI** | قطع الواجهة، محوّلات، خدمات UI-orchestration، أسس ViewModel عامة | استدعاء Repository مباشرة، منطق أعمال |
| **7.Composition** | تعريفات صفحات/حوارات كبيانات (Definitions)، مُصيِّرات تجمّع القطع (Renderers)، سجلّ الموديولات | أي منطق أعمال، أي XAML مخصص لصفحة واحدة |
| **8.Modules** | تسجيل كل وحدة عمل (ModuleDefinition واحدة لكل شاشة) عبر الاستدعاء المباشر لقطع 6/7 | منطق أعمال (يستدعي 4.Application فقط) |
| **App/** | نقطة الإقلاع (MainWindow = AppShell فقط) | أي منطق عدا تجميع/تسجيل |

---

## مصفوفة الاعتماد المسموح

`✅` مسموح ومُستهلَك · `⛔` ممنوع (يفحصه `Tools/ArchitectureCheck/check.sh`)

| من \ إلى | 1.Platform | 2.Data | 3.Domain | 4.Application | 5.Design | 6.UI | 7.Composition | 8.Modules |
|---|---|---|---|---|---|---|---|---|
| **1.Platform** | — | ✅ (Core/Schema) | ✅ | ⛔ | ⛔ | ⛔ | ⛔ | ⛔ |
| **2.Data** | ✅ (Settings) | — | ✅ | ⛔ | ⛔ | ⛔ | ⛔ | ⛔ |
| **3.Domain** | ⛔ | ⛔ | — | ⛔ | ⛔ | ⛔ | ⛔ | ⛔ |
| **4.Application** | ✅ | ✅ | ✅ | — | ⛔ | ⛔ | ⛔ | ⛔ |
| **5.Design** | ⛔ | ⛔ | ⛔ | ⛔ | — | ⛔ | ⛔ | ⛔ |
| **6.UI** | ✅ | ⛔ | ✅ | ✅ | ✅ | — | ⛔ | ⛔ |
| **7.Composition** | ✅ | ⛔ | ✅ | ✅ | ✅ | ✅ | — | ⛔ |
| **8.Modules** | ✅ | ⛔ | ✅ | ✅ | ✅ | ✅ | ✅ | — |
| **App/** | ✅ | ⛔ | ➖ | ✅ | ➖ | ✅ | ✅ | ✅ |

**انحرافات موثَّقة عن هذه المصفوفة**: `1.Platform/{Settings,Permissions,Audit}` تستدعي `2.Data.Core.DbHelper`/`2.Data.Schema.SchemaBuilder` مباشرة (أدوات اتصال، لا طبقة أعمال — كل خدمة تملك مستودعها الخاص داخل نفس طبقتها). `BackupService.cs` (4.Application) يستدعي `DbHelper` مباشرة (أوامر BACKUP/RESTORE إدارية، لا CRUD كيان له Repository).

---

## القطع المبنية — الاستيراد بدل إعادة الكتابة

قاعدة العمل: أي صفحة/حوار/تقرير جديد هو **تجميع** لهذه القطع عبر `ModuleDefinition` في `8.Modules`، لا كود جديد. أربعة أنماط جاهزة بالكامل:

| النمط | المُصيِّر (Renderer) | يُستهلَك عبر |
|---|---|---|
| قائمة+CRUD | `7.Composition/Renderers/CrudPageRenderer.cs` | `ModuleDefinition.Dialog` أو `.DocumentDialog` + `.Columns` |
| مستند (رأس+سطور) | `7.Composition/Renderers/DocumentRenderer.cs` | `ModuleDefinition.DocumentDialog` (`DocumentDialogDefinition`) |
| شجرة | `7.Composition/Renderers/TreeRenderer.cs` + `TreeBuilder.cs` | `ModuleDefinition.TreeOptions` |
| تقرير | `7.Composition/Renderers/ReportRenderer.cs` | `ModuleDefinition.Report` (`ReportDefinition`) |
| إعدادات | `7.Composition/Renderers/SettingsPageRenderer.cs` | `LayoutKind.Settings` |

`PageRenderer.cs` نقطة التوزيع الوحيدة حسب `ModuleDefinition.LayoutKind`. حوار الحقول المسطّحة (رأس فقط) عبر `DialogRenderer.cs` — تُستهلكه كل من CrudPageRenderer وDocumentRenderer لرأس المستند. `CategoryDialogFactory.cs`/`StandardFields.cs` قطع تعريف قابلة لإعادة الاستخدام (حوار فئة موحّد، حقول نشط/تاريخ إنشاء-تعديل قياسية).

**6.UI/Components** — القطع المرئية، مصنّفة:

- **Display**: `AppDataGrid` (شبكة بيانات موحّدة، أعمدة/فرز/ترقيم داخلي/صف إجراءات)، `AppPagination` (ترقيم من طرف الخادم)، `AppCard`، `AppEmptyState`، `AppLoadingOverlay`
- **Inputs**: `AppTextBox`، `AppTextArea`، `AppNumericBox`، `AppDatePicker`، `AppCheckBox`، `AppComboBox` (بحث+ترشيح)، `AppPasswordBox`، `AppSearchBox`
- **Actions**: `AppButton`، `ToolbarAction` (تعريف زر شريط أدوات جاهز: New/Edit/Delete/Refresh/Save/Cancel/Print/Export/Post/Unpost/ExpandAll/CollapseAll)، `ActionToolbar` (يستهلك `List<ToolbarAction>`، يفلتر حسب الصلاحية تلقائياً)
- **Feedback**: `AppToast`/`ToastService` (إشعارات، Error تُغلَق تلقائياً بعد 5 ثوانٍ)، `AppDialogWindow` (قاعدة كل نوافذ الحوار)، `AppConfirmDialog`، `AppMessageDialog`، `AppProgressDialog`
- **Layout**: `PageHeader` (عنوان+منطقة إجراءات)، `FilterBar` (بحث+عداد نتائج+فلاتر إضافية)
- **Shell**: `AppSidebar`/`NavItem`/`NavItemViewModel` (تنقّل هرمي بمجموعات، صلاحيات تتحكّم بالظهور تلقائياً)، `AppTopBar`
- **Tree**: `AppTreeView`، `TreeNodeViewModel`
- **Pickers**: نوافذ اختيار متخصصة (`AccountPicker`, `CustomerPicker`, `SupplierPicker`, `ProductPicker`, `EmployeePicker`) فوق `PickerGridWindow`/`PickerTreeWindow`

**6.UI/ViewModels/Base** — `PagedViewModelBase<TDto,TFilter>` (صفحات+بحث+فرز عام)، `CrudViewModelBase<TDto,TFilter>` (يضيف Add/Edit/Delete)، `TreeViewModelBase<TDto,TFilter>`، `PermissionAwareViewModel`. كل ViewModel وحدة عمل يرث من هذه فقط — لا منطق تحميل/حفظ مكرر.

**6.UI/Services** — `ToastService`, `DialogService`, `ExportService` (CSV/Excel/PDF)، `IdentityService` (تبديل حزمة الهوية/الوضع الداكن)، `ThemeService`، `NavigationService`، `UIServices` (بوابة الوصول من code-behind بلا حقن مُنشئ).

**4.Application** — `ServiceBase`/`CrudServiceBase`/`PartyServiceBase` (أساس كل خدمة عمل: صلاحيات + Result موحّد)، `Pipeline/Steps`+`Pipeline/Operations` (تركيب عمليات متعددة الخطوات). كل خدمة عمل (Accounting/Parties/Inventory/Sales/Purchasing/HR/Assets/Security/Backup/Print) ترث من هذه.

**2.Data** — `WhereBuilder` (بناء SQL WHERE آمن)، `RepositoryBase`، `StockAdjustmentRepositoryBase` (أساس مشترك لـStockIn/StockOut). `DbFactory`/`IDbProvider` مع ثلاثة مزوّدين (`Sqlite`/`SqlServer`/`PostgreSql`) — قابل التبديل عبر `DbConfig.Provider`.

---

## نظام التصميم (منفَّذ بالكامل — لا يُعاد بناؤه)

سلسلة موارد أربع طبقات في `5.Design/`، تُستهلَك عبر `DynamicResource` فقط (تتبدّل حيّاً بلا إعادة تحميل):

1. **Identity** (`Identity/{Default,Corporate}/Primitives.*.xaml`) — القيم الخام: ألوان، مسافات، خطوط، أبعاد، ظلال. حزمتان بديلتان (يُختار بينهما عبر `IIdentityService.Apply`).
2. **Semantic** (`Semantic/*.xaml`) — تسمية دلالية فوق L1 (`TextPrimary`, `SurfaceDefault`, `BrandDefault`...)، فاتح/داكن منفصلان (`Semantic.Light.xaml`/`Semantic.Dark.xaml`).
3. **Components** (`Components/Tokens.*.xaml`) — رموز خاصة بمكوّن (أبعاد حوار، ارتفاع إدخال...).
4. **Styles** (`Styles/*.xaml` + `Implicit.xaml`) — أنماط WPF فعلية (`Style.Button.xaml`, `Style.Input.xaml`...)، منها أنماط ضمنية (`TargetType="Button"` بلا `x:Key`) تُطبَّق تلقائياً على أي عنصر أساسي.

**لتغيير ألوان/خطوط التطبيق بالكامل**: عدّل `5.Design/Identity/{الحزمة}/Primitives.Color.xaml` أو `Primitives.Type.xaml` فقط — لا تلمس Semantic/Components/Styles. مرجع القيم الكامل: `5.Design/DESIGN_TOKENS.md` (مولَّد عبر `Tools/DesignTokens/generate.sh`) و`DESIGN_SYSTEM.md`. الأيقونات في `5.Design/Icons/Icons.xaml` (مفاتيح `IconAdd`/`IconEdit`/... — `Geometry` فقط). النصوص في `5.Design/Strings/Strings.{ar,en}.xaml` عبر `LocalizationService.Get(key)`.

---

## الدين التقني الحالي (يفحصه `check.sh` تلقائياً في كل تشغيل)

| العنصر | لماذا | الحالة |
|---|---|---|
| `BackupService.cs` يستدعي `DbHelper` مباشرة | أوامر BACKUP/RESTORE إدارية، لا CRUD كيان له Repository | استثناء دائم موثَّق |
| `5.Design/Identity/{Corporate,Default}/Primitives.Color.xaml` — قيم Hex مكررة تحت أسماء مختلفة | تدرّج ألوان متقارب عمداً في التصميم | مقبول، مُراقَب |
| `// TODO` واحد بلا رقم بند صريح | — | يُفضَّل ربطه ببند أو حذفه |

`6.UI/DevTools/{ControlsGalleryPage,MockPickerDataSources}` مُستثناة من بناء Release (أداة تطوير دائمة، خارج المسار الحي).

---

## التحقق

- `dotnet build` — 0 خطأ مطلوب قبل أي التزام.
- `dotnet test` (مشروع `PrimeERP.Tests`) — اختبارات حقيقية (SQLite مؤقتة، WPF حقيقي عبر `WpfApplicationFixture`، بلا Mock للطبقات الداخلية).
- `bash Tools/ArchitectureCheck/check.sh` — يفحص حدود الطبقات/المسؤولية/التصميم/التكرار/المؤقتات، يخرج بكود غير صفري عند أي `FAIL`.
