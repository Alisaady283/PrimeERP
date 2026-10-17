# ANDROID.md — نسخة الأندرويد: ما انتهى وما تبقّى

سجلٌّ حيّ يُحدَّث في مكانه بعد كل خطوة، **ويُقرأ أولاً قبل أي عمل في `frontend-android/`** — فلا يُنسى شيءٌ ولو حُذفت الذاكرة.

**القاعدة**: الأندرويد يحمل كل قطعة في الويب (`frontend-web/src`) بتصميم الويب نفسه على الهاتف. الويب هو المرجع: الألوان والمقاسات من `styles/tokens.css` المشترك، وما تحمله كل صفحة من مواصفتها في `catalog/`. لا يُقال «انتهى» وفي الويب قطعةٌ بلا مقابل.

**وما يقدّمه الأندرويد أفضل أو أسرع يُستعمل بدل محاكاة الويب**: المشاركة من ورقة النظام بدل التنزيل، والكاميرا والمعرض للصورة، والسحب للتحديث، ورجوع الجهاز، والقوائم الأصلية (`FlatList`) بدل الجداول، والرسوم على الـGPU (Skia). الوظيفة واحدة والوسيلة وسيلة الأندرويد.

**السرعة أولاً**: المنطق (الطلبات · القواعد · الصلاحيات · الحساب) يُستورد من الويب ولا يُكتب هنا؛ المكتوب هنا العرض وحده. والحركة على خيط الواجهة الأصلي (Reanimated) لا على JavaScript.

**اقرأ قبل العمل**: `WEB.md` لقرارات الويب التي تُنقل كما هي، و`ARCHITECTURE.md` و`RULES.md`.

## الخطة المعتمدة من المستخدم

1. **التصميم القائم يبقى**: السايدبار والشريط العلوي والبطاقات والقوائم والنموذج — نظير الويب، جذّاب ومقبول. لا يُستبدل بمعيارٍ آخر (أُلغيت تجربة Material 3).
2. **يُصلَح المكسور وحده**، بلا تغيير شكل ما يعمل.
3. **يُكمَل الناقص فوق الموجود**: كل قطعةٍ من الويب تُنقل بقطع الأندرويد القائمة (`components/ui`)، بالترتيب في خطة الإكمال أدناه.
4. **ما الهاتف فيه أقوى يُبنى أصلياً** (البند الثاني من القاعدة).
5. **التحقق**: `tsc` و`expo export` ثم لقطاتٌ من المحاكي لكل صفحة.

## الأساس

- **Expo SDK 57 · React Native 0.86 · Expo Router** (`src/app/`)، والمكتبات كالويب: TanStack Query وzustand.
- **لا نسخ من الويب**: Metro يقرأ من `frontend-web/src` ومن `backend/.../Strings` مباشرةً (`metro.config.js`)، ويحلّ حزمها من `node_modules` الأندرويد وحده — نسخةٌ واحدة من React (مُثبَتٌ بخريطة الحزمة: 15 ملفاً مشتركاً، 0 من حزم الويب).
- **المشترك**: الدليل (`catalog/pages.ts` · `navigation.ts` · `fields.ts` · `actions.ts`) · الأنواع (`features/workspace/spec.ts`) · البيانات (`features/workspace/data.ts`) · الجلسة والواجهة (`features/auth/session.ts` · `interface.ts`) · `lib/api.ts` · `lib/i18n.ts` + `Strings/{ar,en}.json` · `lib/format.ts` · `lib/values.ts` · `lib/motion.ts` · `lib/useDebounced.ts`.
- **ما يختلف**: `@/lib/platform` وحده — الويب `localStorage`/`sessionStorage` وأصلٌ نسبي، والأندرويد `AsyncStorage` وعنوان خادمٍ يُكتب عند الدخول (`src/lib/platform.ts`).
- **الرموز البصرية**: `frontend-web/src/styles/tokens.css` نفسه، يستورده `src/global.css` (Uniwind).
- **ما فُصل في الويب ليصير مشتركاً**: الشاشات الخاصة بأسمائها (`CustomSpec.screen` ← `features/workspace/screens.ts`) · أيقونات الأقسام بأسمائها (`SectionIcon` ← `components/layout/sectionIcons.ts`) · أنواع الرسوم في `spec.ts` · `plainValue`/`dateOnly`/`emptyValue` في `lib/values.ts` · `loadChart` في `features/workspace/charts.ts`.

## البنية — نظيرة الويب (2026-10-14)

الهدف أعلى أداءٍ بواجهةٍ أصلية، والمجلّدات والأسماء نفسها في الويب؛ والربط بالكود المشترك لا بالنسخ.

| الويب `frontend-web/src` | الأندرويد `frontend-android/src` |
|---|---|
| `styles/tokens.css` (`@theme static`) | يستورده `global.css` كما هو — **Uniwind** (Tailwind 4 بالأصناف نفسها، مُترجَمٌ وقت البناء) |
| `components/ui/` Button · Card · Badge · EmptyState · Skeleton · Field · SearchBox · PageHeader · Combobox · StatCard · AnimatedNumber · Chart | الأسماء والخصائص نفسها؛ Combobox ورقةٌ كاملة ببحث، والأيقونات `lucide-react-native` بأسماء الويب |
| `components/layout/AppShell` · `sectionIcons` | الشريط العلوي بلون السايدبار، والسايدبار يُفتح من البداية بمؤشّر الصفحة النشطة |
| `features/auth/LoginPage` | تخطيط الويب على الهاتف + حقل الخادم |
| `features/workspace/` ListPage · Pages · EntityDialog · FieldInput | ListFrame بصفٍّ واحد ومربّعات/تفاصيل للأصناف · PageHost بأنماطه · النموذج شاشةٌ كاملة |
| `motion` (`lib/motion.ts`) | **Reanimated** بالأزمنة نفسها (`Enter` نظير `enterAt`) |
| ECharts | **Victory Native XL** (Skia + Reanimated): الأعمدة والخطوط والمساحات والدائري تُرسم على الـGPU؛ والنص العربي خارج اللوحة لأن Skia لا يشكّله |

مشتركٌ جديد خرج من الويب: `lib/color.ts` (oklch ← hex حساباً بدل حيلة Canvas) · `allowedColumns` في `lib/values.ts` · `fieldRules.ts`.

التحقق: `tsc --noEmit` بلا أخطاء · `expo export --platform android` ناجح · الحزمة تحوي الرموز المشتركة (`--color-brand` · `#0f2442` · `--color-chart-1` · `bg-field`) والخط.

## العودة عن تجربة Material 3 (2026-10-14)

أُعيد بناء الواجهة بمقاييس M3 فضاع تصميم السايدبار والقوائم الذي كان نظير الويب؛ فأُعيد السابق كما هو من نسخ الملفات (لا كتابةً من جديد): AppShell · Button · Field · Combobox · ListPage · EntityDialog · Pages · FieldInput · مسار النموذج · `global.css`، وحُذف `components/m3`. بقي من تلك الجولة ما هو إضافةٌ لا تبديل:

| الإضافة | الموضع |
|---|---|
| إصلاح بطاقات اللوحة (كانت بلا حدّ طولاً) والرقم سطرٌ يصغر ليتّسع | `StatCard` · `AnimatedNumber` |
| رجوع الجهاز يغلق السايدبار، والرسائل تظهر أسفل الشاشة | `AppShell` · `components/ui/Snackbar` |
| **المستندات تُفتح للإضافة والتعديل**: الرأس · السطور (كل سطرٍ في ورقة) · الإجمالي · حفظ وحذف — بقطع النموذج نفسها، والقواعد مشتركة `documentRules.ts` | `DocumentEditor` · `DocumentPage` · مسار النموذج |

**القاعدة من الآن**: ما يعمل لا يُعاد بناؤه؛ يُنقل الناقص فقط، بقطع الأندرويد القائمة، ويُقرأ منطقه من الويب المشترك.

## خطة الإكمال — كل قطعةٍ في موضعها

كل بندٍ: مصدره في الويب ← نظيره هنا. المنطق (الطلبات · القواعد · الصلاحيات) يُستورد من الويب ولا يُكتب؛ المكتوب هنا العرض وحده.

| # | البند | من الويب | على الأندرويد | المنطق المشترك | الحال |
|---|---|---|---|---|---|
| 1 | اللوحة وعدّاداتها ورسومها، والتاريخان والتصدير | DashboardPage · StatCard · Chart | الموجود + `FieldInput compact` للتاريخين · `OutputActions` | `analyticsTable` · `formatKind` · `hasData` · `camel` | منقول |
| 2 | القوائم والنماذج والتقارير، والتصدير والمجاميع | ListPage · EntityDialog · ReportPage · GridTotals | الموجود + `OutputActions` · `GridTotals` | `outputTable` · `reportTotals` · `columnTotal` · `blankValues` | منقول |
| 3 | المستندات: إضافة وتعديل | DocumentEditor · LinesGrid | ورقة سطر (`Sheet`) بدل الجدول | `documentRules` | منقول |
| 4 | المستندات: السحب · الطباعة · رصيد أول المدة · معاملات الفتح · أزرار الصفحة | PullDialog · paper · Pages | `PullDialog` بخطوتين في `Sheet` · `Menu` للمصادر · الطباعة حوار أندرويد | `pull.ts` · `mergePulled` · `openSheet` · `editorFor` · `pullSources` · `pageActions.ts` · `documentPaper` | منقول |
| 5 | التقارير بمعاملات اختيار | ReportPage | `FieldInput` الموجود | — | منقول |
| 6 | الإعدادات | SettingsPage · Tabs | `Tabs` يُمرَّر أفقياً، والحفظ ثابتٌ أسفل الشاشة | `settings/settings.ts` | منقول |
| 7 | الحسابات · التصنيفات | AccountsPage · CategoriesPage · Tree · TreeSplit | تصفّحٌ خطوةً خطوة (`TreeNav`): الطريق · العقدة بملخّصها وأزرارها · فروعها؛ والبحث في الشجرة كلها | `accounts.ts` · `categories.ts` · `lib/tree.ts` | منقول |
| 8 | الصلاحيات (الأدوار · المستخدمون) | PermissionTree · Role/UserPermissionsPage | مجموعاتٌ تُطوى · `Checkbox` · `Segmented` · الحفظ ثابتٌ أسفلها | `permissions.ts` | منقول |
| 9 | الشيكات · قوائم الموارد البشرية | ChequesPage · HrListsPage | `ListFrame` + `Sheet` · `Tabs` + `EntityPage` | `cheques.ts` | منقول |
| 10 | نقطة البيع | PosPage | الأصناف تملأ الشاشة، والسلّة شريطٌ ثابت يفتح ورقتها | `pos/pos.ts` | منقول |
| 11 | صورة الصنف | ImageInput | الكاميرا والمعرض، مقصوصةً مربّعاً ثم 256 وJPEG 0.85 (`expo-image-manipulator`) | `IMAGE_SIZE` · `IMAGE_QUALITY` | منقول |
| 12 | تغيير كلمة المرور | AppShell · FormDialog | قائمة الصورة الرمزية ← `FormDialog` في `Sheet` | `auth/password.ts` | منقول |
| 13 | البنّاء | BuilderExportPage | يبقى «متاح على الويب» — أداة مكتب تكتب في مجلدات | — | مقرّر |

**ما الهاتف فيه أقوى، مبنيٌّ أصلياً**: الطباعة وPDF من حوار الطباعة (`expo-print`) · Excel وCSV إلى ورقة المشاركة (`expo-sharing` · `expo-file-system`) · التاريخ والوقت بمنتقي أندرويد (`@react-native-community/datetimepicker`) · الصورة بالكاميرا · السحب للتحديث · رجوع الجهاز يغلق السايدبار والأوراق · قوائم الاختيار من أسفل الشاشة (`Menu`).

**التسليم لكل عميل في `@/lib/platform`** (لا آلية جديدة): `printHtml` · `saveFile` — الويب iframe وتنزيل، والأندرويد `expo-print` وورقة المشاركة؛ والمحتوى (`paperHtml` · CSV · xlsx) واحدٌ في `lib/output.ts`.

**قطع الأندرويد الجديدة** (`components/ui`): `Sheet` (نظير Dialog، وصار عليه محرّر السطر والسحب) · `Menu` · `Tabs` · `TreeNav` · `Checkbox` · `Segmented` · `GridTotals` · `ImageInput` · `DateField` · `Snackbar` (`info`).

**التحقق**: `tsc` بلا أخطاء في المشروعين · `expo export --platform android` ناجح (Hermes 8.4MB) · خريطة الحزمة: 33 ملفاً مشتركاً من الويب و0 من مكتبات المتصفح (ECharts · motion · react-dom · router).

**يبقى**: المراجعة البصرية على المحاكي لكل صفحة (المستخدم ينزّل Android Studio — SDK في `D:\Android\Sdk`).

## React

الويب على React 19.3.0، والأندرويد على 19.2.3 لأن React Native 0.86 يحمل مُصيِّراً لها بعينها — يُرفع مع إصدار Expo التالي. المشترك hooks عادية تعمل على الاثنين.

## التشغيل

- **الوصول من هاتفٍ حقيقي**: الـAPI في التطوير يستمع على كل الواجهات (`http://*:5080` في `launchSettings.json`)؛ الهاتف على الشبكة نفسها يكتب `http://<عنوان الحاسوب>:5080`، والمحاكي `http://10.0.2.2:5080`.
- **التشغيل**: `npx expo start` ثم Expo Go.

## المستندات والرسوم باحترافية الأندرويد (2026-10-14)

| الطلب | ما صار | الموضع |
|---|---|---|
| سطور الفواتير والقيود كجدول الويب | كل سطرٍ بطاقة: العنوان · «الكمية × السعر» · شرائح الحقول · الصافي أو المدين/الدائن ملوّنَين؛ ولمسها يحرّرها، وسحبها يحذفها مع «تراجع» | `LineCards` · `DocumentEditor` |
| إضافة سطر | ورقةٌ بالحقول والصافي يعدّ حيّاً، و«حفظ وإضافة آخر» لإدخالٍ متتابع؛ وأول سطرٍ بطاقةٌ منقّطة تدعو إليه | `LineSheet` · `AddFirstLine` |
| الملخّص | الإجمالي · الخصم · الضريبة · الخصم من المنبع ← الصافي، أو مجاميع الحقول المُعلَنة `total` وشارة «متزن/الفرق» بـ`balance` | `DocumentSummary` · `documentRules` (`tradeSummary` · `lineTotals` · `imbalance`) |
| الحفظ | شريطٌ ثابت أسفل الشاشة: الصافي يعدّ · «المزيد» (طباعة · حذف) · حفظ | `DocumentEditor` |
| الحركة واللمس | دخول السطور وخروجها وإعادة ترتيبها (Reanimated layout)، واهتزازٌ خفيف عند الإضافة والحذف والخطأ والنجاح (`expo-haptics`) | `LineCards` |
| الرسوم كـECharts | السحب على الأعمدة والخطوط يعرض مؤشّراً وتلميحاً (القيمة · الحصّة · الترتيب · الفرق) · تدرّجٌ من درجتي اللون وتوهّجٌ للخطوط · نموٌّ من الصفر بعد البطاقات · الدائري يدور من الأكبر وتبتعد شريحته لحظةً بالنقر وتُكتب حصّتها في الوسط · المقياس قوسٌ 240° يعدّ · الصفوف الأفقية بتدرّجٍ وحصّةٍ وترتيبٍ باللمس | `Chart` (Skia · Reanimated · victory-native) |

**مشتركٌ جديد**: `lib/chartInsights.ts` (اللون · ترتيب الدائري · محتوى التلميح · `PULL_HOLD`) يستعمله ECharts في الويب وSkia هنا؛ و`total`/`balance` في `spec.ts` إعلانٌ في الدليل (القيود · التوزيعات · الشيكات · المسير)؛ و`lineParts` في `lineCalc.ts` أجزاء السطر، و`lineNet` منها.

## تصفية BI وتصوير الباركود والصلاحيات (2026-10-14)

- **تصفية اللوحة**: لمس عنصرٍ له مفتاح يصفّي اللوحة كلها (الخادم يعيد الحساب)، ولمسه ثانيةً يلغي؛ البطاقة تتبع الإصبع ثم تثبت بزرّ «فتح التفصيل» مُصفّى (`?q=` تبدأ به القائمة)، وتختفي بالإغلاق أو التمرير — `Chart` (`onPick` · `action` · `dismiss`) · `DashboardPage` · `dashboardFilter.ts`.
- **تصوير الباركود**: أيقونة كاميرا في حقل المسح بنقطة البيع، تفتح الكاميرا الخلفية (`expo-camera`) بإطار تصويب وإضاءة، وكل كودٍ جديد يُضاف للسلّة باهتزاز — `BarcodeScanner` · `lib/scan.ts` المشترك (`BARCODE_TYPES` · `scanGate`).
- **شجرة الصلاحيات** من `PermissionCatalog` نفسها في الدسكتوب: كل صفحةٍ باسمها (ومنها صفحات البناء والشيكات والخزائن والسندات) والعرض أولاً والأعمدة «عمود: …».
- **البناء**: `dotnet build` للحلّ بلا أخطاء · `npm run build` للويب · `expo export` للأندرويد.
