# REMAINING.md — ما تبقّى (2026-10-17)

يُقرأ أولاً في الجلسة التالية، ويُحذف منه كل بندٍ يُنجز. القواعد في `CLAUDE.md` و`RULES.md`.

## ١) قسم المشاريع لا يظهر إلا صفحةً واحدة — الأولوية

**السبب (مُتحقَّق باستعلام):** جدولا `BuilderSections` و`BuilderModules` في `%LOCALAPPDATA%\PrimeERP\PrimeERP.db` بلا صفٍّ للمشاريع. الصفحات المكتوبة تُبذر في البناء من الدسكتوب وحده (`BuilderModuleLoader.RegisterAll` ← `SeedSections` ثم `SeedModules`)، ومنها تُولَّد صلاحيات الصفحات (`RegisterPagePermissions`). فالويب يرى المفتاح `Projects` وحده لأنه مجالٌ وصفحة معاً.

**الخطوة الأولى:** أغلق الدسكتوب والـAPI، ثم ابنِ الدسكتوب وشغّله مرة. ثم أعد الاستعلام:
```sql
select Key from BuilderSections where Key='Projects';
select Key, PermissionPrefix from BuilderModules where Key like 'Project%';
```
المتوقّع خمس صفحات: Projects · ProjectTasks · ProjectTimesheets · ProjectBills · ProjectProfitReport.

**إن ظهرت الصفوف وبقي الويب ناقصاً:** راجع `PermissionKeys` (الربط `Home`/`PageOfKey`) لمجال `Projects`. وراجع صلاحيات الدور، فالمفاتيح الجديدة `ProjectTasks.*` وأخواتها لا تُورَّث إلا من صفّ البناء.

**الإصلاح الجذري المقترح (يُسأل المستخدم أولاً):** أن يبذر الـAPI الصفحات المكتوبة أيضاً، فلا يتوقّف الويب على تشغيل الدسكتوب. يعني ذلك نقل قائمة `NavigationMap.Coded` إلى مصدرٍ مشترك يقرؤه الاثنان.

## ٢) المشاريع — ما لم يُبنَ بعد

- **ربط المستندات بالمشروع:** `ProjectId` على فاتورة الشراء وسند الصرف (والبيع إن طُلب)، كحقلٍ في الحوار والمحرّر بالويب. ثم تُضاف إلى `ProjectReportService.Profitability` تكلفةً وإيراداً. اليوم التكلفة من الساعات وحدها.
- **فلترة المهام بالمشروع المختار:** `PickerFilterField` في الدسكتوب و`where` في الويب للحقل `taskId`، في الساعات والمستخلصات.
- **لوحة تحليلات للمشاريع:** `ProjectAnalytics` في `AnalyticsService` بأبعاد Project وStatus، على نمط اللوحات القائمة. التصفية بالنقر والفتح بالمزدوج يعملان تلقائياً.
- **شارة لون للحالة:** `StatusVariant` لصفوف المشاريع والمهام (`NameJoin` ثانٍ يعيد tone).

## ٣) الترجمة (طلب المستخدم: «حارس + ترجمة الناقص مرةً»)

- **تم:** `BuilderCatalogService.Relabel` يستبدل العناوين المحفوظة مفاتيحَ `Str.` بنصّها عند الإقلاع.
- **باقٍ:**
  1. **اختبار حارس** `PrimeERP.Tests/Services/TranslationGuardTests.cs`، يشترط:
     - تطابق مفاتيح `ar.json` و`en.json`.
     - ألّا تحوي قيمةٌ في `en.json` حروفاً عربية، إلا `Str.TopBar.Language`.
     - ألّا يبقى في `BuilderModules.Title` و`BuilderColumns.Header` و`BuilderFilters.Label` نصٌّ يبدأ بـ`Str.` بعد البذر.
  2. **الاسم الإنجليزي للحسابات والأصناف وما يجوز** (طلب المستخدم): حقل `NameEn` موجودٌ في Product وUnit وDepartment وJobTitle وPartyBase وغيرها، وغائبٌ عن `Account`.
     - يُضاف `NameEn` إلى الكيان `Account`، ويملؤه `AccountSeeder` للحسابات المبذورة من عمودها الإنجليزي الموجود.
     - يُضاف `Ensure` لقاعدةٍ قائمة يملأ `NameEn` للحسابات المبذورة بكودها.
     - يُعرض بلغة الواجهة في الشجرة والتقارير (دسكتوب · ويب · أندرويد).
     - يُضاف حقل «الاسم الإنجليزي» في حوار الحساب.
     - **قبل البناء:** يُسأل المستخدم أيّ الكيانات بالضبط.

## ٤) ملاحظات بيئة

- **اختبارات قاعدة البيانات في مجلد المشروع** (`D:\…\مشاريع دجانجو`) تتوقّف دقائق عند الإقلاع: SQLite ينتظر في `sqlite3_reset` بلا قفلٍ من العملية نفسها. الكود نفسه نجح في worktree على `C:\…\Temp` خلال 30 ثانية. يُشغَّل الاختبار من worktree:
  ```
  git worktree add <مسار> HEAD
  dotnet test --filter …
  ```
- اختبارات المشاريع (`ProjectTests`، ٥) والتحليلات (٢١) ناجحة.
- **بيانات المصروفات الوهمية:** ١٢ قيداً وصفها «مصروفات تجريبية». نسخة القاعدة قبلها في `%LOCALAPPDATA%\PrimeERP\PrimeERP.before-demo-expenses.db`.
