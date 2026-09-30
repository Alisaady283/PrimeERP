# الاتجاه: الصفحة تستدعي حالات الاستخدام

## المبدأ

- كل عملية لها حالة استخدام واحدة في `4.Application/Services`، ولا تُكتب مرتين.
- الص- الصفحة ملك `7.Composition` كما هي اليوم: `ModuleDefinition` في `8.Modules` والمُصيِّرات تبنيها. لا صفحات في `4.Application`.
- إعلان الصفحة يسمّي حالة الاستخدام التي تستدعيها وإعدادها، ولا خدمة لكل صفحة ولا نموذج عرض لكل صفحة.
- ما يخصّ صفحةً بعينها (مثل سطور قيد فاتورة البيع) يُكتب حالة استخدام مسمّاة في `Services`، والإعلان يشير إليها باسمها.
- `Legacy` هو القديم. يُفرَّغ عائلةً عائلة، وكل عائلة تُحوَّل تُحذف ملفاتها منه فوراً. لا يبقى مساران.

## هيكل 4.Application

`✔` منفَّذ · `…` مؤجَّل مع الإعلان

```
4.Application/
├── Services/                         المنطق — حالات الاستخدام وحدها
│   ├── Core/
│   │   ├── ServiceBase.cs            ✔ الصلاحية · Commit · الرسائل · الإعدادات
│   │   ├── CrudServiceBase.cs        ✔ القراءة والصفحة
│   │   ├── EntityService.cs          ✔ الكيان بحساباته وحرّاسه
│   │   └── (I)NumberSequenceService  ✔ الترقيم
│   ├── Ledger/
│   │   ├── Posting.cs                ✔ PostEntry · PostTwoSided · Reverse · EnsureReversible
│   │   ├── JournalLines.cs           ✔ سطور القيد بطرفيها، يُسقط الصفري ويرقّم
│   │   ├── OpeningEntry.cs           ✔ القيد الافتتاحي المتوازن
│   │   ├── TreeAccount.cs            ✔ حساب الكيان: فتح · تسمية · إغلاق · قيود
│   │   ├── Guards.cs                 ✔ له أبناء · له قيود · النقدية لا تنزل تحت الصفر
│   │   └── PartyBalance.cs           ✔ رصيد الطرف من حسابه
│   ├── Documents/
│   │   ├── DocumentService.cs        ✔ إنشاء · استبدال · حذف في معاملة
│   │   ├── DocumentPull.cs           ✔ السحب من ← إلى (كان DocumentLinkService)
│   │   ├── StockMove.cs              ✔ حركة المخزون وتكلفتها (كان StockService)
│   │   ├── StatusChange.cs           ✔ الحالة كترحيل: الدخول يُرحِّل والخروج يعكس
│   │   ├── TradeLines.cs · TradeAccounts.cs  ✔
│   │   └── Postings/                 ✔ SalesInvoice · SalesReturn · PurchaseInvoice · PurchaseReturn ·
│   │                                    StockDocument · Voucher · Cheque · Payroll · Asset
│   ├── Entities/
│   │   ├── EntitySpec.cs             ✔ إعداد حالة الاستخدام
│   │   ├── Rows.cs                   ✔ الكيان صفّاً والصفّ كياناً
│   │   ├── Lookup.cs                 ✔ القائمة البسيطة
│   │   └── Account.cs · LinkedEntity.cs   … مع الإعلان
├── Legacy/                           كل صفحة كاملةً هنا، وتستدعي منطقها من Services
│   └── Accounting · Admin · Assets · Backup · Builder · Cheques · Common · Documents · HR ·
│       Inventory · Parties · Print · Purchasing · Sales · Security · Treasury · Vouchers
├── Reporting/  Validation/  DTOs/
```

## حالات الاستخدام

### Ledger — القيد والشجرة

| حالة الاستخدام | ما تفعله | تُغطّي من قائمتك |
|---|---|---|
| `PostEntry(lines)` | قيدٌ بسطور يُنشأ مُرحَّلاً، يمرّ بباب الفترة وحارس النقدية | محرّك القيد لكل المستدعين |
| `PostTwoSided(debit, credit, amount)` | قيدٌ بطرفين مرنين فوق `PostEntry` | الخزينة، البنك، إعادة التقييم، التحصيل |
| `PostOpening(lines)` | `PostEntry` + تحقّق التوازن + مصدرٌ ثابت | القيد الافتتاحي متعدّد الأسطر |
| `ReverseEntry(id)` + `CanReverse(id)` | العكس في المعاملة، ويُرفض في فترة مقفلة أو إن أنزل النقدية تحت الصفر | عدم حذف المرحَّل، الرصيد لا يكون سالباً |
| `TreeAccount` | إعلان حساب الكيان: جذره من الإعدادات، حقله، ورقيٌّ أم تجميعي | حساب شجرة مرن لأي كيان |
| `OpenAccount` · `RenameAccount` · `CloseAccount` | فتح وتسمية وإغلاق حساب الكيان في معاملته | إضافة وتعديل وحذف حساب الشجرة |
| `AccountFromTree` | حسابٌ يُضاف في الشجرة تحت جذرٍ مُعلَن ينشئ كيانه عبر إعلان صفحته نفسه | مزدوج صفحة ↔ شجرة |
| مصفوفة `TreeAccount` | كيانٌ بحسابين في موضعين من الشجرة | مزدوج موقعين (الأصل: تكلفة + مجمّع) |
| `PartyBalance.Refresh` | رصيد الطرف من حسابه داخل المعاملة | حساب الصفحة |
| `Guard.NoChildren` · `Guard.NoEntries` | حارسان واحدان للشجرة وللصفحة معاً | لا حذف لما له أبناء أو قيود |

### Documents — المستند

| حالة الاستخدام | ما تفعله | تُغطّي من قائمتك |
|---|---|---|
| `CreateDocument(plan)` | تحقّقٌ ثم دالة كتابة في معاملة واحدة | الترحيل المرن العام |
| `ReplaceDocument` | عكس القديم وإنشاء الجديد في معاملة واحدة | التعديل الآمن |
| `DeleteDocument` | حرّاس ثم عكس القيد والمخزون والسحب | لا حذف للمرحَّل |
| `Pull(from → to)` | تحقّق الكمية المتبقية، تسجيل السحب، منع حذف المسحوب منه | السحب من ← إلى |
| `StockMove(direction)` | حركة مخزون بتكلفتها، ولا رصيد سالب للمخزن | رصيد المخزن لا يكون سالباً |
| `TradeLines.Prepare` + `TradeAccounts` | سطور الفاتورة ومجاميعها وحساباتها | الفواتير والمرتجعات |
| `StatusChange(from → to)` | تغيير حالة يعمل كترحيل: الدخول يُرحِّل قيده، والخروج يعكسه، والحالة الأولى وحدها تقبل التعديل والحذف | الشيكات، وأي مستند بحالات |

### Entities — الكيان

| حالة الاستخدام | ما تفعله |
|---|---|
| `EntitySpec` | إعدادها: المفتاح، بادئة النصوص، الترقيم، الحسابات، الحرّاس |
| `Lookup<T>` | إضافة وتعديل وحذف قائمة بسيطة (منجز باسم LookupPage: الأقسام، الوظائف، الوحدات، المخازن) |
| `LinkedEntity<T>` | كيان بحساب شجرة وحرّاس، وبالاتجاهين مع الشجرة |

والصفحة تبقى في `7.Composition`: `ModuleDefinition` يستدعي حالة الاستخدام عبر `RowPage` القائم هناك، ولا يُكرَّر شيء منها في `4.Application`.

## شكل الإعلان بعد التحويل

```csharp
// 8.Modules — إعلانٌ فقط
public static readonly EntitySpec Customers = new()
{
    Key = "Customers", Strings = "Str.Customer", NameLabel = "Str.Field.CustomerName", Sequence = "Customer",
    Accounts = { Account.Under(SettingKeys.Accounts.Customers, nameof(Customer.AccountCode)) },
    Guards   = { Guard.NoEntries }
};

public static readonly EntitySpec Assets = new()
{
    Key = "Assets", Strings = "Str.Asset", NameLabel = "Str.Field.AssetName",
    Accounts =
    {
        Account.UnderCategory(nameof(Category.AccountCode),             nameof(Asset.AccountCode)),
        Account.UnderCategory(nameof(Category.DepreciationAccountCode), nameof(Asset.DepreciationAccountCode), Mirror: true)
    },
    Posting = AssetPosting.Acquisition        // حالة استخدام مسمّاة في Services
};
```

والصفحة في `8.Modules` كما هي: `ViewModelFactory = RowPage.ViewModel<LinkedEntity<Customer>>(…)` و `Dialog = RowPage.Dialog<LinkedEntity<Customer>>(…)`.

## ترتيب التحويل

كل خطوة تُحوِّل عائلةً كاملة، وتحذف ملفاتها من `Legacy`، ثم بناءٌ و`check.sh`.

| # | العائلة | ما يُحذف من Legacy |
|---|---|---|
| ١ | القوائم: الأقسام، الوظائف، الوحدات، المخازن | ✔ منجز، ويُنقل من `Services/Pages` إلى `Services/Entities` بأسمائه الجديدة |
د أول المدة للأصناف | خدماتها |
| ٥ | الفواتير والمرتجعات الأربع | خدماتها، وتبقى حالات ترحيلها الأربع مسمّاة في Services |
| ٦ | السندات والشيكات (تغيير الحالة كترحيل، والعكس عند الخروج) | خدماتها |
| ٧ | القيود، القيد الافتتاحي، الفترات والإقفال، الرواتب | خدماتها — محرّك القيد ينتقل إلى `Ledger` |
| ٨ | الباقي: الحضور، حركات الموظفين، الأدوار، المستخدمون، التراخيص، وحدة البناء | ثم يُحذف `Legacy` كلّه |

## ما يُحسم منك قبل البدء

1. اسما المجلدين: `Services` للجديد و`Legacy` للقديم.
2. القوائم والكيانات تُعرض صفوفاً بلا DTO خاصّ. المستندات تحتفظ بـDTO رأسها وسطورها لأن سطورها مكتوبة الأنواع.
3. الاختبارات تُحدَّث وتُشغَّل بعد آخر عائلة فقط، وبإذنك.
