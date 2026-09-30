# المرحلة الجديدة: منطقٌ عامّ بالمعاملات

**الحالة (2026-09-29):** نُفّذت الخطوات ٠ إلى ٥. البناء صفر خطأ و`check.sh` صفر فشل. التفاصيل في [07-ServicesReport.md](07-ServicesReport.md). الاختبارات لم تُشغَّل.

## المبدأ

- الصفحات كاملةً في `Legacy`. يُنقل **المنطق** منها إلى `Services` وتستدعيه، ولا تُنقل الصفحة.
- `Services` و`Validation` منطقٌ عامّ لا يحمل اسم صفحة. كل حالة استخدام تأخذ ما يخصّ الصفحة **معاملاتٍ**، فتعمل للعملاء والموردين والخزائن والأصول بالكود نفسه.
- حالة الاستخدام تفعل شيئاً واحداً. ما يجمع شيئين يُركَّب من اثنين، لا دالةٌ شاملة معقّدة.
- الحساب له موضعٌ واحد معروف. لا دالة تجلب البيانات وتحسب معاً.

## ما وجدته في الكود

| الموضع | المشكلة |
|---|---|
| `Services/Documents/Postings` | تسعة ملفات باسم الصفحات (`SalesInvoicePosting`…). أربعة منها قيدٌ واحد يختلف باتجاهه، وأربعة أخرى قيدٌ بطرفين. منطقٌ مكرّر لكل صفحة |
| `TreeAccount` + `AccountTree` + `IAccountLinkedService` | الحساب مكتوب في ثلاثة مواضع بثلاث قواعد: من الصفحة، ومن الشجرة، ودوال الربط الثلاث مكرّرة في الأطراف والخزينة والموظف |
| حساب الأصل ومجمّعه | «حساب + مجمّع» مبنيٌّ مرتين: في `AssetService` وفي `CategoryService`، واسم المجمّع دالةٌ في `CategoryService` يستدعيها الأصل |
| محرّكا تحقّق | `ValidatorBase` و`Rules.For<T>()` في `3.Domain`، يكتبان الرسائل نفسها. سبعة متحقّقين على الأول وعشرة على الثاني |
| المتحقّقون | «كود مطلوب + اسم مطلوب + طول الاسم + هاتف + بريد + مبلغ غير سالب» مكرّرة حرفياً في العميل والمورد والموظف والصنف والأصل |
| تحقّق داخل الخدمات | «السطور ≥ ١»، «الكمية > ٠»، «المبلغ > ٠»، «الشهر بين ١ و١٢» مكتوبة في الخدمات لا في المتحقّق |
| رسائل المحرّك | قوالب «{الحقل} مطلوب» عربيةٌ ثابتة في `3.Domain` لا تصل القاموس |
| الحساب في ثلاث طبقات | صافي السطر: `DocumentTotals` في `3.Domain`، ونسخةٌ ثانية بتقريبٍ مختلف في `6.UI/LineComputeEngine` |
| الحساب في الكيان | `GrossPay` و`TotalWithheld` في `PayrollLine`، و`BookValue` و`GainOrLoss` في `AssetDisposal`، و`Difference` في `AssetRevaluation` |
| الحساب في الخدمة | صافي الراتب مكتوب مرتين في `PayrollService` بصيغتين، والأجر اليومي والساعة والإضافي والغياب داخل دالةٍ تجلب البيانات |
| الحساب في التقارير | الربح الإجمالي والتشغيلي والتدفق النقدي داخل خدمات التقارير مع جلب البيانات |

## أين يسكن كل نوع — قاعدةٌ واحدة

| النوع | الموضع | مثال |
|---|---|---|
| صيغة حساب نقية (بلا قاعدة بيانات) | `3.Domain/Calculations` | صافي السطر، صافي الراتب، القسط، الدفترية، الربح والخسارة، الربح الإجمالي |
| تحقّق شكل الإدخال (مطلوب، طول، صيغة، موجب، مدى) | `4.Application/Validation` بمحرّكه ومجموعاته | الكود والاسم والهاتف والسطور والكمية |
| حارس يحتاج بيانات (فريد، له قيود، فترة مقفلة، رصيد لا يكفي) | `4.Application/Services/…/Guards` | له أبناء، له قيود، النقدية، المخزن |
| جلب البيانات ثم استدعاء الصيغة | `4.Application/Services` | يجلب ثم يستدعي القاعدة، ولا يكتب صيغة |
| الواجهة | `6.UI` · `7.Composition` | تعرض وتستدعي الصيغة من `3.Domain`، ولا تحسب بنفسها |

## لماذا الحساب في `3.Domain/Calculations` لا في `Services`

- **الحساب النقي لا يلمس قاعدة البيانات**، و`3.Domain` هي الطبقة النقية الوحيدة. لو سكن مجلداً في `Services` لجاور الجلب والمعاملات، وهذا ما تريد منعه.
- **كل الطبقات تصل إليه**: الخدمة والتقرير والواجهة تستدعي الصيغة نفسها. فلا نسخة ثانية في `6.UI` كما هو الآن في `LineComputeEngine`.
- **ما يحتاج بيانات يُقسَم إلى اثنين**: الخدمة تجلب، والصيغة في `Calculations` تحسب. مثال ذلك: `StockMove` يجلب الحركات، و`InventoryCosting` يحسب التكلفة.
- **لماذا مجلدٌ جديد لا `Rules`**: `3.Domain/Rules` اليوم يخلط محرّك التحقق (`RuleSet`) بالصيغ (`DepreciationRules` و`InventoryCosting`). الفصل يعطي كل نوعٍ موضعه: الصيغ في `3.Domain/Calculations`، ومحرّك التحقق كله في `4.Application/Validation`. وبهذا يصل التحقق إلى القاموس، فتُكتب رسائله بمفاتيح لا بقوالب عربية ثابتة في `3.Domain`.

## الهيكل المقترح

```
3.Domain/
├── Entities/               بيانات فقط، بلا خصائص محسوبة
└── Calculations/           كل صيغة حساب نقية — الموضع الوحيد للحساب
    ├── LineCalc.cs             صافي السطر والخصم والضريبة والخصم والإضافة ومجاميعها   (من Helpers/DocumentTotals)
    ├── PayrollCalc.cs          الاستحقاق، الاستقطاع، الصافي، الأجر اليومي والساعة، الإضافي، الغياب
    ├── AssetCalc.cs            الدفترية، الربح والخسارة، فرق التقييم، القسط   (من DepreciationRules والكيانات)
    ├── InventoryCosting.cs     ✔ يُنقل كما هو
    ├── StatementCalc.cs        الربح الإجمالي والتشغيلي، التدفق، التوازن   (من خدمات التقارير)
    └── FiscalPeriodCalc.cs     ✔ يُنقل كما هو

4.Application/Validation/   التحقق كله هنا — يُنقل من 3.Domain
├── Engine/                 RuleSet · ValidationResult · IValidator   (يُحذف ValidatorBase المكرّر)
├── Groups.cs               مجموعات بالمعاملات: CodedNamed(maxName) · Contact() · NonNegative(field) · Lines(min) · Qty()
└── <كيان>Validator.cs       يركّب المجموعات، ولا يكتب إلا قاعدته الخاصة (سعر البيع ≥ الأدنى، أب الفئة)

4.Application/Services/
├── Ledger/Accounts/        الحساب — ثلاث حالات منفصلة + مزدوج
│   ├── AccountSpec.cs          المعاملات: الجذر أو حقل الأب، حقل الحساب، الاسم، ورقي، والمرآة إن وُجدت
│   ├── AddTreeAccount.cs       إضافة حساب في الشجرة وحدها
│   ├── AddEntityAccount.cs     إضافة حساب لكيان صفحة (عميل، مورد، خزينة، موظف، أصل، فئة)
│   ├── AddLinkedAccount.cs     إضافته من الشجرة فينشأ كيانه، ومن الصفحة فينشأ حسابه — يركّب الاثنين
│   ├── AddMirroredAccount.cs   حساب + مجمّعه (يستعمله الأصل والفئة وأي كيان بمرآة)
│   └── RenameAccount.cs · CloseAccount.cs
├── Ledger/Entries/         القيد — بالمعاملات لا باسم المستند
│   ├── PostEntry.cs            قيدٌ بسطور   (Posting الحالي)
│   ├── PostTwoSided.cs         مدين/دائن بمعاملتين: السند، الشيك، الاقتناء، الإهلاك، التقييم
│   ├── TradeEntry.cs           البيع والشراء وفاتورتهما ومرتجعهما بمعاملتَي الاتجاه والمرتجع — يحلّ محلّ الملفات الأربعة
│   ├── OpeningEntry.cs         ✔
│   └── ReverseEntry.cs
├── Ledger/Guards.cs         ✔
├── Documents/               DocumentService · DocumentPull · StockMove · StatusChange(جدول الحالات معاملة)  ✔
└── Entities/                EntityService العامّ يستعمل Accounts بدل TreeAccounts
```

وتُحذف: `Documents/Postings/*` التسعة، و`TreeAccount.cs`، ودوال `IAccountLinkedService` الثلاث المكرّرة، و`ValidatorBase`، و`6.UI/LineComputeEngine`، والخصائص المحسوبة في الكيانات.

## مثال: الحساب بالمعاملات

```csharp
// العميل والمورد والخزينة والموظف — الكود نفسه
AddEntityAccount.Run(db, customer, new AccountSpec(Root: SettingKeys.Accounts.Customers, Field: nameof(Customer.AccountCode)));

// الأصل والفئة — الكود نفسه
AddMirroredAccount.Run(db, asset, new AccountSpec(ParentFrom: category.AccountCode, Field: nameof(Asset.AccountCode),
                                                  Mirror: new(ParentFrom: category.DepreciationAccountCode, Field: nameof(Asset.DepreciationAccountCode))));

// البيع والشراء وفاتورتهما ومرتجعهما — دالةٌ واحدة
TradeEntry.Lines(Trade.Sale, isReturn: false, party, accounts, totals, cost);
```

## صفحة `Legacy` تجمع المنطق من `Services` بالمعاملات

```csharp
// Legacy/Sales/SalesInvoiceService — تجميعٌ فقط
var prepared = TradeLines.Prepare(products, pulls, dto.Lines, totals => TradeAccounts.Sales(settings, totals));
var lines    = TradeEntry.Lines(Trade.Sale, isReturn: false, customer.AccountCode, accounts, totals, cost);
var entryId  = entries.Post(db, date, description, source, lines);          // Services/Ledger/Entries
AddEntityAccount.Run(db, customer, specs);                                   // Services/Ledger/Accounts
```

### ما يُستخرج من صفحات `Legacy` الحالية

| من الصفحة | المنطق المستخرج | إلى |
|---|---|---|
| `JournalService` | التحقق من القيد، الترقيم، الإدراج، الترحيل، إلغاء الترحيل، الحذف، إعادة حساب الأرصدة، حارس النقدية، «قابلٌ للعكس» | `Services/Ledger/Entries` |
| `FiscalPeriodService` | «التاريخ في فترةٍ مفتوحة» | `Services/Ledger/PeriodGate` |
| `AccountService` | توليد كود الابن، إنشاء حساب تحت أب، قاعدة الأب الورقي، التسمية، الحذف وإعادة الأب ورقياً | `Services/Ledger/Accounts/*` |
| `TreeAccounts` | فتح حساب الكيان وتسميته وإغلاقه | `Services/Ledger/Accounts/*` |

وبعدها لا يعتمد شيءٌ في `Services` على خدمة صفحة: `Posting` يستدعي `Entries`، والحساب يستدعي `AddTreeAccount`.

## ترتيب التنفيذ

| # | الخطوة | يُحذف بعدها |
|---|---|---|
| ٠ | القيد والفترة: `Entries` و`PeriodGate` مستخرجان من `JournalService` و`FiscalPeriodService`، و`Posting` يستدعيهما | — |
| ١ | الحساب: `AccountSpec` والحالات الخمس مستخرجةً من `AccountService` و`TreeAccounts`، وتستدعيها صفحة الشجرة والأطراف والخزينة والموظف والفئة والأصل | `TreeAccount.cs` · دوال الربط الثلاث · `Mirror` في الفئة |
| ٢ | القيد: قلب القيد (التحقق، الإنشاء، الترحيل، الحذف، باب الفترة) مستخرجاً من `JournalService` إلى `Ledger/Entries`، ثم `TradeEntry` و`PostTwoSided` بالمعاملات | `Postings/*` |
| ٣ | الحساب النقي إلى `3.Domain/Calculations` | `LineComputeEngine` · الخصائص المحسوبة · الصيغ داخل الخدمات والتقارير |
| ٤ | التحقق: نقل المحرّك من `3.Domain` إلى `4.Application/Validation` بمفاتيح رسائل، ومجموعات عامّة، ونقل تحقّق الخدمات إلى المتحقّقين | `ValidatorBase` · التكرار في المتحقّقين |
| ٥ | تحديث `ARCHITECTURE` و`RULES` و`07-ServicesReport`، ثم بناء و`check.sh` | — |

## ما يُحسم منك

1. **حقول الواجهة** (`IsRequired` و`MaxLength` في `8.Modules`) تكرّر المتحقّق. أقترح أن يبقى المتحقّق هو المصدر، وتبقى الواجهة كما هي الآن، ويُحسم ربطهما مع مرحلة الإعلان.
2. **الخصائص المحسوبة في الكيانات** تُحذف ويحلّ محلّها `Rules`، فيبقى الكيان بيانات فقط كما تقول القاعدة.
3. **مقاييس التقارير** (الربح الإجمالي، التدفق) تنتقل إلى `StatementCalc` في `3.Domain/Calculations`.
