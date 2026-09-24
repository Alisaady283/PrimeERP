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
| 2.04 | PrimeDbContext.cs | نموذج EF مولَّد |
| 2.05 | SchemaSync.cs | إلحاق ناقص النموذج بالقاعدة |
| | **Repositories/** | مستودع لكل جدول: SQL فقط |
| 2.06 | AccountRepository.cs | طبقة وصول بيانات شجرة الحسابات |
| 2.07 | AssetDepreciationRepository.cs | مستودع AssetDepreciation |
| 2.08 | AssetDisposalRepository.cs | مستودع AssetDisposal |
| 2.09 | AssetRepository.cs | مستودع Asset |
| 2.10 | AssetRevaluationRepository.cs | مستودع AssetRevaluation |
| 2.11 | AttendanceRepository.cs | مستودع Attendance |
| 2.12 | AuditRepository.cs | مستودع AuditLog |
| 2.13 | BackupRepository.cs | طبقة وصول بيانات سجل النسخ |
| | **Repositories/Base/** | أسس المستودعات: الصفحة والترتيب والحذف والمستندات |
| 2.14 | CycleDocumentRepositoryBase.cs | أساس مستودع CycleDocument |
| 2.15 | InvoiceRepositoryBase.cs | أساس فاتورة: رأس وسطور |
| 2.16 | LookupRepositoryBase.cs | ما يتقاسمه مستودعات القوائم |
| 2.17 | PartyRepositoryBase.cs | ما يتقاسمه مستودعا العملاء والموردين |
| 2.18 | RepositoryBase.cs | أساس حقيقي بالتوريث لكل Repository |
| 2.19 | ReturnRepositoryBase.cs | أساس مرتجع: رأس وسطور |
| 2.20 | StockAdjustmentRepositoryBase.cs | أساس مستودع StockAdjustment |
| | **Repositories/** | مستودع لكل جدول: SQL فقط |
| 2.21 | BuilderRepository.cs | مستودع ما بناه المستخدم |
| 2.22 | CategoryRepository.cs | مستودع Category |
| 2.23 | ChequeRepository.cs | مستودع Cheque |
| 2.24 | CustomerRepository.cs | طبقة وصول بيانات العملاء |
| 2.25 | CycleDocumentRepositories.cs | مستودع CycleDocument |
| 2.26 | DocumentLinkRepository.cs | مستودع DocumentLink |
| 2.27 | DynamicRepository.cs | مستودع أي جدول بناه المستخدم |
| 2.28 | EditionRepository.cs | اتصالٌ بقاعدة نسخةٍ أخرى |
| 2.29 | EmployeeMovementRepository.cs | البدل والخصم جدولان بشكلٍ واحد |
| 2.30 | EmployeeRepository.cs | مستودع Employee |
| 2.31 | FiscalPeriodRepository.cs | طبقة وصول بيانات السنوات/الفترات المالية |
| 2.32 | IPartyRepository.cs | الشكل المشترك بين ICustomerRepository/ISupplierReposito |
| 2.33 | JournalRepository.cs | طبقة وصول بيانات قيود اليومية |
| 2.34 | LicenseRepository.cs | مستودع License |
| 2.35 | NumberSequenceRepository.cs | طبقة وصول بيانات تسلسل الأرقام |
| 2.36 | PayrollRepository.cs | مستودع Payroll |
| 2.37 | PermissionRepository.cs | مستودع الصلاحيات والأدوار والمستخدمين |
| 2.38 | ProductRepository.cs | مستودع Product |
| 2.39 | PurchaseInvoiceRepository.cs | مستودع PurchaseInvoice |
| 2.40 | PurchaseReturnRepository.cs | مستودع PurchaseReturn |
| 2.41 | SalesInvoiceRepository.cs | مستودع SalesInvoice |
| 2.42 | SalesReturnRepository.cs | مستودع SalesReturn |
| 2.43 | SettingRepository.cs | مستودع AppSettings |
| 2.44 | StockAdjustmentRepositories.cs | مستودع StockIn |
| 2.45 | StockMovementRepository.cs | مستودع StockMovement |
| 2.46 | StockTransferRepository.cs | مستودع StockTransfer |
| 2.47 | SupplierRepository.cs | طبقة وصول بيانات الموردين |
| 2.48 | TreasuryRepository.cs | مستودع Treasury |
| 2.49 | VoucherRepository.cs | مستودع Voucher |
| | **Seeders/** | بذر عدّادات الترقيم |
| 2.50 | NumberSequenceSeeder.cs | يزرع تسلسلات الأرقام ذات بادئة |
| 2.51 | PermissionSeeder.cs | زرع المفاتيح ودور مدير النظام |
| 2.52 | SettingSeeder.cs | زرع المفاتيح الافتراضية بلا استبدال |

## 3. 3.Domain

| # | الملف | الوظيفة |
|---|---|---|
| | **Contracts/** | العقود ومفرداتها: تحقق وطباعة وتصدير وسحب |
| 3.01 | IDocumentExporter.cs | عقد تصدير مستند قابل للطباعة |
| 3.02 | IPrintable.cs | أي مستند قابل للطباعة ينفّذ |
| 3.03 | IPullSourceReader.cs | يقرأ كمية سطر مستند مصدر |
| 3.04 | IValidator.cs | عقد Validator |
| 3.05 | PaperNode.cs | قطعة ورق كبنية مجرّدة |
| 3.06 | ValidationResult.cs | عقد ValidationResult |
| 3.07 | ValidatorBase.cs | قواعد تحقق عامة مشتركة |
| | **Entities/** | كيانات صرفة: خصائص فقط بلا سلوك |
| 3.08 | Account.cs | كيان Account |
| 3.09 | Asset.cs | كيان Asset |
| 3.10 | AssetDepreciation.cs | قسط إهلاكٍ واحد |
| 3.11 | AssetDisposal.cs | بيع أصل واستبعاده |
| 3.12 | AssetRevaluation.cs | إعادة تقييم أصل |
| 3.13 | BackupHistoryRecord.cs | سجل نسخة احتياطية |
| 3.14 | Builder.cs | ما يتقاسمه أبناء الوحدة |
| 3.15 | Category.cs | كيان Category |
| 3.16 | Cheque.cs | شيك وارد أو صادر |
| | **Entities/Common/** | أسس الكيانات المشتركة |
| 3.17 | BaseModel.cs | القاعدة المشتركة لكل الكيانات الرئيسية |
| 3.18 | InvoiceBase.cs | ما تتقاسمه فاتورتا البيع والشراء |
| 3.19 | PartyBase.cs | ما يتقاسمه كل طرف |
| 3.20 | ReturnBase.cs | ما يتقاسمه مرتجعا البيع والشراء |
| | **Entities/** | كيانات صرفة: خصائص فقط بلا سلوك |
| 3.21 | Company.cs | كيان Company |
| 3.22 | Currency.cs | كيان Currency |
| 3.23 | Customer.cs | كيان Customer |
| 3.24 | CycleDocument.cs | كيان CycleDocument |
| 3.25 | Department.cs | كيان Department |
| 3.26 | DocumentLink.cs | ربط سحب واحد |
| 3.27 | Employee.cs | كيان Employee |
| 3.28 | EmployeeMovement.cs | البدل والخصم سواء |
| 3.29 | ExchangeRate.cs | كيان ExchangeRate |
| 3.30 | FiscalPeriod.cs | كيان FiscalPeriod |
| 3.31 | FiscalYear.cs | كيان FiscalYear |
| 3.32 | JobTitle.cs | كيان JobTitle |
| 3.33 | JournalEntry.cs | كيان JournalEntry |
| 3.34 | JournalLine.cs | كيان JournalLine |
| 3.35 | License.cs | ترخيص عميل |
| 3.36 | NumberSequence.cs | عدّاد ترقيم لكل مفتاح |
| 3.37 | Payroll.cs | كيان Payroll |
| 3.38 | PayrollLine.cs | استحقاق موظفٍ في مسير |
| 3.39 | Product.cs | كيان Product |
| 3.40 | PurchaseInvoice.cs | كيان PurchaseInvoice |
| 3.41 | PurchaseReturn.cs | كيان PurchaseReturn |
| 3.42 | SalesInvoice.cs | كيان SalesInvoice |
| 3.43 | SalesReturn.cs | كيان SalesReturn |
| 3.44 | Security.cs | دورٌ ومفاتيحه |
| 3.45 | Setting.cs | كيان Setting |
| 3.46 | StockAdjustment.cs | كيان StockAdjustment |
| 3.47 | StockMovement.cs | كيان StockMovement |
| 3.48 | StockTransferDocument.cs | كيان StockTransferDocument |
| 3.49 | Supplier.cs | كيان Supplier |
| 3.50 | Treasury.cs | خزينة/صندوق أو حساب بنكي |
| 3.51 | Unit.cs | كيان Unit |
| 3.52 | User.cs | كيان User |
| 3.53 | Voucher.cs | سند قبض/صرف |
| 3.54 | Warehouse.cs | كيان Warehouse |
| | **Enums/** | تعدادات النظام |
| 3.55 | Enums.cs | تعدادات النظام |
| | **Helpers/** | أدوات نقية صغيرة |
| 3.56 | ArabicNumberToWords.cs | التفقيط بالعربية |
| 3.57 | DocumentTotals.cs | مصدر واحد لحساب مبالغ المستندات |
| | **Results/** | نتيجة العملية وحالتها |
| 3.58 | PagedResult.cs | نتيجة صفحة واحدة من قائمة |
| 3.59 | Result.cs | نتيجة عملية بلا قيمة راجعة |
| 3.60 | StatusVariant.cs | مفردات حالة الأعمال المقفلة |
| | **Rules/** | قواعد محاسبية نقية تستوردها الخدمات والتقارير |
| 3.61 | AccountingRules.cs | قواعد تحقق محاسبية جاهزة |
| 3.62 | DepreciationRules.cs | قواعد الإهلاك النقيّة |
| 3.63 | FiscalPeriodCalculator.cs | حسابات تواريخ السنة/الفترات المالية |
| 3.64 | InventoryCosting.cs | تسعير المخزون بالمتوسط المرجَّح المتحرّك |
| 3.65 | PartyRules.cs | قواعد الطرف النقيّة |
| 3.66 | RuleSet.cs | قواعد التحقق ومجموعتها |

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
| | **DTOs/HR/** | بيانات الموارد البشرية |
| 4.15 | DepartmentDto.cs | بيانات القسم |
| 4.16 | EmployeeDto.cs | بيانات الموظف |
| 4.17 | EmployeeMovementDto.cs | بدلٌ أو خصمٌ على موظف |
| 4.18 | JobTitleDto.cs | بيانات المسمّى الوظيفي |
| 4.19 | PayrollDto.cs | بيانات مسير الرواتب |
| | **DTOs/Inventory/** | بيانات المخزون |
| 4.20 | OpeningStockDto.cs | أرصدة الأصناف الافتتاحية |
| 4.21 | ProductDto.cs | بيانات الصنف |
| 4.22 | StockAdjustmentDto.cs | بيانات إذن المخزون |
| 4.23 | StockTransferDto.cs | بيانات التحويل المخزني |
| 4.24 | UnitDto.cs | بيانات الوحدة |
| 4.25 | WarehouseDto.cs | بيانات المخزن |
| | **DTOs/Parties/** | بيانات العملاء والموردين |
| 4.26 | CustomerDto.cs | للعرض في الجداول |
| 4.27 | SupplierDto.cs | بيانات المورد |
| | **DTOs/Purchasing/** | بيانات المشتريات |
| 4.28 | PurchaseInvoiceDto.cs | بيانات فاتورة الشراء |
| 4.29 | PurchaseReturnDto.cs | بيانات مرتجع الشراء |
| | **DTOs/Sales/** | بيانات المبيعات |
| 4.30 | SalesInvoiceDto.cs | بيانات فاتورة البيع |
| 4.31 | SalesReturnDto.cs | بيانات مرتجع البيع |
| | **DTOs/Security/** | بيانات المستخدمين والأدوار |
| 4.32 | RoleDto.cs | بيانات الدور |
| 4.33 | UserDto.cs | بيانات المستخدم |
| | **DTOs/Treasury/** | بيانات الخزائن والبنوك |
| 4.34 | TreasuryDto.cs | بيانات الخزينة |
| | **DTOs/Vouchers/** | بيانات السندات |
| 4.35 | VoucherDto.cs | بيانات السند وتخصيصه |
| | **Pipeline/** | تركيب العمليات متعددة الخطوات |
| 4.36 | IStep.cs | عقد الخطوة الواحدة |
| 4.37 | Pipeline.cs | مؤلِّف خطوات للعمليات غير النمطية |
| 4.38 | PipelineContext.cs | سياق يمرّ بين الخطوات |
| | **Pipeline/Steps/** | خطوة واحدة قابلة للتركيب |
| 4.39 | AuditStep.cs | خطوة تسجيل التدقيق |
| 4.40 | FuncStep.cs | غلاف خطوة بسطر واحد |
| 4.41 | PermissionStep.cs | خطوة فحص الصلاحية |
| 4.42 | TransactionStep.cs | خطوة فتح المعاملة أو إعادة |
| 4.43 | ValidationStep.cs | خطوة التحقق |
| | **Reporting/** | خدمات التقارير |
| 4.44 | AssetReportService.cs | تقريرا الأصول |
| 4.45 | BuilderReportService.cs | تقرير مبنيّ |
| 4.46 | FinancialStatementFactory.cs | القوائم المالية بشكلها الرسمي |
| 4.47 | FinancialStatementService.cs | القوائم المالية الثلاث |
| 4.48 | PartyReportService.cs | أرصدة الأطراف وكشوفها |
| 4.49 | PayslipReportService.cs | قسيمة راتب موظف |
| 4.50 | ReportData.cs | مخرَج التقرير |
| 4.51 | ReportRows.cs | ميزان المراجعة القياسي |
| 4.52 | ReportServiceBase.cs | ما يتقاسمه كل تقرير |
| 4.53 | SalesReportService.cs | تقرير المبيعات |
| 4.54 | StockReportService.cs | تقارير المخزون |
| | **Services/Accounting/** | الحسابات والقيود والفترات المالية |
| 4.55 | AccountService.cs | المالك الوحيد لمنطق شجرة الحسابات |
| 4.56 | FiscalPeriodService.cs | المالك الوحيد لمنطق السنوات/الفترات المالية |
| 4.57 | IAccountService.cs | عقد خدمة الحسابات |
| 4.58 | IFiscalPeriodService.cs | عقد الفترات المالية |
| 4.59 | IJournalService.cs | المالك الوحيد لمنطق قيود اليومية |
| 4.60 | JournalService.cs | المالك الوحيد لمنطق قيود اليومية |
| 4.61 | OpeningBalanceService.cs | الأرصدة الافتتاحية ليست جدولاً موازياً |
| | **Services/Admin/** | الإعدادات والتراخيص والنسخ والتحديث |
| 4.62 | ISettingsService.cs | عقد الإعدادات |
| 4.63 | LicenseService.cs | تراخيص العملاء |
| 4.64 | ProgramEditionService.cs | نسخةُ برنامجٍ مستقلّة في مسارٍ |
| 4.65 | SettingsService.cs | طبقة الأعمال فوق ISettingsProvider |
| 4.66 | UpdateService.cs | التحديث سؤالٌ واحد |
| | **Services/Assets/** | الأصل: اقتناءً وإهلاكاً وتقييماً واستبعاداً |
| 4.67 | AssetDepreciationService.cs | قسط الإهلاك سجلٌّ مستقلّ |
| 4.68 | AssetDisposalService.cs | بيع الأصل واستبعاده |
| 4.69 | AssetMovementServiceBase.cs | أساس حركات الأصول |
| 4.70 | AssetRevaluationService.cs | إعادة تقييم الأصل |
| 4.71 | AssetService.cs | الأصل: إنشاءً وتعديلاً وبذراً |
| 4.72 | IAssetService.cs | عقد خدمة الأصول |
| | **Services/Backup/** | النسخ الاحتياطي والاستعادة |
| 4.73 | BackupInfo.cs | وصف نسخة احتياطية |
| 4.74 | BackupService.cs | المكان الوحيد لأخذ/استعادة/التحقق من النسخ |
| 4.75 | IBackupService.cs | عقد النسخ الاحتياطي |
| | **Services/Builder/** | ما يبنيه المستخدم من شاشات |
| 4.76 | BuilderCatalogService.cs | وصفُ ما بناه المستخدم كما |
| 4.77 | BuilderCrudServices.cs | خدمة صفوف |
| 4.78 | ColumnWidths.cs | نِسَب عرض أعمدة الجدول |
| 4.79 | DynamicEntityService.cs | خدمة أي جدول بناه المستخدم |
| | **Services/Cheques/** | دورة الشيك |
| 4.80 | ChequeDocumentService.cs | مستند "استلام/صرف شيكات" |
| 4.81 | ChequeService.cs | دورة الشيك كاملة |
| | **Services/Common/** | الفئات وحساباتها |
| 4.82 | CategoryService.cs | فئات الوحدات وحساباتها |
| 4.83 | ICategoryService.cs | عقد خدمة الفئات |
| | **Services/** | خدمة لكل كيان، ترث ServiceBase |
| 4.84 | CrudServiceBase.cs | القراءة العامة لكيان بصفحات وبحث |
| | **Services/Documents/** | مستندات الدورة وروابط السحب |
| 4.85 | CycleDocumentServiceBase.cs | مستندات الدورة: أساسٌ وستّ خدمات |
| 4.86 | DocumentLinkService.cs | تتبّع السحب بين المستندات |
| | **Services/HR/** | الموظفون والرواتب والحضور |
| 4.87 | AttendanceService.cs | الحضور والانصراف |
| 4.88 | DepartmentService.cs | خدمة الأقسام |
| 4.89 | EmployeeMovementService.cs | البدل والخصم خدمةٌ واحدة بجدولين |
| 4.90 | EmployeeService.cs | خدمة الموظفين |
| 4.91 | IDepartmentService.cs | عقد الأقسام |
| 4.92 | IEmployeeService.cs | عقد الموظفين |
| 4.93 | IJobTitleService.cs | عقد المسمّيات الوظيفية |
| 4.94 | IPayrollService.cs | عقد مسير الرواتب |
| 4.95 | JobTitleService.cs | خدمة المسمّيات الوظيفية |
| 4.96 | PayrollService.cs | مسير الرواتب |
| | **Services/** | خدمة لكل كيان، ترث ServiceBase |
| 4.97 | IAccountLinkedService.cs | كيان يعيش ورقةً في شجرة |
| 4.98 | INumberSequenceService.cs | عقد الترقيم التسلسلي |
| | **Services/Inventory/** | الأصناف والمخازن والأرصدة |
| 4.99 | IProductService.cs | عقد الأصناف |
| 4.100 | IStockInService.cs | عقود أذون المخزون الستة |
| 4.101 | IStockService.cs | عقد أرصدة المخزون |
| 4.102 | IStockTransferService.cs | عقد التحويل المخزني |
| 4.103 | IUnitService.cs | عقد الوحدات |
| 4.104 | IWarehouseService.cs | عقد المخازن |
| 4.105 | OpeningStockService.cs | رصيد أول المدة للأصناف |
| 4.106 | ProductService.cs | خدمة الأصناف |
| 4.107 | StockAdjustmentServiceBase.cs | خدمةٌ بوّابتها مفتاحٌ واحد مُعلَن |
| 4.108 | StockService.cs | أرصدة المخزون وحركته |
| 4.109 | StockTransferService.cs | التحويل بين المخازن |
| 4.110 | UnitService.cs | خدمة الوحدات |
| 4.111 | WarehouseService.cs | خدمة المخازن |
| | **Services/** | خدمة لكل كيان، ترث ServiceBase |
| 4.112 | NumberSequenceService.cs | أرقام متسلسلة لكل مفتاح |
| | **Services/Parties/** | العملاء والموردون |
| 4.113 | CustomerService.cs | المالك الوحيد لمنطق العملاء |
| 4.114 | ICustomerService.cs | المالك الوحيد لمنطق العملاء |
| 4.115 | ISupplierService.cs | المالك الوحيد لمنطق الموردين |
| 4.116 | PartyServiceBase.cs | المنطق المشترك بين العملاء والموردين |
| 4.117 | SupplierService.cs | المالك الوحيد لمنطق الموردين |
| | **Services/** | خدمة لكل كيان، ترث ServiceBase |
| 4.118 | Posting.cs | القيد يُنشأ مُرحَّلاً ويُعكس بحذفه |
| | **Services/Print/** | بناء المستندات المطبوعة |
| 4.119 | ChequePrinter.cs | الشيك يُطبَع على ورق مطبوع |
| 4.120 | Code128.cs | ترميز Code128-B |
| 4.121 | CompanyHeaderComponent.cs | ترويسة الشركة كقطعة واحدة تُستدعى |
| 4.122 | IPrintDialogHost.cs | تنفّذه طبقة الواجهة |
| 4.123 | IPrintService.cs | عقد الطباعة |
| 4.124 | ImageData.cs | تحويل صورة ↔ Base64 |
| 4.125 | PaperNodeRenderer.cs | مترجم PaperNode إلى عناصر WPF |
| 4.126 | PaperTheme.cs | قيم الورق كلها من PrintTheme.xaml |
| 4.127 | PrintService.cs | يبني مستندات الطباعة من IPrintable |
| | **Services/Purchasing/** | فواتير الشراء ومرتجعاتها |
| 4.128 | IPurchaseInvoiceService.cs | عقد فاتورة الشراء |
| 4.129 | IPurchaseReturnService.cs | عقد مرتجع الشراء |
| 4.130 | PurchaseInvoiceService.cs | فاتورة الشراء وقيدها |
| 4.131 | PurchaseReturnService.cs | مرتجع الشراء وقيده |
| | **Services/Sales/** | فواتير البيع ومرتجعاتها |
| 4.132 | ISalesInvoiceService.cs | عقد فاتورة البيع |
| 4.133 | ISalesReturnService.cs | عقد مرتجع البيع |
| 4.134 | SalesInvoiceService.cs | فاتورة البيع وقيدها |
| 4.135 | SalesReturnService.cs | مرتجع البيع وقيده |
| | **Services/Security/** | المستخدمون والأدوار |
| 4.136 | IRoleService.cs | عقد الأدوار |
| 4.137 | IUserService.cs | عقد المستخدمين |
| 4.138 | RoleService.cs | الأدوار وصلاحياتها |
| 4.139 | UserService.cs | المستخدمون وكلمات مرورهم |
| | **Services/** | خدمة لكل كيان، ترث ServiceBase |
| 4.140 | ServiceBase.cs | القاعدة المشتركة لكل خدمة |
| | **Services/Treasury/** | الخزائن والبنوك |
| 4.141 | ITreasuryService.cs | عقد الخزائن والبنوك |
| 4.142 | TreasuryService.cs | الخزائن والبنوك وحساباتها |
| | **Services/Vouchers/** | سندات القبض والصرف |
| 4.143 | VoucherServiceBase.cs | سند قبض/صرف |
| | **Validation/** | متحقّق لكل كيان عبر Rules.For |
| 4.144 | AccountValidator.cs | ينفّذ IValidator&lt;Account&gt; عبر RuleSet |
| 4.145 | AssetDepreciationValidator.cs | تحقّق قسط الإهلاك |
| 4.146 | AssetDisposalValidator.cs | تحقّق استبعاد الأصل |
| 4.147 | AssetRevaluationValidator.cs | تحقّق إعادة التقييم |
| 4.148 | AssetValidator.cs | تحقّق الأصل |
| 4.149 | BuilderModuleValidator.cs | قواعد وصف الصفحة المبنيّة |
| 4.150 | BuilderRowValidator.cs | قواعد صفٍّ في جدول مبنيّ |
| 4.151 | CategoryValidator.cs | تحقّق الفئة |
| 4.152 | ChequeLineValidator.cs | ما يجعل سطر المستند شيكاً |
| 4.153 | CustomerValidator.cs | تحقّق العميل |
| 4.154 | EditionValidator.cs | تحقّق نسخة البرنامج |
| 4.155 | EmployeeValidator.cs | تحقّق الموظف |
| 4.156 | JournalValidator.cs | تحقّق القيد وسطوره |
| 4.157 | ProductValidator.cs | تحقّق الصنف |
| 4.158 | SupplierValidator.cs | تحقّق المورد |
| 4.159 | UserValidator.cs | تحقّق المستخدم |

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
| 7.11 | StandardFields.cs | حقول تتكرر في كل حوار |
| 7.12 | TreeCheckListDefinition.cs | شاشة شجرة قابلة للتأشير مدفوعة |
| | **Print/** | جسر الطباعة للشاشات |
| 7.13 | PrintDocuments.cs | عائلتا المستندات |
| 7.14 | TradePaper.cs | شكل الورق التجاري |
| | **Pull/** | سحب مستند من مستند |
| 7.15 | PullService.cs | محرّك السحب العام |
| | **Registry/** | سجلّ الوحدات وخريطة التنقّل |
| 7.16 | IModuleRegistry.cs | سجل الوحدات المُفعَّلة |
| 7.17 | ModuleRegistry.cs | سجلّ الوحدات |
| 7.18 | NavigationMap.cs | خريطة الشريط الجانبي |
| 7.19 | NavigationSource.cs | أقسام الشريط الجانبي كما يراها |
| | **Renderers/** | تحويل التعريف إلى شاشة عاملة |
| 7.20 | BuilderPickers.cs | قوائم تعدادات النظام وكتالوج أزراره |
| 7.21 | ChequeBoardRenderer.cs | شاشة الشيكات |
| 7.22 | CrudPageRenderer.cs | تصيير صفحة القائمة وCRUD |
| 7.23 | DialogRenderer.cs | تصيير الحوار من وصفه |
| 7.24 | DocumentPageRenderer.cs | تصيير صفحة المستند |
| 7.25 | DocumentPrinter.cs | يجلب المستند الكامل من خدمته |
| 7.26 | DocumentRenderer.cs | تصيير محرّر المستند |
| 7.27 | FieldValidation.cs | تحقّق واجهة واحد لكل الشاشات |
| 7.28 | FilterControls.cs | شريط فلاتر الصفحة المُعلَنة |
| 7.29 | FolderOutput.cs | مسار مجلد من المستخدم |
| 7.30 | ListOutput.cs | طباعة أي قائمة معروضة وتصديرها |
| 7.31 | PageRenderer.cs | نقطة التوزيع الوحيدة حسب ModuleDefinition.LayoutKind |
| 7.32 | PaginationBar.cs | شريط الترقيم مربوطاً بنموذج العرض |
| 7.33 | PullDialog.cs | نافذة "سحب من" |
| 7.34 | ReportRenderer.cs | تصيير التقرير |
| 7.35 | Resolve.cs | حلّ نموذج العرض والخدمة |
| 7.36 | SettingsPageRenderer.cs | تصيير صفحة الإعدادات |
| 7.37 | ToolbarActions.cs | أزرار الصفحة بعد ترشيحها بما |
| 7.38 | TreeBuilder.cs | قائمة مسطَّحة تصير شجرة |
| 7.39 | TreeCheckListRenderer.cs | تصيير شجرة التأشير |
| 7.40 | TreeRenderer.cs | تصيير الشجرة |

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
| 8.08 | ModuleRegistrations.cs | يسجّل كل وحدة عمل فعلية |
| 8.09 | PermissionModuleRegistrations.cs | شاشتا الصلاحيات |
| 8.10 | ReportRegistrations.cs | التقارير الثلاثة عشر |
| 8.11 | StockDocumentFactory.cs | رأس المستند المخزني وسطوره |
| 8.12 | TreasuryRegistrations.cs | الخزائن والسندات والشيكات |

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
| | **Pipeline/** | خطوات العمليات |
| 11.38 | PipelineTests.cs | بنية الـPipeline بكيان وهمي |
| | **Services/** | الخدمات ومنطقها |
| 11.39 | AccountLeafStateTests.cs | الحساب إمّا أب وإمّا يقبل |
| 11.40 | AccountServiceTests.cs | قاعدة معزولة لكل اختبار |
| 11.41 | BackupServiceTests.cs | النسخ الاحتياطي والاستعادة |
| 11.42 | BuilderCatalogTests.cs | وصف ما بناه المستخدم |
| 11.43 | CategoryServiceTests.cs | الفئات وحساباتها |
| 11.44 | CustomerServiceTests.cs | قاعدة بيانات خاصة معزولة لكل |
| 11.45 | CycleDocumentServiceTests.cs | مستندات الدورة |
| 11.46 | CycleVoucherServiceTests.cs | سندات الدورة |
| | **Services/Design/** | التصميم والنصوص |
| 11.47 | IdentityServiceTests.cs | تحميل موارد التصميم |
| 11.48 | LocalizationServiceTests.cs | تبديل قاموس النصوص الحقيقي |
| | **Services/** | الخدمات ومنطقها |
| 11.49 | DocumentLinkServiceTests.cs | تتبّع السحب |
| 11.50 | ExportServiceTests.cs | تصدير CSV وExcel وPDF فعلي |
| 11.51 | FiscalPeriodServiceTests.cs | قاعدة معزولة لكل اختبار |
| 11.52 | FlowModeStockTests.cs | الفرق بين الوضعين |
| 11.53 | InventoryCostingTests.cs | المتوسط المرجَّح المتحرّك |
| 11.54 | ItemCardReportTests.cs | تقرير حركة الصنف بالمتوسط المرجَّح |
| 11.55 | JournalServiceTests.cs | قاعدة بيانات خاصة معزولة لكل |
| 11.56 | JournalToStatementTests.cs | المعاملات كلها تصبّ في القيود |
| 11.57 | NavigationServiceTests.cs | التنقّل بين الشاشات |
| 11.58 | NegativeBalanceGuardTests.cs | المخزن والخزينة والبنك لا يقبلون |
| 11.59 | NumberSequenceServiceTests.cs | الترقيم التسلسلي |
| 11.60 | OpeningBalanceAndDepreciationTests.cs | الأرصدة الافتتاحية والإهلاك |
| 11.61 | PayrollServiceTests.cs | المسير يُنشأ مسوّدةً ثم يُرحَّل |
| 11.62 | PermissionServiceTests.cs | فحص الصلاحيات وتحميلها |
| 11.63 | PrintFormattingTests.cs | الشعار ومحاذاة الجدول في الورق |
| 11.64 | PrintServiceTests.cs | الطباعة |
| 11.65 | PullServiceTests.cs | سلوك السحب كما طُلب حرفياً |
| 11.66 | PurchaseInvoiceServiceTests.cs | فاتورة الشراء وقيدها |
| 11.67 | ReportsGenerateTests.cs | توليد التقارير |
| 11.68 | ReturnsServiceTests.cs | المرتجعات |
| 11.69 | SalesInvoiceServiceTests.cs | فاتورة البيع وقيدها |
| 11.70 | SettingsServiceTests.cs | الإعدادات |
| 11.71 | StatementGroupingTests.cs | مستوى التجميع في القائمة |
| 11.72 | StockDocumentsServiceTests.cs | أذون المخزون |
| 11.73 | StockServiceTests.cs | أرصدة المخزون |
| 11.74 | TreasurySeedTests.cs | قوائم السندات كانت تبدو "لا |
| 11.75 | VoucherAndChequeTests.cs | السندات والشيكات |
| 11.76 | VoucherPrintTests.cs | سند القبض والصرف |
| 11.77 | StaThreadHelper.cs | أنواع WPF تفرض خيط STA |
| 11.78 | TestDatabaseFixture.cs | ملف SQLite مؤقت مستقل لكل |
| | **Validation/** | قواعد التحقق |
| 11.79 | ValidatorsTests.cs | المتحقّقون السبعة |
| | **ViewModels/** | نماذج العرض |
| 11.80 | CrudViewModelBaseTests.cs | نماذج العرض فوق خدمة حقيقية |
| 11.81 | CustomersViewModelTests.cs | يثبت أن CustomersViewModel |
| 11.82 | WpfApplicationFixture.cs | تطبيق WPF لخيط STA |

## 12. PrimeERP.Setup

| # | الملف | الوظيفة |
|---|---|---|
| 12.01 | App.xaml | واجهة |
| 12.02 | App.xaml.cs | إقلاع المنصِّب |
| 12.03 | MainWindow.xaml | واجهة |
| 12.04 | MainWindow.xaml.cs | منصِّبٌ صغير |
