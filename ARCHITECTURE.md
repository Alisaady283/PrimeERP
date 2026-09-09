# ARCHITECTURE.md — PrimeERP

**أين يسكن كل شيء، وما المبنيّ فعلاً.** أما *ماذا يُفعَل وكيف* فهو في [RULES.md](RULES.md).

---

## الشجرة

```
PrimeERP/
├── App.xaml(.cs)      ⚠️ يبقيان في الجذر — قيد مُصرِّف XAML
├── 1.Platform/        Diagnostics/ Security/ Permissions/ Audit/ Localization/ Settings/ Design/
├── 2.Data/            Providers/ Core/ Schema/ Query/ Repositories/ Seeders/
├── 3.Domain/          Entities/ Enums/ Rules/ Results/ Contracts/
├── 4.Application/     Pipeline/ Services/ Validation/ DTOs/
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
| **3.Domain** | كيانات، تعدادات، قواعد محاسبية نقية، `Result`، عقود التحقق | أي استدعاء قاعدة بيانات، أي مرجع لأي طبقة — **النقيّة الوحيدة** |
| **4.Application** | خدمات الأعمال، DTOs، المتحقّقون، Pipeline | أي مرجع لـ 5-8 |
| **5.Design** | XAML فقط: رموز بصرية | أي C# منطقي |
| **6.UI** | القطع المرئية، محوّلات، خدمات واجهة، أسس ViewModel | استدعاء مستودع، منطق أعمال |
| **7.Composition** | التعريفات (بيانات)، المُصيِّرات (تجميع)، السجلّ | منطق أعمال، XAML لصفحة بعينها |
| **8.Modules** | تسجيل كل وحدة عمل إعلاناً | أي منطق أعمال |

**اتجاه الاعتماد**: من أعلى الرقم إلى أدنى فقط. `3.Domain` و`5.Design` لا تعتمدان على شيء. يفحصه `check.sh` آلياً.

**انحرافان موثَّقان**: `1.Platform/{Settings,Permissions,Audit}` تستدعي `2.Data.Core.DbHelper` (أداة اتصال لا طبقة أعمال) · `BackupService` يستدعي `DbHelper` (أوامر إدارية لا CRUD).

---

## المبنيّ فعلاً

### 3.Domain
كيانات صرفة · تعدادات · `Result`/`PagedResult` · `Rules.For<T>()` (`Required`, `MinLength`, `MaxLength`, `Range`, `Positive`, `Email`, `Phone`, `DateValid`, `Unique`, `Custom`) · `IValidator<T>` · `ValidationResult`.

### 2.Data
`RepositoryBase<T>` · `WhereBuilder` (شرط SQL آمن) · `OrderBuilder` (قاعدة الترتيب الواحدة) · `SchemaBuilder` · `MigrationRunner` · `DbFactory`/`IDbProvider` بثلاثة مزوّدين (`Sqlite`/`SqlServer`/`PostgreSql`) قابلة التبديل عبر `DbConfig.Provider`.

**أساسان مشتركان للمستندات**: `StockAdjustmentRepositoryBase` (رأس+سطور بمخزن وتكلفة) و`CycleDocumentRepositoryBase` (رأس+سطور بطرف وسعر بلا أثر مخزني) — يُمرَّر لكلٍّ اسما جدوليه، فكل مستند جديد وارثٌ بسطر.

**قاعدة الترتيب** (`OrderBuilder`): المستند بتاريخه تنازلياً — الأحدث أعلى — ثم برقمه ثم بوقت إنشائه، لأن الرقم وحده لا يعكس الترتيب حين تُضاف السجلات من أكثر من شاشة بسلاسل مختلفة. والبيانات الأساسية بكودها. و`Id` آخر فاصل دائماً فالترتيب قطعيّ. فرز المستخدم من رأس الجدول يسبق ذلك ولا يُلغي الفواصل بعده.

### 4.Application
`ServiceBase` — كل خدمة ترثه: `Can` · `FailDenied` · `Check` · `Audit` · `Tx` · `Msg` · `Settings`.
`CrudServiceBase` · `PartyServiceBase` · `CycleDocumentServiceBase` · `StockAdjustmentServiceBase` · `ChequeDocumentServiceBase` · `VoucherServiceBase`.
`Pipeline/{Steps,Operations}` لتركيب العمليات متعددة الخطوات.
`Validation/` متحقّق لكل كيان.

**الأقسام**: Accounting · Parties · Inventory · Sales · Purchasing · Documents · Cheques · Vouchers · HR · Assets · Treasury · Security · Backup · Print.

### 5.Design
أربع طبقات موارد تُستهلَك بـ`DynamicResource` حصراً (تتبدّل حيّاً بلا إعادة تحميل):

**Identity** (قيم خام — ثلاث حزم: `Signature`/`Default`/`Corporate`) → **Semantic** (معنى — فاتح/داكن منفصلان) → **Components** (رموز المكوّن) → **Styles** (أنماط WPF، منها ضمنية بلا `x:Key` تُطبَّق تلقائياً).

**سلالم الوضع الداكن**: `NeutralDark` للأسطح (الإضاءة تزيد مع الارتفاع) · `Nav` للشريط الجانبي وحده. لا تُخلطان.

⚠️ **قاعدتان تكسران الألوان صامتاً بلا خطأ بناء**:
1. رموز الطبقة الثالثة تقرأ ألوان الثانية عبر مفاتيح `{Name}.Color` يشتقّها `IdentityService.RefreshDerivedColors` من **كل** فرش الشجرة المدموجة — لا تُعِده لقائمة أسماء يدوية: أي فرشاة خارج القائمة تُحلّ **شفافة**.
2. `UI.Identity` مخزَّن في القاعدة و`SettingSeeder` لا يستبدل قيمة قائمة — إطلاق حزمة جديدة يتطلّب رفع `IdentityService.IdentityBaseline`.

**الأيقونات**: مواصفة واحدة في `Tokens.Icon.xaml` وقطعة `AppIcon` مستهلكها الوحيد. المرجع الكامل: `5.Design/DESIGN_TOKENS.md` و`DESIGN_SYSTEM.md`.

### 6.UI
**Components**:
- **Display** — `AppDataGrid` (أعمدة/فرز/ترقيم/صف إجراءات) · `AppPagination` · `AppCard` · `AppEmptyState` · `AppLoadingOverlay`
- **Inputs** — `AppTextBox` · `AppTextArea` · `AppNumericBox` · `AppDatePicker` · `AppCheckBox` · `AppComboBox` (بحث+ترشيح) · `AppPasswordBox` · `AppSearchBox`
- **Actions** — `AppButton` · `ToolbarAction` (New/Edit/Delete/Refresh/Save/Cancel/Print/Export/Post/Unpost/…) · `ActionToolbar` (يُخفي ما لا صلاحية له تلقائياً)
- **Feedback** — `AppDialogWindow` (قاعدة كل الحوارات) · `AppConfirmDialog` · `AppMessageDialog` · `AppProgressDialog` · `AppToast`/`ToastService`
- **Layout** — `PageHeader` · `FilterBar`
- **Shell** — `AppSidebar`/`NavItem` (الصلاحيات تتحكّم بالظهور) · `AppTopBar`
- **Tree** — `AppTreeView` · `TreeNodeViewModel`
- **Pickers** — نوافذ اختيار فوق `PickerGridWindow`/`PickerTreeWindow`

**ViewModels/Base** — `PagedViewModelBase<TDto,TFilter>` · `CrudViewModelBase<TDto,TFilter>` · `TreeViewModelBase` · `PermissionAwareViewModel`.

**Services** — `ToastService` · `DialogService` · `ExportService` (CSV/Excel/PDF) · `IdentityService` · `ThemeService` · `NavigationService` · `UIServices` (بوابة code-behind).

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

`DialogRenderer` يبني الحقول المسطّحة — يستهلكه `CrudPageRenderer` و`DocumentRenderer` لرأس المستند. `FieldValidation` يفحص الرأس والسطور معاً.

**قطع تعريف جاهزة**: `StandardFields` (حقول قياسية + مدى التاريخ) · `CategoryDialogFactory` · `TradePaper` (أعمدة وإجماليات وحساب سطر الفواتير).

### 8.Modules
تسجيل كل وحدة إعلاناً، عبر مصانع مشتركة: `StockDocumentFactory` · `CycleDocumentRegistrations` · `CycleVoucherRegistrations` · `TreasuryRegistrations` · `BalanceReportFactory` · `RegisterLookup`.

**دورتا الشراء والبيع**: `Documents.SimplifiedFlow` (إعداد) يحكم أي المستندات تظهر عبر `ModuleDefinition.FlowScope` و`IModuleRegistry.VisibleFor`. تتبّع السحب في جدول واحد `DocumentLinks` عبر `IDocumentLinkService` — لا يعرف نوع مستند بعينه. `DocumentDialogDefinition.PullSources` يصف من أين يسحب كل مستند.

---

## الدين التقني المسجَّل

| العنصر | لماذا | الحالة |
|---|---|---|
| `BackupService` يستدعي `DbHelper` | أوامر BACKUP/RESTORE إدارية لا CRUD | استثناء دائم |
| قيم Hex مكرّرة في حزمتَي هوية | تدرّج متقارب عمداً | مقبول، مُراقَب |
| `// TODO` واحد بلا بند | — | يُربَط ببند أو يُحذف |

`6.UI/DevTools` مُستثناة من بناء Release.

## التحقق

```
dotnet build                          # صفر خطأ
dotnet test                           # SQLite مؤقتة + WPF حقيقي، بلا Mock للطبقات الداخلية
bash Tools/ArchitectureCheck/check.sh # حدود الطبقات والمسؤولية والتصميم والتكرار والمؤقتات
```
