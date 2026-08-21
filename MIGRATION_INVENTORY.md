# جرد الترحيل — PrimeERP

هذا الملف جرد فعلي (بحث حقيقي في الكود، لا تخمين) للقطع المكررة بين الكود القديم (قبل إعادة البناء) والقطع الجديدة. حُدِّث آخر مرة: 2026-08-21 بعد **البند 1 من التصحيح المعماري (نظام الرموز)**.

## ✅ التصحيح المعماري — البند 1: نظام الرموز (2026-08-21) — توقف 1

بعد F.3.1، رصد المستخدم انحرافاً معمارياً حقيقياً (لا في هذا الملف الحالي فقط، بل عبر ثلاث نقاط): (1) الخدمات
بلا أساس مشترك (~2350 سطر مكرر مقدَّراً)، (2) رموز التصميم قيم مكررة لا سلسلة مراجع — `Palette.xaml` كان موجوداً
لكن `Colors.xaml` لا يشير إليه إطلاقاً (كل قيمة معادة كتابتها حرفياً)، (3) لا طبقة تركيب (صفحة = كود لا بيانات).
وجّه توجيهاً معمارياً شاملاً بديلاً لخطة "Phase T" السابقة (T.1–T.6، أساس خدمات فقط) — يغطي خمس طبقات في كل
نطاق (L1 ذرات → L2 تركيب → L3 أنماط → L4 قوالب → L5 تطبيق) عبر ستة بنود، بأربع نقاط توقف فقط. **البند 1 (نظام
الرموز) هو المُنفَّذ هنا، وحده — بقية البنود (طبقة بيانات، Pipeline منطقي، طبقة عرض، ترحيل 14 قطعة، طبقة تركيب) تنتظر توقف 1.**

### البنية الجديدة (`Resources/Design/`)

```
Identity/Default/Primitives.Color.xaml     L1 — قيم حرفية، أسماء بالدور (P.Color.Brand.*) لا بالصبغة (لا P.Color.Blue.*)
Identity/Corporate/Primitives.Color.xaml   L1 — حزمة بديلة (بنفسجي بدل الأزرق) — إثبات توقف 1
Semantic/Semantic.Light.xaml               L2 — نفس أسماء Colors.xaml القديمة حرفياً (BrandDefault...) — DynamicResource لـ P.Color.*
Semantic/Semantic.Dark.xaml                L2 — نفس النمط، الوضع الداكن
Theme.xaml                                 جذر الدمج — بديل Resources/Themes/Theme.xaml
```

`Palette.xaml`/`Colors.xaml`/`Colors.Dark.xaml`/`Resources/Themes/Theme.xaml` **حُذفت** (لا نُسخ قديمة متروكة).
`Typography.xaml`/`Metrics.xaml`/`Implicit.xaml`/`Components/*.xaml` بقيت في مكانها (`Resources/Themes/`) بلا
تغيير — راجع "ما تعمّد عدم فعله" أدناه.

### لماذا الأسماء بالدور لا بالصبغة (P.Color.Brand لا P.Color.Blue)

مثال المستخدم الحرفي في التوجيه كان `P.Color.Blue.600`. طُبِّق بتغيير مقصود: لو سُمّي المفتاح "Blue" في L1،
فحزمة هوية بديلة (Corporate) تريد علامة بنفسجية ستُضطر لتعريف مفتاح بلون مختلف تماماً عن اسمه (كذب بصري)، أو
لإجبار Semantic (L2) على تغيير أي مفتاح تشير إليه — وهذا يكسر الشرط الحاكم "استبدال حزمة الهوية = استبدال مجلد
واحد بلا لمس أي ملف آخر". التسمية بالدور (`Brand`/`Neutral`/`Success`/`Danger`/`Warning`/`Info`) تحل هذا: كل حزمة
هوية تُعيد تعريف نفس المفاتيح بقيم مختلفة، Semantic لا يتغيّر سطر واحد فيه. Success/Danger/Warning/Info تبقى
متطابقة القيمة عبر كل الحزم عمداً (دلالة عالمية لا تتبع العلامة) — Brand/Neutral فقط يتغيّران بين الحزم.

### ⚠️ الاكتشاف الحرج: DynamicResource المتداخل داخل مورد (لا FrameworkElement) له قواعد إعادة تقييم مختلفة تماماً

التوجيه طلب صراحة اختبار قيد WPF التقني تجريبياً قبل التعميم. الافتراض الأول (تعديل قاموس Primitives.Color.xaml
في مكانه، أو حتى استبدال `Application.Resources` بالكامل بشجرة **جاهزة مسبقاً بالكامل**) **فشل تجريبياً مرتين**
بعد 4 محاولات تشخيص فعلية (`IdentityServiceTests`/تجربة معزولة مؤقتة `DynamicResourceProbeTests`، حُذفت بعد
الاستخدام): عنصر `Border` حيّ متصل بنافذة معروضة فعلياً (`window.Show()`) لم يُحدِّث لونه (`SolidColorBrush`
مُخزَّنة سلفاً، `Color` نفسها `DynamicResource` متداخل لمفتاح `P.Color.*`) رغم أن القراءة المباشرة من القاموس
تعطي القيمة الصحيحة. السبب: `SolidColorBrush` كائن مورد مستقل لا `FrameworkElement` — ربط إعادة التقييم الخاص
به لا يلتقط تغييرات `MergedDictionaries` بعيدة عنه في الشجرة، ولا حتى استبدال شجرة كاملة **جاهزة سلفاً** دفعة
واحدة (`Application.Resources = fullyBuiltTree`).

**الحل الوحيد الذي أثبت عمله تجريبياً** (4 اختبارات حقيقية في `IdentityServiceTests` + عزل مؤقت أكّد السبب):
تعيين `Application.Resources` لقاموس **جديد فارغ من الداخل** أولاً (فيصبح "حياً" فعلياً)، ثم تعديل
`MergedDictionaries` الخاصة بذلك القاموس نفسه **كخطوة منفصلة لاحقة** (إزالة حزمة الألوان القديمة، إدراج الجديدة).
الترتيب حرج — بناء الشجرة كاملة أولاً ثم تعيينها دفعة واحدة يفشل رغم تطابق المحتوى النهائي حرفياً مع الحالة
الناجحة. مُوثَّق بالكامل في تعليق التوثيق أعلى `IdentityService.Apply` (`Services/Design/IdentityService.cs`).

Uri مطلق (`pack://application:,,,/{asm};component/...`) لا نسبي عمداً أيضاً — Uri نسبي يعتمد على
`Application.ResourceAssembly` (خاصية أُحادية الضبط لعمر العملية، تتجمّد على أول قيمة تُضبط بها ولا يمكن تصحيحها
لاحقاً)؛ اكتُشف أثناء بناء نفس الاختبار (أي مضيف ينشئ `Application` قبل هذه الخدمة قد يجمّدها على تجميعة خاطئة).

### إثبات توقف 1 (الاختبار الحيّ المطلوب صراحة)

`IdentityServiceTests.Apply_LiveSwap_ChangesResolvedColorOnAnAlreadyRenderedElement`: عنصر `Border` حقيقي داخل
`Window` معروضة فعلياً (`window.Show()`)، لونه من `BrandDefault` — يبدأ `#2563EB` (Brand.600 الافتراضي)، بعد
`IdentityService.Instance.Apply("Corporate")` يصبح `#7C3AED` (Brand.600 البنفسجي) **على نفس العنصر بلا إعادة
إنشائه** — بلا تعديل حرف واحد في `Semantic.Light.xaml` ولا في أي قطعة. + اختبار مقابل يثبت أن `Danger`/الحالات
الدلالية الأخرى **لا تتغيّر** بين الحزم (قرار مسجَّل، ليس سهواً).

### ما تعمّد عدم فعله (نطاق مُقلَّص صراحة، لا سهو)

- **`Typography.xaml`/`Metrics.xaml` لم تُنقل لـ `Identity/*/Primitives.Type|Space|Shape|Motion.xaml`**: القطع
  الحالية تستهلكها عبر `StaticResource` (لا `DynamicResource`) في مواضع كثيرة (مثال `Components/Buttons.xaml`
  `Height="{StaticResource HeightMd}"`) — تبديل هوية لا يُحدِّثها حيّاً بلا توحيد كل استهلاكها على
  `DynamicResource` أولاً، وهذا يقع ضمن ترحيل القطع (بند لاحق)، لا هنا. بناء الطبقة الآن كان سينتج "تبديل" لا
  يُرى فعلياً — تضليل، لا إصلاح.
- **`Components/Tokens.*.xaml` (L3) و`Styles/Style.*.xaml` (L4) لم تُبنيا**: لا مستهلك حقيقي لهما بعد (الأنماط
  الحالية تستهلك L2 مباشرة) — تُبنيان عند ترحيل القطع فعلياً، لا كهياكل فارغة.
- **`Surfaces/Print.xaml`/`Export.cs` لم يُنقلا**: `PrintTheme.xaml`/`ExportTheme.cs` الحاليان مستقلان بالفعل
  عن `Resources/Themes` (قرار سابق، صحيح) — النقل مجرد إعادة تسمية بلا فائدة وظيفية الآن.

### التحقق النهائي

`dotnet build PrimeERP.csproj -m:1` → 0 أخطاء. `dotnet build PrimeERP.Tests/PrimeERP.Tests.csproj -m:1` → 0
أخطاء. `dotnet test PrimeERP.Tests/PrimeERP.Tests.csproj -m:1 --no-build` → **134/134 ناجح** (131 موروثة من
F.3.1 بلا تعديل + 3 جديدة لـ `IdentityServiceTests`).

## ✅ F.3.1 — ICustomerService (2026-08-18) — أول اختبار حقيقي للربط ثنائي الاتجاه

### حل الحلقة اللانهائية: CreateAccountDto.SkipAutoLink

`CustomerService.Create` يستدعي `AccountService.Create` لإنشاء الحساب المرتبط، و`AccountService.Create` (منذ
F.2.1) يستدعي `ICustomerService.CreateFromAccount` تلقائياً عند الإنشاء تحت `SettingKeys.Accounts.Customers` —
حلقة لا نهائية بلا قاطع. **الحل**: `CreateAccountDto.SkipAutoLink` (bool، افتراضي false) — `CustomerService`
يمرّره `true` دائماً عند استدعاء `AccountService.Create(conn,tx,...)`، و`AccountService.Create` يتخطّى كل منطق
الربط التلقائي (`ResolveAutoLink`) فوراً لو `true`. اختُبر صراحة (`Create_CreatesExactlyOneAccountAndOneCustomer_
NoInfiniteLoop`/`CreateAccount_UnderCustomersRoot_CreatesExactlyOneCustomer_NoInfiniteLoop`) — عدّ فعلي للحسابات/
العملاء الناتجة، لا افتراض.

### توسيع العقد الجزئي من F.2.1 — تغيير توقيع حقيقي، لا مجرد إضافة

`ICustomerService.CreateFromAccount` كانت `(conn, tx, Account linkedAccount)` (F.2.1) — أصبحت
`(conn, tx, string accountCode, string name)` — **تغيير توقيع فعلي**، ليس امتداداً بلا كسر. القرار: الشكل الجديد
أنظف (لا يمرّر Model كامل لخدمة أخرى لا تحتاج غير حقلين)، ويطابق الترتيب `(conn, tx, ...)` المستخدم في **كل**
دالة (conn,tx) أخرى بُنيت في هذه الجلسة (`RecalculateBalance`, `IJournalService.Create`, `NumberSequenceService.
Next`) — لا `(code, name, conn, tx)` كما كتبها المستخدم في وصفه (ترتيب مختلف عن كل مكان آخر). `ISupplierService`
(لم يُبنَ تنفيذها بعد — F.3.2) حُدِّث توقيعه فقط ليطابق، حتى يبقى مسار `AccountService.Create/Update/Delete`
يعامل الاثنين بشكل متماثل تماماً.

### ⚠️ deadlock من نفس العائلة — انتشر لثلاث طبقات هذه المرة

نفس اكتشاف F.2.3 (قراءة بلا (conn,tx) من داخل معاملة خارجية قائمة) تكرّر هنا بشكل أوسع لأن `CustomerService.
Create(conn,tx,...)` تستدعي `AccountService.Create(conn,tx,...)` التي تستدعي بدورها `AccountValidator` —
سلسلة استدعاء عبر ثلاث طبقات (Service→Service→Validator) كل واحدة منها احتاجت مساراً آمناً:
- `AccountRepository.GetById(conn,tx,id)` / `GetChildren(conn,tx,parentCode)` — جديدتان (لـ Create(conn,tx,...) وتوليد الكود).
- `AccountValidator(isEdit:false, checkUniqueness:false)` — باراميتر جديد يتخطّى قراءة GetByCode للتفرّد (غير آمنة هنا، وغير ضرورية أصلاً: الكود مولَّد برمجياً لا يتصادم بحكم طريقة توليده).
- `AccountService.Create(conn,tx,dto)` نفسها لا تستدعي `ToDto` العادية (قراءات `HasTransactions`/`IsSystem` غير آمنة) — تبني `AccountDto` مباشرة من البيانات المتوفرة (`BuildFreshAccountDto`، بلا إعادة قراءة: حساب جديد فعلياً بلا أبناء/قيود/علم نظامي، إجابة صحيحة لا تقريب).
- `AccountRepository.GetByCode(conn,tx,...)` (من F.2.3) — تُستخدم مباشرة في `CustomerService.Create(conn,tx,...)` نفسها بدل `IAccountService.GetByCode` (التي تبني DTO كاملاً بقراءات إضافية غير آمنة) — نفس نمط `JournalService.ValidateAccountsForTransaction`.

**الدرس المتراكم** (F.2.1→F.2.3→F.3.1): كل مرة يُبنى فيها مسار (conn,tx) جديد يمرّ عبر أكثر من خدمة، الفحص
الآن معتاد ومباشر — لكنه ما زال يكتشف مسارات غير آمنة حقيقية في كل مرة، لا نظرية. لم تظهر هذه الأخطاء في أي
اختبار قبل بناء المستهلك الفعلي (هنا `CustomerService`) رغم أن الكود الأساسي (`AccountService.Create` العادية)
كان "يعمل" ويمرّ اختباراته منذ F.2.1.

### ping-pong Update/Delete — نمط "أحادي الاتجاه، بلا استدعاء عكسي"

نفس تحدي F.2.2 (FiscalPeriodService↔JournalService) لكن بمنطق مختلف هذه المرة: بدل `Lazy<T>`، الحل هنا هو
عقدان **أحاديّا الاتجاه صراحة**، لا استدعاء متبادل:
- `IAccountService.UpdateName(conn,tx,code,name)` (حساب→نفسه فقط) يُستدعى من `CustomerService.Update` — يُحدِّث Accounts.Name فقط، لا يستدعي أي شيء آخر.
- `ICustomerService.UpdateNameFromAccount(conn,tx,code,name)` يُستدعى من `AccountService.Update` — يُحدِّث Customers.Name فقط، لا يستدعي `IAccountService.UpdateName` مرة أخرى.
- نفس الشيء لـ `IAccountService.Delete(conn,tx,code)` (رفيعة، بلا استدعاء `DeleteByAccountCode`) مقابل `ICustomerService.DeleteByAccountCode(conn,tx,code)` (تُستدعى من `AccountService.Delete` فقط، ولا تستدعي `IAccountService.Delete` مرة أخرى).

النتيجة: تحديث/حذف من أي طرف يُزامن الطرف الآخر تلقائياً، بلا استدعاء دائري ولا تكرار كتابة (كل دالة (conn,tx)
هنا "ورقة" — لا تستدعي أي شيء يعيدها لنقطة البداية). اختُبر الاتجاهان صراحة (4 اختبارات: Update حساب↔عميل،
Delete حساب↔عميل).

### ✅ إصلاح حقيقي: بادئة NumberSequence كانت ستنتج شرطة مزدوجة

`SettingKeys.Documents.CustomerPrefix` الافتراضي كان `"C-"` — لكن `NumberSequenceService.Format` يضيف `"-"`
فاصلة بنفسه دائماً (`$"{prefix}-{year}-..."`)، فالنتيجة كانت ستكون `"C--2026-00001"` (شرطة مزدوجة) بدل
`"C-2026-00001"` — عكس اصطلاح `JournalPrefix="JE"` القائم (بلا شرطة لاحقة). اكتُشف فعلياً أثناء تصحيح اختبار
`Create_AccountCreationFailure_RollsBackEverything` (كان يحتاج معرفة الكود التالي بدقة عبر `Peek`). **الإصلاح**:
`CustomerPrefix`/`SupplierPrefix`/`ProductPrefix` الافتراضية أصبحت `"C"`/`"S"`/`"P"` (بلا شرطة) في `SettingKeys.
cs` و`NumberSequenceSeeder.cs` معاً.

### قرارات أخرى

- **`CustomerService.Create` تلتقط `Exception` عامة لا `InvalidOperationException` فقط** — أي فشل داخل المعاملة
  (بما فيه استثناء DB خام كخرق قيد تفرّد) يتحوَّل لـ `Result.Fail` بدل تسريبه للمستدعي. اختُبر بتصادم كود عميل
  حقيقي (`Create_AccountCreationFailure_RollsBackEverything`) — تصادم كود **حساب** لا يصلح لهذا الاختبار (توليد
  الكود يتجنّبه تلقائياً بحساب max+1 على الأبناء الفعليين، فأي كود يُزرَع مسبقاً يُقرأ كابن قائم لا يُعاد توليده).
- **تحقق الفواتير في `CustomerService.Delete` مؤجَّل صراحة** (`// TODO F.4`) — لا خدمة مبيعات/مشتريات مبنية بعد؛
  التحقق الحالي يقتصر على قيود الحساب المرتبط (`JournalRepository.HasLinesForAccount`) فقط.
- **`CreditCheckResult.AvailableCredit = decimal.MaxValue` عند CreditLimit=0** (لا حد) — سنتينل واضح لـ"غير
  محدود" بدل قيمة سالبة مضلِّلة لو طُبِّقت الصيغة العامة حرفياً.
- **`Models/Customer.cs` نُظِّف من `AccountId`/`SalesRepId`** (كانا موجودين من السقالة الأصلية بلا أي منطق
  يملؤهما) — الربط بـ`AccountCode` فقط (نفس اصطلاح الجلسة كلها: الكود لا Id هو مفتاح الربط بين الخدمات).

### اختبارات CustomerServiceTests (20/20 ناجحة)
قاعدة بيانات مستقلة **لكل اختبار**. تغطي: الربط ثنائي الاتجاه الكامل (Create عميل←→حساب، Update عميل←→حساب،
Delete عميل←→حساب، كل واحد بعدّ صريح يثبت "لا حلقة" لا افتراضاً)، الأساسيات (حساب عملاء غير مضبوط يفشل، كود
متتابع، أب Leaf يفشل، rollback حقيقي عند فشل داخل المعاملة)، الائتمان (حد صفر يسمح دائماً، تجاوز يفشل بالمبلغ،
IsOverCreditLimit في DTO)، الأرصدة (يطابق رصيد الحساب، يتغيّر بعد قيد فعلي + Recalculate)، والصلاحيات.

### التحقق النهائي
بناء نظيف كامل (`-m:1`) للمشروعين + **131/131 اختباراً ناجحة** (111 سابقة + 20 جديدة، صفر تراجع).

---

## ✅ F.2.4 — إكمال قوالب الطباعة المؤجَّلة (2026-08-18)

### ⚠️ اكتشاف حقيقي ثالث من نفس العائلة: Freezable مشترك بين خيوط STA متعددة

`PrintService.Theme` (مفرد `ResourceDictionary` ثابت `static`، مبني في F.1.3) يُحمَّل مرة واحدة ويُخزَّن في حقل
ثابت مشترك بين **كل** الخيوط. لم يظهر كخطأ في F.1.3 لأن اختبار طباعة واحد فقط كان موجوداً (خيط STA واحد عبر
`StaThreadHelper`). بمجرد إضافة اختبار طباعة **ثانٍ** (`PrintTemplatesTests`، على خيط STA منفصل تماماً أنشأه
`StaThreadHelper.Run` الخاص به)، فشل فوراً: `Cannot use a DependencyObject that belongs to a different thread
than its parent Freezable` — فُرش WPF (`SolidColorBrush`) ترتبط ضمنياً بخيط إنشائها الأول ما لم تُجمَّد
(`Freeze()`)، فأول خيط غير خيط التحميل الأصلي يرمي هذا الخطأ عند أي استخدام. **الإصلاح**: `Theme` تُجمِّد كل
Freezable في القاموس فور تحميله (`foreach (var v in dict.Values) if (v is Freezable f && f.CanFreeze) f.Freeze();`)
— يجعلها غير قابلة للتعديل فتصبح آمنة للمشاركة بين أي عدد من الخيوط، أي استخدام مستقبلي (نافذة طباعة حقيقية على
UI thread + اختبارات متعددة كل منها على STA منفصلة) الآن آمن. **نفس عائلة اكتشافات F.2.1/F.2.3**: خطأ بنية تحتية
مشتركة (`PrintService`, لا القالبين الجديدين أنفسهما) لا يظهر إلا عند **استخدام حقيقي ثانٍ** يكشف افتراضاً ضمنياً
خاطئاً (هنا: "قاموس واحد يكفي لكل الاستدعاءات" كان صحيحاً فقط بالصدفة لوجود مستهلك واحد سابقاً).

### الملفات

- `Services/Print/Templates/AccountStatementPrintTemplate.cs` (جديد) — من `AccountDto` + `List<AccountStatementLine>`
  (مصدرهما `IAccountService.GetByCode`/`GetStatement`، لا يستدعيهما القالب نفسه). Portrait ثابتة (لا حساب "لا تكفي"
  ديناميكي — لا إشارة موثوقة له قبل الطباعة الفعلية، تُترك ثابتة بدل تخمين، قرار موثَّق في كود الملف). الرصيد
  الافتتاحي من أول سطر (`SourceType == "Opening"`) كـ KeyValue منفصل، سطور الحركة (الباقي) في الجدول.
- `Services/Print/Templates/TrialBalancePrintTemplate.cs` (جديد) — من `List<TrialBalanceLine>` (مصدرها
  `IJournalService.GetTrialBalance`). Landscape إلزامية. إزاحة اسم الحساب حسب `Level` محسوبة محلياً في القالب
  (منطق عرض بحت — نفس مكان "أين يعيش المنطق" الذي حدّده فحص التسريب السابق، لا خاصية على DTO/Model). صف تحذير
  `StatusVariant.Danger` بارز يُضاف تلقائياً لو `مجموع ClosingDebit ≠ مجموع ClosingCredit`.
- `Services/Print/IPrintable.cs` — إضافتان عامتان لكل قالب (لا خاصتان بميزان المراجعة فقط):
  `PrintSectionType.Callout` + `PrintSection.Variant` (صندوق تحذير/معلومة ملوَّن بأحد الألوان الستة من
  `PrintTheme.xaml` — نفس المفاتيح `{Variant}Solid/Soft/SoftText` المبنية أصلاً في فحص F.1.3/StatusVariant ولم
  تُستهلَك فعلياً حتى الآن)، و`PrintSection.RowBold` (دالة تمييز صف — نفس نمط `AppDataGrid.RowHighlightSelector`)
  تخدم "الحسابات التجميعية bold" — لا تُفعَّل فعلياً بعد لأن `GetTrialBalance` ترجع أوراقاً فقط (`IsLeaf=true`
  دائماً حالياً)، جاهزة بلا تعديل قالب لو أُضيفت صفوف تجميعية لاحقاً.
- `Services/Print/PrintService.cs` — ينفّذ `Callout` (`BuildCallout`) و`RowBold` في `BuildTable`؛ + إصلاح Freeze أعلاه.
- `Services/Print/PrintTemplates.cs` — تعليق الصنف حُدِّث (لم يعد يذكر AccountStatement/TrialBalance كمؤجَّلين،
  `ReportPrint` فقط يبقى مؤجَّلاً لـ F.5).

### اختبارات PrintTemplatesTests (5/5 ناجحة)
قاعدة بيانات مستقلة **لكل اختبار** (تُنشئ حسابات وتُرحِّل قيوداً حقيقية — نفس سبب `JournalServiceTests`). تغطي:
كلا القالبين يبنيان `FixedDocument` بلا استثناء، الرصيد الجاري لكشف الحساب متسلسل صح، ميزان المراجعة Landscape
فعلياً وإجمالياته صحيحة، وميزان غير متوازن (مبني يدوياً لعزل القالب عن صحة الخدمة) يُظهر صندوق التحذير الأحمر.

### التحقق النهائي
بناء نظيف كامل (`-m:1`) للمشروعين + **111/111 اختباراً ناجحة** (106 سابقة + 5 جديدة، صفر تراجع).

---

## ✅ F.2.3 — IJournalService (2026-08-18) — "أهم خدمة في النظام"

### ⚠️ اكتشاف حرج: deadlock صامت على SQLite داخل معاملات (conn,tx) متداخلة عبر الخدمات

أخطر اكتشاف في هذه المرحلة، ومن نفس عائلة اكتشافات F.2.1 (RunTransaction<T> Stack Overflow). أي قراءة عبر
`DbHelper.Query`/`Db.Scalar` العادية (تفتح **اتصالاً جديداً**) استُدعيت من داخل جسم `Db.RunTransaction` **لخدمة
أخرى** مفتوحة بالفعل على نفس الخيط — تُعلِّق (deadlock) على SQLite: القفل الكتابي للمعاملة الخارجية لا يُحرَّر
حتى يعود الاستدعاء المتزامن، فانتظار اتصال جديد لنفس الملف ينتظر نفسه فعلياً. لا يظهر إلا عند **الاستخدام
الحقيقي الأول** لسلسلة استدعاءات (conn,tx) عابرة للخدمات — بالضبط ما حدث هنا: `FiscalPeriodService.CloseYear`
(F.2.2) يفتح معاملة ويستدعي `IJournalService.Create/Post(conn,tx,...)` الحقيقية لأول مرة (كانت `FakeJournalService`
في الاختبارات لا تلمس أي اتصال منفصل، فأخفت المشكلة تماماً حتى هذه المرحلة).

**كل موضع أُصلح** (بإضافة نسخة (conn,tx) تعمل على نفس الاتصال بدل فتح اتصال جديد):
- `Core/Database/DbHelper.cs`: `Query(DbConnection, DbTransaction, sql, params)` جديدة.
- `Database/JournalRepository.cs`: `GetById`, `GetLines`, `GetPostedLinesForAccount` — نسخ (conn,tx).
- `Database/AccountRepository.cs`: `GetByCode(conn,tx,code)` جديدة.
- `Database/NumberSequenceRepository.cs`: `EnsureRow(conn,tx,key)` جديدة.
- `Services/NumberSequenceService.cs`: `Next(conn,tx,key)` جديدة — **بديل توليد رقم القيد بلا فتح معاملة NumberSequenceService.Next(key) العادية داخلياً** (كانت ستفتح معاملتها الخاصة عبر Db.RunTransaction من داخل معاملة FiscalPeriodService.CloseYear الخارجية — نفس الخطأ بالضبط).
- `Services/Accounting/AccountService.cs`: `RecalculateBalance(conn,tx,code)` جديدة (تستخدم `ComputeBalance(conn,tx,code)` الجديدة لا `ComputeBalance(code)` العادية) — يستدعيها `JournalService.Post/Unpost(conn,tx,...)` لإعادة حساب رصيد كل حساب متأثر.
- `Services/Accounting/JournalService.cs`: `ValidateAccountsForTransaction(conn,tx,lines)` مصغّرة منفصلة عن `ValidateAndResolveAccounts` العادية (تقرأ عبر `AccountRepository.GetByCode(conn,tx,...)` مباشرة لا `IAccountService.GetByCode` — الأخيرة تفتح اتصالاً جديداً لبناء `AccountDto` الكامل)؛ و`BuildDto` (لحظة الإنشاء) تترك `FiscalPeriodName = null` عمداً بدل استدعاء `IFiscalPeriodService.GetPeriodFor` (نفس الخطر، ونادراً ما يفيد لحظة إنشاء قيد إقفال سنة أصلاً).

**الدرس العام** (يتكرر من F.2.1): أي مسار (conn,tx) جديد يُبنى الآن يجب أن يُفحص لكل استدعاء متسلسل داخله —
هل يستدعي (مباشرة أو عبر خدمة أخرى) أي قراءة/كتابة بلا (conn,tx) صريحة؟ إن كانت نعم، وقد يُستدعى هذا المسار من
داخل معاملة خارجية فعلياً (لا نظرياً فقط) — أضف نسخة (conn,tx) الآن، لا حين يظهر العطل فعلياً في الإنتاج.

### ⚠️ اكتشاف حقيقي ثانٍ: صيغة تقسيم مدين/دائن في ميزان المراجعة كانت معكوسة

`GetTrialBalance` استخدم في مسودته الأولى تفرّعاً حسب "طبيعة الحساب" (`debitNormal ? ... : ...`) لتقسيم الرصيد
المُوقَّع لعمودي مدين/دائن — وكانت فروع الحسابات الدائنة الطبيعة (خصوم/حقوق ملكية/إيرادات) **معكوسة فعلياً**:
حساب إيراد برصيد دائن (500 دائن) كان يظهر 500 في عمود **المدين** بدل الدائن. اكتُشف فوراً عبر اختبار حقيقي
(`TrialBalance_TotalDebitEqualsTotalCredit` فشل بفارق 2000 بالضبط — إيراد 1000 محسوب في العمود الخطأ يُخطئ
المجموعين بضعف قيمته). **الإصلاح**: صيغة تقسيم واحدة عامة بلا تفرّع إطلاقاً — `Debit = Max(balance,0)`,
`Credit = Max(-balance,0)` — صحيحة لكل نوع حساب دائماً بحكم اصطلاح الإشارة نفسه (Balance = مدين−دائن)؛
"الطبيعة" مفهوم عرضي (أي عمود يُتوقَّع غير صفري لحساب سليم) لا يُغيّر صيغة التقسيم الرياضية. **درس**: عمود واحد
"يبدو صحيحاً منطقياً" (مثال: "دعنا نعكس الإشارة لأن هذا حساب دائن") يستحق التحقق باختبار رقمي حقيقي فوراً، لا
القبول بالحدس المحاسبي وحده.

### ⚠️ فجوة عزل اختبارات ثالثة: SettingsService.Instance ذاكرة مؤقتة مشتركة بين كل ملفات الاختبار

`SettingsService.Instance` مفرد ثابت (`static readonly`) واحد لكل عملية الاختبار كلها — `_cache` الداخلية لا
تُبطَل تلقائياً عند بناء `TestDatabaseFixture` جديدة (قاعدة بيانات SQLite مؤقتة جديدة كلياً لكل اختبار في
`AccountServiceTests`/`FiscalPeriodServiceTests`/`JournalServiceTests`). فاختبار يستدعي `SettingsService.Set(...)`
(مثال: `Create_DuplicateAccount_Succeeds_WhenSettingEnabled` يضبط `AllowDuplicateAccountInEntry=true`) يُسرّب
القيمة لأي اختبار تالٍ يقرأ نفس المفتاح، حتى لو كان على قاعدة بيانات مختلفة كلياً لم تُطلب منها هذه القيمة قط —
اكتُشف فعلياً عبر `Create_DuplicateAccount_Fails_ByDefault` (يفشل أحياناً حسب ترتيب تنفيذ xUnit للاختبارات).
**الإصلاح**: `TestDatabaseFixture` يستدعي `SettingsService.Instance.Reload()` في نهاية الإنشاء — يُبطل الذاكرة
المؤقتة فيُعاد تحميلها من قاعدة البيانات الجديدة الصحيحة عند أول قراءة تالية، بصرف النظر عمّا ضبطه أي اختبار سابق.

### ✅ إصلاح بيانات حقيقي: SettingKeys.Accounts.RetainedEarnings ("3200") لم يكن Leaf

`FiscalPeriodService.CloseYear` (F.2.2) يبني قيداً يُقيَّد على حساب الأرباح المحتجزة مباشرة — لكن
`AccountRepository.SeedDefaults()` كانت تزرع **كل** الحسابات الافتراضية الـ21 بـ `IsLeaf=false` بلا استثناء (قرار
F.2.1 المتعمَّد لحسابات-فئات مثل 1220/2110 التي تحتاج أبناء لكل عميل/مورد). لم يظهر هذا كخطأ في F.2.2 لأن
`FakeJournalService` وقتها لم تتحقق من leaf إطلاقاً. الآن مع `JournalService` الحقيقي (يرفض القيد على حساب غير
Leaf)، أي `CloseYear` حقيقي كان سيفشل دائماً. **الإصلاح**: "3200" (الأرباح المحتجزة) فقط أصبح `IsLeaf=true` في
`SeedDefaults` — لأنه حساب دفتري نهائي واحد في أي شجرة حسابات واقعية (لا يُقسَّم لأبناء، خلافاً لـ1220/2110/4100/
5100 التي لا تزال فئات لأن اختبارات فعلية تُنشئ أبناءً تحتها). لم تُلمَس بقية الحسابات (خارج نطاق F.2.3، ولا
مستدعٍ حي يحتاجها Leaf الآن — ستُراجَع فرداً فرداً عند بناء F.3/F.4 حين تحتاج ترحيلاً فعلياً عليها).

### مهمة مفتوحة سُجِّلت لـ F.4

`Core/Transactions/DocumentService.cs`/`IPostable.cs` (المحذوفان في الفحص السابق) كانا العقد الموحَّد لترحيل أي
مستند (فاتورة بيع/شراء، إذن مخزن...) لقيد يومية + حركة مخزون معاً. بديلهما **يُبنى في F.4** (بعد وجود جداول
المبيعات/المشتريات/المخزون فعلياً — لا قبلها، تجنّباً لتجريد سابق لأوانه)، وبشرط: **يستدعي `IJournalService`
(`Create`/`Post` بنسختي (conن,tx) الجاهزتين الآن) لا يكرر منطق ترحيل القيد** — نفس الخطأ الذي كان في
`DocumentService` القديم.

### قرارات معمارية أخرى (تستحق مراجعتك)

1. **`CreateJournalEntryDto`/`CreateJournalLineDto` (العقد الجزئي من F.2.2) استُبدلا بـ `CreateJournalDto`
   الكامل** (الشكل الجديد: `Id`, `EntryDate` كـ `DateTime` لا `string`, `LineNo` على كل سطر). `FiscalPeriodService.
   CloseYear` عُدِّل ليبني `CreateJournalDto` مباشرة (`Source` أصبحت `"YearClosing"` قيمة كنسية بدل النص العربي
   الحرفي السابق `"الإقفال السنوي"` — التنسيق للعرض الآن عبر `Str.Journal.Source.YearClosing`، لا القيمة المخزَّنة).
2. **PostBatch تُرجع `Result.Ok(batch)` دائماً** (لا `Result.Fail` عند وجود فشول جزئية) — `JournalBatchResult.
   FailedCount`/`Failures` تحمل التفاصيل. قرار متعمَّد: `Result<T>.Fail` في هذا الكود لا يحمل قيمة `Value`، فلا
   طريقة لإرجاع تفاصيل الفشل مع `IsSuccess=false` في آن؛ "نجاح" الاستدعاء هنا يعني "التقرير جاهز" لا "كل عنصر رُحِّل".
   التنفيذ الفعلي "الكل أو لا شيء" حقيقي: يتحقق من الكل أولاً بلا أي كتابة، ولا يُفتح `Db.RunTransaction` إطلاقاً
   لو فشل عنصر واحد (لا حاجة لـ rollback عمّا لم يُكتب أصلاً).
3. **`AccountDto.IsActive` أُضيفت** (لم تكن موجودة) — يحتاجها `JournalService` للتمييز بين رسالتي
   `AccountNotLeaf`/`AccountInactive` بدل رسالة مدمجة واحدة عبر `CanAcceptEntries` (تُرجع bool واحداً يخلط
   الحالتين). `CanAcceptEntries` نفسها لم تُلمَس (لا تزال تُستخدم في مكانها الأصلي).
4. **`JournalRepository.GetAll()`/`Search()` القديمتان حُذفتا** — صفر مستدعٍ فعلي أصلاً، وأصبحتا مكرَّرتين تماماً
   مع `GetPaged` الجديدة (فلترة+بحث+صفحات في استعلام SQL واحد، لا تحميل كل الجدول للذاكرة). `GetSummary()` لم
   تُلمَس (تصلح لواجهة لوحة تحكم مستقبلية، خارج نطاق هذه المرحلة).
5. **`InvoiceValidator`/`ProductValidator` القديمتان القياس المرجعي**: `JournalValidator` الآن يستدعي
   `AccountingRules.MinimumJournalLines`/`NoLineWithBothDebitAndCredit`/`BalancedEntry`/`NonZeroEntry` (أربعتها،
   لا ثلاثة كما في F.2.2) — القاعدة الرابعة (فحص مدين+دائن معاً بنفس السطر) أُضيفت لـ`AccountingRules` هنا (كانت
   محسوبة inline في `JournalValidator` قبلها، نفس نمط الإصلاح الذي طُبِّق على الثلاث الأخريات في فحص التسريب).

### اختبارات JournalServiceTests (32/32 ناجحة)
قاعدة بيانات مستقلة **لكل اختبار** (نفس سبب بقية اختبارات F.2). تغطي: الإنشاء (توازن/حد أدنى سطور/صفر/مدين+دائن
معاً/حساب غير موجود/غير Leaf/مكرر بحالتيه/فترة مقفلة/عدم تغيير أرصدة/ترقيم سطور)، التعديل (مسودة تنجح وتستبدل
السطور، مرحّل يفشل، EntryNo ثابت)، الحذف (مسودة/مرحّل)، الترحيل (تحديث أرصدة صحيح، مرحّل مسبقاً يفشل، فترة مقفلة
تفشل، Unpost يعكس الأرصدة، قيد إقفال سنة يرفض Unpost، PostBatch الكل-أو-لا-شيء مع تحقق أرصدة فعلي)، ميزان
المراجعة (توازن المجاميع، الافتتاحي قبل from، postedOnly يستثني المسوّدات، طبيعة الرصيد الصحيحة)، والصلاحيات
(كل عملية كتابة بلا صلاحيتها). `FiscalPeriodServiceTests`'s `FakeJournalService` (المؤقتة من F.2.2) **حُذفت
بالكامل** — استُبدلت بـ `JournalService` الحقيقية، والاختبارات كلها (32 من F.2.2) لا تزال ناجحة معها بلا تعديل.

### التحقق النهائي
بناء نظيف كامل (`-m:1`) للمشروعين + **106/106 اختباراً ناجحة** (74 سابقة + 32 جديدة، صفر تراجع).

---

## ✅ فحص تسريب المنطق الشامل (2026-08-18، قبل F.2.3)

فحص فعلي (grep + قراءة كل ملف، لا تخمين) لكل الطبقات بحثاً عن "منطق تشغيل" (قرار/حساب/تحقق/تحويل بيانات) خارج
`Services/` (المالك الوحيد لمنطق الأعمال). **كل بند وُجد أُصلح فوراً في نفس هذه الجلسة — لا شيء مؤجَّل**، ما عدا
الاستثناء الصريح: منطق في قطع لم تُرحَّل بصرياً بعد أُصلح منطقه فقط (لا تصميمه).

### جدول الفحص

| الموقع | المنطق | التصنيف | القرار | نُقل إلى |
|---|---|---|---|---|
| `Models/Account.cs` | `TypeName`/`LeafText`/`BalanceText`/`DisplayName` | تحويل حالة + تنسيق نصي | أُزيل بالكامل | `AccountDto` (موجود، F.2.1) |
| `Models/Customer.cs` | `IsOverCreditLimit` (قرار أعمال حقيقي) + `BalanceText`/`CreditLimitText`/`StatusText` | قرار أعمال + تنسيق | أُزيل بالكامل | مؤقتاً محلياً داخل `CustomerPicker.cs` (المستهلك الوحيد الحي) حتى `ICustomerService` — F.3 |
| `Models/Employee.cs` | `BasicSalaryText`/`StatusText` | تنسيق + تحويل حالة | أُزيل | DTO مستقبلي عند بناء F.3 |
| `Models/ExchangeRate.cs` | `RateText` | تنسيق | أُزيل | DTO مستقبلي |
| `Models/FiscalYear.cs`+`FiscalPeriod.cs` | `StatusText` (أُضيفت هذه الجلسة نفسها في F.2.2 — خرق ذاتي اكتُشف بالفحص) | تحويل حالة | أُزيل | `FiscalPeriodDto.StatusVariant` (موجود بالفعل) |
| `Models/JournalEntry.cs` | `DebitText`/`CreditText`/`StatusText`/`IsBalanced` | تنسيق + تحويل حالة + قرار أعمال (التوازن) | أُزيل بالكامل | `Services/Print/PrintTemplates.cs` (تنسيق محلي لغرض الطباعة)، `IsBalanced` الحقيقي سيكون في `AccountingRules.BalancedEntry` (موجودة، تُستدعى من `JournalService` — F.2.3) |
| `Models/Payroll.cs`+`PayrollLine.cs` | `NetTotalText`/`StatusText`/`NetSalaryText` | تنسيق + تحويل حالة | أُزيل | DTO مستقبلي (F.3+) |
| `Models/Product.cs` | `CostPriceText`/`SalePriceText`/`StatusText`/`TypeText`/`CurrentStockText`/`CostMethodText` | تنسيق + تحويل حالة | أُزيل | `PickerColumn.Format` (موجودة أصلاً، فُعِّلت في `MockPickerDataSources.cs`) |
| `Models/PurchaseInvoice(Line).cs`+`SalesInvoice(Line).cs` | `*Text` + `StatusText` (تحويل InvoiceStatus) | تنسيق + تحويل حالة | أُزيل | DTO مستقبلي (F.3.x) |
| `Models/StockMovement.cs` | `QtyText`/`MovementTypeText` | تنسيق + تحويل حالة | أُزيل | DTO مستقبلي |
| `Models/Supplier.cs` | `BalanceText`/`CreditLimitText`/`StatusText`/`SupplierTypeText` | تنسيق + تحويل حالة | أُزيل | DTO مستقبلي (F.3) |
| `Models/TaxGroup.cs` | `RateText` | تنسيق | أُزيل | DTO مستقبلي |
| `Models/User.cs` | `StatusText` | تحويل حالة | أُزيل | DTO مستقبلي (F.6) |
| `Database/FiscalYearSeeder.cs` | حساب حدود السنة + تقسيم الفترات (حلقة `AddMonths`) — **مكرَّر حرفياً** مع `FiscalPeriodService.CreateYear` | معالجة تواريخ (منطق تشغيل) داخل Repository/Seeder | نُقل | `Core/Validation/FiscalPeriodCalculator.cs` (دالة نقية جديدة) — يستخدمها الاثنان الآن، صفر تكرار |
| `Database/PermissionDb.cs` | `GetEffectivePermissions` (اتحاد صلاحيات الدور ∪ منح المستخدم − سحب المستخدم) — **فعّال** ويُستدعى من كل تحقق صلاحية حياً في النظام حالياً | قرار تفويض (Authorization) داخل Repository | نُقل | `Core/Permissions/PermissionService.GetUserPermissions` — `PermissionDb` أصبحت 4 قراءات خام فقط |
| `Database/JournalRepository.cs` | `GetDebitCreditSum(code)` — صفر مستدعٍ، وحساب **غير صحيح** أصلاً (يجمع كل السطور بلا فلترة `IsPosted`، خلافاً لـ `GetPostedLinesForAccount` الصحيحة المستخدمة فعلياً في `AccountService`) | كود ميت + حساب مكرَّر خاطئ | حُذف بالكامل | — |
| `Core/Transactions/DocumentService.cs`+`IPostable.cs` | ترحيل قيود يومية كامل (بناء قيد، حساب توازن عبر `AccountingRules`، إعادة حساب رصيد SQL خام، حركة مخزون) — **صفر مستدعٍ فعلي**، ويستخدم مخطط `StockMovements`/`Products` غير موجود إطلاقاً في إعادة البناء الحالية | منطق أعمال كامل داخل `Core/` (الطبقة الممنوع أن تحوي منطق أعمال) | حُذف بالكامل | يُعاد بناؤه من الصفر في `JournalService` (F.2.3) وخدمة مخزون مستقبلية — لا يُستعاد بشكله الحالي (Premature abstraction أيضاً: `IPostable` عام لنوع مستند لا يوجد بعد ولا واحد منه) |
| `Core/Validation/Validators/JournalValidator.cs` | فحص "سطرين على الأقل" و"قيد بقيمة صفر" محسوبان مباشرة داخل الـ Validator (بجانب `AccountingRules.BalancedEntry` المستدعاة بشكل صحيح أصلاً للتوازن) | قاعدة أعمال محاسبية مركّبة خارج `AccountingRules` | نُقل | `AccountingRules.MinimumJournalLines`/`NonZeroEntry` (دالتان نقيتان جديدتان، نفس نمط `BalancedEntry`) |
| `Views/Controls/Pickers/CustomerPicker.cs` | `RowHighlight` يستهلك `Customer.IsOverCreditLimit` (الخاصية المُزالة أعلاه) | استهلاك مباشر لقرار أعمال كان مسرَّباً في الـ Model | أُصلح | المقارنة (`CreditLimit>0 && Balance>CreditLimit`) الآن محلية داخل هذه القطعة فقط، بتعليق موثِّق أنها مؤقتة حتى `ICustomerService` (F.3) |
| `Services/Print/PrintTemplates.cs` | استهلك `JournalEntry.StatusText`/`DebitText`/`CreditText` (خصائص Model مُزالة) | استهلاك تنسيق مسرَّب | أُصلح | التنسيق محسوب محلياً هنا الآن (طبقة الطباعة/العرض الصحيحة له) |
| `Views/Dev/ControlsGalleryPage.xaml.cs`+`MockPickerDataSources.cs` | استخدما `*Text` المُزالة من `Product`/`Customer`/`Supplier` كحقول عرض/أعمدة Picker | استهلاك تنسيق مسرَّب | أُصلح | حقول خام + `PickerColumn.Format="N2"` (آلية موجودة أصلاً، `Binding.StringFormat`)؛ `PickerBase.FormatTemplate` وُسِّع ليدعم `{Prop:Format}` لا `{Prop}` فقط |

### أقسام فُحصت ولم يوجد فيها خرق (نتيجة، لا افتراض)

- `Core/Validation/AccountingRules.cs` — دوال نقية فعلاً (بلا DB/إعدادات)، تُستدعى بشكل صحيح من `AccountValidator`/`JournalValidator`/`AccountService`.
- `Database/AccountRepository.cs`, `JournalRepository.cs` (عدا البند المحذوف), `BackupRepository.cs`, `SettingRepository.cs`, `SettingSeeder.cs`, `NumberSequenceRepository.cs`, `FiscalPeriodRepository.cs` — CRUD خام فقط، بلا قرار/معاملة/Audit/إعدادات داخلها.
- `Views/Controls/Documents/LineComputeEngine.cs`+`LineValidationEngine.cs`+`DocumentLinesGrid.xaml.cs` — الفصل بين "حساب/تحقق عرض" (مقبول: Qty×Price، Qty>0، مدين-أو-دائن-لا-كلاهما لكل سطر) و"قاعدة أعمال" (لا توجد هنا فعلياً — إجمالي التذييل عرض بحت، لا قرار "متوازن أم لا" يمنع الحفظ) — القطعة مطابقة للقاعدة من البداية، لا تعديل لزم.
- `Views/Controls/Pickers/*` (عدا `CustomerPicker`) — لا استدعاء `DbHelper`/`Repository` واحد في كل `Views/` (فحص شامل بالبحث)، كل الوصول عبر `IPickerDataSource<T>`.
- `Core/Validation/Validators/{Account,Customer,Employee,Invoice,Product,Supplier,User}Validator.cs` — تحقق بيانات/تفرّد فقط (والتفرّد يحتاج قراءة DB وهذا مقبول صراحة)، بلا كتابة DB وبلا قواعد محاسبية مركّبة مسرَّبة.
- فحص شامل لأكواد حسابات مكتوبة حرفياً (`grep` لكل نمط `"1220"`-شكل) — لا استخدام حي خارج `AccountRepository.SeedDefaults` (وهي مصدر تعريف الشجرة الافتراضية نفسه، لا استهلاك لكود خاص).

### التحقق النهائي
بناء نظيف كامل (`-m:1`) للمشروعين + **74/74 اختباراً ناجحة** (بلا تغيير في العدد — الفحص لم يكسر شيئاً، فقط أزال تسريباً).

---

## ⚠️ ثلاثة أخطاء حقيقية في البنية التحتية المشتركة (Core/Database) — اكتُشفت أثناء F.2.1

كلها **كامنة منذ بنائها** (Step 0 أو قبله) ولم تظهر إلا الآن لأن `AccountService`/`AccountServiceTests` أول من نفّذ فعلياً
`SchemaBuilder.Audit()` على جدول حقيقي، وأول من نفّذ `DbHelper.RunTransaction<T>` (بقيمة راجعة) فعلياً. تؤثر على
**كل جدول** يستخدم النمط المتأثر، لا على الحسابات فقط — أُصلحت في `Core/Database` نفسها فأصلحت كل المستخدمين دفعة واحدة.

1. **`SchemaBuilder.Audit()`: `DEFAULT datetime('now')` خطأ نحوي في SQLite** — يحتاج قوسين خارجيين حول أي `DEFAULT`
   باستدعاء دالة: `DEFAULT (datetime('now'))`. بلا الأقواس: `SQLite Error 1: near "(": syntax error` عند أي
   `CREATE TABLE` يستخدم `.Audit()`. **الأثر**: كل جدول يستخدم `.Audit()` كان سيفشل عند أول تنفيذ فعلي —
   `Accounts`، `JournalEntries`، `Roles`، `Users` (`PermissionDb.cs`). أُصلحت في `SchemaBuilder.Audit()` نفسها
   (إضافة قوسين حول `{_provider.CurrentTimestampFunction}`) — يصلح المُستخدِمين الأربعة دفعة واحدة، ويبقى صالحاً
   لـ SQL Server/PostgreSQL (`GETDATE()`/`NOW()` يقبلان الأقواس الخارجية بلا مشكلة).
2. **`JournalRepository.CreateTable()`: عمود `CreatedBy` مُعرَّف مرتين** — `.Text("CreatedBy", 100)` صريحة ثم
   `.Audit()` (تضيف `CreatedBy` أيضاً) على نفس جدول `JournalEntries` → `SQLite Error 1: duplicate column name`.
   أُصلحت بحذف التعريف الصريح الزائد (الاعتماد على `.Audit()` وحدها).
3. **`DbHelper.RunTransaction<T>` — استدعاء ذاتي متكرر لا نهائي (Stack Overflow)** — الأخطر. كانت مكتوبة:
   `RunTransaction<T>(...) { ... RunTransaction((conn,tx) => result = action(conn,tx)); ... }` — لامدا
   `result = action(conn, tx)` تُرجع قيمة من نوع `T` فعلياً (تعبير إسناد)، فيحلّ تحميل C# الزائد (Overload
   Resolution) هذا الاستدعاء كنداء ذاتي لنفس `RunTransaction<T>` (تطابق `Func<...,T>` أدق من تحويلها لـ
   `Action<...>` مع تجاهل القيمة) بدل الدالة اللا-عامة المقصودة — تكرار لا نهائي حتى تحطّم المكدس. **لم يكتشفه
   أي اختبار سابق** لأن لا استدعاء فعلي لـ `RunTransaction<T>` (بقيمة راجعة) وُجد قبل `AccountService.Create`
   في هذه الجلسة كلها — `NumberSequenceService.Next` يستخدمه أيضاً لكن بلا اختبار مكتوب له بعد. أُصلحت بإزالة
   التفويض الهش كلياً: `RunTransaction<T>` الآن تُنفّذ `BeginTransaction`/`Commit`/`Rollback` مباشرة بنفسها
   (تكرار بسيط ومقصود مع `RunTransaction` اللا-عامة، أفضل من تفويض عرضة لهذا الخطأ).

**درس عام**: "الكود يُصرَّف بلا أخطاء" لا يعني أنه صحيح — هذه الأخطاء الثلاثة **كلها كانت موجودة من البداية** في
كود اجتاز `dotnet build` مرات عديدة عبر هذه الجلسة كلها، ولم يكشفها إلا التنفيذ الفعلي (اختبار حقيقي بقاعدة بيانات
حقيقية). أي طبقة بيانات/معاملات جديدة تحتاج اختباراً فعلياً واحداً على الأقل يُنفِّذها حرفياً قبل اعتبارها "جاهزة".

---

## ✅ تصحيح: `ServiceLocator.TryGet` كانت تُسكت فشل الربط التلقائي (2026-08-17)

**الملاحظة**: `TryGet` تُرجع `false` بهدوء لو الخدمة غير مسجَّلة — لو بُنيت F.3 لاحقاً ولم تُسجَّل `ICustomerService`
بشكل صحيح (خطأ تسجيل، ترتيب تحميل خاطئ...)، الربط التلقائي كان سيتوقف بصمت بلا أي إشارة خطأ — أخطر من رفض
العملية صراحة، لأن المستخدم يظن أن كل شيء تم بنجاح بينما لا عميل أُنشئ فعلياً.

**الحل**: مفتاح إعداد جديد `SettingKeys.Accounts.AutoLinkEnabled` (bool، افتراضي `true`):
- `false` → تعطيل الربط التلقائي بالكامل عمداً، بلا فحص توفّر الخدمة وبلا خطأ (قرار إداري صريح لإيقاف الميزة).
- `true` (الافتراضي) والخدمة **غير مسجَّلة** → `Result.Fail` صريح **قبل أي كتابة في قاعدة البيانات**
  (`Str.Accounts.CustomerLinkUnavailable`/`SupplierLinkUnavailable`)، لا سكوت.
- `true` والخدمة مسجَّلة → الربط يحدث كالمعتاد ضمن نفس المعاملة.

طُبِّق في `AccountService.Create` **و** `Delete` كليهما (الوصف الأصلي ذكر المشكلة لـ Create فقط، لكن `Delete`
كانت تستخدم نفس نمط `TryGet` الصامت — نفس الخطر بالضبط، فطُبِّق نفس الإصلاح عليها). فحص التوفّر الآن يحدث **قبل**
فتح المعاملة (لا داخلها) — يتجنّب فتح/التراجع عن معاملة فارغة لمجرد فشل فحص توفّر خدمة.

**أثر جانبي على الاختبارات**: `AccountServiceTests` (باقي الاختبارات غير المتعلقة بالربط) بدأت تفشل فوراً لأنها
تُنشئ حسابات تحت جذر العملاء بلا تسجيل `ICustomerService` — وهذا **صحيح تماماً**، هو بالضبط السلوك الجديد
المطلوب. أُصلحت بتسجيل نسخة وهمية افتراضية (`FakeCustomerService`/`FakeSupplierService`) في مُنشئ فئة الاختبار
نفسها؛ الاختباران اللذان يفحصان الربط فعلياً (`Create_UnderCustomersRoot_CreatesLinkedCustomer`،
`Delete_RemovesLinkedCustomer`) يُسجّلان نسختهما الخاصة فوق التسجيل الافتراضي عند الحاجة لتتبّع الاستدعاء.

---

## ✅ F.2.1 — IAccountService (2026-08-17)

`Services/Accounting/AccountService.cs` هو المالك الوحيد الآن لكل منطق شجرة الحسابات (توليد الكود، حساب المستوى،
تحديث الأرصدة، قواعد الحذف/التعديل). `AccountRepository` بقيت CRUD صرفاً (وُسِّعت بنسخ `(conn,tx)` من
`Insert`/`Update`/`SetIsLeaf`/`Delete`/`UpdateBalance` لتشارك معاملة الخدمة، و`GetById`/`GetAll(includeInactive)`
جديدتان). `JournalRepository` اكتسبت `GetPostedLinesForAccount` (قراءة صرفة، تخدم `GetBalanceAsOf`/`GetStatement`).

### قرارات معمارية اتُّخذت أثناء البناء (تستحق مراجعتك)

1. **"الأب ليس Leaf" — توضيح تناقض ظاهري في الوصف الأصلي**: النص طلب تحقق "4. الأب ليس IsLeaf" ضمن التحقق،
   **و** خطوة تنفيذ "تحديث الأب IsLeaf=false" — متناقضان حرفياً (لو الشرط يمنع الإنشاء إلا لو الأب already
   غير Leaf، فخطوة "اجعله غير Leaf" لن تُنفَّذ أبداً على أب Leaf أصلاً). تأكّدت أن `AccountRepository.SeedDefaults()`
   تُنشئ **كل** الحسابات الجذرية بـ `IsLeaf=false` من البداية (سطر `("@leaf", false)` صريح للـ 21 حساباً)، فلا
   تعارض عملي: الأب يجب أن يكون **غير Leaf مسبقاً** (قرار إداري صريح عبر `Update`) قبل قبول أبناء تحته — هذا ما
   نفّذته فعلياً، واختبار `Create_UnderLeafAccount_Fails` يتحقق منه. حذفت خطوة "SetIsLeaf(parent,false)
   التلقائية" من `Create` بناءً على هذا (لم تعد منطقية بعد إضافة الشرط).
2. **الربط التلقائي بالعميل/المورد — عقد جزئي حقيقي لا محاكاة**: `ICustomerService`/`ISupplierService`
   (`Services/Parties/`) لم تُبنَ بعد كخدمة كاملة (المرحلة F.3) — بدل تأجيل الميزة بصمت أو اختراع منطق مؤقت،
   عرّفت **عقداً جزئياً حقيقياً** بالضبط بما يحتاجه `AccountService` الآن (`CreateFromAccount`/`DeleteByAccountCode`،
   كلاهما بـ `(conn,tx)` ليبقيا داخل معاملة `AccountService` الذرّية)، ويستدعيه عبر `ServiceLocator.TryGet`
   (لا خطأ لو غير مسجَّلة بعد — ربط لا يحدث بصمت). F.3 تُنفِّذ هذا العقد فعلياً، لا تُعيد تصميمه.
   `ServiceLocator.TryGet<T>` جديدة (لم تكن موجودة) خصيصاً لهذا النمط.
3. **مزامنة اسم العميل/المورد عند `Update`**: **مؤجَّلة عمداً** — تحتاج دالة `UpdateLinkedName` على العقد
   الجزئي أعلاه، ولا اختبار صريح طلبها في القائمة المُعطاة. مُسجَّلة كملاحظة كود، لا تنفيذ وهمي.
4. **`IsSystem` بلا عمود جديد في الجدول**: الوصف طلب `Delete` يمنع حذف حساب `IsSystem` لكن `Models/Account.cs`
   لا يملك عمود `IsSystem` أصلاً. بدل تعديل المخطط، `IsSystemAccount(code)` **تُشتق** بمقارنة الكود مع كل قيم
   `SettingKeys.Accounts.*` الحالية (`_settings.GetSection("Accounts").Values.Contains(code)`) — يبقى متوافقاً
   تلقائياً مع أي تغيير مستقبلي في الإعدادات، بلا حاجة لعمود/مزامنة إضافية، ومتّسق مع قاعدة "الكود من الإعدادات
   لا من الكود" نفسها.
5. **الأداء — `HasTransactions` لكل حساب في `GetTree`/`GetPaged`**: استعلام واحد لكل صف (N+1) — مقبول لحجم شجرة
   حسابات نموذجي (مئات لا ملايين الصفوف)، مُوثَّق كتنازل معروف في تعليق الكود، لا صامت.

### اختبارات AccountServiceTests (10/10 ناجحة)
قاعدة بيانات مستقلة **لكل اختبار** (لا `[Collection("Database")]` المشتركة) عمداً — الاختبارات تفترض شجرة حسابات
نظيفة (كود أول ابن = `1220001` بالضبط)، وهذا يتطلب عدم تسرّب حسابات من اختبار سابق.

---

## ✅ F.2.2 — IFiscalPeriodService (2026-08-18)

### التبعية الدائرية مع F.2.3 (IJournalService) — القرار والسبب
`FiscalPeriodService.ClosePeriod/CloseYear` تحتاج عدّ القيود غير المرحّلة وإنشاء/ترحيل/حذف قيد الإقفال —
عمليات `IJournalService` (F.2.3، غير مبني بعد). بالمقابل `JournalService` المستقبلي سيحتاج
`IFiscalPeriodService.IsOpen` عند كل قيد. **الحل المُطبَّق** (بتفضيل صريح من المستخدم): `Lazy<IJournalService>`
في الـ constructor، يُحلّ عبر `ServiceLocator.Get<IJournalService>()` عند أول استخدام فعلي (أول استدعاء لـ
`ClosePeriod`/`CloseYear`/`ReopenYear`) لا عند إنشاء `FiscalPeriodService` نفسها — بحلول تلك اللحظة كل الخدمات
مسجَّلة بالفعل من `App.xaml.cs`. `IsOpen`/`GetPeriodFor`/بقية القراءة لا تحتاج `IJournalService` إطلاقاً فبُنيت
بلا أي اعتماد.

**عقد جزئي جديد**: `Services/Accounting/IJournalService.cs` — يحوي فقط ما تحتاجه `FiscalPeriodService` الآن
(`CountUnposted`, `Create`, `Post`, `Unpost`, `Delete`)، بنفس نمط `ICustomerService`/`ISupplierService` من
F.2.1. **قرار إضافي عن ذلك النمط**: `Create`/`Post`/`Unpost`/`Delete` هنا تأخذ `(DbConnection, DbTransaction)`
لا تفتح معاملتها الخاصة — عمداً، لأن `CloseYear`/`ReopenYear` يجب أن تُنفَّذا ذرّياً في معاملة واحدة مع
`SetYearClosed`/`SetYearReopened` (بناء قيد الإقفال + ترحيله + تعليم السنة مقفلة كلها تنجح معاً أو تتراجع معاً؛
لو فتحت كل عملية معاملتها الخاصة قد ينجح إنشاء/ترحيل القيد بينما تفشل `SetYearClosed` فيتدلّى قيد إقفال غير
مربوط بسنة). `CreateJournalEntryDto`/`CreateJournalLineDto` (`Services/Accounting/DTOs/JournalDto.cs`) شكل
مبدئي معقول — F.2.3 الفعلية حرة في تعديله لو احتاجت حقلاً إضافياً، `FiscalPeriodService` لن يحتاج إعادة تصميم
لمجرد إضافة حقل.

### قرارات معمارية أخرى
1. **`Models/FiscalYear.cs`/`FiscalPeriod.cs`** كانا stubs من مخطط قديم (`BaseModel`، تواريخ `DateTime`، بلا
   `IsCurrent`/`ClosingEntryId`) بلا أي مستخدم فعلي — أُعيد بناؤهما ليطابقا نمط `Account`/`JournalEntry` الفعلي
   (POCO خفيف، تواريخ `string` بصيغة `yyyy-MM-dd` مطابقة لبقية الجداول، بلا `BaseModel`) مع إبقاء أعمدة
   `SoftDelete`/`Concurrency` التي طلبها المستخدم صراحة لجدول `FiscalYears`.
2. **`GetLeaves`/القراءات في `FiscalPeriodService` بلا تحقق صلاحية عمداً** — تُستدعى بكثرة من خدمات أخرى (مثال:
   `JournalService.IsOpen` المستقبلية عند كل قيد)، بلا صلاحية `Fiscal.*` معرَّفة أصلاً؛ نفس منطق
   `AccountService.CanAcceptEntries`. `CreateYear`/`SetCurrent` تستخدمان `Settings.Edit` (لا صلاحية Fiscal
   مخصَّصة عُرِّفت لهما) — قرار عملي، مُوثَّق هنا لا في الكود فقط.
3. **`CloseYear` تستدعي `IAccountService.GetLeaves`، وهي محكومة بصلاحية `Accounts.View`** — أي مستخدم يملك
   `Settings.CloseYear` لكن ليس `Accounts.View` سيُرفض ضمنياً. لم يُبنَ تحايل حول هذا (لا طلب صريح من المستخدم
   بذلك) — تركيبة صلاحيات غير محتملة عملياً (من يُقفل سنة مالية يفترض أن يرى الحسابات) لكنها ملاحظة تستحق
   المراجعة لو ظهرت في الإنتاج.
4. **تصفير رصيد الإيرادات/المصروفات بمنطق واحد موحَّد** (`ReverseLine`): أي حساب برصيد سالب (دائن) يُصفَّر بسطر
   مدين، وأي رصيد موجب (مدين) بسطر دائن — نفس القاعدة للإيرادات والمصروفات معاً بلا تمييز نوع، لأنها فعلياً نفس
   العملية الحسابية (عكس الرصيد). صافي الربح المحسوب (`revenue - expense`) يُقيَّد على حساب الأرباح المحتجزة
   دائناً لو ربح، مديناً لو خسارة.
5. **سنة بلا أي حركة إيرادات/مصروفات (كل الأرصدة صفر)**: `CloseYear` لا تُنشئ قيد إقفال أصلاً (`ClosingEntryId`
   يبقى `null`) بدل قيد فارغ بلا سطور — حالة حافة موثَّقة في الكود، غير مُختبرة صراحة (كل اختبارات F.2.2 تُنشئ
   حركة حقيقية).
6. **`FiscalYearSeeder.cs`** يقرأ `SettingKeys.Financial.FiscalYearStartMonth` عبر `SettingRepository` مباشرة
   لا `SettingsService` — يبقى ضمن طبقة الوصول للبيانات بلا اعتماد على طبقة الخدمات (نفس سبب أن الـ Seeder
   يكتب عبر `FiscalPeriodRepository` مباشرة لا `FiscalPeriodService`: يعمل عند إقلاع التطبيق قبل أي تسجيل دخول،
   فتحقق الصلاحية لا معنى له هناك — تماماً مثل `AccountRepository.SeedDefaults`).

### اختبارات FiscalPeriodServiceTests (26/26 ناجحة — 22 دالة اختبار، منها Theory بأربع حالات)
قاعدة بيانات مستقلة **لكل اختبار** (نفس سبب `AccountServiceTests`). `FakeJournalService` بديل مؤقت لـ
`IJournalService` يستخدم `JournalRepository` الحقيقي (مكتمل ومُختبر من F.2.1) بدل بيانات وهمية — قيد الإقفال في
اختبارات `CloseYear`/`ReopenYear` مخزَّن ومُتحقَّق منه فعلياً في قاعدة البيانات، لا مجرد استدعاء وهمي ناجح.

---

## دروس تقنية من F.1.3 (بناء PrintService)

1. **`ResourceDictionary.Source` بـ Uri نسبية بسيطة يفشل بلا `Application` قائمة فعلياً** (اختبارات xUnit مثلاً): `new Uri("Resources/Print/PrintTheme.xaml", UriKind.Relative)` رمى `Cannot locate resource` رغم أن `Application.ResourceAssembly` مضبوطة. **الحل الموثوق**: pack URI مطلقة صريحة باسم التجميعة: `pack://application:,,,/{AssemblyName};component/Resources/Print/PrintTheme.xaml` — تعمل بلا أي `Application` قائمة إطلاقاً.
2. **`FlowDocument.DocumentPaginator.PageCount` يبقى 0 حتى تُفرَض حسبة متزامنة كاملة**: الترقيم افتراضياً كسول (Lazy/Dynamic). **الحل**: `((DynamicDocumentPaginator)paginator).ComputePageCount()` قبل قراءة `PageCount`، وإلا `FixedDocument` الناتج فارغ بلا أي خطأ ظاهر (فشل صامت).
3. **أنواع WPF (`FlowDocument`/`Table`/أي `DispatcherObject`) تفرض خيط STA — xUnit يُشغّل الاختبارات على MTA افتراضياً**: `System.Threading.ThreadStateException`/"calling thread must be STA". **حزمة `Xunit.StaFact` غير صالحة هنا**: كل إصدار متاح حالياً على NuGet (جرَّبت 3.0.13 ثم 1.2.77 التي تحلّلت فعلياً لـ 2.0.44) يجلب `xunit.v3.*` الذي يتصادم مع `xunit 2.9.3` المُستخدَمة فعلياً في المشروع (`CS0433` على `FactAttribute`/`CollectionAttribute`/`ICollectionFixture` — معرَّفة مرتين). **الحل**: `PrimeERP.Tests/StaThreadHelper.cs` يدوي (خيط `STA` صريح + `ExceptionDispatchInfo` لنقل أي استثناء لخيط الاختبار) — بلا أي اعتمادية جديدة، يعمل مع أي إصدار xUnit.

### قوالب طباعة مؤجَّلة (غير مبنية بعد، بقرار لا سهواً)

`PrintTemplates.cs` يحتوي `JournalEntryPrint(entry)` فقط (مبني على `Models.JournalEntry` الحقيقي). **`AccountStatementPrint`/`TrialBalancePrint`/`ReportPrint` من الوصف الأصلي لم تُبنَ** — تحتاج أشكال بيانات (`StatementLine`، `TrialBalanceLine`، `ReportResult`) غير موجودة بعد (مخرجات `IAccountService.GetStatement`/`IJournalService.GetTrialBalance` في **F.2**، و`IReportService` في **F.4**). اختراع شكل مؤقت الآن يعني إعادة بنائه بمجرد ظهور الشكل الحقيقي — تُضاف فور بناء تلك الخدمات.

---

## ✅ فحص التكرار الشامل (StatusVariant، 2026-08-17)

### 5.1 — تكرار القيم البصرية

| وُجد في | التفصيل | الإجراء |
|---|---|---|
| `Core/AppTheme.cs` | كلاس ثوابت ألوان/أبعاد كامل بديل عن `Resources/Themes` (Color.FromRgb حرفية)، مُعلَّم LEGACY أصلاً | **حُذف** — صفر مستدعٍ فعلي (grep + build) |
| `Converters/StatusColorConverter.cs` | يحوّل نصوصاً حرة ("confirmed"/"نشط"...) لألوان عبر `AppTheme` مباشرة | **حُذف** — صفر مستدعٍ فعلي، طغى عليه `VariantToBrushConverter` الجديد |
| `Converters/NegativeColorConverter.cs` | نفس النمط لأرقام سالبة/موجبة عبر `AppTheme` | **حُذف** — صفر مستدعٍ فعلي |
| `Views/Controls/Feedback/AppDialogWindow.xaml` | `Background="#33FFFFFF"`, `Foreground="#DDFFFFFF"` حرفيان (خلفية أيقونة الهيدر شبه الشفافة، نص العنوان الفرعي) + مفاتيح قديمة (`AppFontFamily`/`CanvasBrush`/`PanelBrush`/`OutlineBrush`) | **رُحِّلت القطعة كاملة الآن** (لم تُسجَّل لـ ب.3) — أُنشئ `Resources/Themes/Components/Dialogs.xaml`، استُبدل `#33FFFFFF` بـ `TextOnBrand` + `Opacity` على عنصر منفصل (لا لون بألفا مُدمج)، و`HeaderVariant` تحوّلت من `string` إلى `StatusVariant` |
| `AppConfirmDialog.cs`/`AppMessageDialog.cs`/`AppProgressDialog.cs`/`PickerTreeWindow.cs`/`PickerGridWindow.cs` | `HeaderVariant = "primary"/"danger"/...` (نص حر يكرر نفس مفردة الحالة)، ومفاتيح قديمة (`AppFontFamily`/`BodyTextBrush`/`BrandBrush`/`FaintTextBrush`) | كلها → `StatusVariant`. المفاتيح القديمة → الجديدة. `Width=420` (PickerTreeWindow) → `{StaticResource DialogWidthSm}` (نفس القيمة، مفتاح جاهز). `Height=8` الزائدة على ProgressBar (PickerProgressDialog) حُذفت — مكرِّرة لقيمة الـ Style الضمني نفسها في `Implicit.xaml` |
| `IDialogService.ShowMessageAsync`/`ConfirmAsync` | `string variant = "info"` يمرَّر حتى `AppMessageDialog` — نفس مشكلة النص الحر على مستوى الواجهة العامة | التوقيع تحوّل لـ `StatusVariant variant = StatusVariant.Info` |
| `Views/Controls/Actions/AppIconButton.xaml.cs`, `Display/AppDataGrid.xaml.cs`, `Display/AppTabControl.xaml.cs` | `Brushes.Transparent` فقط (لا لون هيكس ولا مفتاح قديم فعلي في نطاق الـ grep) | **لم تُمَس** — استثناء وظيفي مقبول (Transparent ليس لوناً من نظام التصميم)؛ هذه القطع لا تزال بانتظار ترحيل ب.2/ب.3 الكامل (تحتوي مفاتيح Theme قديمة أخرى غير مغطاة بأنماط `Colors\|Brushes` — ستُكتشف عند ترحيلها فعلياً) |
| `Services/ExportService.cs` | `XLColor.FromHtml("#F1F5F9")`, `.FontSize(9/15)`, `Colors.Grey.Lighten2/3` (لون QuestPDF المدمج، ليس لوننا)، `.FontFamily("Arial")` مباشرة في كود QuestPDF/ClosedXML | **رُبطت بـ `ExportTheme.cs`** — `HeaderBackgroundHex`/`OutlineHex`/`FontFamily`/`TitleFontSize`/`HeaderFontSize`/`BodyFontSize` بدل كل قيمة حرفية |

### 5.2 — تكرار المفاتيح والألوان في التصميم

- **`FocusRingWidth`**: موجود في `Colors.xaml` (فاتح) **وغير موجود** في `Colors.Dark.xaml` — الفحص الآلي (مقارنة كل مفاتيح الملفين) وجده الثغرة الوحيدة من نوعها. **أُضيف** لـ `Colors.Dark.xaml` بنفس القيمة (2 — عرض لا لون، لا داعٍ لاختلافه بين الوضعين).
- **لونان بنفس الست عشري بأسماء مختلفة**: وُجد `#0F172A` تحت 8 مفاتيح مختلفة (`TextPrimary`, `TopBarText`, `TableRowSelectedText`, `StateHoverOverlay`, `StatePressedOverlay`, `ShadowSm/Md/Lg/Xl`). **قرار**: ليست تكراراً ضاراً — كل مفتاح يمثّل *سياق استخدام* مختلف (نص عام/نص شريط علوي/نص صف محدد/تراكب مرور/تراكب ضغط/ظل)، تتشارك نفس لون الحبر الأساسي حالياً لكن قد تختلف مستقبلاً بلا حاجة لتتبّع كل استخدام — هذا تصميم رموز سليم (context tokens)، ليس نفس المشكلة التي يحذّر منها القسم 1 (تكرار *معنى الحالة*، لا تكرار *قيمة اللون الخام*).
- **مفاتيح غير مستخدمة حالياً**: فحص آلي شامل (كل مفتاح في `Colors.xaml` × كل صيغة استخدام محتملة: `{Static/DynamicResource}`, `FindResource("")`, نص حرفي داخل `VariantToBrushConverter`) وجد 38 مفتاحاً بلا أي استخدام فعلي الآن — كلها من فئتين: (أ) مفاتيح `Nav*`/`TopBar*`/`Table*` مُعدَّة سلفاً لقطع `AppSidebar`/`AppTopBar`/`AppDataGrid` (مواصفة تفصيلية أُعطيت مسبقاً في هذه الجلسة لـ ب.3/ب.2، لم تُنفَّذ بعد)، (ب) رموز حالة تفاعل عامة محجوزة (`StateSelectedBg`, `BrandActive`, `FocusRing`, `ShadowLg/Xl`, `TextLink`, `SurfaceOverlay`) بلا مستهلك أول بعد. **لم تُحذف** — حذفها يعني إعادة إنشائها حرفياً خلال نفس الجلسة عند تنفيذ ب.2/ب.3، وهذا خلاف "غير مستخدم إطلاقاً" بالمعنى الضار (لا مسار مستقبلي) الذي تحذّر منه القاعدة.

### 5.3 — تكرار الكلاسات والدوال

- **`ValidationResult` مُعرَّفة مرتين**: `Core/Validation/ValidationResult.cs` (الجديدة، تُستخدم فعلياً في كل Validators الجديدة) و`Core/Validator.cs` (قديمة، تعريف مضمَّن بجانب كلاس `Validator` الساكن). تحقّق: صفر مستدعٍ فعلي لـ `Validator.Required/MaxLength/...` أو لـ `Core.ValidationResult` بالاسم المجرَّد في أي ملف نشِط (فحصت الـ 15 ملفاً التي تستورد `using PrimeERP.Core;` تحديداً لخطر التصادم الضمني — لا استخدام). **حُذف `Core/Validator.cs` بالكامل**.
- **`Company`/`Accounts` بنفس الاسم في مكانين**: `Models/Company.cs` مقابل `SettingKeys.Company` (متداخلة)، و`PermissionKeys.Accounts` مقابل `SettingKeys.Accounts` (متداخلتان في أبوين مختلفين). **لا تصادم فعلي** — الوصول دائماً عبر التأهيل الكامل (`SettingKeys.Accounts.Customers` لا `Accounts.Customers` مجرَّدة)، تشابه أسماء طبيعي بين "الحسابات كمفهوم إعداد" و"الحسابات كمفهوم صلاحية"، لا فعل مطلوب.
- **منطق حساب أرصدة/توليد أرقام**: راجَعته خطوة 0 سابقاً (`GenerateEntryNo` المكرِّرة لـ `NumberSequenceService` — محذوفة، `DocumentService.RecalculateAccountBalance` المتبقّية بقرار صريح من المستخدم). فحص هذه الجلسة لم يجد تكراراً جديداً في نفس الفئة.
- **ملاحظة غير مُصلَحة (خارج نطاق "الألوان" لكن نفس روح التكرار)**: أكثر من 17 خاصية `XxxText => Xxx.ToString("N2")` مكرَّرة حرفياً عبر كل ملفات `Models/*.cs` — لا تحترم `SettingKeys.Financial.DecimalPlaces`/`UI.UseArabicNumerals` المُعرَّفتين فعلياً في F.1.1. **لم تُصلَح** — خارج نطاق تكرار "الألوان/الخطوط" الذي يستهدفه هذا الفحص تحديداً، ويحتاج قراراً معمارياً منفصلاً (هل يستدعي Model خدمة إعدادات؟ يخالف "Model لا يعرف Service" من معمارية الطبقات) — مُسجَّلة لمناقشة لاحقة.

### 5.4 — تكرار الخدمات والقطع

- `Core/AppTheme.cs`: انظر 5.1 أعلاه — كان تكراراً كاملاً لـ `Resources/Themes`، حُذف.
- `_Legacy/*` (12 ملفاً): فُحصت كلها — **لا بديل مكتمل بعد لأي منها** (البديل الحقيقي هو صفحة حسابات/قيود كاملة من القطع الجاهزة، مجدولة في **المرحلة H**، لم تُبنَ). لا حذف.
- لا تكرار Repository↔Repository أو Service↔Service جديد وُجد خارج ما رصدته خطوة 0 سابقاً.

### 5.5 — تكرار النصوص

نصوص عربية حرفية مكرِّرة لمفاتيح **موجودة أصلاً** في `Strings.ar.xaml` (`Str.Confirm`/`Str.Cancel`/`Str.Close`/`Str.SearchPlaceholder`) وُجدت في: `Services/DialogService.cs` (قيمة افتراضية `confirmText = "تأكيد"` + `"إلغاء"` مباشرة)، `AppConfirmDialog.cs`، `AppMessageDialog.cs` ("إغلاق")، `AppProgressDialog.cs` ("إلغاء")، `PickerTreeWindow.cs`/`PickerGridWindow.cs` ("إلغاء" + "بحث..."). **كلها استُبدلت** بـ `LocalizationService.Get("Str.X")` (القيم الافتراضية للبارامترات تحوّلت لـ `null` مع حل داخل الجسم — C# لا يسمح باستدعاء دالة كقيمة افتراضية).

نصوص عربية أخرى وُجدت في `Services/`/`Core/` (رسائل استثناء داخلية في `ServiceLocator`/`NavigationService`، تفاصيل Audit log، قيم Seed افتراضية في `SettingKeys.cs`) — **لم تُنقل** بوعي: رسائل استثناء لا تُعرض للمستخدم إطلاقاً (أخطاء برمجية)، تفاصيل الـ Audit نص وصفي حر بنفس نمط `DocumentService.cs` المُعتمَد أصلاً منذ المرحلة C، وقيم Seed بيانات افتراضية لا نص واجهة (تماماً كأسماء الحسابات الافتراضية في `AccountRepository.SeedDefaults`).

---

## ✅ خطوة 0 — تصحيح طبقات المسؤولية (المرحلة F، قبل F.1، 2026-08-17)

القاعدة: Repository = SQL خام ↔ Models فقط، بلا منطق أعمال ولا معاملات ولا Audit (راجع `DESIGN_SYSTEM.md` § معمارية الطبقات). تحقّقت أولاً أن لا مستدعٍ فعلي نشِط (خارج `_Legacy/`) لأي من الدوال المحذوفة أدناه (`grep` شامل)، فلا كسر حقيقي حدث.

### AccountRepository.cs — نُقل/حُذف
| كان في Repository | الوجهة |
|---|---|
| حساب `Level` من الأب داخل `Insert` | `AccountService.Create` (F.2) |
| تحديث `IsLeaf=false` للأب داخل `Insert` | `AccountService.Create` (F.2) — دالة جديدة `AccountRepository.SetIsLeaf(code, isLeaf)` أُضيفت كـ CRUD صرف يستدعيها |
| مزامنة اسم Customers/Suppliers داخل `Update` | `AccountService.Update` (F.2) — Repository الآن يحدّث صف الحساب فقط |
| حذف Customers/Suppliers المرتبط داخل `Delete` | `AccountService.Delete` (F.2) |
| `AuditLogger.Log(...)` داخل `Insert`/`Update`/`Delete` | الخدمة (F.2) — بعد نجاح المعاملة |
| `GenerateChildCode(parentCode)` بالكامل | `AccountService.GenerateChildCode` (F.2) — خوارزمية عمل، ليست SQL خام |
| `HasTransactions(code)` (كان يقرأ جدول `JournalEntryLines` — ليس جدول هذا الـ Repository) | **حُذف نهائياً** — استُبدل بـ `JournalRepository.HasLinesForAccount(code)` جديدة، تستدعيها `AccountService.Delete` مباشرة |

### JournalRepository.cs — نُقل/حُذف
| كان في Repository | الوجهة |
|---|---|
| `InsertEntry(entry)` بالكامل (فتح معاملة + حساب إجماليات + إدراج سطور + تحديث أرصدة + Audit) | فُكِّك إلى دوال CRUD صرفة تأخذ `(conn, tx)` من الخدمة: `InsertHeader`, `InsertLine`, `UpdateTotals` — الخدمة (F.2) تفتح `Db.RunTransaction` وتنسّق الاستدعاءات + Audit |
| `UpdateAccountBalance(code)` (يكتب في جدول `Accounts` — ليس جدول هذا الـ Repository) | **حُذف** — استُبدلت بقراءة صرفة `GetDebitCreditSum(code)`؛ الكتابة الفعلية على `Accounts` عبر `AccountRepository.UpdateBalance` تستدعيها الخدمة |
| `DeleteEntry(entryId)` (حذف + إعادة حساب أرصدة + Audit) | فُكِّك إلى `DeleteLines`, `DeleteHeader` صرفتين — التنسيق + إعادة حساب الأرصدة + Audit في `JournalService.Delete` (F.2) |
| `Post(entryId)` (تحديث + Audit) | فُكِّكت إلى `SetPosted(id, bool)` صرفة — `JournalService.Post` (F.2) تتحقق من الصلاحية وتُسجّل الـ Audit |
| **`GenerateEntryNo()`** | **حُذفت نهائياً — كانت تكراراً كاملاً لـ `Services/NumberSequenceService.Next(key)` المبنية في S.1.** الخدمة (F.2) تستدعي `INumberSequenceService.Next("Journal")` بدلها |

### Core/Validation/AccountingRules.cs — إصلاح نقاء الدالة
`NoNegativeStock` كانت تقرأ `Core.Settings.AppSettings.AllowNegativeStock` مباشرة من داخلها (خرق: "AccountingRules دوال نقية، بلا وصول لقاعدة بيانات"). التوقيع الآن يأخذ `allowNegativeStock` بارامتر — المستدعي (`StockService` مستقبلاً) يقرأه من `ISettingsService` قبل النداء.

### Services/NumberSequenceService.cs — خرق مُكتشف ومُصلَح فوراً
كانت الخدمة الوحيدة الموجودة فعلياً (بُنيت في S.1، قبل قاعدة الطبقات هذه) تستدعي `Core.Database.DbHelper` **مباشرة** (خرق: "Service لا SQL مباشر — يمر بـ Repository"). أُنشئت `Database/NumberSequenceRepository.cs` جديدة (SQL خام صرف: `EnsureRow`/`GetRow`/`UpdateNext`)، ونُقل منطق "هل نُصفّر لأن السنة تغيّرت؟" (قرار عمل) إلى الخدمة نفسها، التي تستدعي الـ Repository الآن فقط، وتدير المعاملة (`Db.RunTransaction`) بنفسها كما تقتضي القاعدة.

### فحص "لا نسخة مكرّرة" (0.1) — نتيجة مهمة
`Core/Transactions/DocumentService.cs` (بُني في المرحلة C) يحتوي فعلياً **نفس منطق** إدراج قيد + سطور + إعادة حساب رصيد الحساب الذي كان مكرَّراً في `JournalRepository.InsertEntry`/`UpdateAccountBalance` (الآن مُزالين). بتوجيه صريح من المستخدم: `DocumentService.cs` **يبقى كما هو دون تعديل** (هو المسار العام لترحيل أي مستند عبر `IPostable` — فواتير/أذون مخزن/رواتب)، ولا يُعاد بناؤه ليمر بـ `JournalRepository`. **ملاحظة لِـ F.2**: `IJournalService.Create` (قيود يدوية) و`DocumentService.Post` (قيود مستندات) سيتشاركان نفس الشكل المنطقي (إدراج رأسية+سطور+إعادة حساب رصيد) بلا مشاركة كود فعلية بينهما حالياً — قرار مقصود من المستخدم، ليس سهواً؛ يُترك كما هو ما لم يُطلب توحيدهما لاحقاً.

فحصتُ أيضاً: لا `ViewModel`/`Page`/`Control` نشِط يستدعي `*Repository` مباشرة، لا `Repository` يستدعي `Repository`/`Service` آخر، لا `Repository` يفتح معاملة (`RunTransaction`)، لا `Validator` يكتب لقاعدة البيانات، ولا كلاس Audit آخر غير `Core/Audit/AuditLogger.cs`. كل الفحوصات عبر `grep` شامل على الشجرة النشطة (باستثناء `_Legacy/`)، لا افتراضاً.

### Core/Settings/AppSettings.cs و Core/BackupManager.cs — لم يُحذفا بعد (قرار، لا نسيان)
كلاهما بلا أي مستدعٍ فعلي حالياً (تحقّق بالـ grep). `Core/Settings/AppSettings.cs` مصنَّف "ثوابت مُحمَّلة من قاعدة البيانات" لا "خدمة حقيقية" (لا واجهة `ISettingsService`، لا `Get<T>`/`Set<T>` عام، لا Category/DataType، لا Cache/Event) — **لن يُبنى بجانبه `SettingsService` منفصلة؛ يُحذف هو عند اكتمال F.1 مباشرة**، لا قبل ذلك. نفس القرار لـ `Core/BackupManager.cs` مقابل `IBackupService` في F.1.

---

## ✅ ما نُفِّذ فعلاً في الجزء أ (2026-08-17)

القرار الأصلي كان "لا حذف قبل المرحلة G" — المستخدم قرر لاحقاً تقديم التنظيف إلى المرحلة E. هذا القسم يوثّق ما حدث فعلاً، بديلاً عن الأقسام الافتراضية أدناه حيثما تعارضتا.

### أ.1 — حذف القطع/الملفات صفرية الاستخدام (كلها placeholder فارغة أو مؤكَّدة بلا مستخدم)
حُذفت فعلياً، وبُني المشروع بنجاح بعد كل حذفة:
`Views/Controls/{AppButton,AppTextBox,AppComboBox,AppDataGrid,FilterBar,PageHeader,SummaryBar,EntityPicker}.xaml(.cs)`،
`Models/{Invoice,InvoiceLine}.cs`،
`Core/AuditLogger.cs` (القديم — **انظر التصحيح أدناه، لم يكن فعلاً بلا مستخدم**)،
`Database/{CustomerDb,SupplierDb,ProductDb,InvoiceDb,EmployeeDb,StockDb,UserDb}.cs` (كلها 6 أسطر placeholder)،
`Validators/{CustomerValidator,InvoiceValidator}.cs` (placeholder فارغ).

**⚠️ تصحيح على القسم "القطع الآمنة تماماً" السابق**: الادعاء بأن `Core/AuditLogger.cs` (القديم) "غير مستدعى من أي مكان" كان **خطأً**. عند حذفه فعلياً ظهرت أخطاء ترجمة حقيقية في `Core/AppSession.cs` (خاصية `AuditLogger.CurrentUser`)، `Database/AccountDb.cs`، و`Database/JournalDb.cs` — كلها كانت تستدعيه عبر آلية "ancestor-namespace" (بلا `using` صريح، اعتماداً على القرب من `PrimeERP.Core`). أُصلِح بإزالة استخدام `CurrentUser` من `AppSession.cs` (النمط الجديد لا يحتاجه، `Core.Audit.AuditLogger` يقرأ `AppSession.Username` مباشرة) وإضافة `using PrimeERP.Core.Audit;` صريحة إلى `AccountDb.cs`/`JournalDb.cs` (قبل حذفهما لاحقاً في أ.5). **الدرس**: بحث grep عن اسم الكلاس وحده لا يكفي لإثبات "صفر استخدام" — لازم فحص الحل الكامل (dotnet build) لأن الاستدعاء البسيط بلا namespace مؤهل بالكامل يُخفي الاعتمادية.

### أ.2 — استبدال `PaginationControl` (القديمة) بـ `AppPagination` (الجديدة) في الصفحتين الحقيقيتين
`Views/Pages/AccountsPage.xaml` و`JournalPage.xaml` (+ `.xaml.cs`) — تم التحويل الكامل (`xmlns` + وسم العنصر + `pagination.TotalItems = count` بدل `pagination.Setup(...)`). ثم حُذفت `Views/Controls/PaginationControl.xaml(.cs)` نهائياً.

### أ.3 — نقل الصفحات/الحوارات/ViewModels القديمة (بلا بديل بعد) إلى `_Legacy/`
`Views/Pages/{Accounts,Journal}Page.xaml(.cs)`، `Views/Dialogs/{Account,Journal}Dialog.xaml(.cs)`، `ViewModels/{Accounts,Journal}ViewModel.cs`، ولاحقاً في أ.5 أيضاً `Validators/{Account,Journal}Validator.cs` (القديمتان — مستخدَمتان فقط من الحوارات المنقولة). **`Views/Windows/MainWindow.xaml(.cs)` أُعيد كتابته بالكامل** (حُذف `_navItems`/`BuildNav()`/معالج F12/استدعاء `DbHelper.Initialize()` القديم) ليصبح مجرد `<ContentControl x:Name="pageContainer"/>` يحمّل `ControlsGalleryPage` — هذا الآن **سلوك دائم**، ليس "TEMP" مؤقتاً للاختبار. `PrimeERP.csproj` استُثنيت منه `_Legacy/**` بالكامل عبر (`*.cs`/`*.xaml` بامتداد صريح — الصيغة `_Legacy\**` بلا امتداد فشلت صامتة، انظر ملاحظة الأخطاء).

### أ.4 — إزالة دمج `Resources/Themes.xaml` القديم من `App.xaml`
`Resources/Themes.xaml` نُقل فعلياً إلى `_Legacy/Resources/Themes.xaml` (لا يزال مطلوباً من الحوارات المنقولة في أ.3). `App.xaml` الآن يدمج فقط `Theme.xaml` + `Strings.ar.xaml`. تم التأكد أن نمط `ScrollBar` الضمني الجديد (E.1) يغطي فقدان القديم بالكامل.

### أ.5 — إعادة بناء طبقة البيانات
- **`Database/AccountRepository.cs`** (جديد) — بديل `AccountDb.cs`، فوق `Core/Database/DbHelper` + `SchemaBuilder` (نمط `PermissionDb.cs` المرجعي)، يرجع `Models.Account` مباشرة.
- **`Database/JournalRepository.cs`** (جديد) — بديل `JournalDb.cs`، نفس النمط، مع `InsertEntry` مبني على `DbHelper.RunTransaction`/`CreateCommand` العامّين (لا `SqliteCommand` مباشر كما في القديم) — أول كود ترحيل قيود يومية provider-agnostic فعلياً في المشروع.
- **`Core/Settings/AppSettings.cs`** (جديد، namespace `PrimeERP.Core.Settings`) — بديل `Core/AppSettings.cs`، نفس الواجهة العامة (`CompanyName`/`Currency`/`FiscalYearStart`/`InvoicePrefix`/`CustomerAccountRoot`/`SupplierAccountRoot`/`AllowNegativeStock`/`CreateTable`/`Load`/`Set`) لكن فوق `SchemaBuilder`/`DbHelper` الجديدين بدل SQL خام.
- **`Core/Validation/AccountingRules.cs`** — أُضيفت `using PrimeERP.Core.Settings;` صريحة (كانت تعتمد على `AppSettings` القديم عبر ancestor-namespace من `Core.Validation`؛ الجديد في namespace شقيق `Core.Settings` فاحتاج `using` صريحة).
- **`Core/Validation/Validators/AccountValidator.cs`** (Phase C) — `AccountDb.GetByCode` → `AccountRepository.GetByCode`.
- **`Core/Transactions/DocumentService.cs`** — الاستيراد المستعار `using AccountDb = PrimeERP.Database.AccountDb;` → `... = PrimeERP.Database.AccountRepository;` (بلا تغيير باقي الكود، فقط استبدال الهدف).
- **`Validators/AccountValidator.cs` + `JournalValidator.cs`** (القديمتان) نُقلتا إلى `_Legacy/Validators/` (مستخدَمتان فقط من حواري `_Legacy` المنقولين في أ.3).
- **حُذف نهائياً**: `Database/AccountDb.cs`، `Database/JournalDb.cs`، `Core/AppSettings.cs` (القديم).
- **تصحيح لاحق (بداية المرحلة S)**: الادعاء أعلاه بأن مسح `obj/bin` كان كافياً كان **غير دقيق** — بناء نظيف فعلي (`rm -rf obj bin`) أعاد نفس فشل `MC3074` على `_Legacy/Views/Pages/{Accounts,Journal}Page.xaml`. السبب الحقيقي: صيغة `<Page Remove="_Legacy/**/*.xaml" />` (رغم Slash أمامي + امتداد صريح) **لا تُستبعد فعلياً** من عناصر `Page` الضمنية لـ WPF SDK (فقط `Compile Remove` كانت تعمل، فتُستبعد الكلاسات لكن XAML يُترجَم رغم ذلك ويفشل لعدم وجودها). **الإصلاح الجذري**: استبدال كل عناصر `Remove` الأربعة بخاصية واحدة `<DefaultItemExcludes>$(DefaultItemExcludes);_Legacy/**</DefaultItemExcludes>` في `PropertyGroup` — تمنع الجلب الضمني (globbing) من الأساس بدل محاولة الاستبعاد بعده. `dotnet build` نظيف بعدها فعلياً: **0 تحذير / 0 خطأ** (تم التحقق ببناء نظيف كامل، لا كاش).

### أ.6 — التحقق النهائي
- ✅ `dotnet build PrimeERP.csproj` بعد حذف `obj/bin`: **Build succeeded, 0 Warning(s), 0 Error(s)**.
- ✅ لا كلاس بنفس الاسم مرتين في الكود الفعّال (خارج `_Legacy/`) — تم فحص كل تعريفات `public (static) class` في المشروع؛ `AppButton`/`AppTextBox`/`AppComboBox`/`AppDataGrid`/`DbHelper`(القديم)/`AuditLogger`(القديم)/`AccountValidator`(القديم)/`JournalValidator`(القديم) كلها كانت مصدر الخطر المذكور سابقاً في G.5 — **زال هذا الخطر فعلياً** لأن كل نسخها القديمة إما حُذفت أو انتقلت إلى `_Legacy/` (مستثنى من البناء).
- ✅ لا تعارض `x:Key` فعلي وقت التشغيل: التكرارات الوحيدة الموجودة (`BrandDefault`, `SurfaceDefault`, `Str.Add`...) هي بين *أزواج متغيّرات* — `Colors.xaml` مقابل `Colors.Dark.xaml` (فاتح/داكن)، و`Strings.ar.xaml` مقابل `Strings.en.xaml` (عربي/إنجليزي) — كل زوج يُدمَج ملف واحد منه فقط وقت التشغيل (`App.xaml` يدمج `Colors.xaml`+`Strings.ar.xaml` فقط حالياً)، فلا تصادم فعلي. تم التحقق أنه لا يوجد أي تكرار *داخل* نفس الملف.

---

## ما تبقّى (ولماذا لم يُحذف/يُرحَّل بعد)

| القطعة القديمة | لماذا باقية | الخطوة التالية |
|---|---|---|
| `ViewModels/AccountsViewModel.cs` + `JournalViewModel.cs` | منقولة إلى `_Legacy/` (مستثناة من البناء) — لا بديل جديد (صفحة+ViewModel من القطع) بُني بعد | تُستبدل بالكامل في **المرحلة H** عند تجميع صفحتي الحسابات/القيود من القطع الجاهزة |
| `Views/Pages/{Accounts,Journal}Page.xaml(.cs)` + `Views/Dialogs/{Account,Journal}Dialog.xaml(.cs)` | نفس السبب أعلاه | **المرحلة H** |
| `_Legacy/Validators/{Account,Journal}Validator.cs` | مستخدَمتان فقط من الحوارات القديمة المنقولة أعلاه | تُحذف نهائياً عند حذف تلك الحوارات في **المرحلة H** |
| `_Legacy/Resources/Themes.xaml` | تحتاجه ملفات `_Legacy` الأربعة (Pages/Dialogs) فقط | يُحذف نهائياً مع بقية `_Legacy/` في **المرحلة H** |
| `Views/Pages/{Customers,Products,Suppliers,Purchases,Sales,HR,Reports}Page.xaml` + حواراتها | placeholder فارغ من الأصل، لا علاقة بالتنظيف | تُبنى من الصفر في **المرحلة H** |
| ~~`Views/Controls/Documents/Tests/LineEngineTests.cs`~~ | ✅ **تم** — نُقلت وحُوِّلت لاختبارات xUnit حقيقية (`PrimeERP.Tests/Documents/LineEngineTests.cs`، 22 اختبار) عند بناء `PrimeERP.Tests` في F.1.1 | — |
| `Core/Settings/AppSettings.cs` | ✅ **حُذف** — استُبدل بـ `Services/Settings/SettingsService.cs` (F.1.1) | — |
| `Core/BackupManager.cs` | ✅ **حُذف** — استُبدل بـ `Services/Backup/BackupService.cs` (F.1.2) | — |
| `Core/Transaction.cs` (غلاف SQLite-فقط فوق `RunTransaction`) | ✅ **حُذف** — صفر مستدعٍ فعلي، وطغى عليه تماماً `Core.Database.DbHelper.RunTransaction` العام؛ اكتُشف أثناء تتبّع آخر مستخدم لـ `Database/DbHelper.cs` القديم (F.1.2) | — |
| `Database/DbHelper.cs` القديم (namespace `PrimeERP.Database`) | ✅ **حُذف** — كان آخر مستخدمَين له (`BackupManager`، `Core/Transaction.cs`) حُذفا معاً في F.1.2؛ تأكّد الصفر بـ `grep` شامل ثم `dotnet build` ناجح | — |

---

## القسم 2 — تضارب مفاتيح الموارد (تاريخي — كان قبل أ.4)

قبل أ.4، لم يكن هناك أي تعارض `x:Key` بين `Resources/Themes.xaml` القديم و`Resources/Themes/*.xaml` الجديدة (أسماء مختلفة عمداً). هذا القسم بلا موضوع الآن — `Resources/Themes.xaml` لم يعد مُدمَجاً في `App.xaml` (انتقل إلى `_Legacy/`، أ.4).

### نمط `ScrollBar` الضمني — تم التعامل معه نهائياً
`Resources/Themes/ScrollBars.xaml` (من E.1) يغطي كل أشرطة التمرير في التطبيق الآن. لا خطر متبقٍ من إزالة دمج القديم.

---

## دروس تقنية من الجزء أ (تفيد أي تنظيف مستقبلي)

1. **"صفر استدعاءات باسم الكلاس" عبر grep لا يكفي لإثبات عدم الاستخدام** — استدعاء بلا `namespace` مؤهل قد يعتمد على قرب namespace (ancestor) بلا `using` صريح. التأكيد الحقيقي الوحيد: حذف الملف فعلياً ثم `dotnet build`.
2. **صيغة استثناء MSBuild الخاطئة تفشل صامتة، وحتى الصيغة "الصحيحة" ظاهرياً قد تفشل لعنصر `Page`**: `<Compile Remove="_Legacy\**" />` (باك سلاش، بلا امتداد) لم تستبعد شيئاً. حتى بعد تصحيحها لـ `_Legacy/**/*.cs` (سلاش أمامي + امتداد)، ظل `<Page Remove="_Legacy/**/*.xaml" />` بلا تأثير فعلي على عناصر `Page` الضمنية في WPF SDK (اختبار: بناء نظيف كامل بعد حذف `obj/bin`). **الحل الموثوق**: `<DefaultItemExcludes>$(DefaultItemExcludes);_Legacy/**</DefaultItemExcludes>` في `PropertyGroup` — يمنع الجلب الضمني قبل حدوثه، بدل الاعتماد على `Remove` بعده.
3. **كاش `obj/` قد يُبقي استثناء glob قديماً عالقاً** — بعد أي تعديل جوهري على أنماط `Remove` في `.csproj`، أو بعد نقل ملفات جماعي إلى/من مجلد مستثنى، نظّف `obj/`+`bin/` قبل التشك في الكود نفسه.
4. **نقل namespace من "متداخل تحت الأب" إلى "شقيق" يكسر المراجع الضمنية** — مثال حي: `AppSettings` كان `PrimeERP.Core` (أب مباشر لـ `PrimeERP.Core.Validation`، فيُرى بلا `using`)؛ بعد النقل لـ `PrimeERP.Core.Settings` (شقيق لـ `Core.Validation`، ليس أباً) احتاج `using` صريحة. أي نقل namespace لاحق يجب فحص كل المستخدمين الحاليين لنفس هذا السبب.
5. **مشروع جديد داخل مجلد المشروع الرئيسي يُجلَب ضمنياً في بنائه ما لم يُستثنَ صراحة** — عند إنشاء `PrimeERP.Tests/` (F.1.1) فشل بناء `PrimeERP.csproj` بأخطاء `Fact`/`ICollectionFixture` غير معروفة، لأن `**/*.cs` الضمنية جلبت ملفات اختبارات xUnit كأنها جزء من `PrimeERP.csproj` نفسه. **الحل**: أضيف `PrimeERP.Tests/**` لنفس `DefaultItemExcludes` المستخدَمة لـ `_Legacy/**` (الدرس 2 أعلاه) — أي مشروع فرعي جديد بملف `.csproj` خاص به يحتاج نفس المعالجة فوراً عند إنشائه.
6. **مشروع WPF مُشار إليه عبر `ProjectReference` من مشروع اختبارات قد يفشل ببناء غير مكتمل (`.g.cs` "could not be found") — سباق (race) في MSBuild العقدي المتوازي، لا مشكلة ثابتة**: أول مرة ظهر الخطأ عند بناء المشروعين بأمرين منفصلين متتاليين؛ جرّبت "نظّف الاثنين وابنِ من مشروع الاختبار مباشرة" فنجحت مرة، ثم **فشلت بنفس الخطأ تماماً في محاولة لاحقة بنفس الأوامر** — أثبت أن الحل السابق لم يكن جذرياً، فقط قلّل احتمال السباق. **الحل الموثوق فعلياً**: تمرير `-m:1` (تعطيل تعدد عُقد MSBuild المتوازية) لكل من بناء `PrimeERP.csproj` أولاً ثم `dotnet test`/`dotnet build` لـ `PrimeERP.Tests.csproj` — عمل بثبات دون أي فشل. القاعدة العملية: أي أمر `dotnet build`/`dotnet test` يمسّ المشروعين معاً (مباشرة أو عبر ProjectReference) يُمرَّر له `-m:1`.

---

## المهام المتبقية (كانت G، الآن مؤجَّلة فعلياً للمرحلة H أو F كما هو موضّح أعلاه)

- استبدال `ViewModels/{Accounts,Journal}ViewModel.cs` + الصفحات/الحوارات القديمة بتجميع من القطع — **المرحلة H**.
- بناء بديل لـ `Core/BackupManager.cs` فوق `Core/Database/DbHelper` الجديد (F.1.2)، ثم حذف `Database/DbHelper.cs` القديم نهائياً — **المرحلة F**.
- ✅ مشروع `PrimeERP.Tests` (xUnit) موجود الآن، بـ 31 اختباراً تعمل (22 `LineEngineTests` + 9 `SettingsServiceTests`).
- حذف `_Legacy/` بالكامل بعد اكتمال المرحلة H.
