# تقرير معماري كامل — PrimeERP

تاريخ الفحص: 2026-08-21. كل رقم في هذا التقرير نتيجة بحث فعلي (`grep`/`wc -l`/فحص محتوى) وقت كتابته، لا تقدير. تقرير وصفي بحت — لا حلول، لا إصلاحات، لا اقتراحات.

**مفاتيح التصنيف المستخدمة في كل الجداول:**

الحالة: `نهائي` (مبني، مختبَر أو مُستهلَك فعلياً، لا نقص معروف) · `مؤقت` (موثّق صراحة كحل مؤقت في تعليقاته أو محتوى تطوير فقط) · `مكرر` (نفس المنطق موجود منفصلاً في أكثر من ملف) · `ميت` (صفر مستهلك حقيقي، أو مستبعد من الترجمة) · `ناقص` (مبني جزئياً، بلا اتصال بمصدر بيانات حقيقي، أو عقد بلا تنفيذ كافٍ)

---

# 1 — خريطة المشروع الفعلية

315 ملف مصدر (`.cs`+`.xaml`) خارج `bin/`/`obj/`. مجمَّعة حسب المجلد الجذر.

## App.xaml / App.xaml.cs / AssemblyInfo.cs

| الملف | Namespace | أسطر | الطبقة | الحالة |
|---|---|---|---|---|
| App.xaml | — (Application) | 12 | تشغيل | نهائي |
| App.xaml.cs | PrimeERP | 40 | تشغيل | نهائي — يسجّل 13 خدمة في ServiceLocator، يستدعي IdentityService.Initialize فقط؛ لا تهيئة قاعدة بيانات هنا (راجع القسم 5) |
| AssemblyInfo.cs | — | 10 | تشغيل | نهائي |

## Converters/ (جذر، منفصل عن Views/Converters)

| الملف | Namespace | أسطر | الطبقة | الحالة |
|---|---|---|---|---|
| Converters/BoolToVisibilityConverter.cs | (يُتحقق عند القراءة) | 24 | Converter | نهائي |
| Converters/DecimalFormatConverter.cs | — | 26 | Converter | نهائي |
| Converters/StringToVisibilityConverter.cs | — | 23 | Converter | نهائي — **ملاحظة**: يوجد أيضاً `Views/Converters/VariantToBrushConverter.cs` — مجلدا Converters منفصلان (`/Converters` و`/Views/Converters`)، لا تجميع واحد |

## Core/

| الملف | Namespace | أسطر | الطبقة | الحالة |
|---|---|---|---|---|
| Core/AppSession.cs | PrimeERP.Core | 59 | Core | نهائي — `DevMode` مضبوطة `true` تلقائياً في DEBUG (راجع القسم 5) |
| Core/Audit/AuditLogger.cs | PrimeERP.Core.Audit | 133 | Core | نهائي |
| Core/Common/PagedResult.cs | PrimeERP.Core.Common | 16 | Core | نهائي |
| Core/Common/Result.cs | PrimeERP.Core.Common | 71 | Core | نهائي |
| Core/Common/StatusVariant.cs | PrimeERP.Core.Common | 29 | Core | نهائي |
| Core/Database/BackupCapability.cs | PrimeERP.Core.Database | 15 | Core | نهائي |
| Core/Database/DatabaseProvider.cs | PrimeERP.Core.Database | 9 | Core | نهائي |
| Core/Database/DbConfig.cs | PrimeERP.Core.Database | 71 | Core | نهائي |
| Core/Database/DbConnectionTester.cs | PrimeERP.Core.Database | 28 | Core | نهائي |
| Core/Database/DbFactory.cs | PrimeERP.Core.Database | 42 | Core | نهائي |
| Core/Database/DbHelper.cs | PrimeERP.Core.Database | 232 | Core | نهائي |
| Core/Database/IDbProvider.cs | PrimeERP.Core.Database | 75 | Core | نهائي |
| Core/Database/MigrationRunner.cs | PrimeERP.Core.Database | 57 | Core | نهائي |
| Core/Database/Providers/PostgreSqlProvider.cs | PrimeERP.Core.Database.Providers | 83 | Core | ناقص — لا اختبار واحد يغطيها (القسم 9)، لا استهلاك فعلي (DbFactory الحالي يُهيَّأ SQLite فقط في كل الاختبارات) |
| Core/Database/Providers/SqlServerProvider.cs | PrimeERP.Core.Database.Providers | 92 | Core | ناقص — نفس الملاحظة |
| Core/Database/Providers/SqliteProvider.cs | PrimeERP.Core.Database.Providers | 73 | Core | نهائي — المزوّد الوحيد المُستخدَم فعلياً |
| Core/Database/SchemaBuilder.cs | PrimeERP.Core.Database | 125 | Core | نهائي |
| Core/Enums.cs | PrimeERP.Core | 115 | Core | نهائي |
| Core/Permissions/IPermissionService.cs | PrimeERP.Core.Permissions | 13 | Core | نهائي |
| Core/Permissions/PermissionKeys.cs | PrimeERP.Core.Permissions | 157 | Core | نهائي |
| Core/Permissions/PermissionService.cs | PrimeERP.Core.Permissions | 53 | Core | ناقص — `LoadForUser` غير مستدعاة من أي مكان في الكود الحي (لا شاشة دخول) — راجع القسم 5 |
| Core/Security/PasswordHasher.cs | PrimeERP.Core.Security | 32 | Core | ناقص — لا `UserService` يستهلكها |
| Core/Transactions/IUnitOfWork.cs | PrimeERP.Core.Transactions | 11 | Core | ميت — صفر مستهلك خارج تعريفها هي وUnitOfWork.cs نفسها |
| Core/Transactions/UnitOfWork.cs | PrimeERP.Core.Transactions | 55 | Core | ميت — نفس الملاحظة؛ كل الخدمات الفعلية تستخدم `Db.RunTransaction` بدلاً منها |
| Core/Validation/AccountingRules.cs | PrimeERP.Core.Validation | 76 | Core | نهائي |
| Core/Validation/FiscalPeriodCalculator.cs | PrimeERP.Core.Validation | 42 | Core | نهائي |
| Core/Validation/IValidator.cs | PrimeERP.Core.Validation | 7 | Core | نهائي |
| Core/Validation/ValidationResult.cs | PrimeERP.Core.Validation | 28 | Core | نهائي |
| Core/Validation/ValidatorBase.cs | PrimeERP.Core.Validation | 79 | Core | نهائي |
| Core/Validation/Validators/AccountValidator.cs | PrimeERP.Core.Validation.Validators | 36 | Core | نهائي — تُستهلَك من AccountService |
| Core/Validation/Validators/CustomerValidator.cs | PrimeERP.Core.Validation.Validators | 21 | Core | نهائي — تُستهلَك من CustomerService |
| Core/Validation/Validators/EmployeeValidator.cs | PrimeERP.Core.Validation.Validators | 22 | Core | ناقص — صفر مستهلك (لا EmployeeService) |
| Core/Validation/Validators/InvoiceValidator.cs | PrimeERP.Core.Validation.Validators | 35 | Core | ناقص — صفر مستهلك (لا InvoiceService) |
| Core/Validation/Validators/JournalValidator.cs | PrimeERP.Core.Validation.Validators | 33 | Core | نهائي — تُستهلَك من JournalService |
| Core/Validation/Validators/ProductValidator.cs | PrimeERP.Core.Validation.Validators | 24 | Core | ناقص — صفر مستهلك (لا ProductService) |
| Core/Validation/Validators/SupplierValidator.cs | PrimeERP.Core.Validation.Validators | 21 | Core | ناقص — صفر مستهلك (لا SupplierService) |
| Core/Validation/Validators/UserValidator.cs | PrimeERP.Core.Validation.Validators | 33 | Core | ناقص — صفر مستهلك (لا UserService) |

## Database/ (طبقة Repository)

| الملف | Namespace | أسطر | الطبقة | الحالة |
|---|---|---|---|---|
| Database/AccountRepository.cs | PrimeERP.Database | 219 | Data | مكرر — راجع القسم 4 (نمط (conn,tx) التوأم، CRUD) |
| Database/BackupRepository.cs | PrimeERP.Database | 99 | Data | مكرر — نفس الملاحظة (أخف) |
| Database/CustomerRepository.cs | PrimeERP.Database | 284 | Data | مكرر — أكبر تكرار WHERE-builder (راجع القسم 4) |
| Database/FiscalPeriodRepository.cs | PrimeERP.Database | 201 | Data | مكرر |
| Database/FiscalYearSeeder.cs | PrimeERP.Database | 57 | Data | نهائي — لكن يستورد `PrimeERP.Services.Settings` (راجع القسم 2، استدعاء لأعلى) |
| Database/JournalRepository.cs | PrimeERP.Database | 412 | Data | مكرر — أكبر ملف Repository في المشروع |
| Database/NumberSequenceRepository.cs | PrimeERP.Database | 87 | Data | مكرر |
| Database/NumberSequenceSeeder.cs | PrimeERP.Database | 26 | Data | نهائي — يستورد `PrimeERP.Services.Settings` (القسم 2) |
| Database/PermissionDb.cs | PrimeERP.Database | 191 | Data | ناقص — مبني كاملاً، صفر مستهلك حي (لا شاشة إدارة صلاحيات، لا تسجيل دخول يستدعيها) |
| Database/SettingRepository.cs | PrimeERP.Database | 130 | Data | نهائي |
| Database/SettingSeeder.cs | PrimeERP.Database | 21 | Data | نهائي — يستورد `PrimeERP.Services.Settings` (القسم 2) |

## Models/ (12+1 كلاس بيانات)

كل ملفات Models/ نهائية بلا استثناء — POCO صرفة، لا منطق، مطابقة لقاعدة "لا منطق تشغيل خارج طبقة المنطق". أسطرها: Account.cs 20، Common/BaseModel.cs 20، Company.cs 18، Currency.cs 13، Customer.cs 27، Department.cs 12، Employee.cs 30، ExchangeRate.cs 12، FiscalPeriod.cs 18، FiscalYear.cs 19، JobTitle.cs 11، JournalEntry.cs 24، JournalLine.cs 14، Payroll.cs 23، PayrollLine.cs 15، Product.cs 42، ProductCategory.cs 12، PurchaseInvoice.cs 31، PurchaseInvoiceLine.cs 21، SalesInvoice.cs 32، SalesInvoiceLine.cs 21، Setting.cs 6، StockMovement.cs 23، Supplier.cs 30، TaxGroup.cs 12، Unit.cs 12، User.cs 17، Warehouse.cs 13. **12 من 22 Model (Company/Currency/Department/Employee/ExchangeRate/JobTitle/Payroll/PayrollLine/Product/ProductCategory/PurchaseInvoice*/SalesInvoice*/StockMovement/Supplier/TaxGroup/Unit/User/Warehouse) بلا أي Repository أو Service يستهلكها بعد** — بيانات نهائية الشكل، لكن معزولة (لا مسار حقيقي يقرأها/يكتبها).

## Resources/

| الملف | أسطر | الطبقة | الحالة |
|---|---|---|---|
| Resources/Design/Identity/Corporate/Primitives.Color.xaml | 84 | Design L1 | نهائي |
| Resources/Design/Identity/Default/Primitives.Color.xaml | 90 | Design L1 | نهائي |
| Resources/Design/Semantic/Semantic.Dark.xaml | 113 | Design L2 | نهائي |
| Resources/Design/Semantic/Semantic.Light.xaml | 116 | Design L2 | نهائي |
| Resources/Design/Theme.xaml | 33 | Design (جذر دمج) | نهائي |
| Resources/Export/ExportTheme.cs | 56 | Design (تصدير) | نهائي |
| Resources/Icons.xaml | 3 | — | ميت — قاموس فارغ تماماً، غير مُدمَج في Theme.xaml؛ الفعلي هو Resources/Icons/Icons.xaml (65 سطراً) |
| Resources/Icons/Icons.xaml | 65 | Design | نهائي |
| Resources/Print/PrintTheme.xaml | 59 | Design (طباعة) | نهائي |
| Resources/Strings.xaml | 3 | — | ميت — قاموس فارغ تماماً، غير مُدمَج؛ الفعلي هو Resources/Strings/Strings.ar.xaml/en.xaml |
| Resources/Strings/Strings.ar.xaml | 135 | Design (نصوص) | نهائي |
| Resources/Strings/Strings.en.xaml | 135 | Design (نصوص) | نهائي |
| Resources/Themes/Components/Buttons.xaml | 133 | Design L3/L4 (غير مفصولتين بعد) | نهائي لكن مرحلة انتقالية — راجع القسم 6 |
| Resources/Themes/Components/Dialogs.xaml | 84 | Design | نهائي/انتقالي |
| Resources/Themes/Components/Inputs.xaml | 265 | Design | نهائي/انتقالي |
| Resources/Themes/Implicit.xaml | 518 | Design | نهائي/انتقالي — أكبر ملف تصميم في المشروع |
| Resources/Themes/Metrics.xaml | 73 | Design L1 (لم تُنقل) | نهائي/انتقالي |
| Resources/Themes/ScrollBars.xaml | 115 | Design | نهائي |
| Resources/Themes/Typography.xaml | 39 | Design L1 (لم تُنقل) | نهائي/انتقالي |

## Services/ — جدول كامل في القسم 8 (لكل خدمة فعلية جدول تفصيلي). قائمة الملفات:

Accounting/AccountService.cs 660، Accounting/DTOs/AccountDto.cs 87، Accounting/DTOs/FiscalPeriodDto.cs 35، Accounting/DTOs/JournalDto.cs 112، Accounting/FiscalPeriodService.cs 401، Accounting/IAccountService.cs 50، Accounting/IFiscalPeriodService.cs 35، Accounting/IJournalService.cs 44، Accounting/JournalService.cs 624، Backup/BackupInfo.cs 38، Backup/BackupService.cs 295، Backup/IBackupService.cs 25، Design/IIdentityService.cs 46، Design/IdentityService.cs 136، DialogService.cs 87، ExportService.cs 281، IDialogService.cs 23، IExportService.cs 18، INavigationService.cs 15، INumberSequenceService.cs 16، IProgressHandle.cs 12، IToastService.cs 13، LocalizationService.cs 64، NavigationService.cs 38، NumberSequenceService.cs 57، Parties/CustomerService.cs 472، Parties/DTOs/CustomerDto.cs 99، Parties/ICustomerService.cs 55، Parties/ISupplierService.cs 21 (**ناقص — عقد بلا تنفيذ**)، Print/IPrintDialogHost.cs 17، Print/IPrintService.cs 16، Print/IPrintable.cs 69، Print/PrintService.cs 439، Print/PrintTemplates.cs 85، Print/Templates/AccountStatementPrintTemplate.cs 112، Print/Templates/TrialBalancePrintTemplate.cs 120، ServiceLocator.cs 37 (**مؤقت** — راجع القسم 5)، Settings/ISettingsService.cs 22، Settings/SettingKeys.cs 177، Settings/SettingsService.cs 148، ThemeService.cs 49، ToastHostWindow.xaml 14 + .xaml.cs 36، ToastService.cs 69.

## ViewModels/

| الملف | أسطر | الحالة |
|---|---|---|
| ViewModels/Base/PermissionAwareViewModel.cs | 38 | ناقص — أساس مبني، صفر ViewModel حقيقي يرثه بعد |
| ViewModels/BaseViewModel.cs | 61 | ناقص — نفس الملاحظة |
| ViewModels/CustomersViewModel.cs | 6 | مؤقت — كلاس فارغ تماماً (`class CustomersViewModel { }`) |
| ViewModels/HRViewModel.cs | 6 | مؤقت — نفس الشيء |
| ViewModels/MainViewModel.cs | 6 | مؤقت — نفس الشيء |
| ViewModels/ProductsViewModel.cs | 6 | مؤقت — نفس الشيء |
| ViewModels/PurchasesViewModel.cs | 6 | مؤقت — نفس الشيء |
| ViewModels/ReportsViewModel.cs | 6 | مؤقت — نفس الشيء |
| ViewModels/SalesViewModel.cs | 6 | مؤقت — نفس الشيء |
| ViewModels/SuppliersViewModel.cs | 6 | مؤقت — نفس الشيء |

**كل الـ8 ViewModels المتخصصة كلاسات فارغة حرفياً (6 أسطر: تعريف namespace + كلاس فارغ). لا AccountsViewModel ولا JournalViewModel ولا CustomerViewModel حقيقية تستهلك IAccountService/IJournalService/ICustomerService في المسار الحي — الاستهلاك الوحيد للخدمات الثلاث هو من الاختبارات.**

## Views/Controls/ (القطع) — القسم 7 يفصّل حالة الترحيل البصري؛ هنا الجرد الأولي فقط

Actions (7 قطع: ActionToolbar، AppButton، AppDropdownButton، AppIconButton، PermissionButton + ToolbarAction.cs مساعد) — Display (10 قطع: AppBadge، AppBreadcrumb، AppCard، AppDataGrid [أكبرها: 498 سطر .cs]، AppEmptyState، AppLoadingOverlay، AppPagination، AppStatCard، AppTabControl، AppTreeView + AppTabItem.cs/GridColumn.cs مساعدان) — Documents (14 ملف؛ DocumentLinesGrid.xaml.cs **1160 سطراً — أكبر ملف C# في كامل المشروع**) — Feedback (AppConfirmDialog، AppDialogWindow، AppMessageDialog، AppProgressDialog، AppToast) — Inputs (8 قطع) — Layout (FilterBar، PageHeader) — Pickers (13 ملف: PickerBase + PickerBaseControl + 5 تخصصات + PickerGridWindow/PickerTreeWindow/PickerResultItem/PickerWindowGeometry/IPickerDataSource) — Shell (AppShell، AppSidebar، AppTopBar + IconKeyToGeometryConverter/NavItem/NavItemViewModel) — Tree (TreeFilterEngine، TreeNodeViewModel). كل هذه القطع **نهائية بنائياً** (مُترجَمة، لا أخطاء) لكن معظمها **ناقص الاستهلاك الحي** — راجع القسم 7 للتفصيل الدقيق.

## Views/Converters/

VariantToBrushConverter.cs (71 سطراً) — نهائي، يُستهلَك فعلياً (AppDialogWindow وغيرها).

## Views/Dev/

| الملف | أسطر | الحالة |
|---|---|---|
| ControlsGalleryPage.xaml | 627 | مؤقت — موثّقة صراحة "لا يظهر في نسخة الإنتاج" (Str.Gallery.Subtitle) |
| ControlsGalleryPage.xaml.cs | 493 | مؤقت |
| MockPickerDataSources.cs | 313 | مؤقت — 5 كلاسات Mock لكل نوع Picker، الوحيدة المُستهلِكة لواجهة `IPickerDataSource<T>` في كامل المشروع الحي |

**هذه الصفحة هي فعلياً الشاشة الافتراضية الوحيدة التي يعرضها التطبيق عند التشغيل** (`MainWindow.xaml.cs` يضعها مباشرة في `pageContainer.Content`) — راجع القسم 5.

## Views/Dialogs/ و Views/Pages/ (14 ملف زوجي)

CustomerDialog، EmployeeDialog، ProductDialog، PurchaseInvoiceDialog، SalesInvoiceDialog، SupplierDialog (كل XAML 11 سطراً فارغاً + .cs 12 سطراً `InitializeComponent()` فقط) — نفس الشيء لـ CustomersPage، HRPage، ProductsPage، PurchasesPage، ReportsPage، SalesPage، SuppliersPage. **كل الـ14 ملفاً مؤقت/قشرة فارغة — Page/Window بلا محتوى XAML حقيقي وبلا ViewModel مربوط.**

## Views/Windows/

| الملف | أسطر | الحالة |
|---|---|---|
| LoginWindow.xaml | 5 | مؤقت — `<Grid/>` فارغ تماماً، Title="Login" حرفياً بالإنجليزية بلا ربط لغة |
| LoginWindow.xaml.cs | 12 | مؤقت — InitializeComponent فقط، بلا أي منطق مصادقة |
| MainWindow.xaml | 16 | مؤقت — تعليق داخلي صريح: "مؤقت أثناء مرحلة الترحيل... يُستبدل هذا كله بـ AppShell وحدها في المرحلة H" |
| MainWindow.xaml.cs | 17 | مؤقت — نفس التوثيق، يضع ControlsGalleryPage مباشرة |

## _Legacy/ (10 ملف)

**مستبعد بالكامل من الترجمة صراحة** — `PrimeERP.csproj` يحتوي `<DefaultItemExcludes>...;_Legacy/**;...</DefaultItemExcludes>`. كل ملف هنا: Resources/Themes.xaml (317)، Validators/AccountValidator.cs (30)، Validators/JournalValidator.cs (38)، ViewModels/AccountsViewModel.cs (160)، ViewModels/JournalViewModel.cs (141)، Views/Dialogs/AccountDialog.xaml (315)+.cs (168)، Views/Dialogs/JournalDialog.xaml (461)+.cs (243)، Views/Pages/AccountsPage.xaml (361)+.cs (52)، Views/Pages/JournalPage.xaml (387)+.cs (51). **الحالة: ميت** لكل الملفات (غير مُترجَمة، غير قابلة للتشغيل) — **لكنها تحمل مساحات أسماء (namespace) مطابقة حرفياً للكود الحي الحالي**: `PrimeERP.ViewModels`، `PrimeERP.Validators`، `PrimeERP.Views.Pages`، `PrimeERP.Views.Dialogs`، `PrimeERP.Database` — لا `PrimeERP._Legacy.*` مميزة. (هذا وصف حالة، لا اقتراح.)

## PrimeERP.Tests/ — تفصيل كامل في القسم 9

---

# 2 — من يستدعي من

جدول الاعتماديات الفعلي (بحث `using PrimeERP.*` داخل كل مجلد جذر، مُجمَّع):

| المجلد | يستدعي (using PrimeERP.*) |
|---|---|
| Core/ | Core.Database، Core.Database.Providers، **Database**، Models |
| Database/ | Core، Core.Database، Core.Security، Core.Validation، Models، **Services.Settings** |
| Models/ | Core، Models.Common |
| Services/ | Core، Core.Common، Core.Database، Core.Permissions، Core.Validation، Core.Validation.Validators، Database، Models، Resources.Export، Services.Accounting[.DTOs]، Services.Parties[.DTOs]، Services.Print، Services.Settings، **Views.Controls.Display**، **Views.Controls.Feedback** |
| ViewModels/ | Core.Permissions |
| Views/ | Core، Core.Common، Core.Permissions، Models، Services، ViewModels، Views.Controls.* (Actions/Display/Documents/Feedback/Inputs/Pickers/Shell/Tree)، Views.Converters |
| Resources/ | Core.Common |
| Converters/ | (لا شيء) |
| _Legacy/ | Core، Database، Models، Validators، ViewModels، Views.Dialogs (namespaces قديمة، خارج شجرة الترجمة) |

## اعتماد دائري

**Database ↔ Services.Settings**: `Services/Settings/SettingsService.cs` يستورد `PrimeERP.Database` (يستدعي `SettingRepository`)، بينما `Database/NumberSequenceSeeder.cs`، `Database/FiscalYearSeeder.cs`، `Database/SettingSeeder.cs` الثلاثة يستوردون `PrimeERP.Services.Settings` (يستهلكون `SettingKeys`). اتجاهان متعاكسان بين نفس المجلدين الاثنين — دائرية فعلية على مستوى المجلد/الـ namespace.

## استدعاء يتخطى طبقة (Core → Database)

- `Core/Validation/Validators/AccountValidator.cs` يستورد `PrimeERP.Database` (يستدعي `AccountRepository.GetByCode` للتحقق من التفرّد).
- `Core/Validation/Validators/UserValidator.cs` يستورد `PrimeERP.Database`.
- `Core/Permissions/PermissionService.cs` يستورد `PrimeERP.Database` (يستدعي `PermissionDb`).

الطبقة `Core` مصمَّمة (بحسب توثيق DESIGN_SYSTEM.md وبنية المشروع) كطبقة أساس بلا اعتماديات على طبقات أعلى — الثلاثة أعلاه اعتماديات من Core باتجاه Database.

## استدعاء لأعلى (Data → Services)

`Database/NumberSequenceSeeder.cs`، `Database/FiscalYearSeeder.cs`، `Database/SettingSeeder.cs` — الثلاثة يستوردون `PrimeERP.Services.Settings` من داخل مجلد `Database/` (طبقة Repository).

## استدعاء لأعلى (Services → Views)

`Services/ExportService.cs`، `Services/IExportService.cs`، `Services/DialogService.cs`، `Services/ToastService.cs`، `Services/ToastHostWindow.xaml.cs` — الخمسة يستوردون `PrimeERP.Views.Controls.Display` و/أو `PrimeERP.Views.Controls.Feedback` (يستخدمون أنواعاً من `GridColumn`، `AppConfirmDialog`، `AppMessageDialog`، `AppProgressDialog`، `AppToast` مباشرة).

## استدعاء يتخطى طبقة (ViewModel → Repository)

لم يُرصَد — لأن كل الـ ViewModels المتخصصة (CustomersViewModel وغيرها) كلاسات فارغة (راجع القسم 1)؛ لا كود فعلي فيها يستدعي أي شيء بعد.

---

# 3 — الأسس الموجودة والناقصة

| النطاق | يوجد أساس مشترك؟ | التفصيل |
|---|---|---|
| التصميم (رموز بصرية) | ✅ جزئياً | L1 (Identity/Primitives.Color) وL2 (Semantic) موجودان للألوان فقط منذ آخر جلسة. لا L1/L2 لـ Type/Space/Shape (لا تزال Typography.xaml/Metrics.xaml خارج السلسلة). لا L3 (Components/Tokens) ولا L4 (Styles) — راجع القسم 6. |
| القطع (Views/Controls) | ✅ نعم | نمط ثابت: قطعة UserControl "بنية فقط" + Style منفصل بـ `x:Key` في `Resources/Themes/Components/*.xaml` — مطبَّق باتساق عبر ~40 قطعة. |
| البيانات (Repository) | ❌ لا | لا `RepositoryBase`، لا `WhereBuilder`. 7 Repository (Account/Backup/Customer/FiscalPeriod/Journal/NumberSequence/Setting) — كل واحد يكرر CRUD ونمط (conn,tx) التوأم بشكل منفصل. عدد دوال (conn,tx) في Database/: **40** (راجع القسم 4). بناء WHERE ديناميكي مكرر حرفياً في ملفين (CustomerRepository.GetPaged، JournalRepository.GetPaged). |
| المنطق (Service — Pipeline/Steps) | ❌ لا | لا `Core/Pipeline`، لا خطوات (Permission/Validation/Transaction/Audit) قابلة لإعادة الاستخدام. كل خدمة تكتب هذا يدوياً — راجع القسم 4 للأرقام الدقيقة. |
| الخدمات (ServiceBase) | ❌ لا | لا `Services/Common/ServiceBase.cs`. 9 خدمة فعلية (Account/Backup/Customer/FiscalPeriod/Journal/NumberSequence/Print/Settings/Identity)، كل واحدة: Singleton `Instance` خاص بها (13 تكراراً)، `Denied`/`CurrentUser` مُعاد تعريفها حرفياً (4+3 مرات)، فحص صلاحية يدوي (55 موضعاً). |
| ViewModels | ❌ لا استهلاك | `BaseViewModel`/`PermissionAwareViewModel` موجودان (99 سطراً معاً) لكن **صفر ViewModel متخصص يرثهما فعلياً** — الثمانية الموجودة كلاسات فارغة. |
| التحقق (Validator) | ✅ جزئياً | `ValidatorBase`/`IValidator`/`ValidationResult` أساس عام موجود ومُستهلَك (Account/Customer/Journal Validators). 5 Validators أخرى (Customer... لا، أقصد Employee/Invoice/Product/Supplier/User) مبنية فوق نفس الأساس لكن **بلا أي Service يستهلكها بعد**. |
| التركيب (صفحة = تكوين) | ❌ لا | لا `Composition/Definitions`، لا `ModuleDefinition`/`PageDefinition`/`Renderers`. كل صفحة حالياً إما فارغة تماماً (Views/Pages/*) أو مبنية XAML يدوياً بالكامل (ControlsGalleryPage: 627+493 سطراً). |

---

# 4 — التكرار المرصود

جدول من بحث فعلي في `Services/` و`Database/` فقط (9 خدمات، 7 مستودعات):

| المنطق المكرر | موجود في | عدد التكرارات | الأسطر المهدرة (تقدير مبني على عدّ فعلي، لا تخمين) |
|---|---|---|---|
| فحص صلاحية `_permissions.Can(...)` في بداية دالة | AccountService(14)، JournalService(15)، FiscalPeriodService(6)، CustomerService(13)، BackupService(3)، SettingsService(4) | 55 موضعاً | ~55 سطر (سطر واحد نمطي لكل موضع) |
| خاصية `Denied =>` مُعاد تعريفها حرفياً | Account/FiscalPeriod/Journal/CustomerService | 4 ملفات | 4 أسطر تعريف + المفهوم مكرر بالكامل |
| خاصية `CurrentUser =>` مُعاد تعريفها حرفياً | FiscalPeriod/Journal/CustomerService | 3 ملفات | 3 أسطر |
| `Result.Fail(LocalizationService.Get(...))` | 8 ملفات خدمة (Account 4، FiscalPeriod 10، Journal 26، Backup 14، Identity 2، Dialog 2، Customer 19، Settings 4) | 81 موضعاً | ~81 سطر |
| `Db.RunTransaction((conn, tx) => {...})` | Account(5)، FiscalPeriod(6)، Journal(6)، NumberSequence(1)، Customer(5)، Settings(1) | 24 موضعاً | ~48 سطر (فتح+إغلاق لكل موضع) |
| `Auditor.Log(...)` بعد كتابة | Account(5)، FiscalPeriod(6)، Journal(6)، Backup(3)، Customer(3)، Settings(2) | 25 موضعاً | ~25 سطر |
| `new PagedResult<T>{Items,TotalCount,Page,PageSize}` | AccountService، JournalService، CustomerService | 3 مواضع | ~15 سطر (5 أسطر/بناء تقريباً) |
| تحويل `ValidationResult` → `Result` (`string.Join(Errors)`/`Errors.Values`) | AccountService(2)، JournalService(1)، CustomerService(4) | 8 مواضع | ~16 سطر |
| قراءة إعداد بقيمة افتراضية `_settings.Get(key, default)` | Account(3)، FiscalPeriod(3)، Journal(3)، Backup(4)، Identity(2)، Customer(4)، PrintService(4) | 23 موضعاً | ~23 سطر |
| توليد كود عبر `INumberSequenceService` | JournalService، CustomerService | 2 موضعاً (نمطان مختلفان: `Next(key)` و`Next(conn,tx,key)`) | غير كبير عددياً، لكن كل موضع منطق مطابق |
| حساب `CanEdit`/`CanDelete`/`StatusVariant` داخل `ToDto` خاصة | AccountService.ToDto، JournalService.ToDto/ToDetailDto/ToLineDto، CustomerService.ToDto | 4 دوال تحويل مستقلة | غير مُقاس سطرياً (منطق لا نص متطابق حرفياً) |
| توأم الدوال العادية/`(conn,tx)` في الخدمات | Account(6)، IAccountService(4)، IJournalService(4)، Journal(6)، NumberSequenceService(1)+Interface(1)، Customer(4)، ICustomerService(4)، ISupplierService(3) | 33 توقيعاً | **الأكبر حجماً — كل زوج جسم دالة كامل (5–30 سطراً)، غير مُجمَّع رقمياً هنا لتفادي التقدير بلا قياس فردي** |
| توأم الدوال العادية/`(conn,tx)` في المستودعات | Account(9)، Backup(1)، Customer(6)، FiscalPeriod(9)، Journal(11)، NumberSequence(3)، Setting(1) | 40 توقيعاً | نفس الملاحظة أعلاه |
| بناء `WHERE` ديناميكي يدوياً لكل صفحة (List\<string\>+List\<(string,object)\>) | JournalRepository.GetPaged، CustomerRepository.GetPaged | 2 موضعاً | كل GetPaged بين 40–60 سطراً، جزء كبير منه بناء WHERE/ORDER/LIMIT متطابق البنية |
| Singleton `public static readonly X Instance = new()` | 13 خدمة (Account/Backup/Customer/Design.Identity/Dialog/Export/Navigation/NumberSequence/Print/Settings/Toast/FiscalPeriod/Journal) | 13 موضعاً | 13 سطر |

---

# 5 — الأجزاء المؤقتة

| الجزء | لماذا بُني مؤقتاً | الحالة الحالية |
|---|---|---|
| `Services/ServiceLocator.cs` (37 سطراً) | جسر لحل الاعتماد الدائري بين AccountService↔CustomerService/SupplierService (SkipAutoLink) بلا حقن اعتماديات كامل | لا يزال الآلية الوحيدة لربط الخدمات المتقاطعة؛ 13 تسجيلاً في App.xaml.cs |
| `ISupplierService.cs` (21 سطراً) | عقد جزئي — وُسِّع توقيعه في F.3.1 ليطابق ICustomerService تحسباً لـ F.3.2 | **لا `SupplierService` تنفيذ فعلي بعد** — الواجهة وحدها موجودة |
| `_Legacy/` (10 ملف، مستبعد من الترجمة) | نُقلت صفحات الحسابات/القيود القديمة (نمط DataTable مباشر) هنا عند إعادة البناء بدل حذفها | ميتة فعلياً (لا تُترجَم)، محفوظة كمرجع تاريخي فقط |
| `Views/Dev/ControlsGalleryPage.xaml(.cs)` + `MockPickerDataSources.cs` (1433 سطراً مجتمعة) | معرض مرئي لكل القطع أثناء البناء، موثّق صراحة "لا يظهر في نسخة الإنتاج" | **هي الشاشة الوحيدة التي يعرضها التطبيق فعلياً عند التشغيل** (MainWindow يحمّلها مباشرة) — لا صفحة إنتاج حقيقية تعمل بعد |
| `Views/Windows/MainWindow.xaml(.cs)` | تعليق داخلي صريح: "مؤقت أثناء مرحلة الترحيل ... يُستبدل بـ AppShell وحدها في المرحلة H" | كما هي؛ AppShell (Views/Controls/Shell) مبنية لكن غير مُستخدَمة من MainWindow |
| `Views/Windows/LoginWindow.xaml(.cs)` | Grid فارغ — لم تُبنَ شاشة الدخول بعد | بلا أي منطق مصادقة؛ `AppSession.SignIn` غير مُستدعاة من أي مكان حي |
| `Views/Pages/*.xaml(.cs)` (7 صفحات) و`Views/Dialogs/*.xaml(.cs)` (6 حوارات) | قوالب مُنشأة مسبقاً (Scaffolding) بانتظار بناء كل وحدة | كل واحدة 11–12 سطراً: `InitializeComponent()` فقط، بلا محتوى ولا ViewModel |
| `ViewModels/*ViewModel.cs` (8 ملفات، 6 أسطر لكل واحد) | نفس السبب — أسماء كلاسات محجوزة بانتظار البناء | كلاسات فارغة حرفياً |
| `Core/Permissions/PermissionService.cs` + `Database/PermissionDb.cs` (244 سطراً معاً) | نظام صلاحيات كامل بُني مبكراً (Roles/Permissions/Grants/Revokes) تحسباً لمرحلة لاحقة | **غير مُفعَّل عملياً** — `AppSession.DevMode=true` في DEBUG يتجاوز كل فحص صلاحية؛ `LoadForUser` غير مستدعاة أبداً في مسار حي |
| `Resources/Icons.xaml`، `Resources/Strings.xaml` (3 أسطر لكل واحد، جذر Resources/) | نسخ أولى قبل نقل المحتوى الفعلي لمجلدات فرعية (`Icons/`، `Strings/`) | متروكة فارغة، غير مُدمَجة في `Theme.xaml` — لم تُحذف |
| `Core/Transactions/UnitOfWork.cs` + `IUnitOfWork.cs` (66 سطراً) | غلاف بديل لـ `Db.RunTransaction` للعمليات متعددة الاستدعاءات | صفر مستهلك — كل الخدمات التسع تستخدم `Db.RunTransaction` حصراً |

---

# 6 — طبقة التصميم

| الطبقة | موجودة؟ | التفصيل |
|---|---|---|
| L1 Identity | ✅ جزئياً | `Resources/Design/Identity/{Default,Corporate}/Primitives.Color.xaml` فقط — **ألوان حصراً**. لا `Primitives.Type/Space/Shape/Motion.xaml` تحت `Identity/*` — هذه الأبعاد الأربعة لا تزال في `Resources/Themes/Typography.xaml`+`Metrics.xaml` خارج بنية Identity كلياً. |
| L2 Semantic | ✅ للألوان فقط | `Resources/Design/Semantic/Semantic.Light.xaml`+`Semantic.Dark.xaml` — كل قيمة `Color="{DynamicResource P.Color.*}"` (سلسلة مراجع حقيقية، لا قيم حرفية). لا `Semantic.Type/Space/Shape.xaml` — القطع لا تزال تستهلك `Typography.xaml`/`Metrics.xaml` مباشرة (لا طبقة Semantic بينهما). |
| L3 Components | ❌ غير موجودة | لا `Resources/Design/Components/Tokens.*.xaml`. |
| L4 Styles | ❌ غير موجودة | لا `Resources/Design/Styles/Style.*.xaml`. أنماط القطع (Style الفعلية) لا تزال في مكانها الأصلي `Resources/Themes/Components/{Buttons,Inputs,Dialogs}.xaml` (482 سطراً مجتمعة) — تشير لمفاتيح L2/Metrics مباشرة، لا L3. |
| القطع تشير لأي طبقة فعلياً | — | للألوان: L2 (`Semantic.*`) عبر `DynamicResource`، بأسماء المفاتيح القديمة نفسها (`BrandDefault`، `TextPrimary`...) لا التسمية الجديدة (`P.Color.*`/مكافئ Semantic بادئة `S.`). للأبعاد (حجم/مسافة/خط): `Resources/Themes/Typography.xaml`+`Metrics.xaml` مباشرة — لا طبقة وسيطة إطلاقاً. |
| قطع تستهلك StaticResource بدل DynamicResource | — | **166 موضعاً** `StaticResource` مقابل **50 موضعاً** `DynamicResource` لمفاتيح Height/Radius/FontSize/FontFamily/FontWeight/Space/Icon، عبر **26 ملفاً** (`Resources/Themes/Components/*.xaml`، `Implicit.xaml`، وملفات `Views/Controls/*.xaml`). يعني: تبديل حزمة هوية أو حجم لا يُحدِّث هذه الـ166 موضعاً حيّاً بلا إعادة تحميل الصفحة. الألوان (`Brand`/`Surface`/`Text`/... إلخ) على النقيض: **DynamicResource حصراً** في كل ملف فُحص (صفر استخدام `StaticResource` لمفتاح لون واحد عبر Views/Controls). |
| قيم حرفية خارج نظام التصميم | — | فُحص كامل المشروع بحثاً عن `#RRGGBB` خارج `Resources/Design/`: **صفر نتيجة** في القطع أو الخدمات الحية. الوحيدة: `Resources/Export/ExportTheme.cs` (ثوابت C# مقصودة، موثّقة) و`Resources/Print/PrintTheme.xaml` (مستقلة عمداً عن الشاشة، موثّقة) و`_Legacy/` (خارج الترجمة). |

---

# 7 — القطع

**المبنية** (بمعنى: UserControl/Control كامل، مُترجَم، له Style مطابق) — ~40 قطعة عبر Actions(4)/Display(10)/Documents(عدة أجزاء تركيبية)/Feedback(5)/Inputs(8)/Layout(2)/Pickers(7 كلاسات تخصيص+3 نوافذ مساعدة)/Shell(3).

**المُرحّلة بصرياً** (تستهلك سلسلة L1→L2 الجديدة للون فعلياً عبر `DynamicResource` بأسماء Semantic): **كل القطع** — لأن Semantic.Light/Dark.xaml أبقت على نفس أسماء المفاتيح القديمة (BrandDefault إلخ)، فكل قطعة كانت تستهلك `DynamicResource BrandDefault` قبل بند التصحيح المعماري لا تزال تستهلك نفس المفتاح — الفرق أن هذا المفتاح الآن يُشتقّ من P.Color.* بدل قيمة حرفية. **من زاوية "تغيّر الملف نفسه": صفر قطعة عُدِّلت** — الترحيل حدث كله في طبقة الموارد لا في القطع.

**غير المُرحّلة** (بمعنى: لا تزال تعتمد على أبعاد خارج أي سلسلة مرجعية — Type/Space/Shape): **كل القطع تقريباً** بالنسبة للحجم/الخط/نصف القطر — لأن L1/L2 لهذه الأبعاد غير موجودتين أصلاً (القسم 6).

**قطعة تحتوي قيمة بصرية ما زالت في ملفها الخاص (لا في نظام الموارد)**:
- `Views/Controls/Pickers/CustomerPicker.cs` — منطق أعمال (`c.CreditLimit > 0 && c.Balance > c.CreditLimit ? "danger" : null`) محسوب مباشرة من `Models.Customer` (الـ Model الخام، لا `CustomerDto.IsOverCreditLimit` المبني في F.3.1) داخل القطعة نفسها. تعليق داخل الملف نفسه يوثّق أنه حل مؤقت "حتى يُبنى ICustomerService" — `ICustomerService` مبني فعلياً الآن، لكن الملف لم يُحدَّث.
- لا قيم لون حرفية (Hex) مباشرة داخل أي ملف قطعة (مؤكَّد بالبحث، القسم 6).

**بيانات ربط حقيقية**: كل الـ`Picker`ات (`AccountPicker`/`CustomerPicker`/`EmployeePicker`/`ProductPicker`/`SupplierPicker`) تعتمد على `IPickerDataSource<T>` — **المُنفِّذ الوحيد الموجود في كامل المشروع هو `Views/Dev/MockPickerDataSources.cs` (بيانات وهمية، Dev فقط)**. لا `AccountPickerDataSource`/`CustomerPickerDataSource` حقيقي يقرأ من `IAccountService`/`ICustomerService`.

---

# 8 — الخدمات

| الاسم | الأسطر | تستخدم أساساً؟ | المنطق المكرر فيها | المنطق الفريد (يجب الحفاظ عليه) |
|---|---|---|---|---|
| `AccountService` | 660 | لا | Denied/فحص صلاحية (14)/RunTransaction(5)/Audit(5)/PagedResult(1)/ValidationResult→Result(2)/settings.Get(3) | توليد كود الابن (`GenerateChildCodeInternal`/`BuildChildCode`)، حل الربط التلقائي حساب↔عميل/مورد (`ResolveAutoLink`+`SkipAutoLink`، يمنع الحلقة اللانهائية)، حساب الرصيد من قيود مرحّلة (`ComputeBalance`)، بناء شجرة الحسابات (`BuildTree`) |
| `JournalService` | 624 | لا | Denied/CurrentUser/فحص صلاحية (15)/RunTransaction(6)/Audit(6)/PagedResult(1)/ValidationResult→Result(1)/settings.Get(3) | ميزان المراجعة (`GetTrialBalance`، صيغة موحّدة بلا تفرّع مدين/دائن)، ترحيل/إلغاء ترحيل بقيود منع (`ClosingEntrySource`)، `PostBatch` كل-أو-لا-شيء |
| `FiscalPeriodService` | 401 | لا | Denied/CurrentUser/فحص صلاحية (6)/RunTransaction(6)/Audit(6)/settings.Get(3) | إقفال/إعادة فتح فترة وسنة، قيد الإقفال السنوي التلقائي (يستدعي JournalService عبر `Lazy<IJournalService>`) |
| `CustomerService` | 472 | لا | Denied/CurrentUser/فحص صلاحية (13)/RunTransaction(5)/Audit(3)/PagedResult(1)/ValidationResult→Result(4)/settings.Get(4) | المزامنة ثنائية الاتجاه حساب↔عميل (`CreateFromAccount`/`UpdateNameFromAccount`/`DeleteByAccountCode`، أحادية الاتجاه تفادياً للـ ping-pong)، `CheckCreditLimit` (`decimal.MaxValue` كعلامة "بلا حد") |
| `BackupService` | 295 | لا | فحص صلاحية (3)/Audit(3)/settings.Get(4) | التحقق من سلامة الملف حسب نوع القاعدة (`ValidateSqlite`/`ValidateSqlServer`)، `ApplyRetention`، نسخة أمان تلقائية قبل الاستعادة |
| `SettingsService` | 148 | لا | فحص صلاحية (4)/RunTransaction(1)/Audit(2) | Cache في الذاكرة (`_cache`/`Reload`)، تحويل نوع عام (`FormatValue<T>`/`Get<T>`) |
| `NumberSequenceService` | 57 | لا | RunTransaction(1) | تصفير سنوي شرطي (`ResetYearly`/`LastYear`)، صيغة التوليد (`Prefix-Year-Number`) |
| `PrintService` | 439 | لا | settings.Get(4) | بناء `FixedDocument` من `IPrintable` عام (لا يعرف أي Model)، Cache قاموس الطباعة مع `.Freeze()` لكل Freezable |
| `IdentityService` | 136 | لا (هو نفسه بداية أساس محتمل لخدمات تصميم مستقبلية، لكن لا يرث من شيء ولا يُورَّث منه بعد) | فحص لا (بلا صلاحية أصلاً)/settings.Get(2) | آلية استبدال `Application.Resources` المُثبتة تجريبياً (ترتيب التعيين ثم التعديل) |

**`ISupplierService`**: عقد فقط (21 سطراً)، **لا تنفيذ `SupplierService` موجود إطلاقاً** — غير مُدرَج في الجدول أعلاه لعدم وجوده كملف تنفيذ.

---

# 9 — الاختبارات

**131 دالة اختبار معلنة** (`[Fact]`+`[Theory]`) في 10 ملفات، تُنفَّذ فعلياً كـ**134 حالة اختبار** (فرق 3 من توسيع `[Theory]`/`[InlineData]`). كلها ناجحة حالياً (`dotnet test` آخر تشغيل: 134/134).

| الملف | عدد الدوال المعلنة | يغطي |
|---|---|---|
| JournalServiceTests.cs | 32 | `JournalService` (Create/Update/Delete/Post/Unpost/PostBatch/TrialBalance/صلاحيات) |
| FiscalPeriodServiceTests.cs | 23 | `FiscalPeriodService` (سنوات/فترات/إقفال/إعادة فتح) |
| CustomerServiceTests.cs | 20 | `CustomerService` (8 اختبارات ربط ثنائي الاتجاه + أساسيات/ائتمان/أرصدة/صلاحيات) |
| LineEngineTests.cs | 22 | `LineComputeEngine`/`LineValidationEngine` (Views/Controls/Documents) — الوحيد الذي يغطي قطعة واجهة لا خدمة |
| AccountServiceTests.cs | 10 | `AccountService` |
| SettingsServiceTests.cs | 9 | `SettingsService` |
| BackupServiceTests.cs | 6 | `BackupService` |
| PrintTemplatesTests.cs | 5 | قوالب الطباعة (كشف حساب/ميزان مراجعة) |
| IdentityServiceTests.cs | 3 | `IdentityService` (سواب حيّ + تحقق) |
| PrintServiceTests.cs | 1 | `PrintService` |

## بلا أي تغطية اختبارية

`NumberSequenceService`، `ThemeService`، `LocalizationService`، `DialogService`، `ToastService`، `NavigationService`، `ExportService`، `PermissionService`/`PermissionDb`، `SchemaBuilder`، `MigrationRunner`، `DbHelper` (غير مباشر فقط عبر بقية الاختبارات)، كل الـ Validators السبعة تحت `Core/Validation/Validators` (لا اختبار وحدة مباشر لأي منها — تُختبَر ضمنياً فقط عبر اختبارات الخدمة التي تستهلكها)، `UnitOfWork`، أي Model، أي Picker، أي قطعة واجهة عدا `LineComputeEngine`/`LineValidationEngine`.

## اختبار يعتمد على تفصيلة تنفيذ لا سلوك

- `CustomerServiceTests` — اختبار الـ rollback (`Create_AccountCreationFailure_RollsBackEverything`) يستخدم `NumberSequenceService.Instance.Peek("Customer")` لهندسة تصادم كود مقصود — يعتمد على معرفة داخلية بآلية توليد الكود (تفصيلة تنفيذ) لا على سلوك عام قابل للوصف من الخارج.
- عدة اختبارات في `AccountServiceTests`/`CustomerServiceTests` تستخدم `FakeCustomerService`/`FakeSupplierService` بديلتين مُسجَّلتين عبر `ServiceLocator` — الاختبار يتحقق من `LastCreatedFor`/استدعاءات الـ Fake (تفصيلة تنفيذ التكامل)، لا فقط ناتج نهائي ملاحَظ من الخارج.

---

# 10 — ما لم يُبنَ بعد

بحسب الخطة (المراحل F.3.2 فصاعداً) وما يظهر فعلياً في الشجرة:

- **F.3.2 — `SupplierService`**: العقد (`ISupplierService`) موجود، لا تنفيذ.
- **F.3.3/F.3.4 — `IProductService`/`IStockService`**: لا عقد ولا تنفيذ. `Models/Product.cs`+`ProductCategory.cs`+`Warehouse.cs`+`StockMovement.cs`+`Unit.cs` موجودة بلا Repository/Service.
- **المبيعات/المشتريات**: `Models/SalesInvoice*`+`PurchaseInvoice*` موجودة بلا Repository/Service. `Views/Controls/Documents/DocumentLinesGrid` (1160 سطراً) مبنية جاهزة لكن غير مربوطة بأي صفحة حية. `Views/Pages/SalesPage`/`PurchasesPage` فارغتان.
- **الموارد البشرية**: `Models/Employee`+`Department`+`JobTitle`+`Payroll`+`PayrollLine` موجودة بلا Repository/Service. `Core/Security/PasswordHasher` مبني بلا مستهلك. `Views/Pages/HRPage` فارغة.
- **التقارير**: `Views/Pages/ReportsPage` فارغة، `ViewModels/ReportsViewModel` فارغ.
- **المستخدمون كبيانات**: `Models/User.cs` موجود، `Core/Validation/Validators/UserValidator.cs` مبني، لا `UserRepository`/`UserService`.
- **نظام الصلاحيات الكامل**: `PermissionDb`+`PermissionService` مبنيان بنيوياً، **غير مفعَّلين** (لا شاشة إدارة صلاحيات، لا شاشة دخول تستدعي `LoadForUser`، `DevMode=true` في DEBUG يتجاوز كل شيء).
- **شاشة الدخول (Login)**: `LoginWindow.xaml` فارغة تماماً (`<Grid/>`)، بلا أي كود مصادقة.
- **`AppShell` كنقطة دخول حقيقية**: القطعة مبنية (`Views/Controls/Shell/AppShell.xaml`) لكن `MainWindow` لا يستخدمها — يعرض `ControlsGalleryPage` مباشرة.
- **`Composition/` (طبقة التركيب)**: لا `ModuleDefinition`/`PageDefinition`/`Renderers`/`ModuleRegistry` — لم يُبدأ بناؤها.
- **`Core/Pipeline/` (Steps/Pipeline/Operations)**: لم يُبدأ بناؤها (كانت محل "توقف 2" المُعلَّق في التوجيه المعماري السابق).
- **`RepositoryBase`/`WhereBuilder`/`QuerySpec`**: لم تُبنَ.
- **`ServiceBase`/`CrudServiceBase`**: لم تُبنَ.
- **`PagedViewModelBase`/`CrudViewModelBase`**: لم تُبنَ؛ لا ViewModel واحد يستهلكهما لأنهما غير موجودتين أصلاً.
- **`Resources/Design/Identity/*/Primitives.Type|Space|Shape|Motion.xaml`**: لم تُبنَ (راجع القسم 6).
- **`Resources/Design/Components/Tokens.*.xaml`** و**`Resources/Design/Styles/Style.*.xaml`** (L3/L4): لم تُبنيا.
- **مُصدِّر بيانات Picker حقيقي** (غير Mock) لأي نوع.
- **`ProductPicker`/`EmployeePicker`/`SupplierPicker` مصادر بيانات حقيقية**: غير موجودة (Mock فقط).
