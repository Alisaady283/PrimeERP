# FILES.md — فهرس الملفات

مولَّد بـ`bash Tools/Docs/generate.sh` — لا يُعدَّل يدوياً. الوظيفة سطرٌ واحد، وما لا يُقال في سطر مكانه `ARCHITECTURE.md`.

## 1. 1.Platform

| # | الملف | الوظيفة |
|---|---|---|
| 1.01 | AppInfo.cs | هوية النسخة المُشغَّلة |
| | **Audit/** | سجلّ التدقيق: من فعل ماذا ومتى |
| 1.02 | AuditLogger.cs | سجلّ التدقيق بقيمة قبل وبعد |
| 1.03 | IAuditLogger.cs | عقد سجلّ التدقيق |
| 1.04 | IAuditStore.cs | مخزن جدول AuditLog |
| | **Design/** | عقد تحميل موارد التصميم |
| 1.05 | IIdentityService.cs | عقد تحميل موارد التصميم |
| | **Localization/** | قاموس النصوص وتبديل اللغة |
| 1.06 | ILocalizationService.cs | عقد النصوص للحقن |
| 1.07 | LocalizationAdapter.cs | جسر الحقن لقاموس النصوص |
| 1.08 | LocalizationService.cs | قاموس النصوص واتجاه الواجهة |
| | **Net/** | الشبكة في موضع واحد |
| 1.09 | HttpGateway.cs | الموضع الوحيد الذي يلمس الشبكة |
| 1.10 | MachineFingerprint.cs | بصمة الجهاز |
| | **Permissions/** | مفاتيح الصلاحيات وجلسة المستخدم |
| 1.11 | AppSession.cs | حالة المستخدم الحالي |
| 1.12 | IPermissionService.cs | فحص الصلاحية وتحميلها |
| 1.13 | IPermissionStore.cs | مخزن جداول الصلاحيات والأدوار والمستخدمين |
| 1.14 | PermissionKeys.cs | كل مفتاح صلاحية في النظام |
| 1.15 | PermissionRules.cs | لا فعل بلا عرض |
| 1.16 | PermissionService.cs | فحص الصلاحيات وإدارة منحها |
| 1.17 | PermissionState.cs | حالة مفتاح صلاحية |
| | **Security/** | تجزئة كلمات المرور |
| 1.18 | PasswordHasher.cs | تجزئة كلمات المرور بـPBKDF2 |
| | **Settings/** | قراءة وكتابة إعدادات النظام |
| 1.19 | ISettingStore.cs | مخزن جدول AppSettings |
| 1.20 | ISettingsProvider.cs | عقد قراءة وكتابة الإعدادات |
| 1.21 | SettingKeys.cs | كل مفتاح إعداد في النظام |
| 1.22 | SettingsProvider.cs | قراءة الإعدادات وكتابتها بذاكرة مؤقتة |

## 2. 2.Data

| # | الملف | الوظيفة |
|---|---|---|
| | **Core/** | الاتصال والتنفيذ وبناء الجداول والترحيل |
| 2.01 | BuiltTables.cs | جداول يبنيها المستخدم |
| 2.02 | DbConfig.cs | قاعدة البيانات DbConfig |
| 2.03 | DbContextFactory.cs | سياقٌ فوق الاتصال والمعاملة القائمين |
| 2.04 | ModelConventions.cs | اتفاقيات النموذج العامّة |
| 2.05 | PrimeDbContext.cs | نموذج EF مولَّد |
| 2.06 | SchemaSync.cs | إلحاق ناقص النموذج بالقاعدة |
| | **Repositories/** | مستودع لكل جدول: SQL فقط |
| 2.07 | AccountRepository.cs | طبقة وصول بيانات شجرة الحسابات |
| 2.08 | AssetDepreciationRepository.cs | مستودع AssetDepreciation |
| 2.09 | AssetDisposalRepository.cs | مستودع AssetDisposal |
| 2.10 | AssetRepository.cs | مستودع Asset |
| 2.11 | AssetRevaluationRepository.cs | مستودع AssetRevaluation |
| 2.12 | AttendanceRepository.cs | مستودع Attendance |
| 2.13 | AuditRepository.cs | مستودع AuditLog |
| 2.14 | BackupRepository.cs | طبقة وصول بيانات سجل النسخ |
| | **Repositories/Base/** | أسس المستودعات: الصفحة والترتيب والحذف والمستندات |
| 2.15 | CycleDocumentRepositoryBase.cs | أساس مستودع CycleDocument |
| 2.16 | InvoiceRepositoryBase.cs | أساس فاتورة: رأس وسطور |
| 2.17 | LookupRepositoryBase.cs | ما يتقاسمه مستودعات القوائم |
| 2.18 | PartyRepositoryBase.cs | ما يتقاسمه مستودعا العملاء والموردين |
| 2.19 | RepositoryBase.cs | أساس حقيقي بالتوريث لكل Repository |
| 2.20 | ReturnRepositoryBase.cs | أساس مرتجع: رأس وسطور |
| 2.21 | StockAdjustmentRepositoryBase.cs | أساس مستودع StockAdjustment |
| | **Repositories/** | مستودع لكل جدول: SQL فقط |
| 2.22 | BuilderRepository.cs | مستودع ما بناه المستخدم |
| 2.23 | CategoryRepository.cs | مستودع Category |
| 2.24 | ChequeRepository.cs | مستودع Cheque |
| 2.25 | CustomerRepository.cs | طبقة وصول بيانات العملاء |
| 2.26 | CycleDocumentRepositories.cs | مستودع CycleDocument |
| 2.27 | DocumentLinkRepository.cs | مستودع DocumentLink |
| 2.28 | DynamicRepository.cs | مستودع أي جدول بناه المستخدم |
| 2.29 | EditionRepository.cs | اتصالٌ بقاعدة نسخةٍ أخرى |
| 2.30 | EmployeeMovementRepository.cs | البدل والخصم جدولان بشكلٍ واحد |
| 2.31 | EmployeeRepository.cs | مستودع Employee |
| 2.32 | FiscalPeriodRepository.cs | طبقة وصول بيانات السنوات/الفترات المالية |
| 2.33 | IPartyRepository.cs | الشكل المشترك بين ICustomerRepository/ISupplierReposito |
| 2.34 | JournalRepository.cs | طبقة وصول بيانات قيود اليومية |
| 2.35 | LicenseRepository.cs | مستودع License |
| 2.36 | NumberSequenceRepository.cs | طبقة وصول بيانات تسلسل الأرقام |
| 2.37 | PayrollRepository.cs | مستودع Payroll |
| 2.38 | PermissionRepository.cs | مستودع الصلاحيات والأدوار والمستخدمين |
| 2.39 | ProductRepository.cs | مستودع Product |
| 2.40 | PurchaseInvoiceRepository.cs | مستودع PurchaseInvoice |
| 2.41 | PurchaseReturnRepository.cs | مستودع PurchaseReturn |
| 2.42 | SalesInvoiceRepository.cs | مستودع SalesInvoice |
| 2.43 | SalesReturnRepository.cs | مستودع SalesReturn |
| 2.44 | SettingRepository.cs | مستودع AppSettings |
| 2.45 | StockAdjustmentRepositories.cs | مستودع StockIn |
| 2.46 | StockMovementRepository.cs | مستودع StockMovement |
| 2.47 | StockTransferRepository.cs | مستودع StockTransfer |
| 2.48 | SupplierRepository.cs | طبقة وصول بيانات الموردين |
| 2.49 | TreasuryRepository.cs | مستودع Treasury |
| 2.50 | VoucherRepository.cs | مستودع Voucher |
| | **Seeders/** | بذر عدّادات الترقيم |
| 2.51 | NumberSequenceSeeder.cs | يزرع تسلسلات الأرقام ذات بادئة |
| 2.52 | PermissionSeeder.cs | زرع المفاتيح ودور مدير النظام |
| 2.53 | SettingSeeder.cs | زرع المفاتيح الافتراضية بلا استبدال |

## 3. 3.Domain

| # | الملف | الوظيفة |
|---|---|---|
| | **Calculations/** | كل صيغة حساب نقية: السطر والراتب والأصل والتكلفة والقوائم والفترات |
| 3.01 | AssetCalc.cs | قواعد الإهلاك النقيّة |
| 3.02 | FiscalPeriodCalc.cs | حسابات تواريخ السنة/الفترات المالية |
| 3.03 | InventoryCosting.cs | تسعير المخزون بالمتوسط المرجَّح المتحرّك |
| 3.04 | LayoutCalc.cs | مقاييس العرض والطباعة |
| 3.05 | LineCalc.cs | مصدر واحد لحساب مبالغ المستندات |
| 3.06 | PartyCalc.cs | قواعد الطرف النقيّة |
| 3.07 | PayrollCalc.cs | حساب الراتب |
| 3.08 | StatementCalc.cs | مقاييس القوائم المالية |
| | **Contracts/** | العقود ومفرداتها: طباعة وتصدير وسحب |
| 3.09 | IDocumentExporter.cs | عقد تصدير مستند قابل للطباعة |
| 3.10 | IPrintable.cs | أي مستند قابل للطباعة ينفّذ |
| 3.11 | IPullSourceReader.cs | يقرأ كمية سطر مستند مصدر |
| 3.12 | PaperNode.cs | قطعة ورق كبنية مجرّدة |
| | **Entities/** | كيانات صرفة: خصائص فقط بلا سلوك |
| 3.13 | Account.cs | كيان Account |
| 3.14 | Asset.cs | كيان Asset |
| 3.15 | AssetDepreciation.cs | قسط إهلاكٍ واحد |
| 3.16 | AssetDisposal.cs | بيع أصل واستبعاده |
| 3.17 | AssetRevaluation.cs | إعادة تقييم أصل |
| 3.18 | BackupHistoryRecord.cs | سجل نسخة احتياطية |
| 3.19 | Builder.cs | ما يتقاسمه أبناء الوحدة |
| 3.20 | Category.cs | كيان Category |
| 3.21 | Cheque.cs | شيك وارد أو صادر |
| | **Entities/Common/** | أسس الكيانات المشتركة |
| 3.22 | BaseModel.cs | القاعدة المشتركة لكل الكيانات الرئيسية |
| 3.23 | InvoiceBase.cs | ما تتقاسمه فاتورتا البيع والشراء |
| 3.24 | PartyBase.cs | ما يتقاسمه كل طرف |
| 3.25 | ReturnBase.cs | ما يتقاسمه مرتجعا البيع والشراء |
| | **Entities/** | كيانات صرفة: خصائص فقط بلا سلوك |
| 3.26 | Company.cs | كيان Company |
| 3.27 | Currency.cs | كيان Currency |
| 3.28 | Customer.cs | كيان Customer |
| 3.29 | CycleDocument.cs | كيان CycleDocument |
| 3.30 | Department.cs | كيان Department |
| 3.31 | DocumentLink.cs | ربط سحب واحد |
| 3.32 | Employee.cs | كيان Employee |
| 3.33 | EmployeeMovement.cs | البدل والخصم سواء |
| 3.34 | ExchangeRate.cs | كيان ExchangeRate |
| 3.35 | FiscalPeriod.cs | كيان FiscalPeriod |
| 3.36 | FiscalYear.cs | كيان FiscalYear |
| 3.37 | JobTitle.cs | كيان JobTitle |
| 3.38 | JournalEntry.cs | كيان JournalEntry |
| 3.39 | JournalLine.cs | كيان JournalLine |
| 3.40 | License.cs | ترخيص عميل |
| 3.41 | NumberSequence.cs | عدّاد ترقيم لكل مفتاح |
| 3.42 | Payroll.cs | كيان Payroll |
| 3.43 | PayrollLine.cs | استحقاق موظفٍ في مسير |
| 3.44 | Product.cs | كيان Product |
| 3.45 | PurchaseInvoice.cs | كيان PurchaseInvoice |
| 3.46 | PurchaseReturn.cs | كيان PurchaseReturn |
| 3.47 | SalesInvoice.cs | كيان SalesInvoice |
| 3.48 | SalesReturn.cs | كيان SalesReturn |
| 3.49 | Security.cs | دورٌ ومفاتيحه |
| 3.50 | Setting.cs | كيان Setting |
| 3.51 | StockAdjustment.cs | كيان StockAdjustment |
| 3.52 | StockMovement.cs | كيان StockMovement |
| 3.53 | StockTransferDocument.cs | كيان StockTransferDocument |
| 3.54 | Supplier.cs | كيان Supplier |
| 3.55 | Treasury.cs | خزينة/صندوق أو حساب بنكي |
| 3.56 | Unit.cs | كيان Unit |
| 3.57 | User.cs | كيان User |
| 3.58 | Voucher.cs | سند قبض/صرف |
| 3.59 | Warehouse.cs | كيان Warehouse |
| | **Enums/** | تعدادات النظام |
| 3.60 | Enums.cs | تعدادات النظام |
| 3.61 | FieldFormat.cs | صيغة الحقل |
| | **Helpers/** | أدوات نقية صغيرة |
| 3.62 | ArabicNumberToWords.cs | التفقيط بالعربية |
| | **Results/** | نتيجة العملية وحالتها |
| 3.63 | PagedResult.cs | نتيجة صفحة واحدة من قائمة |
| 3.64 | Result.cs | نتيجة عملية بلا قيمة راجعة |
| 3.65 | StatusVariant.cs | مفردات حالة الأعمال المقفلة |

## 4. 4.Application

| # | الملف | الوظيفة |
|---|---|---|
| | **DTOs/Accounting/** | بيانات المحاسبة |
| 4.01 | AccountDto.cs | للعرض في الجداول |
| 4.02 | FiscalPeriodDto.cs | بيانات السنة والفترة المالية |
| 4.03 | JournalDto.cs | للجداول/القوائم |
| | **DTOs/Assets/** | بيانات الأصول |
| 4.04 | AssetDepreciationDto.cs | قسط إهلاكٍ كما يُعرَض |
| 4.05 | AssetDisposalDto.cs | بيانات استبعاد الأصل |
| 4.06 | AssetDto.cs | بيانات الأصل |
| 4.07 | AssetRevaluationDto.cs | بيانات إعادة التقييم |
| | **DTOs/Cheques/** | بيانات الشيكات |
| 4.08 | ChequeDto.cs | بيانات الشيك وحركته |
| | **DTOs/Common/** | بيانات مشتركة: فئة وترخيص ونسخة |
| 4.09 | CategoryDto.cs | بيانات الفئة |
| 4.10 | EditionDto.cs | طلب إنشاء نسخة برنامج |
| 4.11 | LicenseDto.cs | بيانات الترخيص |
| | **DTOs/Documents/** | بيانات مستندات الدورة |
| 4.12 | CycleDocumentDto.cs | بيانات مستند الدورة |
| 4.13 | IPullableLine.cs | سطرٌ قابل للسحب |
| 4.14 | PullLinkFields.cs |  |
| 4.15 | TradeLineDto.cs | سطر فاتورةٍ أو مرتجع |
| | **DTOs/HR/** | بيانات الموارد البشرية |
| 4.16 | EmployeeFilter.cs | مرشّح الموظفين |
| 4.17 | EmployeeMovementDto.cs | بدلٌ أو خصمٌ على موظف |
| 4.18 | PayrollDto.cs | بيانات مسير الرواتب |
| | **DTOs/Inventory/** | بيانات المخزون |
| 4.19 | OpeningStockDto.cs | أرصدة الأصناف الافتتاحية |
| 4.20 | ProductFilter.cs | مرشّح الأصناف |
| 4.21 | StockAdjustmentDto.cs | بيانات إذن المخزون |
| 4.22 | StockTransferDto.cs | بيانات التحويل المخزني |
| | **DTOs/Parties/** | مرشّحا العملاء والموردين |
| 4.23 | CustomerFilter.cs | مرشّح العملاء |
| 4.24 | SupplierFilter.cs | مرشّح الموردين |
| | **DTOs/Purchasing/** | بيانات المشتريات |
| 4.26 | PurchaseInvoiceDto.cs | بيانات فاتورة الشراء |
| 4.27 | PurchaseReturnDto.cs | بيانات مرتجع الشراء |
| | **DTOs/Sales/** | بيانات المبيعات |
| 4.28 | SalesInvoiceDto.cs | بيانات فاتورة البيع |
| 4.29 | SalesReturnDto.cs | بيانات مرتجع البيع |
| | **DTOs/Security/** | بيانات المستخدمين والأدوار |
| 4.30 | RoleFilter.cs | مرشّح الأدوار |
| 4.31 | UserDto.cs | بيانات المستخدم |
| | **DTOs/Treasury/** | بيانات الخزائن والبنوك |
| 4.32 | TreasuryFilter.cs | مرشّح الخزائن |
| | **DTOs/Vouchers/** | بيانات السندات |
| 4.33 | VoucherDto.cs | بيانات السند وتخصيصه |
| | **Legacy/Accounting/** | الحسابات والقيود والفترات المالية |
| 4.34 | AccountService.cs | صفحة شجرة الحسابات |
| 4.35 | FiscalPeriodService.cs | المالك الوحيد لمنطق السنوات/الفترات المالية |
| 4.36 | IAccountService.cs | عقد خدمة الحسابات |
| 4.37 | IFiscalPeriodService.cs | عقد الفترات المالية |
| 4.38 | IJournalService.cs | المالك الوحيد لمنطق قيود اليومية |
| 4.39 | JournalService.cs | صفحة قيود اليومية |
| 4.40 | OpeningBalanceService.cs | الأرصدة الافتتاحية ليست جدولاً موازياً |
| | **Legacy/Admin/** | الإعدادات والتراخيص والنسخ والتحديث |
| 4.41 | ISettingsService.cs | عقد الإعدادات |
| 4.42 | LicenseService.cs | تراخيص العملاء |
| 4.43 | ProgramEditionService.cs | نسخةُ برنامجٍ مستقلّة في مسارٍ |
| 4.44 | SettingsService.cs | طبقة الأعمال فوق ISettingsProvider |
| 4.45 | UpdateService.cs | التحديث سؤالٌ واحد |
| | **Legacy/Assets/** | الأصل: اقتناءً وإهلاكاً وتقييماً واستبعاداً |
| 4.46 | AssetDepreciationService.cs | قسط الإهلاك سجلٌّ مستقلّ |
| 4.47 | AssetDisposalService.cs | بيع الأصل واستبعاده |
| 4.48 | AssetMovementServiceBase.cs | أساس حركات الأصول |
| 4.49 | AssetRevaluationService.cs | إعادة تقييم الأصل |
| 4.50 | AssetService.cs | الأصل: إنشاءً وتعديلاً وبذراً |
| 4.51 | IAssetService.cs | عقد خدمة الأصول |
| | **Legacy/Backup/** | النسخ الاحتياطي والاستعادة |
| 4.52 | BackupInfo.cs | وصف نسخة احتياطية |
| 4.53 | BackupService.cs | المكان الوحيد لأخذ/استعادة/التحقق من النسخ |
| 4.54 | IBackupService.cs | عقد النسخ الاحتياطي |
| | **Legacy/Builder/** | ما يبنيه المستخدم من شاشات |
| 4.55 | BuilderCatalogService.cs | وصفُ ما بناه المستخدم كما |
| 4.56 | BuilderCrudServices.cs | خدمة صفوف |
| 4.57 | DynamicEntityService.cs | خدمة أي جدول بناه المستخدم |
| | **Legacy/Cheques/** | دورة الشيك |
| 4.58 | ChequeDocumentService.cs | مستند "استلام/صرف شيكات" |
| 4.59 | ChequeService.cs | دورة الشيك كاملة |
| | **Legacy/Common/** | الفئات وحساباتها |
| 4.60 | CategoryService.cs | فئات الوحدات وحساباتها |
| 4.61 | ICategoryService.cs | عقد خدمة الفئات |
| | **Legacy/Documents/** | مستندات الدورة وروابط السحب |
| 4.62 | CycleDocumentServiceBase.cs | مستندات الدورة: أساسٌ وستّ خدمات |
| | **Legacy/HR/** | الموظفون والرواتب والحضور |
| 4.63 | AttendanceService.cs | الحضور والانصراف |
| 4.64 | EmployeeMovementService.cs | البدل والخصم خدمةٌ واحدة بجدولين |
| 4.65 | EmployeeService.cs | خدمة الموظفين |
| 4.66 | IEmployeeService.cs | عقد الموظفين |
| 4.67 | IPayrollService.cs | عقد مسير الرواتب |
| 4.68 | PayrollService.cs | مسير الرواتب |
| | **Legacy/Inventory/** | الأصناف والمخازن والأرصدة |
| 4.69 | IProductService.cs | عقد الأصناف |
| 4.70 | IStockInService.cs | عقود أذون المخزون الستة |
| 4.71 | IStockTransferService.cs | عقد التحويل المخزني |
| 4.72 | OpeningStockService.cs | رصيد أول المدة للأصناف |
| 4.73 | ProductService.cs | خدمة الأصناف |
| 4.74 | StockAdjustmentServiceBase.cs | خدمةٌ بوّابتها مفتاحٌ واحد مُعلَن |
| 4.75 | StockTransferService.cs | التحويل بين المخازن |
| | **Legacy/Parties/** | العملاء والموردون |
| 4.76 | CustomerService.cs | المالك الوحيد لمنطق العملاء |
| 4.77 | ICustomerService.cs | المالك الوحيد لمنطق العملاء |
| 4.78 | ISupplierService.cs | المالك الوحيد لمنطق الموردين |
| 4.79 | PartyServiceBase.cs | المنطق المشترك بين العملاء والموردين |
| 4.80 | SupplierService.cs | المالك الوحيد لمنطق الموردين |
| | **Legacy/Print/** | بناء المستندات المطبوعة |
| 4.81 | ChequePrinter.cs | الشيك يُطبَع على ورق مطبوع |
| 4.82 | Code128.cs | ترميز Code128-B |
| 4.83 | CompanyHeaderComponent.cs | ترويسة الشركة كقطعة واحدة تُستدعى |
| 4.84 | IPrintDialogHost.cs | تنفّذه طبقة الواجهة |
| 4.85 | IPrintService.cs | عقد الطباعة |
| 4.86 | ImageData.cs | تحويل صورة ↔ Base64 |
| 4.87 | PaperNodeRenderer.cs | مترجم PaperNode إلى عناصر WPF |
| 4.88 | PaperTheme.cs | قيم الورق كلها من PrintTheme.xaml |
| 4.89 | PrintService.cs | يبني مستندات الطباعة من IPrintable |
| | **Legacy/Purchasing/** | فواتير الشراء ومرتجعاتها |
| 4.90 | IPurchaseInvoiceService.cs | عقد فاتورة الشراء |
| 4.91 | IPurchaseReturnService.cs | عقد مرتجع الشراء |
| 4.92 | PurchaseInvoiceService.cs | فاتورة الشراء وقيدها |
| 4.93 | PurchaseReturnService.cs | مرتجع الشراء وقيده |
| | **Legacy/Sales/** | فواتير البيع ومرتجعاتها |
| 4.94 | ISalesInvoiceService.cs | عقد فاتورة البيع |
| 4.95 | ISalesReturnService.cs | عقد مرتجع البيع |
| 4.96 | SalesInvoiceService.cs | فاتورة البيع وقيدها |
| 4.97 | SalesReturnService.cs | مرتجع البيع وقيده |
| | **Legacy/Security/** | المستخدمون والأدوار |
| 4.98 | IRoleService.cs | عقد الأدوار |
| 4.99 | IUserService.cs | عقد المستخدمين |
| 4.100 | RoleService.cs | الأدوار وصلاحياتها |
| 4.101 | UserService.cs | المستخدمون وكلمات مرورهم |
| | **Legacy/Treasury/** | الخزائن والبنوك |
| 4.102 | ITreasuryService.cs | عقد الخزائن والبنوك |
| 4.103 | TreasuryService.cs | الخزائن والبنوك وحساباتها |
| | **Legacy/Vouchers/** | سندات القبض والصرف |
| 4.104 | VoucherServiceBase.cs | سند قبض/صرف |
| | **Reporting/** | خدمات التقارير |
| 4.105 | AssetReportService.cs | تقريرا الأصول |
| 4.106 | BuilderReportService.cs | تقرير مبنيّ |
| 4.107 | FinancialStatementFactory.cs | القوائم المالية بشكلها الرسمي |
| 4.108 | FinancialStatementService.cs | القوائم المالية الثلاث |
| 4.109 | PartyReportService.cs | أرصدة الأطراف وكشوفها |
| 4.110 | PayslipReportService.cs | قسيمة راتب موظف |
| 4.111 | ReportData.cs | مخرَج التقرير |
| 4.112 | ReportRows.cs | ميزان المراجعة القياسي |
| 4.113 | ReportServiceBase.cs | ما يتقاسمه كل تقرير |
| 4.114 | SalesReportService.cs | تقرير المبيعات |
| 4.115 | StockReportService.cs | تقارير المخزون |
| | **Services/Core/** | أساس كل خدمة: الصلاحية والمعاملة والكيان والترقيم |
| 4.116 | CrudServiceBase.cs | القراءة العامة لكيان بصفحات وبحث |
| 4.117 | EntityService.cs | إضافة الكيان وتعديله وحذفه |
| 4.118 | INumberSequenceService.cs | عقد الترقيم التسلسلي |
| 4.119 | NumberSequenceService.cs | أرقام متسلسلة لكل مفتاح |
| 4.120 | ServiceBase.cs | القاعدة المشتركة لكل خدمة |
| | **Services/Documents/** | المستند والسحب وحركة المخزون وتغيير الحالة وسطور التجارة |
| 4.121 | DocumentPull.cs | تتبّع السحب بين المستندات |
| 4.122 | DocumentService.cs | المستند: قراءةً وإنشاءً واستبدالاً وحذفاً |
| 4.123 | IStockMove.cs | عقد أرصدة المخزون |
| 4.124 | ProductLines.cs | سطور المستند بأصنافها |
| 4.125 | StatusChange.cs | الحالة كترحيل |
| 4.126 | StockMove.cs | أرصدة المخزون وحركته |
| 4.127 | TradeAccounts.cs | حسابات البيع والشراء |
| 4.128 | TradeLines.cs | سطور الفواتير والمرتجعات ومجاميعها |
| | **Services/Entities/** | الكيان من إعداده: القائمة البسيطة وتحويل الصفّ |
| 4.129 | ByCode.cs | سطورٌ تُحلّ بكود كيانها |
| 4.130 | EntitySpec.cs | إعلان صفحة كيان |
| 4.131 | Lookup.cs | صفحة قائمةٍ من إعلانها |
| 4.132 | Rows.cs | الكيان صفّاً والصفّ كياناً |
| 4.133 | Tree.cs | الشجرة من صفوفٍ مسطّحة |
| | **Services/Ledger/Accounts/** | الحساب: في الشجرة، للكيان، بالاتجاهين، ومع مجمّعه |
| 4.134 | AccountCases.cs | حالات الحساب التي يستدعيها الكيان |
| 4.135 | AccountOf.cs | حساب الكيان أو الإعداد |
| 4.136 | AccountSpec.cs | حساب كيانٍ بمعاملاته |
| 4.137 | AddEntityAccount.cs | إضافة حساب لكيان صفحة |
| 4.138 | AddLinkedAccount.cs | حسابٌ في الشجرة ينشئ كيانه |
| 4.139 | AddMirroredAccount.cs | حسابٌ ومجمّعه لأي كيان |
| 4.140 | AddTreeAccount.cs | إضافة حساب في الشجرة |
| 4.141 | CloseAccount.cs | حذف الحساب وإعادة أبيه ورقياً |
| 4.142 | CloseLinkedAccount.cs | حذف الحساب وكيانه |
| 4.143 | EditLinkedAccount.cs | تعديل الحساب واسم كيانه |
| 4.144 | EditTreeAccount.cs | تعديل حساب في الشجرة |
| 4.145 | IAccountLinkedService.cs | كيان يعيش ورقةً في شجرة |
| 4.146 | LinkedAccounts.cs | كيان الجذر المرتبط |
| 4.147 | RenameAccount.cs | تسمية الحساب |
| 4.148 | SettingAccounts.cs | حسابات الإعدادات |
| | **Services/Ledger/** | قلب القيد، أرصدة الحسابات، الفترة، القيد بطرفيه والتجارة، الحرّاس، رصيد الطرف |
| 4.149 | AccountBalances.cs | أرصدة الحسابات من قيودها |
| 4.150 | Entries.cs | قلب القيد لكل مستدعٍ |
| 4.151 | Guards.cs | حرّاس الشجرة والنقدية |
| 4.152 | JournalLines.cs | سطور قيدٍ بطرفيها |
| 4.153 | OpeningEntry.cs | القيد الافتتاحي متعدّد الأسطر |
| 4.154 | PartyBalance.cs | رصيد الطرف من حسابه |
| 4.155 | PartyByKind.cs | الطرف بنوعه |
| 4.156 | PeriodGate.cs | التاريخ في فترةٍ مفتوحة |
| 4.157 | Posting.cs | القيد يُنشأ مُرحَّلاً ويُعكس بحذفه |
| 4.158 | Statement.cs | كشف الحساب برصيده الجاري |
| 4.159 | TradeEntry.cs | قيد البيع والشراء وعكسهما |
| 4.160 | TrialBalance.cs | ميزان المراجعة لفترة |
| 4.161 | TwoSided.cs | طرفا القيد باتجاهه |
| | **Validation/** | دالة التحقق الوحيدة Check بشروطٍ معاملاتٍ في Field، وسطور المستند |
| 4.162 | Check.cs | الدالة الوحيدة للتحقق |
| 4.163 | DocumentLines.cs | سطور المستند بشروط Check |
| 4.164 | Field.cs | شرطُ حقلٍ بكل معاملاته |
| 4.165 | ValidationResult.cs | عقد ValidationResult |

## 5. 5.Design

| # | الملف | الوظيفة |
|---|---|---|
| 5.01 | Colors.xaml | ألوان النظام |
| | **Icons/** | الأيقونات |
| 5.02 | Icons.xaml | الأيقونات |
| 5.03 | Sizes.xaml | مقاسات النظام |
| | **Strings/** | النصوص الظاهرة بالعربية والإنجليزية |
| 5.04 | Strings.ar.xaml | النصوص الظاهرة |
| 5.05 | Strings.en.xaml | النصوص الظاهرة |
| | **Styles/** | أنماط عناصر WPF |
| 5.06 | Implicit.xaml | واجهة |
| 5.07 | ScrollBars.xaml | واجهة |
| 5.08 | Style.Button.xaml | نمط عنصر WPF |
| 5.09 | Style.Dialog.xaml | نمط عنصر WPF |
| 5.10 | Style.Input.xaml | نمط عنصر WPF |
| | **Surfaces/** | ألوان الورق والتصدير، خارج موارد الشاشة |
| 5.11 | ExportTheme.cs | ألوان ملفات Excel/CSV/PDF |
| 5.12 | PrintTheme.xaml | ألوان الطباعة |
| 5.13 | Theme.xaml | دمج طبقات التصميم |

## 6. 6.UI

| # | الملف | الوظيفة |
|---|---|---|
| | **Components/Actions/** | الأزرار وشريطها |
| 6.01 | ActionToolbar.xaml | واجهة |
| 6.02 | ActionToolbar.xaml.cs | شريط أدوات ببناء برمجي (ButtonsSource) |
| 6.03 | AppButton.xaml | واجهة |
| 6.04 | AppButton.xaml.cs | أزرار AppButton |
| 6.05 | AppDropdownButton.xaml | واجهة |
| 6.06 | AppDropdownButton.xaml.cs | أزرار AppDropdownButton |
| 6.07 | AppIconButton.xaml | واجهة |
| 6.08 | AppIconButton.xaml.cs | أزرار AppIconButton |
| 6.09 | PermissionButton.xaml | واجهة |
| 6.10 | PermissionButton.xaml.cs | زرّ محكوم بالصلاحية |
| 6.11 | ToolbarAction.cs | تعريف زر شريط أدوات |
| | **Components/Display/** | الجدول والترقيم والبطاقة |
| 6.12 | AppBadge.xaml | واجهة |
| 6.13 | AppBadge.xaml.cs | عرض AppBadge |
| 6.14 | AppBreadcrumb.xaml | واجهة |
| 6.15 | AppBreadcrumb.xaml.cs | عرض AppBreadcrumb |
| 6.16 | AppCard.xaml | واجهة |
| 6.17 | AppCard.xaml.cs | عرض AppCard |
| 6.18 | AppDataGrid.xaml | واجهة |
| 6.19 | AppDataGrid.xaml.cs | الشبكة: أعمدةٌ وترقيمٌ وتحديد |
| 6.20 | AppEmptyState.xaml | واجهة |
| 6.21 | AppEmptyState.xaml.cs | عرض AppEmptyState |
| 6.22 | AppIcon.xaml | واجهة |
| 6.23 | AppIcon.xaml.cs | أيقونة بمواصفة واحدة |
| 6.24 | AppLoadingOverlay.xaml | واجهة |
| 6.25 | AppLoadingOverlay.xaml.cs | عرض AppLoadingOverlay |
| 6.26 | AppPagination.xaml | واجهة |
| 6.27 | AppPagination.xaml.cs | عرض AppPagination |
| 6.28 | AppStatCard.xaml | واجهة |
| 6.29 | AppStatCard.xaml.cs | عرض AppStatCard |
| 6.30 | AppTabControl.xaml | واجهة |
| 6.31 | AppTabControl.xaml.cs | عرض AppTabControl |
| 6.32 | AppTabItem.cs | عرض AppTabItem |
| 6.33 | AppTreeView.xaml | واجهة |
| 6.34 | AppTreeView.xaml.cs | شجرة مربوطة على TreeNodeViewModel.VisibleChildren |
| 6.35 | GridColumn.cs | تعريف عمود AppDataGrid |
| | **Components/Documents/** | محرر سطور المستند |
| 6.36 | DocumentFooter.xaml | واجهة |
| 6.37 | DocumentFooter.xaml.cs | تذييل مستند عام |
| 6.38 | DocumentLine.cs | سطر مستند مرن |
| 6.39 | DocumentLinesGrid.Clipboard.cs | شبكة سطور مستند |
| 6.40 | DocumentLinesGrid.Keys.cs | شبكة سطور مستند |
| 6.41 | DocumentLinesGrid.Pickers.cs | شبكة سطور مستند |
| 6.42 | DocumentLinesGrid.Rows.cs | شبكة سطور مستند |
| 6.43 | DocumentLinesGrid.xaml | واجهة |
| 6.44 | DocumentLinesGrid.xaml.cs | شبكة سطور مستند عامة |
| 6.45 | FooterTotal.cs | عنصر إجمالي واحد في DocumentFooter |
| 6.46 | LineCellTemplateSelector.cs | يختار قالب الخلية |
| 6.47 | LineColumn.cs | تعريف عمود في DocumentLinesGrid |
| 6.48 | LineColumnPresets.cs | تعريفات أعمدة جاهزة لأنماط المستندات |
| 6.49 | LineComputeEngine.cs | محرك حساب أعمدة السطر |
| 6.50 | LineValidationEngine.cs | يتحقق من سطر مستند واحد |
| | **Components/Feedback/** | الحوارات والتنبيهات |
| 6.51 | AppConfirmDialog.cs | حوارات وتنبيهات AppConfirmDialog |
| 6.52 | AppDialogWindow.xaml | واجهة |
| 6.53 | AppDialogWindow.xaml.cs | القاعدة الموحّدة لكل نوافذ الحوار |
| 6.54 | AppMessageDialog.cs | حوارات وتنبيهات AppMessageDialog |
| 6.55 | AppProgressDialog.cs | حوارات وتنبيهات AppProgressDialog |
| 6.56 | AppToast.xaml | واجهة |
| 6.57 | AppToast.xaml.cs | حوارات وتنبيهات AppToast |
| | **Components/Inputs/** | حقول الإدخال |
| 6.58 | AppCheckBox.xaml | واجهة |
| 6.59 | AppCheckBox.xaml.cs | حقل إدخال AppCheckBox |
| 6.60 | AppComboBox.xaml | واجهة |
| 6.61 | AppComboBox.xaml.cs | حقل إدخال AppComboBox |
| 6.62 | AppDatePicker.xaml | واجهة |
| 6.63 | AppDatePicker.xaml.cs | حقل إدخال AppDatePicker |
| 6.64 | AppImagePicker.cs | حقل صورة قيمته نص Base64 |
| 6.65 | AppNumericBox.xaml | واجهة |
| 6.66 | AppNumericBox.xaml.cs | حقل إدخال AppNumericBox |
| 6.67 | AppPasswordBox.xaml | واجهة |
| 6.68 | AppPasswordBox.xaml.cs | نفس بنية AppTextBox بالضبط |
| 6.69 | AppSearchBox.xaml | واجهة |
| 6.70 | AppSearchBox.xaml.cs | حقل إدخال AppSearchBox |
| 6.71 | AppTextArea.xaml | واجهة |
| 6.72 | AppTextArea.xaml.cs | حقل إدخال AppTextArea |
| 6.73 | AppTextBox.xaml | واجهة |
| 6.74 | AppTextBox.xaml.cs | حقل إدخال AppTextBox |
| 6.75 | AppToggleSwitch.xaml | واجهة |
| 6.76 | AppToggleSwitch.xaml.cs | حقل إدخال AppToggleSwitch |
| | **Components/Layout/** | ترويسة الصفحة وشريط الفلاتر |
| 6.77 | FilterBar.xaml | واجهة |
| 6.78 | FilterBar.xaml.cs | تخطيط FilterBar |
| 6.79 | PageHeader.xaml | واجهة |
| 6.80 | PageHeader.xaml.cs | تخطيط PageHeader |
| | **Components/Pickers/** | نوافذ الاختيار |
| 6.81 | AccountPicker.cs | اختيار حساب من شجرة الحسابات |
| 6.82 | CustomerPicker.cs | اختيار عميل بجدول بحث |
| 6.83 | EmployeePicker.cs | اختيار موظف بجدول بحث |
| 6.84 | IPickerDataSource.cs | مصدر بيانات لأي Picker |
| 6.85 | PickerBase.cs | الطبقة المعمَّمة فوق PickerBaseControl |
| 6.86 | PickerBaseControl.xaml | واجهة |
| 6.87 | PickerBaseControl.xaml.cs | القاعدة غير المعمَّمة لكل Picker |
| 6.88 | PickerGridWindow.cs | نافذة اختيار بجدول مشتركة لكل |
| 6.89 | PickerResultItem.cs | تمثيل موحّد وغير معمَّم لأي |
| 6.90 | PickerTreeWindow.cs | نافذة اختيار بشجرة مشتركة |
| 6.91 | PickerWindowGeometry.cs | يتذكر آخر حجم/موضع لكل نافذة |
| 6.92 | ProductPicker.cs | اختيار صنف بجدول بحث |
| 6.93 | SupplierPicker.cs | اختيار مورد بجدول بحث |
| | **Components/Shell/** | الشريط الجانبي والعلوي |
| 6.94 | AppShell.xaml | واجهة |
| 6.95 | AppShell.xaml.cs | القطعة الجامعة |
| 6.96 | AppSidebar.xaml | واجهة |
| 6.97 | AppSidebar.xaml.cs | شريط تنقّل جانبي هرمي |
| 6.98 | AppTopBar.xaml | واجهة |
| 6.99 | AppTopBar.xaml.cs | شريط علوي عام |
| 6.100 | NavItem.cs | عنصر تنقّل واحد في AppSidebar |
| 6.101 | NavItemViewModel.cs | حالة عنصر التنقّل |
| | **Components/Tree/** | شجرة الحسابات وعقدها |
| 6.102 | NodeCheckState.cs | حالة تحديد عقدة في شجرة |
| 6.103 | TreeFilterEngine.cs | فلترة إخفاء حقيقية على شجرة |
| 6.104 | TreeLayoutOptions.cs | خيارات تخطيط الشجرة |
| 6.105 | TreeNodeViewModel.cs | عقدة شجرة قابلة للمراقبة |
| | **Components/** | القطع المرئية بأقسامها |
| 6.106 | VisualTree.cs | البحث في الشجرة المرئية |
| | **Converters/** | محوّلات الربط |
| 6.107 | BoolToVisibilityConverter.cs | محوّل عرض BoolToVisibility |
| 6.108 | FooterVariantToBrushConverter.cs | يحوّل FooterTotal |
| 6.109 | IconKeyToGeometryConverter.cs | يحوّل NavItem |
| 6.110 | LineCellConverters.cs | يقرأ قيمة خلية بالمفتاح الديناميكي |
| 6.111 | StringToVisibilityConverter.cs | نص فارغ/فارغ تماماً يعني Collapsed |
| 6.112 | VariantToBrushConverter.cs | حالة ← لون |
| | **Services/** | خدمات الواجهة: حوار وتنبيه وتصدير وتنقّل |
| 6.113 | DialogService.cs | واجهة async فوق حوارات معتمدة |
| 6.114 | ExportService.cs | يصدّر بيانات AppDataGrid فعلياً |
| 6.115 | IDialogService.cs | حوار يريد إرجاع نتيجة نمطية |
| 6.116 | IExportService.cs | خدمة واجهة Export |
| 6.117 | INavigationService.cs | خدمة واجهة Navigation |
| 6.118 | IProgressHandle.cs | خدمة واجهة ProgressHandle |
| 6.119 | IToastService.cs | خدمة واجهة Toast |
| 6.120 | IdentityService.cs | تحميل موارد التصميم |
| 6.121 | NavigationService.cs | ينقل بين صفحات مسجَّلة بمفتاح |
| 6.122 | PrintDialogHost.cs | الواجهة المرئية للطباعة |
| 6.123 | ToastHostWindow.xaml | واجهة |
| 6.124 | ToastHostWindow.xaml.cs | نافذة الإشعارات العائمة |
| 6.125 | ToastService.cs | يعرض إشعارات Toast فوق أي |
| 6.126 | UIServices.cs | نقطة وصول واحدة لحاوية DI |
| 6.127 | UpdateFlow.cs | البحث عن تحديث وتنزيله |
| | **ViewModels/** | نماذج العرض مجموعةً بأقسامها |
| 6.128 | AccountsViewModel.cs | نماذج عرض AccountsViewModel |
| | **ViewModels/Base/** | أسس نماذج العرض: صفحة وCRUD وصلاحية |
| 6.129 | CrudViewModelBase.cs | يضيف على PagedViewModelBase حذف عام |
| 6.130 | PagedViewModelBase.cs | صفحات+بحث عامان لأي كيان |
| 6.131 | PermissionAwareViewModel.cs | قاعدة لأي ViewModel يحتاج التحقق |
| | **ViewModels/** | نماذج العرض مجموعةً بأقسامها |
| 6.132 | BaseViewModel.cs | نماذج عرض BaseViewModel |
| 6.133 | CategoryListViewModel.cs | نماذج عرض CategoryFilter |
| 6.134 | CycleDocumentViewModels.cs | نماذج عرض CycleDocumentViewModelBase |
| 6.135 | CycleVoucherViewModels.cs | نماذج عرض CycleVoucherViewModelBase |
| 6.136 | DocumentViewModels.cs | نماذج عرض JournalsViewModel |
| 6.137 | DynamicViewModel.cs | نموذج عرض أي شاشة صفوفها |
| 6.138 | HrMovementViewModels.cs | البدل والخصم شاشةٌ واحدة بخدمتين |
| 6.139 | ListViewModels.cs | نماذج عرض UnitsViewModel |
| 6.140 | LookupViewModels.cs | نماذج عرض CategoriesLookupViewModel |
| 6.141 | TreeViewModelBase.cs | نماذج عرض TreeViewModelBase |
| 6.142 | VoucherViewModels.cs | نماذج عرض السندات والشيكات |

## 7. 7.Composition

| # | الملف | الوظيفة |
|---|---|---|
| | **Definitions/** | وصف الشاشة والحوار والتقرير بياناً |
| 7.01 | CategoryDialogFactory.cs | حوار الفئة لكل وحدة |
| 7.02 | DialogDefinition.cs | وصف الحوار وحقوله |
| 7.03 | DocumentDialogDefinition.cs | وصف حوار المستند وسطوره |
| 7.04 | FilterDefinition.cs | وصف فلتر الصفحة |
| 7.05 | FlowScope.cs | نطاق الدورة |
| 7.06 | ModuleDefinition.cs | وصف تعريفي كامل لصفحة قائمة+CRUD |
| 7.07 | PermissionTreeFactory.cs | يبني شجرة الصلاحيات من PermissionKeys |
| 7.08 | PullSource.cs | مصدر السحب وأثره المخزني |
| 7.09 | ReportDefinition.cs | وصف التقرير ومعاملاته |
| 7.10 | RowAction.cs | إجراء إضافي على السجل المحدَّد |
| 7.11 | RowPage.cs | صفحة صفوفٍ من خدمتها |
| 7.12 | StandardFields.cs | حقول تتكرر في كل حوار |
| 7.13 | TreeCheckListDefinition.cs | شاشة شجرة قابلة للتأشير مدفوعة |
| | **Print/** | جسر الطباعة للشاشات |
| 7.14 | PrintDocuments.cs | عائلتا المستندات |
| 7.15 | TradePaper.cs | شكل الورق التجاري |
| | **Pull/** | سحب مستند من مستند |
| 7.16 | PullService.cs | محرّك السحب العام |
| | **Registry/** | سجلّ الوحدات وخريطة التنقّل |
| 7.17 | IModuleRegistry.cs | سجل الوحدات المُفعَّلة |
| 7.18 | ModuleRegistry.cs | سجلّ الوحدات |
| 7.19 | NavigationMap.cs | خريطة الشريط الجانبي |
| 7.20 | NavigationSource.cs | أقسام الشريط الجانبي كما يراها |
| | **Renderers/** | تحويل التعريف إلى شاشة عاملة |
| 7.21 | BuilderPickers.cs | قوائم تعدادات النظام وكتالوج أزراره |
| 7.22 | ChequeBoardRenderer.cs | شاشة الشيكات |
| 7.23 | CrudPageRenderer.cs | تصيير صفحة القائمة وCRUD |
| 7.24 | DialogRenderer.cs | تصيير الحوار من وصفه |
| 7.25 | DocumentPageRenderer.cs | تصيير صفحة المستند |
| 7.26 | DocumentPrinter.cs | يجلب المستند الكامل من خدمته |
| 7.27 | DocumentRenderer.cs | تصيير محرّر المستند |
| 7.28 | FieldValidation.cs | تحقّق واجهة واحد لكل الشاشات |
| 7.29 | FilterControls.cs | شريط فلاتر الصفحة المُعلَنة |
| 7.30 | FolderOutput.cs | مسار مجلد من المستخدم |
| 7.31 | ListOutput.cs | طباعة أي قائمة معروضة وتصديرها |
| 7.32 | PageRenderer.cs | نقطة التوزيع الوحيدة حسب ModuleDefinition.LayoutKind |
| 7.33 | PaginationBar.cs | شريط الترقيم مربوطاً بنموذج العرض |
| 7.34 | PullDialog.cs | نافذة "سحب من" |
| 7.35 | ReportRenderer.cs | تصيير التقرير |
| 7.36 | Resolve.cs | حلّ نموذج العرض والخدمة |
| 7.37 | SettingsPageRenderer.cs | تصيير صفحة الإعدادات |
| 7.38 | ToolbarActions.cs | أزرار الصفحة بعد ترشيحها بما |
| 7.39 | TreeBuilder.cs | قائمة مسطَّحة تصير شجرة |
| 7.40 | TreeCheckListRenderer.cs | تصيير شجرة التأشير |
| 7.41 | TreeRenderer.cs | تصيير الشجرة |

## 8. 8.Modules

| # | الملف | الوظيفة |
|---|---|---|
| 8.01 | BuilderModuleLoader.cs | وصفُ ما بناه المستخدم ← |
| 8.02 | BuilderRegistrations.cs | شاشات وحدة البناء نفسها |
| 8.03 | CycleDocumentRegistrations.cs | تسجيل مستندات الدورة |
| 8.04 | CycleFlow.cs | سلسلة السحب في الدورة الشاملة |
| 8.05 | CycleVoucherRegistrations.cs | تسجيل سندات الدورة |
| 8.06 | FinancialStatementColumns.cs | أعمدة القوائم المالية |
| 8.07 | HrRegistrations.cs | شاشات الموارد البشرية التي يجمع |
| 8.08 | LookupPages.cs | إعلانات صفحات القوائم |
| 8.09 | ModuleRegistrations.cs | يسجّل كل وحدة عمل فعلية |
| 8.10 | PermissionModuleRegistrations.cs | شاشتا الصلاحيات |
| 8.11 | ReportRegistrations.cs | التقارير الثلاثة عشر |
| 8.12 | StockDocumentFactory.cs | رأس المستند المخزني وسطوره |
| 8.13 | TreasuryRegistrations.cs | الخزائن والسندات والشيكات |

## 9. App

| # | الملف | الوظيفة |
|---|---|---|
| | **Bootstrap/** | تسجيل كل الخدمات وتهيئة القاعدة |
| 9.01 | DependencyInjection.cs | تسجيل الخدمات في الحاوية |
| 9.02 | LoginWindow.xaml | واجهة |
| 9.03 | LoginWindow.xaml.cs | أول نافذة حقيقية |
| 9.04 | MainWindow.xaml | واجهة |
| 9.05 | MainWindow.xaml.cs | القطعة الوحيدة هنا AppShell |

## 10. Tools

| # | الملف | الوظيفة |
|---|---|---|
| | **ArchitectureCheck/** | فحص الحدود والمسؤولية والتكرار |
| 10.01 | check.sh |  |
| | **Docs/** | توليد فهرس الملفات والمخططات |
| 10.02 | generate.sh |  |

## 11. PrimeERP.Tests

| # | الملف | الوظيفة |
|---|---|---|
| | **Architecture/** | حدود الطبقات واكتمال الوحدات |
| 11.01 | LayerBoundaryTests.cs | حدود الطبقات على IL المُصرَّف |
| 11.02 | AssemblyInfo.cs |  |
| | **Composition/** | تصيير الشاشات الحقيقية |
| 11.03 | AddButtonEnabledTests.cs | زرّ «جديد» المُصيَّر فعلياً |
| 11.04 | CategoryPickerTests.cs | قائمة الفئات في الحوار |
| 11.05 | ChequeDocumentPartyTests.cs | مستند الشيكات دفعةُ إدخال |
| 11.06 | CrudPageRendererTests.cs | صفحة CRUD حقيقية من تعريفها |
| 11.07 | CustomerDialogTests.cs | حوار العميل من الشاشة الحقيقية |
| 11.08 | DialogRendererTests.cs | تصيير الحوار من تعريفه |
| 11.09 | DocumentColumnParityTests.cs | كل حقل مُدخَل يصل الورق |
| 11.10 | DocumentRendererTests.cs | تصيير محرّر المستند |
| 11.11 | FlowScopeTests.cs | نطاق الدورة |
| 11.12 | LineMathCheck.cs | صافي السطر يُحسب حيّاً |
| 11.13 | LookupPagesRenderTests.cs | القوائم المرجعية تُفتح فعلاً |
| 11.14 | MasterListFilterTests.cs | فلترة القائمة الرئيسية |
| 11.15 | ModuleCompletenessTests.cs | حارس اكتمال الشاشة |
| 11.16 | NavigationGroupsTests.cs | مفاتيح الشريط الجانبي |
| 11.17 | PermissionFlowTests.cs | دورة الصلاحيات كما يعيشها المستخدم |
| 11.18 | PermissionRequirementsTests.cs | كل ما طُلب لشجرة الصلاحيات |
| 11.19 | PermissionScreenTests.cs | الشاشة نفسها لا التعريف |
| 11.20 | PermissionScreensTests.cs | شاشتا الصلاحيات تكوين فوق TreeCheckListRenderer |
| 11.21 | PrintDocumentsTests.cs | عائلتا المستندات تبنيان من مصدر |
| 11.22 | PrintPaperTests.cs | خيارات الورق |
| 11.23 | ProductDialogTests.cs | حوار الصنف |
| 11.24 | QuotationDocumentTests.cs | حفظ عرض سعر من الشاشة |
| 11.25 | ReorderButtonsTests.cs | زرّ الشريط يُبنى بأيقونته |
| 11.26 | ReportRendererTests.cs | تصيير التقرير |
| 11.27 | SettingsPageRendererTests.cs | صفحة الإعدادات |
| 11.28 | SupplierDialogTests.cs | حوار المورد |
| 11.29 | ToastCloseTests.cs | إغلاق التنبيه |
| 11.30 | ToolbarEnabledTests.cs | أزرار الشريط |
| 11.31 | ToolbarLevelsTests.cs | مستويا الإجراءات |
| 11.32 | TreasuryDialogTests.cs | إضافة بنك من الشاشة الحقيقية |
| 11.33 | TreeRendererTests.cs | تصيير الشجرة |
| 11.34 | VoucherTreasuryPickerTests.cs | قائمة الخزينة/البنك تتبع طريقة الدفع |
| | **Design/** | الألوان والرموز والنصوص |
| 11.35 | DesignTokenResolutionTests.cs | رموز التصميم تُحلّ لألوان مرئية |
| | **Documents/** | المستندات ودوراتها |
| 11.36 | LineEngineTests.cs | محرّك سطور المستند |
| | **Helpers/** | أدوات الاختبار |
| 11.37 | ArabicNumberToWordsTests.cs | تفقيط الأرقام بالعربية |
| 11.38 | Localized.cs | رسالةٌ من القاموس |
| 11.39 | LookupRows.cs | صفٌّ في صفحة قائمة |
| | **Services/** | الخدمات ومنطقها |
| 11.40 | AccountLeafStateTests.cs | الحساب إمّا أب وإمّا يقبل |
| 11.41 | AccountServiceTests.cs | قاعدة معزولة لكل اختبار |
| 11.42 | BackupServiceTests.cs | النسخ الاحتياطي والاستعادة |
| 11.43 | BuilderCatalogTests.cs | وصف ما بناه المستخدم |
| 11.44 | CategoryServiceTests.cs | الفئات وحساباتها |
| 11.45 | CustomerServiceTests.cs | قاعدة بيانات خاصة معزولة لكل |
| 11.46 | CycleDocumentServiceTests.cs | مستندات الدورة |
| 11.47 | CycleVoucherServiceTests.cs | سندات الدورة |
| | **Services/Design/** | التصميم والنصوص |
| 11.48 | IdentityServiceTests.cs | تحميل موارد التصميم |
| 11.49 | LocalizationServiceTests.cs | تبديل قاموس النصوص الحقيقي |
| | **Services/** | الخدمات ومنطقها |
| 11.50 | DocumentLinkServiceTests.cs | تتبّع السحب |
| 11.51 | EmployeeListTests.cs | القائمة تحمل اسمَي القسم والوظيفة |
| 11.52 | ExportServiceTests.cs | تصدير CSV وExcel وPDF فعلي |
| 11.53 | FiscalPeriodServiceTests.cs | قاعدة معزولة لكل اختبار |
| 11.54 | FlowModeStockTests.cs | الفرق بين الوضعين |
| 11.55 | InventoryCostingTests.cs | المتوسط المرجَّح المتحرّك |
| 11.56 | ItemCardReportTests.cs | تقرير حركة الصنف بالمتوسط المرجَّح |
| 11.57 | JournalServiceTests.cs | قاعدة بيانات خاصة معزولة لكل |
| 11.58 | JournalToStatementTests.cs | المعاملات كلها تصبّ في القيود |
| 11.59 | NavigationServiceTests.cs | التنقّل بين الشاشات |
| 11.60 | NegativeBalanceGuardTests.cs | المخزن والخزينة والبنك لا يقبلون |
| 11.61 | NumberSequenceServiceTests.cs | الترقيم التسلسلي |
| 11.62 | OpeningBalanceAndDepreciationTests.cs | الأرصدة الافتتاحية والإهلاك |
| 11.63 | PayrollServiceTests.cs | المسير يُنشأ مسوّدةً ثم يُرحَّل |
| 11.64 | PermissionServiceTests.cs | فحص الصلاحيات وتحميلها |
| 11.65 | PrintFormattingTests.cs | الشعار ومحاذاة الجدول في الورق |
| 11.66 | PrintServiceTests.cs | الطباعة |
| 11.67 | PullServiceTests.cs | سلوك السحب كما طُلب حرفياً |
| 11.68 | PurchaseInvoiceServiceTests.cs | فاتورة الشراء وقيدها |
| 11.69 | ReportsGenerateTests.cs | توليد التقارير |
| 11.70 | ReturnsServiceTests.cs | المرتجعات |
| 11.71 | SalesInvoiceServiceTests.cs | فاتورة البيع وقيدها |
| 11.72 | SettingsServiceTests.cs | الإعدادات |
| 11.73 | SoftDeleteTests.cs | المحذوف منطقياً خارج القراءة |
| 11.74 | StatementGroupingTests.cs | مستوى التجميع في القائمة |
| 11.75 | StockDocumentsServiceTests.cs | أذون المخزون |
| 11.76 | StockServiceTests.cs | أرصدة المخزون |
| 11.77 | TreasurySeedTests.cs | قوائم السندات كانت تبدو "لا |
| 11.78 | VoucherAndChequeTests.cs | السندات والشيكات |
| 11.79 | VoucherPrintTests.cs | سند القبض والصرف |
| 11.80 | StaThreadHelper.cs | أنواع WPF تفرض خيط STA |
| 11.81 | TestDatabaseFixture.cs | ملف SQLite مؤقت مستقل لكل |
| | **Validation/** | قواعد التحقق |
| 11.82 | ValidatorsTests.cs | المتحقّقون السبعة |
| | **ViewModels/** | نماذج العرض |
| 11.83 | CrudViewModelBaseTests.cs | نماذج العرض فوق خدمة حقيقية |
| 11.84 | CustomersViewModelTests.cs | يثبت أن CustomersViewModel |
| 11.85 | WpfApplicationFixture.cs | تطبيق WPF لخيط STA |

## 12. PrimeERP.Setup

| # | الملف | الوظيفة |
|---|---|---|
| 12.01 | App.xaml | واجهة |
| 12.02 | App.xaml.cs | إقلاع المنصِّب |
| 12.03 | MainWindow.xaml | واجهة |
| 12.04 | MainWindow.xaml.cs | منصِّبٌ صغير |
