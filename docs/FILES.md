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
| 2.15 | BuilderRepository.cs | مستودع ما بناه المستخدم |
| 2.16 | CategoryRepository.cs | مستودع Category |
| 2.17 | ChequeRepository.cs | مستودع Cheque |
| 2.18 | CustomerRepository.cs | طبقة وصول بيانات العملاء |
| 2.19 | CycleDocumentRepositories.cs | مستودع CycleDocument |
| 2.20 | DocumentLinkRepository.cs | مستودع DocumentLink |
| 2.21 | DynamicRepository.cs | مستودع أي جدول بناه المستخدم |
| 2.22 | EditionRepository.cs | اتصالٌ بقاعدة نسخةٍ أخرى |
| 2.23 | EmployeeMovementRepository.cs | البدل والخصم جدولان بشكلٍ واحد |
| 2.24 | EmployeeRepository.cs | مستودع Employee |
| 2.25 | FiscalPeriodRepository.cs | طبقة وصول بيانات السنوات/الفترات المالية |
| 2.26 | IPartyRepository.cs | الشكل المشترك بين ICustomerRepository/ISupplierReposito |
| 2.27 | JournalRepository.cs | طبقة وصول بيانات قيود اليومية |
| 2.28 | LicenseRepository.cs | مستودع License |
| 2.29 | NumberSequenceRepository.cs | طبقة وصول بيانات تسلسل الأرقام |
| 2.30 | PayrollRepository.cs | مستودع Payroll |
| 2.31 | PermissionRepository.cs | مستودع الصلاحيات والأدوار والمستخدمين |
| 2.32 | ProductRepository.cs | مستودع Product |
| 2.33 | PurchaseInvoiceRepository.cs | مستودع PurchaseInvoice |
| 2.34 | PurchaseReturnRepository.cs | مستودع PurchaseReturn |
| 2.35 | SalesInvoiceRepository.cs | مستودع SalesInvoice |
| 2.36 | SalesReturnRepository.cs | مستودع SalesReturn |
| 2.37 | SettingRepository.cs | مستودع AppSettings |
| 2.38 | StockAdjustmentRepositories.cs | مستودع StockIn |
| 2.39 | StockMovementRepository.cs | مستودع StockMovement |
| 2.40 | StockTransferRepository.cs | مستودع StockTransfer |
| 2.41 | SupplierRepository.cs | طبقة وصول بيانات الموردين |
| 2.42 | TreasuryRepository.cs | مستودع Treasury |
| 2.43 | VoucherRepository.cs | مستودع Voucher |
| | **Repositories/Base/** | أسس المستودعات: الصفحة والترتيب والحذف والمستندات |
| 2.44 | CycleDocumentRepositoryBase.cs | أساس مستودع CycleDocument |
| 2.45 | InvoiceRepositoryBase.cs | أساس فاتورة: رأس وسطور |
| 2.46 | LookupRepositoryBase.cs | ما يتقاسمه مستودعات القوائم |
| 2.47 | PartyRepositoryBase.cs | ما يتقاسمه مستودعا العملاء والموردين |
| 2.48 | RepositoryBase.cs | أساس حقيقي بالتوريث لكل Repository |
| 2.49 | ReturnRepositoryBase.cs | أساس مرتجع: رأس وسطور |
| 2.50 | StockAdjustmentRepositoryBase.cs | أساس مستودع StockAdjustment |
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
| 3.22 | Company.cs | كيان Company |
| 3.23 | Currency.cs | كيان Currency |
| 3.24 | Customer.cs | كيان Customer |
| 3.25 | CycleDocument.cs | كيان CycleDocument |
| 3.26 | Department.cs | كيان Department |
| 3.27 | DocumentLink.cs | ربط سحب واحد |
| 3.28 | Employee.cs | كيان Employee |
| 3.29 | EmployeeMovement.cs | البدل والخصم سواء |
| 3.30 | ExchangeRate.cs | كيان ExchangeRate |
| 3.31 | FiscalPeriod.cs | كيان FiscalPeriod |
| 3.32 | FiscalYear.cs | كيان FiscalYear |
| 3.33 | JobTitle.cs | كيان JobTitle |
| 3.34 | JournalEntry.cs | كيان JournalEntry |
| 3.35 | JournalLine.cs | كيان JournalLine |
| 3.36 | License.cs | ترخيص عميل |
| 3.37 | NumberSequence.cs | عدّاد ترقيم لكل مفتاح |
| 3.38 | Payroll.cs | كيان Payroll |
| 3.39 | PayrollLine.cs | استحقاق موظفٍ في مسير |
| 3.40 | Product.cs | كيان Product |
| 3.41 | PurchaseInvoice.cs | كيان PurchaseInvoice |
| 3.42 | PurchaseReturn.cs | كيان PurchaseReturn |
| 3.43 | SalesInvoice.cs | كيان SalesInvoice |
| 3.44 | SalesReturn.cs | كيان SalesReturn |
| 3.45 | Security.cs | دورٌ ومفاتيحه |
| 3.46 | Setting.cs | كيان Setting |
| 3.47 | StockAdjustment.cs | كيان StockAdjustment |
| 3.48 | StockMovement.cs | كيان StockMovement |
| 3.49 | StockTransferDocument.cs | كيان StockTransferDocument |
| 3.50 | Supplier.cs | كيان Supplier |
| 3.51 | Treasury.cs | خزينة/صندوق أو حساب بنكي |
| 3.52 | Unit.cs | كيان Unit |
| 3.53 | User.cs | كيان User |
| 3.54 | Voucher.cs | سند قبض/صرف |
| 3.55 | Warehouse.cs | كيان Warehouse |
| | **Entities/Common/** | أسس الكيانات المشتركة |
| 3.56 | BaseModel.cs | القاعدة المشتركة لكل الكيانات الرئيسية |
| 3.57 | InvoiceBase.cs | ما تتقاسمه فاتورتا البيع والشراء |
| 3.58 | PartyBase.cs | ما يتقاسمه كل طرف |
| 3.59 | ReturnBase.cs | ما يتقاسمه مرتجعا البيع والشراء |
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
| | **DTOs/Assets/** | مرشّحات الأصول وحركاتها |
| 4.04 | AssetDepreciationFilter.cs | مرشّح أقساط الإهلاك |
| 4.05 | AssetDisposalFilter.cs | مرشّح استبعاد الأصول |
| 4.06 | AssetFilter.cs | مرشّح الأصول |
| 4.07 | AssetRevaluationFilter.cs | مرشّح إعادة التقييم |
| | **DTOs/Cheques/** | بيانات الشيكات |
| 4.08 | ChequeDto.cs | بيانات الشيك وحركته |
| | **DTOs/Common/** | بيانات مشتركة: ترخيص ونسخة |
| 4.09 | EditionDto.cs | طلب إنشاء نسخة برنامج |
| 4.10 | LicenseDto.cs | بيانات الترخيص |
| | **DTOs/Documents/** | بيانات مستندات الدورة |
| 4.11 | CycleDocumentDto.cs | بيانات مستند الدورة |
| 4.12 | IPullableLine.cs | سطرٌ قابل للسحب |
| 4.13 | PullLinkFields.cs |  |
| 4.14 | TradeLineDto.cs | سطر فاتورةٍ أو مرتجع |
| | **DTOs/HR/** | بيانات الموارد البشرية |
| 4.15 | EmployeeFilter.cs | مرشّح الموظفين |
| 4.16 | EmployeeMovementDto.cs | بدلٌ أو خصمٌ على موظف |
| 4.17 | PayrollDto.cs | بيانات مسير الرواتب |
| | **DTOs/Inventory/** | بيانات المخزون |
| 4.18 | OpeningStockDto.cs | أرصدة الأصناف الافتتاحية |
| 4.19 | ProductFilter.cs | مرشّح الأصناف |
| 4.20 | StockAdjustmentDto.cs | بيانات إذن المخزون |
| 4.21 | StockTransferDto.cs | بيانات التحويل المخزني |
| | **DTOs/Parties/** | مرشّحا العملاء والموردين |
| 4.22 | CustomerFilter.cs | مرشّح العملاء |
| 4.23 | SupplierFilter.cs | مرشّح الموردين |
| | **DTOs/Purchasing/** | بيانات المشتريات |
| 4.24 | PurchaseInvoiceDto.cs | بيانات فاتورة الشراء |
| 4.25 | PurchaseReturnDto.cs | بيانات مرتجع الشراء |
| | **DTOs/Sales/** | بيانات المبيعات |
| 4.26 | SalesInvoiceDto.cs | بيانات فاتورة البيع |
| 4.27 | SalesReturnDto.cs | بيانات مرتجع البيع |
| | **DTOs/Security/** | بيانات المستخدم ومرشّح الأدوار |
| 4.28 | RoleFilter.cs | مرشّح الأدوار |
| 4.29 | UserDto.cs | بيانات المستخدم |
| | **DTOs/Treasury/** | مرشّح الخزائن والبنوك |
| 4.30 | TreasuryFilter.cs | مرشّح الخزائن |
| | **DTOs/Vouchers/** | بيانات السندات |
| 4.31 | VoucherDto.cs | بيانات السند وتخصيصه |
| | **Legacy/Accounting/** | الحسابات والقيود والفترات المالية |
| 4.32 | AccountService.cs | صفحة شجرة الحسابات |
| 4.33 | FiscalPeriodService.cs | المالك الوحيد لمنطق السنوات/الفترات المالية |
| 4.34 | IAccountService.cs | عقد خدمة الحسابات |
| 4.35 | IFiscalPeriodService.cs | عقد الفترات المالية |
| 4.36 | IJournalService.cs | المالك الوحيد لمنطق قيود اليومية |
| 4.37 | JournalService.cs | صفحة قيود اليومية |
| 4.38 | OpeningBalanceService.cs | الأرصدة الافتتاحية ليست جدولاً موازياً |
| | **Legacy/Admin/** | الإعدادات والتراخيص والنسخ والتحديث |
| 4.39 | ISettingsService.cs | عقد الإعدادات |
| 4.40 | LicenseService.cs | تراخيص العملاء |
| 4.41 | ProgramEditionService.cs | نسخةُ برنامجٍ مستقلّة في مسارٍ |
| 4.42 | SettingsService.cs | طبقة الأعمال فوق ISettingsProvider |
| 4.43 | UpdateService.cs | التحديث سؤالٌ واحد |
| | **Legacy/Assets/** | الأصل: اقتناءً وإهلاكاً وتقييماً واستبعاداً |
| 4.44 | AssetDepreciationService.cs | قسط الإهلاك سجلٌّ مستقلّ |
| 4.45 | AssetDisposalService.cs | بيع الأصل واستبعاده |
| 4.46 | AssetMovementServiceBase.cs | أساس حركات الأصول |
| 4.47 | AssetRevaluationService.cs | إعادة تقييم الأصل |
| 4.48 | AssetService.cs | الأصل: إنشاءً وتعديلاً وبذراً |
| 4.49 | IAssetService.cs | عقد خدمة الأصول |
| | **Legacy/Backup/** | النسخ الاحتياطي والاستعادة |
| 4.50 | BackupInfo.cs | وصف نسخة احتياطية |
| 4.51 | BackupService.cs | المكان الوحيد لأخذ/استعادة/التحقق من النسخ |
| 4.52 | IBackupService.cs | عقد النسخ الاحتياطي |
| | **Legacy/Builder/** | ما يبنيه المستخدم من شاشات |
| 4.53 | BuilderCatalogService.cs | وصفُ ما بناه المستخدم كما |
| 4.54 | BuilderCrudServices.cs | خدمة صفوف |
| 4.55 | DynamicEntityService.cs | خدمة أي جدول بناه المستخدم |
| | **Legacy/Cheques/** | دورة الشيك |
| 4.56 | ChequeDocumentService.cs | مستند "استلام/صرف شيكات" |
| 4.57 | ChequeService.cs | دورة الشيك كاملة |
| | **Legacy/Common/** | الفئات وحساباتها |
| 4.58 | CategoryService.cs | فئات الوحدات وحساباتها |
| 4.59 | ICategoryService.cs | عقد خدمة الفئات |
| | **Legacy/Documents/** | مستندات الدورة وروابط السحب |
| 4.60 | CycleDocumentServiceBase.cs | مستندات الدورة: أساسٌ وستّ خدمات |
| | **Legacy/HR/** | الموظفون والرواتب والحضور |
| 4.61 | AttendanceService.cs | الحضور والانصراف |
| 4.62 | EmployeeMovementService.cs | البدل والخصم خدمةٌ واحدة بجدولين |
| 4.63 | EmployeeService.cs | خدمة الموظفين |
| 4.64 | IEmployeeService.cs | عقد الموظفين |
| 4.65 | IPayrollService.cs | عقد مسير الرواتب |
| 4.66 | PayrollService.cs | مسير الرواتب |
| | **Legacy/Inventory/** | الأصناف والمخازن والأرصدة |
| 4.67 | IProductService.cs | عقد الأصناف |
| 4.68 | IStockInService.cs | عقود أذون المخزون الستة |
| 4.69 | IStockTransferService.cs | عقد التحويل المخزني |
| 4.70 | OpeningStockService.cs | رصيد أول المدة للأصناف |
| 4.71 | ProductService.cs | خدمة الأصناف |
| 4.72 | StockAdjustmentServiceBase.cs | خدمةٌ بوّابتها مفتاحٌ واحد مُعلَن |
| 4.73 | StockTransferService.cs | التحويل بين المخازن |
| | **Legacy/Parties/** | العملاء والموردون |
| 4.74 | CustomerService.cs | المالك الوحيد لمنطق العملاء |
| 4.75 | ICustomerService.cs | المالك الوحيد لمنطق العملاء |
| 4.76 | ISupplierService.cs | المالك الوحيد لمنطق الموردين |
| 4.77 | PartyServiceBase.cs | المنطق المشترك بين العملاء والموردين |
| 4.78 | SupplierService.cs | المالك الوحيد لمنطق الموردين |
| | **Legacy/Print/** | بناء المستندات المطبوعة |
| 4.79 | ChequePrinter.cs | الشيك يُطبَع على ورق مطبوع |
| 4.80 | Code128.cs | ترميز Code128-B |
| 4.81 | CompanyHeaderComponent.cs | ترويسة الشركة كقطعة واحدة تُستدعى |
| 4.82 | IPrintDialogHost.cs | تنفّذه طبقة الواجهة |
| 4.83 | IPrintService.cs | عقد الطباعة |
| 4.84 | ImageData.cs | تحويل صورة ↔ Base64 |
| 4.85 | PaperNodeRenderer.cs | مترجم PaperNode إلى عناصر WPF |
| 4.86 | PaperTheme.cs | قيم الورق كلها من PrintTheme.xaml |
| 4.87 | PrintService.cs | يبني مستندات الطباعة من IPrintable |
| | **Legacy/Purchasing/** | فواتير الشراء ومرتجعاتها |
| 4.88 | IPurchaseInvoiceService.cs | عقد فاتورة الشراء |
| 4.89 | IPurchaseReturnService.cs | عقد مرتجع الشراء |
| 4.90 | PurchaseInvoiceService.cs | فاتورة الشراء وقيدها |
| 4.91 | PurchaseReturnService.cs | مرتجع الشراء وقيده |
| | **Legacy/Sales/** | فواتير البيع ومرتجعاتها |
| 4.92 | ISalesInvoiceService.cs | عقد فاتورة البيع |
| 4.93 | ISalesReturnService.cs | عقد مرتجع البيع |
| 4.94 | SalesInvoiceService.cs | فاتورة البيع وقيدها |
| 4.95 | SalesReturnService.cs | مرتجع البيع وقيده |
| | **Legacy/Security/** | المستخدمون والأدوار |
| 4.96 | IRoleService.cs | عقد الأدوار |
| 4.97 | IUserService.cs | عقد المستخدمين |
| 4.98 | RoleService.cs | الأدوار وصلاحياتها |
| 4.99 | UserService.cs | المستخدمون وكلمات مرورهم |
| | **Legacy/Treasury/** | الخزائن والبنوك |
| 4.100 | ITreasuryService.cs | عقد الخزائن والبنوك |
| 4.101 | TreasuryService.cs | الخزائن والبنوك وحساباتها |
| | **Legacy/Vouchers/** | سندات القبض والصرف |
| 4.102 | VoucherServiceBase.cs | سند قبض/صرف |
| | **Reporting/** | خدمات التقارير |
| 4.103 | AssetReportService.cs | تقريرا الأصول |
| 4.104 | BuilderReportService.cs | تقرير مبنيّ |
| 4.105 | FinancialStatementFactory.cs | القوائم المالية بشكلها الرسمي |
| 4.106 | FinancialStatementService.cs | القوائم المالية الثلاث |
| 4.107 | PartyReportService.cs | أرصدة الأطراف وكشوفها |
| 4.108 | PayslipReportService.cs | قسيمة راتب موظف |
| 4.109 | ReportData.cs | مخرَج التقرير |
| 4.110 | ReportRows.cs | ميزان المراجعة القياسي |
| 4.111 | ReportServiceBase.cs | ما يتقاسمه كل تقرير |
| 4.112 | SalesReportService.cs | تقرير المبيعات |
| 4.113 | StockReportService.cs | تقارير المخزون |
| | **Services/Core/** | أساس كل خدمة: الصلاحية والمعاملة والكيان والترقيم |
| 4.114 | CrudServiceBase.cs | القراءة العامة لكيان بصفحات وبحث |
| 4.115 | EntityService.cs | إضافة الكيان وتعديله وحذفه |
| 4.116 | INumberSequenceService.cs | عقد الترقيم التسلسلي |
| 4.117 | NumberSequenceService.cs | أرقام متسلسلة لكل مفتاح |
| 4.118 | ServiceBase.cs | القاعدة المشتركة لكل خدمة |
| | **Services/Documents/** | المستند والسحب وحركة المخزون وتغيير الحالة وسطور التجارة |
| 4.119 | DocumentPull.cs | تتبّع السحب بين المستندات |
| 4.120 | DocumentService.cs | المستند: قراءةً وإنشاءً واستبدالاً وحذفاً |
| 4.121 | IStockMove.cs | عقد أرصدة المخزون |
| 4.122 | ProductLines.cs | سطور المستند بأصنافها |
| 4.123 | StatusChange.cs | الحالة كترحيل |
| 4.124 | StockMove.cs | أرصدة المخزون وحركته |
| 4.125 | TradeAccounts.cs | حسابات البيع والشراء |
| 4.126 | TradeLines.cs | سطور الفواتير والمرتجعات ومجاميعها |
| | **Services/Entities/** | الكيان من إعداده: القائمة البسيطة وتحويل الصفّ |
| 4.127 | ByCode.cs | سطورٌ تُحلّ بكود كيانها |
| 4.128 | EntitySpec.cs | إعلان صفحة كيان |
| 4.129 | Lookup.cs | صفحة قائمةٍ من إعلانها |
| 4.130 | Rows.cs | الكيان صفّاً والصفّ كياناً |
| 4.131 | Tree.cs | الشجرة من صفوفٍ مسطّحة |
| | **Services/Ledger/** | قلب القيد، أرصدة الحسابات، الفترة، القيد بطرفيه والتجارة، الحرّاس، رصيد الطرف |
| 4.132 | AccountBalances.cs | أرصدة الحسابات من قيودها |
| 4.133 | Entries.cs | قلب القيد لكل مستدعٍ |
| 4.134 | Guards.cs | حرّاس الشجرة والنقدية |
| 4.135 | JournalLines.cs | سطور قيدٍ بطرفيها |
| 4.136 | OpeningEntry.cs | القيد الافتتاحي متعدّد الأسطر |
| 4.137 | PartyBalance.cs | رصيد الطرف من حسابه |
| 4.138 | PartyByKind.cs | الطرف بنوعه |
| 4.139 | PeriodGate.cs | التاريخ في فترةٍ مفتوحة |
| 4.140 | Posting.cs | القيد يُنشأ مُرحَّلاً ويُعكس بحذفه |
| 4.141 | Statement.cs | كشف الحساب برصيده الجاري |
| 4.142 | TradeEntry.cs | قيد البيع والشراء وعكسهما |
| 4.143 | TrialBalance.cs | ميزان المراجعة لفترة |
| 4.144 | TwoSided.cs | طرفا القيد باتجاهه |
| | **Services/Ledger/Accounts/** | الحساب: في الشجرة، للكيان، بالاتجاهين، ومع مجمّعه |
| 4.145 | AccountCases.cs | حالات الحساب التي يستدعيها الكيان |
| 4.146 | AccountOf.cs | حساب الكيان أو الإعداد |
| 4.147 | AccountSpec.cs | حساب كيانٍ بمعاملاته |
| 4.148 | AddEntityAccount.cs | إضافة حساب لكيان صفحة |
| 4.149 | AddLinkedAccount.cs | حسابٌ في الشجرة ينشئ كيانه |
| 4.150 | AddMirroredAccount.cs | حسابٌ ومجمّعه لأي كيان |
| 4.151 | AddTreeAccount.cs | إضافة حساب في الشجرة |
| 4.152 | CloseAccount.cs | حذف الحساب وإعادة أبيه ورقياً |
| 4.153 | CloseLinkedAccount.cs | حذف الحساب وكيانه |
| 4.154 | EditLinkedAccount.cs | تعديل الحساب واسم كيانه |
| 4.155 | EditTreeAccount.cs | تعديل حساب في الشجرة |
| 4.156 | IAccountLinkedService.cs | كيان يعيش ورقةً في شجرة |
| 4.157 | LinkedAccounts.cs | كيان الجذر المرتبط |
| 4.158 | RenameAccount.cs | تسمية الحساب |
| 4.159 | SettingAccounts.cs | حسابات الإعدادات |
| | **Validation/** | دالة التحقق الوحيدة Check بشروطٍ معاملاتٍ في Field، وسطور المستند |
| 4.160 | Check.cs | الدالة الوحيدة للتحقق |
| 4.161 | DocumentLines.cs | سطور المستند بشروط Check |
| 4.162 | Field.cs | شرطُ حقلٍ بكل معاملاته |
| 4.163 | ValidationResult.cs | عقد ValidationResult |

## 5. 5.Design

| # | الملف | الوظيفة |
|---|---|---|
| 5.01 | Colors.xaml | ألوان النظام |
| 5.02 | Sizes.xaml | مقاسات النظام |
| 5.03 | Theme.xaml | دمج طبقات التصميم |
| | **Icons/** | الأيقونات |
| 5.04 | Icons.xaml | الأيقونات |
| | **Strings/** | النصوص الظاهرة بالعربية والإنجليزية |
| 5.05 | Strings.ar.xaml | النصوص الظاهرة |
| 5.06 | Strings.en.xaml | النصوص الظاهرة |
| | **Styles/** | أنماط عناصر WPF |
| 5.07 | Implicit.xaml | واجهة |
| 5.08 | ScrollBars.xaml | واجهة |
| 5.09 | Style.Button.xaml | نمط عنصر WPF |
| 5.10 | Style.Dialog.xaml | نمط عنصر WPF |
| 5.11 | Style.Input.xaml | نمط عنصر WPF |
| | **Surfaces/** | ألوان الورق والتصدير، خارج موارد الشاشة |
| 5.12 | ExportTheme.cs | ألوان ملفات Excel/CSV/PDF |
| 5.13 | PrintTheme.xaml | ألوان الطباعة |

## 6. 6.UI

| # | الملف | الوظيفة |
|---|---|---|
| | **Components/** | القطع المرئية بأقسامها |
| 6.01 | VisualTree.cs | البحث في الشجرة المرئية |
| | **Components/Actions/** | الأزرار وشريطها |
| 6.02 | ActionToolbar.xaml | واجهة |
| 6.03 | ActionToolbar.xaml.cs | شريط أدوات ببناء برمجي (ButtonsSource) |
| 6.04 | AppButton.xaml | واجهة |
| 6.05 | AppButton.xaml.cs | أزرار AppButton |
| 6.06 | AppDropdownButton.xaml | واجهة |
| 6.07 | AppDropdownButton.xaml.cs | أزرار AppDropdownButton |
| 6.08 | AppIconButton.xaml | واجهة |
| 6.09 | AppIconButton.xaml.cs | أزرار AppIconButton |
| 6.10 | PermissionButton.xaml | واجهة |
| 6.11 | PermissionButton.xaml.cs | زرّ محكوم بالصلاحية |
| 6.12 | ToolbarAction.cs | تعريف زر شريط أدوات |
| | **Components/Display/** | الجدول والترقيم والبطاقة |
| 6.13 | AppBadge.xaml | واجهة |
| 6.14 | AppBadge.xaml.cs | عرض AppBadge |
| 6.15 | AppBreadcrumb.xaml | واجهة |
| 6.16 | AppBreadcrumb.xaml.cs | عرض AppBreadcrumb |
| 6.17 | AppCard.xaml | واجهة |
| 6.18 | AppCard.xaml.cs | عرض AppCard |
| 6.19 | AppDataGrid.xaml | واجهة |
| 6.20 | AppDataGrid.xaml.cs | الشبكة: أعمدةٌ وترقيمٌ وتحديد |
| 6.21 | AppEmptyState.xaml | واجهة |
| 6.22 | AppEmptyState.xaml.cs | عرض AppEmptyState |
| 6.23 | AppIcon.xaml | واجهة |
| 6.24 | AppIcon.xaml.cs | أيقونة بمواصفة واحدة |
| 6.25 | AppLoadingOverlay.xaml | واجهة |
| 6.26 | AppLoadingOverlay.xaml.cs | عرض AppLoadingOverlay |
| 6.27 | AppPagination.xaml | واجهة |
| 6.28 | AppPagination.xaml.cs | عرض AppPagination |
| 6.29 | AppStatCard.xaml | واجهة |
| 6.30 | AppStatCard.xaml.cs | عرض AppStatCard |
| 6.31 | AppTabControl.xaml | واجهة |
| 6.32 | AppTabControl.xaml.cs | عرض AppTabControl |
| 6.33 | AppTabItem.cs | عرض AppTabItem |
| 6.34 | AppTreeView.xaml | واجهة |
| 6.35 | AppTreeView.xaml.cs | شجرة مربوطة على TreeNodeViewModel.VisibleChildren |
| 6.36 | GridColumn.cs | تعريف عمود AppDataGrid |
| | **Components/Documents/** | محرر سطور المستند |
| 6.37 | DocumentFooter.xaml | واجهة |
| 6.38 | DocumentFooter.xaml.cs | تذييل مستند عام |
| 6.39 | DocumentLine.cs | سطر مستند مرن |
| 6.40 | DocumentLinesGrid.Clipboard.cs | شبكة سطور مستند |
| 6.41 | DocumentLinesGrid.Keys.cs | شبكة سطور مستند |
| 6.42 | DocumentLinesGrid.Pickers.cs | شبكة سطور مستند |
| 6.43 | DocumentLinesGrid.Rows.cs | شبكة سطور مستند |
| 6.44 | DocumentLinesGrid.xaml | واجهة |
| 6.45 | DocumentLinesGrid.xaml.cs | شبكة سطور مستند عامة |
| 6.46 | FooterTotal.cs | عنصر إجمالي واحد في DocumentFooter |
| 6.47 | LineCellTemplateSelector.cs | يختار قالب الخلية |
| 6.48 | LineColumn.cs | تعريف عمود في DocumentLinesGrid |
| 6.49 | LineColumnPresets.cs | تعريفات أعمدة جاهزة لأنماط المستندات |
| 6.50 | LineComputeEngine.cs | محرك حساب أعمدة السطر |
| 6.51 | LineValidationEngine.cs | يتحقق من سطر مستند واحد |
| | **Components/Feedback/** | الحوارات والتنبيهات |
| 6.52 | AppConfirmDialog.cs | حوارات وتنبيهات AppConfirmDialog |
| 6.53 | AppDialogWindow.xaml | واجهة |
| 6.54 | AppDialogWindow.xaml.cs | القاعدة الموحّدة لكل نوافذ الحوار |
| 6.55 | AppMessageDialog.cs | حوارات وتنبيهات AppMessageDialog |
| 6.56 | AppProgressDialog.cs | حوارات وتنبيهات AppProgressDialog |
| 6.57 | AppToast.xaml | واجهة |
| 6.58 | AppToast.xaml.cs | حوارات وتنبيهات AppToast |
| | **Components/Inputs/** | حقول الإدخال |
| 6.59 | AppCheckBox.xaml | واجهة |
| 6.60 | AppCheckBox.xaml.cs | حقل إدخال AppCheckBox |
| 6.61 | AppComboBox.xaml | واجهة |
| 6.62 | AppComboBox.xaml.cs | حقل إدخال AppComboBox |
| 6.63 | AppDatePicker.xaml | واجهة |
| 6.64 | AppDatePicker.xaml.cs | حقل إدخال AppDatePicker |
| 6.65 | AppImagePicker.cs | حقل صورة قيمته نص Base64 |
| 6.66 | AppNumericBox.xaml | واجهة |
| 6.67 | AppNumericBox.xaml.cs | حقل إدخال AppNumericBox |
| 6.68 | AppPasswordBox.xaml | واجهة |
| 6.69 | AppPasswordBox.xaml.cs | نفس بنية AppTextBox بالضبط |
| 6.70 | AppSearchBox.xaml | واجهة |
| 6.71 | AppSearchBox.xaml.cs | حقل إدخال AppSearchBox |
| 6.72 | AppTextArea.xaml | واجهة |
| 6.73 | AppTextArea.xaml.cs | حقل إدخال AppTextArea |
| 6.74 | AppTextBox.xaml | واجهة |
| 6.75 | AppTextBox.xaml.cs | حقل إدخال AppTextBox |
| 6.76 | AppToggleSwitch.xaml | واجهة |
| 6.77 | AppToggleSwitch.xaml.cs | حقل إدخال AppToggleSwitch |
| | **Components/Layout/** | ترويسة الصفحة وشريط الفلاتر |
| 6.78 | FilterBar.xaml | واجهة |
| 6.79 | FilterBar.xaml.cs | تخطيط FilterBar |
| 6.80 | PageHeader.xaml | واجهة |
| 6.81 | PageHeader.xaml.cs | تخطيط PageHeader |
| | **Components/Pickers/** | نوافذ الاختيار |
| 6.82 | AccountPicker.cs | اختيار حساب من شجرة الحسابات |
| 6.83 | CustomerPicker.cs | اختيار عميل بجدول بحث |
| 6.84 | EmployeePicker.cs | اختيار موظف بجدول بحث |
| 6.85 | IPickerDataSource.cs | مصدر بيانات لأي Picker |
| 6.86 | PickerBase.cs | الطبقة المعمَّمة فوق PickerBaseControl |
| 6.87 | PickerBaseControl.xaml | واجهة |
| 6.88 | PickerBaseControl.xaml.cs | القاعدة غير المعمَّمة لكل Picker |
| 6.89 | PickerGridWindow.cs | نافذة اختيار بجدول مشتركة لكل |
| 6.90 | PickerResultItem.cs | تمثيل موحّد وغير معمَّم لأي |
| 6.91 | PickerTreeWindow.cs | نافذة اختيار بشجرة مشتركة |
| 6.92 | PickerWindowGeometry.cs | يتذكر آخر حجم/موضع لكل نافذة |
| 6.93 | ProductPicker.cs | اختيار صنف بجدول بحث |
| 6.94 | SupplierPicker.cs | اختيار مورد بجدول بحث |
| | **Components/Shell/** | الشريط الجانبي والعلوي |
| 6.95 | AppShell.xaml | واجهة |
| 6.96 | AppShell.xaml.cs | القطعة الجامعة |
| 6.97 | AppSidebar.xaml | واجهة |
| 6.98 | AppSidebar.xaml.cs | شريط تنقّل جانبي هرمي |
| 6.99 | AppTopBar.xaml | واجهة |
| 6.100 | AppTopBar.xaml.cs | شريط علوي عام |
| 6.101 | NavItem.cs | عنصر تنقّل واحد في AppSidebar |
| 6.102 | NavItemViewModel.cs | حالة عنصر التنقّل |
| | **Components/Tree/** | شجرة الحسابات وعقدها |
| 6.103 | NodeCheckState.cs | حالة تحديد عقدة في شجرة |
| 6.104 | TreeFilterEngine.cs | فلترة إخفاء حقيقية على شجرة |
| 6.105 | TreeLayoutOptions.cs | خيارات تخطيط الشجرة |
| 6.106 | TreeNodeViewModel.cs | عقدة شجرة قابلة للمراقبة |
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
| 6.129 | BaseViewModel.cs | نماذج عرض BaseViewModel |
| 6.130 | CategoryListViewModel.cs | نماذج عرض CategoryFilter |
| 6.131 | CycleDocumentViewModels.cs | نماذج عرض CycleDocumentViewModelBase |
| 6.132 | CycleVoucherViewModels.cs | نماذج عرض CycleVoucherViewModelBase |
| 6.133 | DocumentViewModels.cs | نماذج عرض JournalsViewModel |
| 6.134 | DynamicViewModel.cs | نموذج عرض أي شاشة صفوفها |
| 6.135 | HrMovementViewModels.cs | البدل والخصم شاشةٌ واحدة بخدمتين |
| 6.136 | ListViewModels.cs | نماذج عرض UnitsViewModel |
| 6.137 | LookupViewModels.cs | نماذج عرض CategoriesLookupViewModel |
| 6.138 | TreeViewModelBase.cs | نماذج عرض TreeViewModelBase |
| 6.139 | VoucherViewModels.cs | نماذج عرض السندات والشيكات |
| | **ViewModels/Base/** | أسس نماذج العرض: صفحة وCRUD وصلاحية |
| 6.140 | CrudViewModelBase.cs | يضيف على PagedViewModelBase حذف عام |
| 6.141 | PagedViewModelBase.cs | صفحات+بحث عامان لأي كيان |
| 6.142 | PermissionAwareViewModel.cs | قاعدة لأي ViewModel يحتاج التحقق |

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
| 9.01 | LoginWindow.xaml | واجهة |
| 9.02 | LoginWindow.xaml.cs | أول نافذة حقيقية |
| 9.03 | MainWindow.xaml | واجهة |
| 9.04 | MainWindow.xaml.cs | القطعة الوحيدة هنا AppShell |
| | **Bootstrap/** | تسجيل كل الخدمات وتهيئة القاعدة |
| 9.05 | DependencyInjection.cs | تسجيل الخدمات في الحاوية |

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
| 11.01 | AssemblyInfo.cs |  |
| 11.02 | StaThreadHelper.cs | أنواع WPF تفرض خيط STA |
| 11.03 | TestDatabaseFixture.cs | ملف SQLite مؤقت مستقل لكل |
| 11.04 | WpfApplicationFixture.cs | تطبيق WPF لخيط STA |
| | **Architecture/** | حدود الطبقات واكتمال الوحدات |
| 11.05 | LayerBoundaryTests.cs | حدود الطبقات على IL المُصرَّف |
| | **Composition/** | تصيير الشاشات الحقيقية |
| 11.06 | AddButtonEnabledTests.cs | زرّ «جديد» المُصيَّر فعلياً |
| 11.07 | CategoryPickerTests.cs | قائمة الفئات في الحوار |
| 11.08 | ChequeDocumentPartyTests.cs | مستند الشيكات دفعةُ إدخال |
| 11.09 | CrudPageRendererTests.cs | صفحة CRUD حقيقية من تعريفها |
| 11.10 | CustomerDialogTests.cs | حوار العميل من الشاشة الحقيقية |
| 11.11 | DialogRendererTests.cs | تصيير الحوار من تعريفه |
| 11.12 | DocumentColumnParityTests.cs | كل حقل مُدخَل يصل الورق |
| 11.13 | DocumentRendererTests.cs | تصيير محرّر المستند |
| 11.14 | FlowScopeTests.cs | نطاق الدورة |
| 11.15 | LineMathCheck.cs | صافي السطر يُحسب حيّاً |
| 11.16 | LookupPagesRenderTests.cs | القوائم المرجعية تُفتح فعلاً |
| 11.17 | MasterListFilterTests.cs | فلترة القائمة الرئيسية |
| 11.18 | ModuleCompletenessTests.cs | حارس اكتمال الشاشة |
| 11.19 | NavigationGroupsTests.cs | مفاتيح الشريط الجانبي |
| 11.20 | PermissionFlowTests.cs | دورة الصلاحيات كما يعيشها المستخدم |
| 11.21 | PermissionRequirementsTests.cs | كل ما طُلب لشجرة الصلاحيات |
| 11.22 | PermissionScreenTests.cs | الشاشة نفسها لا التعريف |
| 11.23 | PermissionScreensTests.cs | شاشتا الصلاحيات تكوين فوق TreeCheckListRenderer |
| 11.24 | PrintDocumentsTests.cs | عائلتا المستندات تبنيان من مصدر |
| 11.25 | PrintPaperTests.cs | خيارات الورق |
| 11.26 | ProductDialogTests.cs | حوار الصنف |
| 11.27 | QuotationDocumentTests.cs | حفظ عرض سعر من الشاشة |
| 11.28 | ReorderButtonsTests.cs | زرّ الشريط يُبنى بأيقونته |
| 11.29 | ReportRendererTests.cs | تصيير التقرير |
| 11.30 | SettingsPageRendererTests.cs | صفحة الإعدادات |
| 11.31 | SupplierDialogTests.cs | حوار المورد |
| 11.32 | ToastCloseTests.cs | إغلاق التنبيه |
| 11.33 | ToolbarEnabledTests.cs | أزرار الشريط |
| 11.34 | ToolbarLevelsTests.cs | مستويا الإجراءات |
| 11.35 | TreasuryDialogTests.cs | إضافة بنك من الشاشة الحقيقية |
| 11.36 | TreeRendererTests.cs | تصيير الشجرة |
| 11.37 | VoucherTreasuryPickerTests.cs | قائمة الخزينة/البنك تتبع طريقة الدفع |
| | **Design/** | الألوان والرموز والنصوص |
| 11.38 | DesignTokenResolutionTests.cs | رموز التصميم تُحلّ لألوان مرئية |
| | **Documents/** | المستندات ودوراتها |
| 11.39 | LineEngineTests.cs | محرّك سطور المستند |
| | **Helpers/** | أدوات الاختبار |
| 11.40 | ArabicNumberToWordsTests.cs | تفقيط الأرقام بالعربية |
| 11.41 | Localized.cs | رسالةٌ من القاموس |
| 11.42 | LookupRows.cs | صفٌّ في صفحة قائمة |
| | **Services/** | الخدمات ومنطقها |
| 11.43 | AccountLeafStateTests.cs | الحساب إمّا أب وإمّا يقبل |
| 11.44 | AccountServiceTests.cs | قاعدة معزولة لكل اختبار |
| 11.45 | BackupServiceTests.cs | النسخ الاحتياطي والاستعادة |
| 11.46 | BuilderCatalogTests.cs | وصف ما بناه المستخدم |
| 11.47 | CategoryServiceTests.cs | الفئات وحساباتها |
| 11.48 | CustomerServiceTests.cs | قاعدة بيانات خاصة معزولة لكل |
| 11.49 | CycleDocumentServiceTests.cs | مستندات الدورة |
| 11.50 | CycleVoucherServiceTests.cs | سندات الدورة |
| 11.51 | DocumentLinkServiceTests.cs | تتبّع السحب |
| 11.52 | EmployeeListTests.cs | القائمة تحمل اسمَي القسم والوظيفة |
| 11.53 | ExportServiceTests.cs | تصدير CSV وExcel وPDF فعلي |
| 11.54 | FiscalPeriodServiceTests.cs | قاعدة معزولة لكل اختبار |
| 11.55 | FlowModeStockTests.cs | الفرق بين الوضعين |
| 11.56 | InventoryCostingTests.cs | المتوسط المرجَّح المتحرّك |
| 11.57 | ItemCardReportTests.cs | تقرير حركة الصنف بالمتوسط المرجَّح |
| 11.58 | JournalServiceTests.cs | قاعدة بيانات خاصة معزولة لكل |
| 11.59 | JournalToStatementTests.cs | المعاملات كلها تصبّ في القيود |
| 11.60 | NavigationServiceTests.cs | التنقّل بين الشاشات |
| 11.61 | NegativeBalanceGuardTests.cs | المخزن والخزينة والبنك لا يقبلون |
| 11.62 | NumberSequenceServiceTests.cs | الترقيم التسلسلي |
| 11.63 | OpeningBalanceAndDepreciationTests.cs | الأرصدة الافتتاحية والإهلاك |
| 11.64 | PayrollServiceTests.cs | المسير يُنشأ مسوّدةً ثم يُرحَّل |
| 11.65 | PermissionServiceTests.cs | فحص الصلاحيات وتحميلها |
| 11.66 | PrintFormattingTests.cs | الشعار ومحاذاة الجدول في الورق |
| 11.67 | PrintServiceTests.cs | الطباعة |
| 11.68 | PullServiceTests.cs | سلوك السحب كما طُلب حرفياً |
| 11.69 | PurchaseInvoiceServiceTests.cs | فاتورة الشراء وقيدها |
| 11.70 | ReportsGenerateTests.cs | توليد التقارير |
| 11.71 | ReturnsServiceTests.cs | المرتجعات |
| 11.72 | SalesInvoiceServiceTests.cs | فاتورة البيع وقيدها |
| 11.73 | SettingsServiceTests.cs | الإعدادات |
| 11.74 | SoftDeleteTests.cs | المحذوف منطقياً خارج القراءة |
| 11.75 | StatementGroupingTests.cs | مستوى التجميع في القائمة |
| 11.76 | StockDocumentsServiceTests.cs | أذون المخزون |
| 11.77 | StockServiceTests.cs | أرصدة المخزون |
| 11.78 | TreasurySeedTests.cs | قوائم السندات كانت تبدو "لا |
| 11.79 | VoucherAndChequeTests.cs | السندات والشيكات |
| 11.80 | VoucherPrintTests.cs | سند القبض والصرف |
| | **Services/Design/** | التصميم والنصوص |
| 11.81 | IdentityServiceTests.cs | تحميل موارد التصميم |
| 11.82 | LocalizationServiceTests.cs | تبديل قاموس النصوص الحقيقي |
| | **Validation/** | قواعد التحقق |
| 11.83 | ValidatorsTests.cs | المتحقّقون السبعة |
| | **ViewModels/** | نماذج العرض |
| 11.84 | CrudViewModelBaseTests.cs | نماذج العرض فوق خدمة حقيقية |
| 11.85 | CustomersViewModelTests.cs | يثبت أن CustomersViewModel |

## 12. PrimeERP.Setup

| # | الملف | الوظيفة |
|---|---|---|
| 12.01 | App.xaml | واجهة |
| 12.02 | App.xaml.cs | إقلاع المنصِّب |
| 12.03 | MainWindow.xaml | واجهة |
| 12.04 | MainWindow.xaml.cs | منصِّبٌ صغير |
