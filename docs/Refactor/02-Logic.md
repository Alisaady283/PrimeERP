# 02 — المنطق: ما تفعله الطبقة، مصنَّفاً بالحالة لا بالصفحة

`01-Mapping.md` يطابق قائمتك بالقائم. هذا الملف يجيب «ماذا» على مستوى الطبقة كلها: كل منطقٍ في `4.Application` مصنَّفاً بنوعه، وكم نسخةً منه مكتوبة، وأين.
القياس من الشجرة عند `d143d35` مع التعديلات غير الملتزمة على فرع `refactor/ef-linq`.

---

## ١. الخدمات القائمة في ثماني عائلات

كل خدمةٍ قائمة تنتمي لعائلةٍ واحدة من ثمانٍ. العائلة تحدّد المنطق المشترك، والخدمة تضيف فرقها فقط.

| # | العائلة | ما يميّزها | الخدمات القائمة | أساسٌ مشترك اليوم |
|---|---|---|---|---|
| ع١ | **قائمة بسيطة** | اسم ونشاط، بلا حساب ولا قيد | `Department` · `JobTitle` · `Unit` · `Warehouse` · `Role` · `User` | لا شيء — أربع نسخ متطابقة ([Department](../../4.Application/Services/HR/DepartmentService.cs) ≈ [JobTitle](../../4.Application/Services/HR/JobTitleService.cs) ≈ [Unit](../../4.Application/Services/Inventory/UnitService.cs) ≈ [Warehouse](../../4.Application/Services/Inventory/WarehouseService.cs)) |
| ع٢ | **كيان رئيسي** | ترقيم وتحقق وصفحات، بلا حساب | `Product` · `Attendance` · `Allowance`/`Deduction` · `License` · `DynamicEntity` · خدمات البنّاء الخمس | `CrudServiceBase` للقراءة فقط، والكتابة يدوية |
| ع٣ | **كيان مرتبط بحساب** | الكيان وحسابه في الشجرة يُنشآن ويُسمَّيان ويُحذفان معاً | `Customer` · `Supplier` · `Employee` · `Treasury` · `Category` · `Asset` | `PartyServiceBase` للعميل والمورد وحدهما |
| ع٤ | **مستند رأس وسطور بلا قيد** | سطور أصناف، ترقيم، سحب، وأثر مخزني أحياناً | الدورة الأربع · الأذون الستة · `StockTransfer` | `CycleDocumentServiceBase` · `StockAdjustmentServiceBase` |
| ع٥ | **مستند مُرحِّل** | ينشئ قيداً مرحَّلاً ويعكسه بحذفه | الفواتير والمرتجعات الأربع · السندان · `Payroll` · `OpeningStock` · `OpeningBalance` · حركة الشيك | `VoucherServiceBase` للسندين وحدهما |
| ع٦ | **حركة أصل** | تغيّر قيمة الأصل وتقيّد الفرق | `AssetRevaluation` · `AssetDisposal` · `AssetDepreciation` | `AssetMovementServiceBase` — الأساس الوحيد الذي يستعمل `Posting` |
| ع٧ | **محرّكات النواة** | منطقٌ حقيقي تستدعيه بقية الخدمات | `Account` · `Journal` · `FiscalPeriod` · `Stock` · `DocumentLink` · `NumberSequence` · `Settings` · `Posting` | — |
| ع٨ | **تقارير وبنية** | قراءة تجميعية أو تشغيل | التقارير السبعة · `Print` · `Backup` · `Update` · `ProgramEdition` · `BuilderCatalog` | `ReportServiceBase` |

**الخلاصة:** العائلات ٣ و٥ و٦ تتقاسم منطقاً واحداً، ولكلٍّ منها أساسٌ جزئي يخدم بعض أفرادها ويترك الباقي يكتبه يدوياً.

---

## ٢. العمليات القانونية — كم مرة كُتبت

| العملية | عدد التنفيذات | أسطرها | موروثة من أساس | مكتوبة يدوياً |
|---|---:|---:|---|---|
| `GetPaged` | 19 | 175 | `CrudServiceBase` لتسع خدمات | 18 خدمة، كلها بالشكل نفسه: صلاحية ← مرشِّح افتراضي ← مستودع ← `Select(ToDto)` |
| `GetById` | 21 | 315 | `CrudServiceBase` | 20 خدمة، منها 9 تبني تفصيل المستند بنسخ حقول الملخّص يدوياً |
| `GetAll` | 9 | 25 | — | خدمات القوائم والأمان والخزينة |
| `Create` | 41 | 1555 | لا شيء عامّ | كلها |
| `Update` | 33 | 507 | لا شيء عامّ | كلها — منها 5 بالحذف ثم الإنشاء، و7 ترفض بسطر |
| `Delete` | 37 | 560 | لا شيء عامّ | كلها |
| `ToDto` | 36 | 442 | — | كلها، 21 منها تستعلم لكل صفّ (راجع 03 §ب) |

**الخلاصة:** القراءة موحَّدة جزئياً، والكتابة غير موحَّدة إطلاقاً: 111 دالة كتابة بـ2622 سطراً.

---

## ٣. الخطوات العابرة — تمرّ بها كل عملية كتابة

| الخطوة | مواضعها | الآلية اليوم | القطعة الجاهزة غير المستعملة |
|---|---:|---|---|
| فحص الصلاحية | 182 | `if (!Can("X")) return FailDenied<T>();` في رأس كل دالة | `Pipeline.Permission` · `ServiceBase.Require` |
| التحقق | 27 بمتحقّق + عشرات مضمَّنة | `Check(new XValidator(), e)` أو `if` بنصّ عربي | `Pipeline.Validate` |
| المعاملة | 75 | `Tx(db => …)` | `ServiceBase.Tx(AuditAction, Func<db, Result>)` — تلغي بلا استثناء وتدقّق بعد النجاح، و**لا مستدعي لها** |
| الإلغاء داخل المعاملة | ~40 | `throw new InvalidOperationException` داخل `Tx` ثم `catch` خارجه | النسخة نفسها من `Tx` أعلاه |
| التدقيق | 112 | `Audit.Log(EntityName, id, action, …)` بعد المعاملة | `Pipeline.Audit` |
| نصّ الخطأ | 453 نصّاً عربياً مكتوباً | `Result.Fail("الحساب غير موجود", …)` | `Msg(key)` موجودة في `ServiceBase` |

`4.Application/Pipeline` — ثمانية ملفات بـ278 سطراً — مبنيٌّ لهذه الخطوات بالضبط، ومستهلكوه خارج اختباره: **صفر**. ومجلد `Pipeline/Operations` فارغ.

---

## ٤. المنطق المزدوج — دالّتان تُنفَّذان معاً في معاملة واحدة

هذا ما وصفتَه بـ«delegate يأخذ اثنين ويُنفّذهما». هو أكثر ما يتكرر في الطبقة، وكل نسخةٍ منه مكتوبة بيدها.

### ٤.١ الكيان ↔ حسابه في الشجرة

ثلاث حركات لكل كيان: إنشاء الحساب تحت جذرٍ مضبوط في الإعدادات، وتسميته مع الكيان، وحذفه معه.

| الحركة | النسخ | المواضع |
|---|---:|---|
| إنشاء حساب ورقي تحت جذر | 7 | [PartyServiceBase:61](../../4.Application/Services/Parties/PartyServiceBase.cs#L61) · [CustomerService:115](../../4.Application/Services/Parties/CustomerService.cs#L115) (نسخة داخلية تعيد كتابة الأولى) · [EmployeeService:82](../../4.Application/Services/HR/EmployeeService.cs#L82) · [TreasuryService:51](../../4.Application/Services/Treasury/TreasuryService.cs#L51) · [CategoryService:160](../../4.Application/Services/Common/CategoryService.cs#L160) · [AssetService:315](../../4.Application/Services/Assets/AssetService.cs#L315) · [AssetService:96](../../4.Application/Services/Assets/AssetService.cs#L96) |
| تسمية الحساب مع الكيان | 9 | [Customer:211](../../4.Application/Services/Parties/CustomerService.cs#L211) · [Supplier:167](../../4.Application/Services/Parties/SupplierService.cs#L167) · [Employee:109](../../4.Application/Services/HR/EmployeeService.cs#L109) · [Treasury:185](../../4.Application/Services/Treasury/TreasuryService.cs#L185) · [Category:111](../../4.Application/Services/Common/CategoryService.cs#L111) و[114](../../4.Application/Services/Common/CategoryService.cs#L114) · [Asset:172](../../4.Application/Services/Assets/AssetService.cs#L172) و[175](../../4.Application/Services/Assets/AssetService.cs#L175) · [Account:238](../../4.Application/Services/Accounting/AccountService.cs#L238) (الاتجاه المعاكس) |
| حذف الحساب مع الكيان | 8 | [Customer:235](../../4.Application/Services/Parties/CustomerService.cs#L235) · [Supplier:188](../../4.Application/Services/Parties/SupplierService.cs#L188) · [Employee:126](../../4.Application/Services/HR/EmployeeService.cs#L126) · [Treasury:205](../../4.Application/Services/Treasury/TreasuryService.cs#L205) · [Category:138](../../4.Application/Services/Common/CategoryService.cs#L138) و[141](../../4.Application/Services/Common/CategoryService.cs#L141) · [Asset:218](../../4.Application/Services/Assets/AssetService.cs#L218) و[221](../../4.Application/Services/Assets/AssetService.cs#L221) |
| الاتجاه المعاكس: حسابٌ يُنشئ كيانه | 4 | `IAccountLinkedService` في Customer/Supplier (عبر الأساس) · Employee · Treasury |

والفروق بين النسخ السبع ليست منطقاً بل مصادفة: الخزينة تنشئ حسابها **خارج** المعاملة، والفئة تنشئه تجميعياً، والموظف يتجاهل فشله بصمت.

### ٤.٢ المستند ↔ قيده

| الحركة | النسخ | المواضع |
|---|---:|---|
| قيد بطرفين: إنشاء ← ترحيل ← رمي عند الفشل | 3 يدوية + `Posting.Entry` | [VoucherServiceBase:197](../../4.Application/Services/Vouchers/VoucherServiceBase.cs#L197) · [ChequeService:178](../../4.Application/Services/Cheques/ChequeService.cs#L178) · [OpeningStockService:63](../../4.Application/Services/Inventory/OpeningStockService.cs#L63) — وكلها نسخة حرفية من [Posting.Entry](../../4.Application/Services/Posting.cs) ذي الطرفين |
| قيد متعدد السطور | 6 يدوية | الفواتير والمرتجعات الأربع (أسطر 176/171/189/189) · [PayrollService:184](../../4.Application/Services/HR/PayrollService.cs#L184) · [FiscalPeriodService:276](../../4.Application/Services/Accounting/FiscalPeriodService.cs#L276) |
| عبر `Posting` | 7 | حركات الأصول الأربع عبر [AssetMovementServiceBase](../../4.Application/Services/Assets/AssetMovementServiceBase.cs) |
| عكس القيد بحذف المستند | 12 | `_journal.Delete(db, id)` أو `ReverseEntry` |

`Posting` مبنيٌّ ويُنتج القيد نفسه، ويستعمله قسم الأصول وحده.

### ٤.٣ المستند ↔ حركة المخزون

`RecordMovement` لكل سطر في 7 خدمات: الأذون، والتحويل، ورصيد أول المدة، والفواتير والمرتجعات الأربع، و`RemoveMovements` في حذفها. والتحويل بين مخزنين زوجٌ «صرف + إضافة» مكتوب مرّتين: [StockService:119](../../4.Application/Services/Inventory/StockService.cs#L119) و[StockTransferService:93](../../4.Application/Services/Inventory/StockTransferService.cs#L93).

### ٤.٤ المستند ↔ روابط السحب

فحصُ «سُحب منه فلا يُحذف» مكتوب 6 مرات بالنصّ نفسه: [Cycle:152](../../4.Application/Services/Documents/CycleDocumentServiceBase.cs#L152) · [StockAdjustment:143](../../4.Application/Services/Inventory/StockAdjustmentServiceBase.cs#L143) · الفواتير والمرتجعات الأربع.

### ٤.٥ السطر ↔ صنفه

حلّ كود الصنف إلى كيانه ثم فحص الكمية، لكل سطر باستعلام، في 7 خدمات: الدورة، والأذون، والتحويل، والفواتير والمرتجعات الأربع. والنمط نفسه للموظف بكوده في الحضور والبدلات والرواتب.

### ٤.٦ التعديل = حذف + إنشاء

| الخدمة | الموضع | في معاملة واحدة؟ |
|---|---|---|
| `CycleDocumentServiceBase` | [السطر 137](../../4.Application/Services/Documents/CycleDocumentServiceBase.cs#L137) | لا |
| `VoucherServiceBase` | [السطر 158](../../4.Application/Services/Vouchers/VoucherServiceBase.cs#L158) | لا |
| `AssetRevaluationService` | [السطر 106](../../4.Application/Services/Assets/AssetRevaluationService.cs#L106) | لا |
| `AssetDisposalService` | [السطر 114](../../4.Application/Services/Assets/AssetDisposalService.cs#L114) | لا |
| `AssetDepreciationService` | [السطر 105](../../4.Application/Services/Assets/AssetDepreciationService.cs#L105) | لا |

خمس نسخٍ من «استبدال» واحد، ولا واحدة منها ذرّية — راجع الخلل ١ في `03-LinqEfReview.md`.

### ٤.٧ المستند ↔ رصيد طرفه

الفواتير والمرتجعات الأربع تعيد حساب رصيد الطرف بعد الإنشاء، خارج المعاملة، عبر دالةٍ تشترط صلاحية تعديل العملاء. ولا تعيده بعد الحذف. والسندات لا تعيده أبداً.

---

## ٥. التحويل إلى DTO والأسماء

| النمط | المواضع | الصحيح القائم في الكود نفسه |
|---|---|---|
| تفصيل المستند يُبنى بنسخ حقول ملخّصه حقلاً حقلاً | Sales/Purchase × 2 · StockAdjustment · Transfer · Voucher · Journal | `ChequeService.ToDto<T>() where T : ChequeDto, new()` — [السطر 224](../../4.Application/Services/Cheques/ChequeService.cs#L224) — دالة واحدة تبني الاثنين |
| تفصيل الرواتب يُنسخ بالانعكاس | [PayrollService.ToDetail](../../4.Application/Services/HR/PayrollService.cs#L313) | الدالة نفسها أعلاه |
| اسم الطرف/المخزن/الخزينة/الأصل يُجلب لكل صفّ بنداء خدمة كاملة | الفواتير الأربع · السندات · الشيكات · الأذون · التحويل · حركات الأصول | `RepositoryBase.WithNames` — ضمّة واحدة للصفحة (من التعديل الجاري) |
| الحالة (نشط/موقوف) تُحسب بسطرين متطابقين | Employee · Product · Asset · User · الأطراف | لا شيء — تُبنى |
| مرشِّحات بـ`SearchText`/`SortBy`/`SortDescending` مكرَّرة | 22 مرشِّحاً | لا أساس — يُبنى |

---

## ٦. المنطق الحقيقي — يبقى بجسمه

هذا ليس تكراراً. ينتقل كما هو، ويُنزع منه الحارس والتدقيق والمعاملة فقط.

| المنطق | موضعه |
|---|---|
| ترقيم الحساب الابن، والأب والورقة، والجذور المُدارة بالأصول والمخزون | `AccountService` — `BuildChildCode` · `IsAssetManaged` · `ResolveAutoLink` |
| صحة القيد وتوازنه، ومنع الصندوق من السالب، وملكية المصدر | `JournalService` + `JournalValidator` + `AccountingRules` |
| فتح الفترات وإقفالها، وقيد إقفال السنة | `FiscalPeriodService` + `FiscalPeriodCalculator` |
| تكلفة المتوسط المتحرك | `InventoryCosting` (3.Domain) عبر `StockService` |
| توليد سطور الرواتب من البدلات والخصومات والحضور | `PayrollService.GenerateLines` |
| آلة حالات الشيك | `ChequeService.Allowed` + `Move` |
| الإهلاك وإعادة التقييم والاستبعاد | `DepreciationRules` (3.Domain) + خدمات الأصول |
| ضريبة السطر وخصمه | `DocumentTotals` (3.Domain) |
| حدّ الائتمان | `PartyServiceBase.CheckCreditLimit` + `PartyRules` |
| قيود الفواتير والمرتجعات الأربع | أجسامها كما هي — قرار «لا تُدمَج» في `ARCHITECTURE.md` |
| البنّاء والكيان الديناميكي | `BuilderCrudServiceBase` + `DynamicEntityService` — القالب الوحيد القائم فعلاً |

---

## ٧. ما هو رفيعٌ فعلاً — النموذج المطلوب محقَّقاً

`OpeningBalanceService` — 53 سطراً: يضبط مصدراً ووصفاً ثابتين ثم يفوّض كل شيء لخدمة القيود. و`ChequeDocumentServiceBase` يكيّف شكل الشيك إلى شكل المستند ويفوّض. و`AllowanceService`/`DeductionService` صفرُ سطرٍ فوق أساسهما. هذا هو الشكل الذي تصير إليه كل خدمة صفحة: إعلانٌ فوق قطعٍ مشتركة، وفرقٌ حقيقي وحده.
