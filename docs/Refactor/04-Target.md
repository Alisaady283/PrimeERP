# 04 — المنطق والهيكل المقترحان لـ4.Application

مبنيٌّ على [مطابقة قائمتك بالقائم](01-Mapping.md). المبدأ: **ما يختلف بين الصفحات يُمرَّر معاملاً أو delegate، وما يتكرر يُكتب مرّة.** كل صفحةٍ تُعلن فرقها في أسطر، والقطع الخمس تنفّذه.

الشرط المسبق: المرحلة الأولى (LINQ و EF) تُنجز قبله، لأن القطع تستعمل استعلاماتها — راجع [03](03-LinqEfReview.md) و§٦ أدناه.

---

## ١. شجرة الملفات المقترحة

```
4.Application/
├── DTOs/                          كما هي + Common/PageFilter.cs (البحث والفرز لكل المرشِّحات)
├── Validation/                    كما هي + متحقّقات القوائم والحضور والحركات (التحقق المضمَّن اليوم)
├── Reporting/                     كما هي، فوق استعلامات تقارير في المستودعات
└── Services/
    ├── ServiceBase.cs             + Run: البوابة الوحيدة (صلاحية ← معاملة ← تدقيق)
    ├── Contracts.cs               IReadService · IWriteService — العقد الذي تجده المُصيِّرات بالاسم اليوم
    ├── EntityService.cs           أي كيان: إضافة · تعديل · حذف · حساباته في الشجرة · حرّاس حذفه
    ├── DocumentService.cs         أي مستند: رأس وسطور · قيده · مخزونه · سحبه · حرّاسه
    ├── NumberSequenceService.cs   كما هو
    │
    ├── Ledger/                    ← القطع الخمس، ولا شيء غيرها
    │   ├── TreeAccount.cs         حساب الشجرة المرتبط — م١ م٥ م٦ م٧ م٨
    │   ├── Entry.cs               القيد: بطرفين أو بسطور — م٢ م٣ م٤ م١٤
    │   ├── Posting.cs             الترحيل: رحّل · اعكس · استبدل — م٩ (ينتقل من Services/Posting.cs)
    │   ├── Guards.cs              حرّاس الحذف والرصيد — م١١ م١٢ م١٣
    │   └── Pull.cs                السحب من ← إلى — م١٠
    │
    ├── Accounting/   AccountService (الشجرة) · JournalService (المحرّك) · FiscalPeriodService · OpeningBalanceService
    ├── Parties/      CustomerService · SupplierService · PartyService (حدّ الائتمان والكشف فقط)
    ├── Treasury/     TreasuryService
    ├── HR/           EmployeeService · DepartmentService · JobTitleService · AttendanceService · EmployeeMovementService · PayrollService
    ├── Inventory/    ProductService · UnitService · WarehouseService · StockService · StockDocuments (الأذون الستة والتحويل)
    ├── Documents/    CycleDocuments (الأربعة) · DocumentLinkService
    ├── Sales/  Purchasing/          الفواتير والمرتجعات الأربع — أجسام قيودها كما هي
    ├── Vouchers/     Vouchers (القبض والصرف)
    ├── Cheques/      ChequeService (آلة الحالات) · ChequeDocumentService (المكيِّف)
    ├── Assets/       AssetService · AssetRevaluationService · AssetDisposalService · AssetDepreciationService
    ├── Common/       CategoryService
    └── Security/ Admin/ Backup/ Builder/ Print/     لا تُمسّ إلا بنقل النصوص
```

**يُحذف:** `Pipeline/` (ثمانية ملفات بلا مستهلك) · `Services/Posting.cs` (ينتقل إلى `Ledger/`) · `AssetMovementServiceBase.cs` (يذوب في `DocumentService`) · `PartyServiceBase` بصورته الحالية (إنشاؤه وتسميته وحذفه تنتقل إلى `EntityService`).

المجلد الجديد الوحيد `Services/Ledger/`، ويُضاف سطره إلى `Tools/Docs/folders.txt` مع أول ملف فيه.

---

## ٢. القطع الخمس

### ٢.١ حساب الشجرة — `Ledger/TreeAccount.cs`

الحساب المرتبط بالكيان وصفٌ من أربعة: أين يُنشأ، وباسمه أم بمرآته، وأين يُحفظ كوده، وورقةٌ أم تجميعي.

```csharp
public sealed record TreeAccount<T>(
    Func<T, string> Parent,          // كود الأب: من الإعدادات أو من فئة الكيان
    Func<T, string> Name,            // الاسم أو «مجمع » + الاسم
    Func<T, string> Get,
    Action<T, string> Set,
    bool Leaf = true);

public sealed class TreeAccounts                     // يُحقن
{
    public Result Open<T>(PrimeDbContext db, T entity, IReadOnlyList<TreeAccount<T>> accounts);
    public Result Rename<T>(PrimeDbContext db, T entity, IReadOnlyList<TreeAccount<T>> accounts);
    public Result Close<T>(PrimeDbContext db, T entity, IReadOnlyList<TreeAccount<T>> accounts); // والأب يعود ورقة
    public static Func<T, string> FromSetting<T>(string settingKey);
}
```

- `Open` يمرّ بجسم الشجرة **الواحد** في `AccountService` بـ`SkipAutoLink` دائماً، وداخل معاملة الكيان دائماً.
- حسابٌ واحد (عميل، مورد، موظف، خزينة) مصفوفةٌ بعنصر. موقعان (فئة، أصل) مصفوفةٌ بعنصرين.
- الاتجاه المعاكس (من الشجرة إلى الصفحة) يبقى كما هو: `LinkedRoots` + `IAccountLinkedService`، ويُنفَّذ مرّة في `EntityService`.

**يحلّ محلّ:** 7 نسخ إنشاء · 9 تسمية · 8 حذف · مدخلَي الشجرة المختلفين `Create`/`Create(db)`. وفحص «له قيود» لا يُكتب هنا بل في الحرّاس (§٢.٤)، و`EntityService` يضيفه تلقائياً لكل حسابٍ معلَن.

### ٢.٢ القيد — `Ledger/Entry.cs`

القيد وصفٌ: مصدرٌ ثابت، وتاريخٌ ووصفٌ من المستند، وسطور. والطرفان حالةٌ خاصة من السطور.

```csharp
public sealed record EntryLine(string Account, decimal Debit, decimal Credit, string Note = null);

public sealed record Entry<T>(
    string Source,
    Func<T, DateTime> Date,
    Func<T, string> Description,
    Func<T, IEnumerable<EntryLine>> Lines,
    Func<T, Result> Validate = null)                 // تحقّق الصفحة الخاص، إن وُجد
{
    public static Entry<T> TwoSided(string source, Func<T, DateTime> date, Func<T, string> description,
        Func<T, string> debit, Func<T, string> credit, Func<T, decimal> amount,
        Func<T, Result> validate = null);
}
```

| المستهلك | الإعلان |
|---|---|
| سند قبض | `TwoSided("ReceiptVoucher", v => v.Date, v => …, debit: v => v.Cash, credit: v => v.Party, amount: v => v.Amount)` |
| سند صرف | الإعلان نفسه بتبديل `debit` و`credit` |
| شيك محصَّل / مصروف | جدول `ChequeStatus → Entry<ChequeMove>`، لكل حالةٍ مرحِّلة طرفاها |
| إهلاك | `TwoSided(…, debit: c => expense, credit: c => c.Asset.MirrorAccount, amount: c => c.Amount)` |
| إعادة تقييم | `TwoSided(…, debit: r => r.Up ? r.Asset.Account : losses, credit: r => r.Up ? gains : r.Asset.Account, amount: r => Math.Abs(r.Difference))` |
| رصيد أول المدة للأصناف | `TwoSided(…, debit: inventory, credit: openingAdjustments, amount: d => d.Total)` |
| الرصيد الافتتاحي | `new Entry<CreateJournalDto>("OpeningBalance", _ => startDate, _ => fixedText, d => d.Lines)` |
| الفواتير والمرتجعات ×4 · الرواتب · الاستبعاد · إقفال السنة | `Lines` = دالة بناء السطور القائمة في كل خدمة، **منقولةٌ بنصّها** |

ترقيم السطور وإسقاط السطر الصفري وتحويلها إلى `CreateJournalDto` يحدث مرّة في `Posting`.

### ٢.٣ الترحيل — `Ledger/Posting.cs`

ثلاث حركات فقط، كلها داخل معاملة المستند، وكلها تمرّ بمحرّك القيود الواحد:

```csharp
public sealed class Posting                          // يُحقن
{
    public Result<int> Post<T>(PrimeDbContext db, Entry<T> entry, T document);
    public Result Reverse(PrimeDbContext db, int? entryId);
    public Result<int> Replace<T>(PrimeDbContext db, int? entryId, Entry<T> entry, T document);
}
```

`Post` = تحقّق الصفحة ← تحقّق القيد ← **الفترة مفتوحة** ← الخزينة والبنك لا يسلبان ← إنشاءٌ مرحَّل. و`Reverse` = الفترة ← الرصيد ← حذف. و`Replace` = الاثنان في المعاملة نفسها. الفترة تُفحص هنا مرّة فيُغلق خ٢، و`Replace` يُغلق خ١.

والترحيل المنفصل (شاشة القيود والرواتب) يبقى في `JournalService` و`PayrollService` كما هو.

### ٢.٤ الحرّاس — `Ledger/Guards.cs`

كل حارس دالةٌ `Func<T, Result>` تُصنع مرّة، وتُعلنها الصفحة في مصفوفة:

```csharp
public sealed class Guards                           // يُحقن
{
    public Func<T, Result> NoChildren<T>(Func<T, int> id);
    public Func<T, Result> NoEntries<T>(params Func<T, string>[] accounts);
    public Func<T, Result> NoMovements<T>(Func<T, int> productOrWarehouse);
    public Func<T, Result> NotPulled<T>(string documentType, Func<T, int> id);
    public Func<T, Result> PeriodOpen<T>(Func<T, DateTime> date);
}
```

وحارسا الرصيد لا يُعلَنان على الصفحات لأنهما في المحرّكين: الخزينة والبنك في `JournalService.EnsureCashStaysPositive` (يُحذف `EnsureFunds` و`EnsureAffordable` المسبق)، والمخزن في `StockService.RecordMovement`. كلاهما يقرأ رصيداً واحداً بمجموعٍ في SQL.

**يحلّ محلّ:** 6 نسخ «سُحب منه» · 5 صيغ «له قيود» · فحوص «له أبناء» المتفرقة. ويُكمل الناقص في الموظف والصنف والمخزن بإعلان سطر.

### ٢.٥ السحب — `Ledger/Pull.cs`

الإعلان في `CycleFlow` يبقى. المحرّك يصير عقداً مكتوباً بدل الانعكاس:

```csharp
public interface IPullSource
{
    Result<List<PullCandidate>> Open(IReadOnlyDictionary<string, object> match);
    Result EnsureAvailable(IEnumerable<(int LineId, decimal Qty)> lines);
}
```

`DocumentService` ينفّذه مرّة لكل المستندات باستعلامٍ واحد في المستودع: سطور المصدر ناقصاً مجموع المسحوب من `DocumentLinks`. و`PullService` في `7.Composition` يستدعيه بالنوع بدل `GetPaged(1, 500)` و`GetById` بالانعكاس. و`IPullSourceReader` في `3.Domain` يسقط.

---

## ٣. القاعدتان

### ٣.١ `EntityService<TEntity, TDto, TCreate, TUpdate, TFilter>` — أي كيان

```csharp
protected abstract TEntity New(TCreate dto);
protected abstract void Apply(TEntity entity, TUpdate dto);
protected abstract TDto ToDto(TEntity entity);
protected virtual IValidator<TEntity> Validator => null;
protected virtual string SequenceKey => null;
protected virtual IReadOnlyList<TreeAccount<TEntity>> Accounts => [];
protected virtual IReadOnlyList<Func<TEntity, Result>> DeleteGuards => [];
```

`Create` = `Run("Create", db => New ← تحقق ← كود ← إدراج ← TreeAccounts.Open)`. `Update` = تحميل ← `Apply` ← تحقق ← حفظ ← `Rename` إن تغيّر الاسم. `Delete` = `Guards.NoEntries` على الحسابات المعلَنة تلقائياً ← الحرّاس المعلَنة ← `Close` ← تعطيل. وقاعدةٌ واحدة تغطّي القوائم والكيانات والمرتبطة بحساب، لأن الحساب مصفوفةٌ قد تكون فارغة.

### ٣.٢ `DocumentService<THead, TLine, TRow, TDetail, TCreate, TFilter>` — أي مستند

```csharp
protected abstract Result<THead> Build(PrimeDbContext db, TCreate dto);   // جسم الإنشاء الخاص بكل عائلة
protected virtual Entry<THead> Ledger => null;                            // قيده
protected virtual StockMove<THead, TLine> Stock => null;                  // أثره المخزني: الاتجاه والمخزن، أو الزوج من ← إلى
protected virtual bool Editable => false;                                 // true ⇒ استبدالٌ ذرّي بالرقم نفسه
protected virtual IReadOnlyList<Func<THead, Result>> DeleteGuards => [];
```

يملك: الصفحات والتفصيل (بـ`ToDto<T>()` واحدة) · الإنشاء = `Build` ← حفظ الرأس بسطوره ← السحب ← المخزون ← `Posting.Post` · الحذف = الحرّاس (`NotPulled` تلقائياً) ← `Reverse` ← محو المخزون والسحب ← الرأس · التعديل = `Replace` أو رفضٌ بمفتاح نصٍّ واحد · وتنفيذ `IPullSource`.

و`StockMove` في الملف نفسه، بالشكل نفسه للقيد بطرفين: `(Func<THead,int> from, Func<THead,int> to)` للتحويل، أو `(direction, warehouse)` للأذون والفواتير.

---

## ٤. الصفحات بعد التحويل — أمثلة

**العميل**، كل ما يخصّه (والمورد مثله بحقل النوع):

```csharp
public class CustomerService : EntityService<Customer, CustomerDto, CreateCustomerDto, UpdateCustomerDto, CustomerFilter>, ICustomerService
{
    protected override string PermissionPrefix => "Customers";
    protected override string StringPrefix => "Str.Customer";
    protected override string EntityName => "Customers";
    protected override string SequenceKey => "Customer";
    protected override IValidator<Customer> Validator => Rules;
    protected override IReadOnlyList<TreeAccount<Customer>> Accounts => Account;

    private static readonly CustomerValidator Rules = new();
    private static readonly TreeAccount<Customer>[] Account =
        [new(TreeAccounts.FromSetting<Customer>(SettingKeys.Accounts.Customers), c => c.Name, c => c.AccountCode, (c, v) => c.AccountCode = v)];

    protected override Customer New(CreateCustomerDto d) => …;          // التحويلات الثلاث
    protected override void Apply(Customer c, UpdateCustomerDto d) => …;
    protected override CustomerDto ToDto(Customer c) => …;
}
```

**الأصل** يُعلن حسابين: `Parent = a => a.Category.AccountCode` و`Parent = a => a.Category.DepreciationAccountCode` بالاسم ومرآته، وقيد اقتنائه `TwoSided(debit: a => a.AccountCode, credit: a => a.FundingAccountCode, amount: a => a.PurchaseCost)`.

**السند** يصير `DocumentService` بلا سطور: `Ledger` بطرفين يتبادلان بنوعه، و`Editable = true`.

**إعادة التقييم** `DocumentService` بلا سطور: `Ledger` بطرفين بالإشارة، و`Build` يطبّق القيمة الجديدة على الأصل.

**الفاتورة** تُبقي جسمها: `Build` ينقل بناء السطور والإجماليات كما هو، و`Ledger.Lines` ينقل بناء سطور القيد كما هو، و`Stock` يعلن الصرف من مخزنها. والذي يسقط منها: القراءة والتحويل والحذف والترحيل اليدوي — احتراماً لقرار «لا تُدمَج» الأجسام الأربعة تبقى أربعة.

---

## ٥. البوابة والعقود والنتائج

- **`ServiceBase.Run`** يُبنى من `Tx(AuditAction, …)` القائمة بلا مستدعٍ: صلاحية ← معاملة تُلغى بالنتيجة لا بالاستثناء ← تدقيق. تسقط به ~40 كتلة `try/throw/catch`، ويُحذف `Pipeline` و`Require`.
- **`Result.Then`** في `3.Domain` بجوار `As<T>()` القائمة. تسقط به 71 نسخة من نقل الفشل يدوياً.
- **`IReadService`/`IWriteService`** ترثهما الواجهات القائمة بلا تغيير عضو، فلا يتأثر المُصيِّر اليوم.

---

## ٦. ما تحتاجه القطع من طبقة البيانات — المرحلة الأولى

| القطعة | تحتاج | البند في 03 |
|---|---|---|
| `TreeAccounts` · `Guards.NoEntries` | «أيّ هذه الحسابات عليه قيود» باستعلامٍ واحد، وحسابٌ واحد بلا تحميل الشجرة | أ٩ أ١٠ + §ب |
| `Posting` · حارس الخزينة | مجموع الحساب في SQL | أ٦ |
| `DocumentService` | `DocumentRepositoryBase` بـ`Lines` حقيقية وأسماء مُسقَطة وإجماليات | أ٣ أ٤ |
| `IPullSource` | استعلام «المتبقي من سطور المصدر» | جديد في `DocumentLinkRepository` |
| `EntityService` | مرشِّح الحذف العامّ، ومعترِض الختم، و`Set` المجمَّع | أ١ أ٢ أ٥ |
| الأرصدة | رصيد الطرف مُسقَطٌ من حسابه | أ٣ + خ٣ |

---

## ٧. ما ينتظر قرارك قبل بنائه

| البند | السؤال |
|---|---|
| م١٢ «لا حذف لما هو مرحَّل» | يخالف قرار «الحذف عكس» المثبت. هل المقصود: لا حذف في فترة مقفلة + لا حذف لقيدٍ يملكه مستند (القائم)؟ أم إلغاء الحذف عن كل مرحَّل؟ |
| خ٤ الشيك الراجع من حالةٍ مرحِّلة | يعكس قيده · أو يُمنع · أو السلوك الحالي مقصود |
| الفواتير الأربع | تنتقل قراءتها وحذفها وترحيلها إلى `DocumentService` وتبقى أجسامها — موافق؟ |
| `Pipeline` | يُحذف ويحلّ `Run` محلّه — موافق؟ |
| صلاحية الخدمات الست | تُضاف في `Run` — المفاتيح موجودة والواجهة تخفي الأزرار أصلاً |
