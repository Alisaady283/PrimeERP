# DESIGN_SYSTEM.md

هذا الملف يوثّق القواعد المعمارية المُلزمة لمشروع PrimeERP. يُستكمل تدريجياً مع كل مرحلة
(قسم الفصل البصري/الهيكلي لقطع الواجهة يُضاف في نهاية المرحلة E — القسم 5).

---

## القاعدة الأولى — الأساس قبل التكرار

قبل بناء أي شيء جديد (خدمة، Repository، ViewModel، قطعة):

1. **يوجد أساس مشترك له بالفعل؟** → رِثه، لا تُعد بناءه.
2. **لا يوجد، وسيتكرر في أكثر من موضع؟** → ابنِ الأساس المشترك أولاً، ثم ابنِ فوقه.
3. **منطق ظهر في مكانين بالفعل؟** → استخرجه إلى الأساس فوراً، لا لاحقاً "عند التنظيف".
4. **يمكن وصفه بتكوين (بيانات) بدل كود؟** → افعل ذلك بدل تكرار الكود بصيغ متشابهة.

**السبب**: بُنيت خدمات F.2.1–F.3.1 (Account/Journal/FiscalPeriod/Customer) كل واحدة منفصلة عن الأخرى بلا أساس
مشترك، فتكرر نفس المنطق (صلاحية، معاملة، Audit، ترقيم، رسائل) في كل خدمة — عكس مبدأ "القطع" المعتمد للمشروع
كله. القاعدة هنا تمنع تكرار ذلك في أي طبقة أخرى (تصميم، بيانات، منطق، عرض، تركيب).

جدول الأسس المعتمدة لكل طبقة (تُستكمل الفارغة مع كل مرحلة):

| الطبقة | الأساس |
|---|---|
| التصميم (الرموز البصرية) | `5.Design/Identity` (L1) → `Semantic` (L2) → `Components/Tokens.*` (L3) → `Styles/Style.*` (L4) — كاملة منذ R4 |
| القطع (Views/Controls) | نمط الأنماط في `Resources/Themes/Components/*.xaml` |
| البيانات (Repository) | *(لم يُبنَ بعد)* |
| المنطق (Service) | *(لم يُبنَ بعد)* |
| العرض (ViewModel) | *(لم يُبنَ بعد)* |
| التحقق (Validator) | `Core/Validation/` |
| التركيب (صفحة = تكوين) | *(لم يُبنَ بعد)* |

---

## مفردات الحالة المقفلة (StatusVariant)

`Core/Common/StatusVariant.cs` — ستة قيم فقط، لا سابع أبداً: `Neutral`, `Info`, `Brand`, `Success`, `Warning`, `Danger`.

**السبب**: بلا مفردات مقفلة، كل خدمة/قطعة تخترع تصنيفها الخاص لنفس المعنى (نجاح/تم/مقبول = ثلاثة أخضر مختلفة
بثلاثة أسماء مختلفة). القفل يفرض: أي حالة أعمال جديدة تُصنَّف لواحدة من الستة، لا تُضاف كقيمة سابعة.

### جدول التصنيف الكامل

| حالة الأعمال | StatusVariant |
|---|---|
| Posted / Active / Balanced / Paid / Valid / Completed | `Success` |
| Draft / Pending / PartialPaid / LowStock / Warning | `Warning` |
| Cancelled / Inactive / Unbalanced / OutOfStock / Invalid / Failed / Overdue | `Danger` |
| Note / Information / Hint | `Info` |
| PrimaryAction / Selected | `Brand` |
| NoState / Disabled / Empty | `Neutral` |

### توزيع المسؤولية

| الطبقة | مسموح | ممنوع |
|---|---|---|
| Service/Repository/Validator/Core/Model | يرجع `StatusVariant` أو enum حالة أعمال | يعرف لوناً أو خطاً إطلاقاً |
| ViewModel | يحوّل حالة الأعمال → `StatusVariant` | يرجع `Brush` |
| Converter (`Views/Converters/VariantToBrushConverter.cs`) | `StatusVariant` → `Brush` من Themes فقط، parameter: `Solid`/`Soft`/`SoftText`/`Hover`/`Border` | — |
| `Resources/Print/PrintTheme.xaml` عبر `PrintService` | `StatusVariant` → لون طباعة ثابت | — |
| `Resources/Export/ExportTheme.cs` عبر `ExportService` | `StatusVariant` → لون تصدير ثابت (Hex) | — |

مثال حقيقي طُبِّق أثناء بناء هذا القسم: `AppDialogWindow.HeaderVariant` كانت `string` ("primary"/"danger"/...)
تقرأ مفاتيح Themes قديمة مباشرة من داخل القطعة — تحوّلت لـ `StatusVariant` + `VariantToBrushConverter`،
وامتد نفس التحويل عبر `IDialogService.ShowMessageAsync`/`ConfirmAsync` وكل حوارات الاختيار (Pickers).

---

## أين تعيش القيم البصرية

ثلاث مناطق فقط — لا رابعة:

| المنطقة | تتبدّل فاتح/داكن؟ | تُدمَج في App.Resources؟ | تخدم |
|---|---|---|---|
| `Resources/Design/` (+ `Resources/Themes/Typography.xaml`/`Metrics.xaml`/`Implicit.xaml`/`Components/*` — لم تُنقل بعد) | ✅ نعم | ✅ نعم | الشاشة (WPF Views) |
| `Resources/Print/` | ❌ لا — ثابتة دائماً | ❌ لا، يحمّلها `PrintService` بنفسه وقت البناء | المستندات المطبوعة (`FlowDocument`) |
| `Resources/Export/` | ❌ لا — ثابتة دائماً | ❌ لا — ثوابت C# صرفة (`ExportTheme.cs`)، لا XAML | ملفات Excel/CSV/PDF المُصدَّرة |

**5.Design/ (سلسلة كاملة منذ R4)**: أربع طبقات — `Identity/{Pack}/Primitives.{Color,Type,Space,Shape,Motion,
Elevation}.xaml` (L1، قيم حرفية، بالدور لا بالصبغة: `P.Color.Brand.*` لا `P.Color.Blue.*`) → `Semantic/
Semantic.Light.xaml`/`Semantic.Dark.xaml`/`Semantic.Type.xaml` (L2) → `Components/Tokens.*.xaml` (L3) →
`Styles/Style.*.xaml` (L4). `IIdentityService` (`1.Platform/Design/`) / `IdentityService` (`6.UI/Services/`)
يبدّل حزمة الهوية (L1) بأكملها وقت التشغيل — الأبعاد الخمسة (لون/خط/مسافة/شكل/ظل) معاً. **آلية التبديل مُثبتة
تجريبياً لا افتراضاً، وتغيَّرت جذرياً في R4** — راجع ARCHITECTURE.md § R4 ⚠️ توقف 5 للتفصيل الكامل: `ResourceDictionary`
تُجمِّد أي مورد مُركَّب متداخل (مثال `DropShadowEffect`) على أول قيمة يراها للأبد إن حُمِّل قبل استقرار الهوية
الصحيحة — الحل: الهوية أولاً في بناء الشجرة، ثم `Theme.xaml`، لا العكس أبداً. `Theme.xaml` نفسه لم يعد يتضمّن
أي ملف هوية — `IIdentityService.Apply`/`Initialize` هو المسؤول الوحيد عن بناء الشجرة الكاملة بالترتيب الصحيح.

**لماذا الطباعة منطقة منفصلة عن الشاشة (لا `DynamicResource` من Themes إطلاقاً)**: لو قرأ قالب الطباعة ألوانه من
`Resources/Design/Semantic/Semantic.Light.xaml` مباشرة، فسيطبع المستخدم ورقة بخلفية سوداء ونص أبيض بمجرد تفعيل
الوضع الداكن في التطبيق — الورق لا "يتبدّل" مع ثيم الشاشة، فيجب أن يكون مصدر ألوانه ثابتاً ومستقلاً تماماً عن `IdentityService`.

**لماذا التصدير ثوابت C# لا XAML**: Excel (عبر ClosedXML) وPDF (عبر QuestPDF) لا يقرآن `ResourceDictionary` —
يحتاجان قيماً كـ `string`/`int` مباشرة في الكود وقت البناء، فـ `ExportTheme.cs` كلاس ثابت صرف، لا `ResourceDictionary`.

**القاعدة المُلزمة**: المجموعات الدلالية الست (Neutral/Info/Brand/Success/Warning/Danger) مُعرَّفة في **كل** منطقة
بنفس القيم الفعلية (نُسخة الوضع الفاتح من `Semantic.Light.xaml` حرفياً في `PrintTheme.xaml`/`ExportTheme.cs`) —
لا تُخترع قيمة جديدة في أي منطقة، ولا يُضاف لون دلالي سابع لأي سبب.

---

## معمارية الطبقات (المرحلة F، خطوة 0)

قاعدة حاكمة واحدة: **كل منطق يوجد في مكان واحد فقط**، ومن يستدعيه من مكان آخر = خطأ معماري.

### توزيع المسؤوليات

| الطبقة | مسؤوليتها الوحيدة | مسموح | ممنوع |
|---|---|---|---|
| **Repository** (`Database/*Repository.cs`) | SQL خام ↔ Models | Select/Insert/Update/Delete بسيطة، استعلامات مركّبة (Join/Group) ترجع Models | تحقق صلاحية، تحقق صحة، فتح معاملة (Transaction)، Audit، منطق أعمال (حساب مستوى/توليد كود/ربط بين جداول)، استدعاء Repository آخر، استدعاء Service |
| **Service** (`Services/**/*Service.cs`) | كل منطق الأعمال — المكان الوحيد له | تحقق الصلاحية (أول سطر)، تحقق الصحة عبر Validator، إدارة المعاملة (يفتحها ويمررها للـ Repositories)، Audit، القواعد المحاسبية، الربط بين الكيانات، استدعاء Repositories وخدمات أخرى | SQL مباشر (`DbHelper`/`Db.*` مباشرة — يمر بـ Repository دائماً)، أي معرفة بالواجهة (`MessageBox`/`Window`/`Dispatcher`) |
| **Validator** (`Core/Validation/Validators/*.cs`) | صحة البيانات فقط | الحقول المطلوبة/الأنواع/النطاقات/التنسيق، التفرّد (يستدعي Repository **للقراءة فقط**) | قواعد أعمال معقدة (توازن، فترة مالية — في `AccountingRules` أو الخدمة)، أي كتابة لقاعدة البيانات |
| **AccountingRules** (`Core/Validation/AccountingRules.cs`) | قواعد محاسبية بحتة — دوال نقية | تأخذ بيانات وترجع نتيجة، بلا وصول لقاعدة بيانات | استدعاء Repository أو Service من داخلها |
| **ViewModel** | تنسيق بين الخدمة والواجهة فقط | استدعاء خدمة → استقبال `Result` → عرضه، حالة الواجهة (IsLoading، الصفحة الحالية، المحدد) | منطق أعمال، استدعاء Repository مباشرة، SQL |
| **Control / Page** | عرض وتفاعل فقط | — | استدعاء Service أو Repository |

### أمثلة

```
مسموح:   AccountsViewModel → IAccountService → AccountRepository
ممنوع:   AccountsViewModel → AccountRepository        (تجاوز الخدمة)
ممنوع:   AccountRepository → CustomerRepository       (ربط بين جداول من Repository)
ممنوع:   AccountValidator → DbHelper.Execute          (كتابة من Validator)
ممنوع:   AccountingRules.NoNegativeStock → AppSettings.AllowNegativeStock مباشرة
         (كسر نقاء الدالة — القيمة تُمرَّر بارامتر من المستدعي، لا تُقرأ من الداخل)
```

### الدرس الذي أثبت الحاجة لهذه القاعدة

عند بناء `AccountRepository`/`JournalRepository` (المرحلة E، الجزء أ.5) قبل وجود طبقة خدمات، انتهى بهما
منطق أعمال حقيقي: حساب `Level` من الأب، تحديث `IsLeaf` للأب، مزامنة اسم العميل/المورد المرتبط عند
تعديل حساب، حذف العميل/المورد المرتبط عند حذف حساب، حساب رصيد الحساب من مجموع سطور القيود، توليد
رقم القيد (مكرر بالكامل مع `Services/NumberSequenceService` المبني لاحقاً!) — كل ذلك استُخرج ونُقل
(المرحلة F، خطوة 0) بعد أن ثبت أن ترك المنطق في الـ Repository قبل بناء الخدمة يُنتج نسخاً مكرّرة
بمجرد بناء الخدمة الحقيقية. القاعدة الآن: **لا يُبنى Repository بمنطق أعمال حتى لو لم تُبنَ الخدمة بعد** —
يبقى CRUD صرفاً، والمنطق يُبنى لاحقاً في الخدمة مباشرة، لا يُكتب مؤقتاً في مكان آخر ثم يُنقل.

## أين يعيش المنطق (فحص شامل، 2026-08-18)

القاعدة الحاكمة: **لا منطق تشغيل في أي مكان خارج طبقة المنطق**. "منطق تشغيل" = أي قرار، حساب، تحقق، أو
تحويل بيانات — وليس مجرد "كود فيه `if`" (مثال بريء: `if (includeInactive) ... else ...` لبناء جملة SQL
شرطية داخل Repository ليس قراراً عملياً، هو بناء استعلام).

### معايير الحكم (اسأل بهذا الترتيب)

| السؤال | الإجابة → المكان |
|---|---|
| هل هو قرار أعمال (يمنع/يسمح/يحسب نتيجة تؤثر على التدفق)؟ | `Service` |
| هل هو قاعدة محاسبية نقية (بلا DB/إعدادات، تأخذ بيانات وترجع نتيجة)؟ | `Core/Validation/AccountingRules.cs` (أو ملف نقي مشابه بجانبه، مثل `FiscalPeriodCalculator.cs`) |
| هل هو تحقق شكل بيانات (مطلوب/نطاق/تنسيق/تفرّد)؟ | `Validator` |
| هل هو SQL أو تحويل صف قاعدة بيانات ↔ Model؟ | `Repository` |
| هل هو حساب/تحقق عرض بحت لا يمنع الحفظ (إجمالي سطر، تلوين صف، حد أدنى حقل)؟ | القطعة نفسها (`Views/Controls/**`) |
| هل هو تنسيق نصي (رقم→نص، Enum→نص، حالة→StatusVariant)؟ | `DTO` أو `Converter` — **ليس الـ Model أبداً** |

### القاعدة الأهم التي أثبتها الفحص: الـ Model بيانات خام فقط

`Models/*.cs` ممنوع أن يحتوي أي خاصية محسوبة تمثّل قراراً أو تحويلاً — لأنه الطبقة التي **كل** الطبقات
الأخرى (Repository, Service, ViewModel, View) تراه وتقدر تستخدمه، فأي منطق يُوضع فيه يتسرّب فوراً لأي مكان
يستهلك الـ Model مباشرة (كما حدث فعلياً — راجع الأمثلة أدناه).

```
ممنوع:  public bool IsOverCreditLimit => Balance > CreditLimit;   (Models/Customer.cs)
ممنوع:  public string StatusText => IsPosted ? "مرحّل" : "مسودة"; (Models/JournalEntry.cs)
ممنوع:  public string BalanceText => Balance.ToString("N2");      (تنسيق حتى لو بسيط)
مسموح:  public string FullName => $"{First} {Last}";              (تجميع نصي بحت من حقول خام موجودة، بلا قرار)
```

الفرق بين المثالين الأخيرين: `BalanceText` يحوّل *قيمة* لعرض (مسؤولية DTO/Converter)، بينما `FullName`
لا يحوّل شيئاً — يجمّع حقلين نصيين خامّين موجودين أصلاً بلا أي تفسير أو قرار جديد.

### أمثلة حقيقية من الفحص

- **`Customer.IsOverCreditLimit`** (قرار أعمال حقيقي، أوضح مثال خرق): كان خاصية Model، ويستهلكه
  `Views/Controls/Pickers/CustomerPicker.cs` مباشرة لتلوين صف عميل تجاوز حدّه الائتماني. أُزيلت من الـ
  Model؛ بما إن `ICustomerService` غير مبني بعد (F.3)، المقارنة الآن محلية داخل `CustomerPicker.cs` نفسها
  فقط (لا Model عام يسرّبها لأي مستهلك آخر)، بتعليق موثِّق أنها مؤقتة.
- **`JournalEntry.IsBalanced`**: قاعدة محاسبية (`التوازن`) كانت مباشرة على الـ Model — القاعدة الحقيقية
  تعيش في `AccountingRules.BalancedEntry` (تُستدعى من `Validator` والخدمة)، لا خاصية على الكيان نفسه.
- **`FiscalYearSeeder` يكرّر منطق تقسيم الفترات من `FiscalPeriodService.CreateYear`**: مثال على منطق
  تشغيل (معالجة تواريخ) تسرّب لطبقة `Database/` لأن المُستدعي (Seeder عند الإقلاع، قبل تسجيل الدخول) لا
  يقدر يمرّ عبر الخدمة المحكومة بصلاحية. الحل: دالة **نقية** مشتركة (`FiscalPeriodCalculator`) في `Core/`
  يستدعيها الاثنان معاً — لا الخدمة تُستثنى، ولا الحساب يُكرَّر.
- **`PermissionDb.GetEffectivePermissions`**: قرار تفويض كامل (اتحاد صلاحيات الدور مع منح/سحب المستخدم)
  كان داخل Repository — نُقل لـ `PermissionService` (وهي كانت تمرير سطر واحد بلا منطق فعلي، رغم أنها
  "الخدمة" في الاسم).
- **تنسيق نصي داخل قطعة العرض نفسها مقبول**: `PrintTemplates.cs` يحسب `_entry.TotalDebit.ToString("N2")`
  محلياً — هذا صحيح لأن `PrintTemplates` **هو** طبقة العرض/الطباعة لهذا الكيان، لا مصدر بيانات مشترك آخرون
  يعتمدون عليه.
