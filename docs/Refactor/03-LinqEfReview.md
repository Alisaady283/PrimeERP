# 03 — المرحلة الأولى: مراجعة LINQ و Entity Framework

السؤال: أين يُكتب بخطّ اليد ما يقدّمه EF Core أو LINQ جاهزاً، وأين يُستعلم بطريقةٍ تُضاعف عدد الذهاب إلى القاعدة؟
النطاق: `2.Data` (حيث يسكن الاستعلام) و`4.Application` (حيث يُستهلك). المواضع مربوطة بأسطرها.

---

## الحكم في فقرة

التحويل إلى EF تمّ على مستوى **الاستعلام** ولم يتمّ على مستوى **النموذج**. النموذج جداولُ بلا علاقات: السطور مُتجاهَلة (`Ignore(x => x.Lines)`)، والأسماء المرتبطة خصائص مُتجاهَلة على الكيان، ولا مرشِّح عامّ للحذف الناعم، ولا معترِض للحفظ. فأعاد `RepositoryBase` بناء هذه الميزات بيده (`Live` · `Stamp` · `WithNames` · ترتيب الحذف اليدوي)، وعوّضت الخدمات غيابها باستعلامٍ لكل صفّ. النتيجة: EF مستعمَلٌ كطبقة LINQ فوق جداول، لا كـORM.

| المؤشّر | العدد |
|---|---:|
| علاقات مُعلَنة في النموذج (`HasOne`/`HasMany`) | 0 |
| `Include` | 0 |
| `ExecuteUpdate` / `ExecuteDelete` | 0 |
| مرشِّحات عامّة `HasQueryFilter` | 0 |
| خصائص عرض مُتجاهَلة على الكيانات (`CategoryName`، `EmployeeName`…) | 17 |
| ختم مستخدمٍ يدوي في الخدمات (`CreatedBy = CurrentUser`…) | 43 |
| ختم وقتٍ يدوي في المستودعات (`UpdatedAt = DateTime.Now`…) | 15 |
| دوال تحويل إلى DTO تستعلم لكل صفّ | 21 |
| متحقّقٌ يُنشأ في كل نداء (`new XValidator()`) | 22 |

---

## أ. ما يُبنى يدوياً وله بديلٌ جاهز

مرتّبٌ بالأثر: الأعلى يُسقط أكثر الأسطر ويصلح أكثر الشاشات.

| # | المكتوب يدوياً | الموضع | البديل الجاهز | ما يسقط |
|---|---|---|---|---|
| أ١ | **الحذف الناعم**: `Live()` يفتح سياقاً ليسأل النموذج عن `IsDeleted` ثم يضيف `Where(!EF.Property…)`، ويُستدعى في كل استعلام بيده | [RepositoryBase:33](../../2.Data/Repositories/Base/RepositoryBase.cs#L33) وكل مستودع | `HasQueryFilter(e => !e.IsDeleted)` لكل كيان يرث `BaseModel`، بحلقةٍ واحدة في `OnModelCreating`؛ و`IgnoreQueryFilters()` حيث يُقصد المحذوف | `Live` واستدعاءاته، ويُغلق فخّ `GetAll` الأساسي الذي لا يستبعد المحذوف ([السطر 182](../../2.Data/Repositories/Base/RepositoryBase.cs#L182)) |
| أ٢ | **ختم الإنشاء والتعديل والحذف**: `Stamp()` بالانعكاس، و`UpdatedAt = DateTime.Now` في 15 موضعاً، و`CreatedBy = CurrentUser` في 43 | [RepositoryBase:170](../../2.Data/Repositories/Base/RepositoryBase.cs#L170) · الخدمات | `SaveChangesInterceptor` واحد يمرّ على `ChangeTracker.Entries<BaseModel>()`: يختم `Added`/`Modified`، ويحوّل `Deleted` إلى تعطيلٍ ناعم | 58 سطر ختم، و`SoftDelete` يصير `Remove` عادياً |
| أ٣ | **الأسماء المرتبطة**: جلب الصفحة ثم استعلامٌ ثانٍ بالمعرّفات ثم قاموسٌ ثم إسنادٌ يدوي | [RepositoryBase.WithNames](../../2.Data/Repositories/Base/RepositoryBase.cs#L96) · 17 خاصية `Ignore` | علاقات تنقّل (`HasOne(p => p.Category)`) وإسقاطٌ في الاستعلام نفسه: `Select(p => new { p, CategoryName = p.Category.Name })` — استعلامٌ واحد للصفحة | الاستعلام الثاني لكل جدول أسماء، والإسناد اليدوي |
| أ٤ | **الرأس والسطور**: `Ignore(Lines)`، و`InsertLine` يحفظ كل سطرٍ بذهابٍ مستقلّ، و`DeleteDocument` يرتّب الحذف بيده «لأن النموذج بلا علاقة» | سبعة مستودعات: الأسس الأربعة + [Journal](../../2.Data/Repositories/JournalRepository.cs) · [Payroll](../../2.Data/Repositories/PayrollRepository.cs) · [StockTransfer](../../2.Data/Repositories/StockTransferRepository.cs) — راجع [InvoiceRepositoryBase:41](../../2.Data/Repositories/Base/InvoiceRepositoryBase.cs#L41) | `HasMany(h => h.Lines).WithOne().HasForeignKey(…)`: الرأس بسطوره يُضاف بـ`Add` واحد و`SaveChanges` واحد، ويُقرأ بـ`Include`، ويُحذف بـ`ExecuteDelete` | خمس دوال × سبعة مستودعات، وذهابٌ لكل سطر |
| أ٥ | **الكتابة صفّاً صفّاً**: تحميل الكيان متتبَّعاً ثم تعديل حقلٍ ثم `SaveChanges` | `SetBalance` · `SetIsLeaf` · `UpdateName(ByAccountCode)` · `SetJournalEntryId` · `SetPosted` · `SoftDelete` · `DeleteBySource` · `DeleteLines` · حذف القوائم | `ExecuteUpdate(s => s.SetProperty(…))` و`ExecuteDelete()` — جملةٌ واحدة بلا تحميل | ذهابان لكل كتابة. تنبيه: يتجاوزان المعترِض، فيُختم `UpdatedAt` صراحةً فيهما |
| أ٦ | **التجميع في الذاكرة**: جلب كل سطور الحساب المرحَّلة ثم `.Sum()` في C# | [AccountService:317](../../4.Application/Services/Accounting/AccountService.cs#L317) و[320](../../4.Application/Services/Accounting/AccountService.cs#L320) و[356](../../4.Application/Services/Accounting/AccountService.cs#L356) و[368](../../4.Application/Services/Accounting/AccountService.cs#L368) · [JournalService:279](../../4.Application/Services/Accounting/JournalService.cs#L279) | `…Where(…).Sum(l => l.Debit - l.Credit)` داخل الاستعلام يُترجَم `SUM` في SQL | تحميل تاريخ الحساب كاملاً **عند كل ترحيل** لكل حسابٍ في القيد — الصندوق والمبيعات يكبران بلا حدّ |
| أ٧ | **إعادة حساب كل الأرصدة** باستعلامٍ لكل حساب ورقي | [AccountService:326](../../4.Application/Services/Accounting/AccountService.cs#L326) · [PartyServiceBase:153](../../4.Application/Services/Parties/PartyServiceBase.cs#L153) | `GroupBy(l => l.AccountCode).Select(g => new { g.Key, Sum = … })` — استعلامٌ واحد. والنمط موجود: [JournalRepository.GetAccountSums](../../2.Data/Repositories/JournalRepository.cs) | N استعلام → 1 |
| أ٨ | **الترقيم والترشيح في الذاكرة** | [AccountService.GetPaged:65](../../4.Application/Services/Accounting/AccountService.cs#L65) · [BuilderCrudServiceBase.FindPaged](../../4.Application/Services/Builder/BuilderCrudServices.cs#L49) · تقرير أرصدة المخزون | `RepositoryBase.Page(page, size, Shape, Order)` — **موجود ومستعمل في كل مكان آخر** | مسارٌ موازٍ يخالف «العدّ والقطع في موضع واحد» |
| أ٩ | **حلّ الأكواد سطراً سطراً**: `_products.GetByCode(l.ProductCode)` داخل حلقة السطور | 7 خدمات — [المرجع §4.5 في 02](02-Logic.md) | `GetByCodes(codes)` باستعلام `codes.Contains(p.Code)` ثم قاموس. **موجود**: [AccountRepository.GetByCodes](../../2.Data/Repositories/AccountRepository.cs#L100) | سطرٌ = استعلام → مستندٌ = استعلام |
| أ١٠ | **فحص الوجود بالعدّ أو بالتحميل** | `Count(…) > 0` في 7 مواضع بالمستودعات · `GetPulledBySource(…).Count > 0` ×6 يحمّل قاموساً ليسأل «هل يوجد» · `_assets.GetPaged(1,1).Total > 0` · `GetAll().Count > 0` | `Any(…)` | عدٌّ كامل → `EXISTS` |
| أ١١ | **جلب الكل ثم التقاط واحد** | [UserService:48](../../4.Application/Services/Security/UserService.cs#L48) · [BackupService:148](../../4.Application/Services/Backup/BackupService.cs#L148) · `BuilderCrudServiceBase.FindById` · قائمة المخازن لكل صفّ | استعلامٌ موجَّه بالمعرّف أو بالمفتاح | الجدول كاملاً لسطرٍ واحد |
| أ١٢ | **سقوفٌ مكتوبة** بدل استعلام تقرير | `GetPaged(1, 5000)` ×5 · `GetPaged(1, 100000)` ×2 · `int.MaxValue` ×2 | إسقاطٌ مخصَّص للتقرير في مستودعه، بلا سقف ولا تحويلٍ إلى DTO كامل | صفوفٌ تختفي بصمت بعد السقف، وكلفة `ToDto` لكل صفّ |
| أ١٣ | **التواريخ نصوصاً**: `EntryDate`/`StartDate` نصّ `yyyy-MM-dd`، و`ParseExact`/`ToString` و`string.Compare` في كل مكان | القيود والفترات المالية | `HasConversion` بين `DateTime` والنصّ نفسه: العمود لا يتغيّر، وLINQ يقارن تواريخ | التحويل اليدوي كلّه. تغيير نوع الخاصية يمسّ مستهلكيها — بندٌ اختياري |
| أ١٤ | **خيارات السياق تُبنى في كل `Open`** | [DbContextFactory:12](../../2.Data/Core/DbContextFactory.cs#L12) | خيارات مخزَّنة لكل (مزوّد، سلسلة اتصال)، أو `PooledDbContextFactory` | كلفة بناءٍ متكرّرة لكل استعلام |
| أ١٥ | **`RowVersion` بلا عمل**: معرَّف على كل كيان ولا يُضبط رمز تزامن، و`ErrorCode.ConcurrencyConflict` بلا مستعمل | [BaseModel:17](../../3.Domain/Entities/Common/BaseModel.cs#L17) | `IsConcurrencyToken()` + زيادته في المعترِض + ترجمة `DbUpdateConcurrencyException` | لا حماية اليوم من كتابةٍ فوق كتابة. و`Modify` يكتب كل الأعمدة من لقطةٍ قُرئت خارج المعاملة، ومنها `Balance` |
| أ١٦ | **نسخ التفصيل من الملخّص** يدوياً، أو بالانعكاس في الرواتب | 6 خدمات + [PayrollService:313](../../4.Application/Services/HR/PayrollService.cs#L313) | `ToDto<T>() where T : XDto, new()` — **موجود** في [ChequeService:224](../../4.Application/Services/Cheques/ChequeService.cs#L224) | ~90 سطر نسخ |
| أ١٧ | **متحقّقٌ جديد في كل نداء** | 22 موضعاً | `static readonly` — **موجود** في [ChequeService.LineRules](../../4.Application/Services/Cheques/ChequeService.cs) | تخصيصٌ لكل نداء |

---

## ب. N+1 — عدد الاستعلامات لكل شاشة

العدّ من قراءة الكود، لصفحةٍ بعشرين صفّاً. «بعد» هو ما تصير إليه بالبنود أ٣ وأ٦ وأ٩.

| الشاشة | ما يحدث لكل صفّ | قبل | بعد |
|---|---|---:|---:|
| قائمة فواتير البيع (والثلاث الأخرى) | `CustomerService.GetById` كاملة: عميل + «هل له قيود» + حسابه، ثم قائمة المخازن كاملة — [SalesInvoiceService:235](../../4.Application/Services/Sales/SalesInvoiceService.cs#L235) | ~82 | 2 |
| قائمة العملاء أو الموردين | «هل له قيود» + اسم حسابه — [CustomerService:266](../../4.Application/Services/Parties/CustomerService.cs#L266) | ~42 | 2–3 |
| قائمة السندات | اسم الطرف عبر خدمة الطرف كاملة + الخزينة ثم رصيدها — [VoucherServiceBase:246](../../4.Application/Services/Vouchers/VoucherServiceBase.cs#L246) | ~100 | 2–3 |
| قائمة الشيكات | الطرف + الخزينة ورصيدها — [ChequeService:224](../../4.Application/Services/Cheques/ChequeService.cs#L224) | ~80 | 2–3 |
| قائمة الأذون والتحويل | قائمة المخازن كاملة + سطور المستند لجمع الكمية — [StockAdjustmentServiceBase:157](../../4.Application/Services/Inventory/StockAdjustmentServiceBase.cs#L157) | ~42 | 2 |
| قائمة مستندات الدورة | سطور المستند لجمع الكمية والقيمة — [CycleDocumentServiceBase:166](../../4.Application/Services/Documents/CycleDocumentServiceBase.cs#L166) | ~22 | 2 |
| قائمة القيود | الفترة المالية لكل قيد — [JournalService:568](../../4.Application/Services/Accounting/JournalService.cs#L568) | ~43 | 3 |
| **تفصيل قيد بعشرة سطور** | لكل سطر: `AccountService.GetByCode` تحمّل **شجرة الحسابات كاملة** + «هل له قيود» — [JournalService:591](../../4.Application/Services/Accounting/JournalService.cs#L591) · [AccountService:118](../../4.Application/Services/Accounting/AccountService.cs#L118) | ~31 + عشر شجرات | 2 |
| **شجرة الحسابات** | «هل له قيود» لكل حساب، و«هل له أبناء» بمسح القائمة لكل عقدة — [AccountService:626](../../4.Application/Services/Accounting/AccountService.cs#L626) | N+1 و O(N²) | 2 |
| **ميزان المراجعة** | `GetLeaves` تمرّ بالتحويل نفسه لكل ورقة، ثم `GetPaged(1, 100000)` للأسماء — [JournalService:387](../../4.Application/Services/Accounting/JournalService.cs#L387) | N+3 | 3 |
| **تقرير أرصدة العملاء** | حتى 5000 عميل، لكلٍّ تحويلٌ باستعلامين ثم تحميل سطور حسابه لجمعها — [PartyReportService:27](../../4.Application/Reporting/PartyReportService.cs) | ~3N | 1 |
| **تقرير أرصدة المخزون** | تاريخ الحركات كلّه إلى الذاكرة ثم `GroupBy` — [StockReportService:47](../../4.Application/Reporting/StockReportService.cs#L47) | 3 + كل الحركات | 1 مجمَّع |
| تقرير حركة المخزون | الصنف لكل حركة، حتى 500 — [StockReportService:104](../../4.Application/Reporting/StockReportService.cs#L104) | ~500 | 2 |

وأخطرها ليس في القوائم بل في **الكتابة**: كل خدمةٍ تستدعي `IAccountService.GetByCode` أو `GetById` لتقرأ حساباً واحداً — الخزينة لرصيدها، والسندات لفحص الرصيد، والرواتب للسلفة، والأصول للجذر — تحمّل الشجرة كاملة في كل نداء ([AccountService:107](../../4.Application/Services/Accounting/AccountService.cs#L107)).

---

## ج. الخلل الوظيفي المكتشف أثناء المراجعة

ليس من LINQ ولا EF، لكنه في المواضع نفسها التي ستُلمَس، فيُحسم قبلها أو معها.

| # | الخلل | الموضع | ما يحدث | الحالة |
|---|---|---|---|---|
| خ١ | **التعديل غير ذرّي**: حذفٌ في معاملة ثم إنشاءٌ في معاملة ثانية | الخمسة في [02 §4.6](02-Logic.md) | فشل الإنشاء (كود صنف خاطئ، رصيد لا يكفي، فترة مقفلة) بعد نجاح الحذف = **المستند الأصلي ضاع**. والرقم يتغيّر بكل تعديل. ومستندات الدورة لا تفحص «سُحب منه» قبل التعديل | مؤكَّد من الكود |
| خ٢ | **قيود المستندات تتجاوز الفترة المقفلة**: `Journal.Create(db, …)` و`Post(db, …)` و`Delete(db, …)` لا تسأل `IsOpen`، والنسخة الوحيدة التي تسأل هي نسخة الشاشة اليدوية | [JournalService:116](../../4.Application/Services/Accounting/JournalService.cs#L116) · [212](../../4.Application/Services/Accounting/JournalService.cs#L212) · [301](../../4.Application/Services/Accounting/JournalService.cs#L301) | فاتورة أو سند أو شيك أو مسير بتاريخٍ في فترةٍ مقفلة يُنشأ قيده ويُحذف بلا اعتراض | مؤكَّد من الكود |
| خ٣ | **رصيد الطرف المخزَّن يتقادم** | الفواتير والمرتجعات الأربع · السندات | يُعاد حسابه بعد الإنشاء فقط، خارج المعاملة، وبشرط صلاحية «تعديل العملاء» فيفشل بصمت لمستخدم المبيعات. لا يُعاد بعد الحذف، ولا بعد أي سند | مؤكَّد من الكود |
| خ٤ | **إرجاع الشيك من «محصَّل» أو «مصروف» لا يعكس قيده** | [ChequeService:58](../../4.Application/Services/Cheques/ChequeService.cs#L58) و[112](../../4.Application/Services/Cheques/ChequeService.cs#L112) | الانتقالات `Collected → InHand/Deposited` و`Paid → Issued` مسموحة، والقيد يُنشأ عند الدخول ولا يُعكس عند الخروج؛ فالتحصيل الثاني يضاعف القيد | محتمل — يُسأل: هل الإرجاع يُقصد به قيدٌ عكسي؟ |
| خ٥ | **الخزينة تنشئ حسابها خارج المعاملة** | [TreasuryService:37](../../4.Application/Services/Treasury/TreasuryService.cs#L37) و[142](../../4.Application/Services/Treasury/TreasuryService.cs#L142) | فشل حفظ الخزينة بعد إنشاء الحساب يترك حساباً يتيماً في الشجرة | مؤكَّد من الكود |
| خ٦ | **إقفال السنة يقرأ الرصيد التراكمي المخزَّن** لا مجاميع السنة | [FiscalPeriodService:244](../../4.Application/Services/Accounting/FiscalPeriodService.cs#L244) | وجود قيودٍ بتاريخ السنة التالية عند الإقفال يُدخلها في قيد إقفال السنة الحالية. والمجاميع المقيَّدة بالتاريخ جاهزة: `GetAccountSums(from, to)` | محتمل عند التداخل |
| خ٧ | **كتابةٌ بلا فحص صلاحية** | الأقسام · الوظائف · الوحدات · المخازن · الفئات · الخزينة — 18 دالة كتابة: إنشاء وتعديل وحذف في كلٍّ منها | أي مستخدم يُنشئ ويعدّل ويحذف فيها | مؤكَّد. إضافته تغيّر سلوك المستخدمين الحاليين فيُقرَّر |
| خ٨ | **مفتاح حذف مخالف**: الأذون والتحويل تُنشأ بمفتاحها الخاص وتُحذف بـ`Inventory.Delete` | [StockAdjustmentServiceBase:140](../../4.Application/Services/Inventory/StockAdjustmentServiceBase.cs#L140) · [StockTransferService:116](../../4.Application/Services/Inventory/StockTransferService.cs#L116) | من يملك الإذن لا يحذفه، ومن يملك الحذف العامّ يحذف كل الأذون | مؤكَّد من الكود |
| خ٩ | **القيد المُنشأ يُعاد كأنه مسوَّدة** | [JournalService:526](../../4.Application/Services/Accounting/JournalService.cs#L526) | `Create` يرحّله ثم يُرجع `IsPosted = false` و«مسوَّدة» | مؤكَّد، أثره عرضي |

---

## د. ما هو سليم ويُحفظ

- `RepositoryBase.Page` + `Shape` + `By`/`DocumentOrder`: مسارٌ واحد للعدّ والقطع والترتيب القطعي. هو المرجع الذي تُعاد إليه المسارات الموازية.
- `EF.Functions.Like` للبحث، و`AsNoTracking` للقراءة، و`Scope` للسياق المُعار، و`RunTransaction` للمعاملة.
- `JournalRepository.GetAccountSums` و`GetLineCounts` و`AccountRepository.GetByCodes`: أمثلةٌ صحيحة للاستعلام المجمَّع موجودةٌ في الكود نفسه، ويُنسج عليها.
- `SchemaSync`: يُلحق الناقص ولا يمسّ القائم.

## هـ. التعديل الجاري على الفرع `refactor/ef-linq`

- **`WithNames`** اتجاهٌ صحيح: استعلامٌ واحد لكل جدول أسماء بدل استعلامٍ لكل صفّ. يبقى جسراً حتى تُعلَن العلاقات (أ٣)، فيصير إسقاطاً في الاستعلام نفسه.
- **حذف القطع من `CrudServiceBase.GetPaged`** يخالف قراراً مثبتاً في `ARCHITECTURE.md` («العدّ والقطع في موضع واحد» و«قطعٌ مضمون في الأساس»). سلوكاً هو آمن اليوم لأن كل `FindPaged` يمرّ بـ`Page`، لكنه يُسقط حارساً موثَّقاً. فإما يبقى القطع، وإما يُعدَّل القرار في `ARCHITECTURE.md` في الالتزام نفسه — **قرارٌ لك**.
- **`EmployeeListTests`** يحرس الأسماء في صفحة الموظفين. وحسب `RULES.md` يُثبَت بإعادة الكسر قبل الالتزام.

---

## و. خطرٌ يُحسب قبل إعلان العلاقات

`SchemaSync` ينشئ الجداول الناقصة من النموذج نفسه. فإعلان `HasOne`/`HasMany` يجعل **القواعد الجديدة وحدها** تُنشأ بقيود مفاتيح أجنبية، والقواعد القائمة بلا قيود — نسختان من المخطط. العلاج سطرٌ في `SchemaSync.Missing`: تفريغ `ForeignKeys` من `CreateTableOperation` قبل التوليد، فتبقى العلاقات معرفةً في النموذج للاستعلام، ولا تصير قيداً في القاعدة. والكيانات المشتركة النوع (`SharedTypeEntity` للدورة والأذون) تحتاج إعلان علاقتها لكل اسم جدول.
