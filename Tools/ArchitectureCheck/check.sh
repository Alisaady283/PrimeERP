#!/usr/bin/env bash
# فحص حدود المعمارية الثمانية-الطبقات — يُشغَّل بعد كل بند (Rn) قبل الاعتبار مكتملاً.
# الاستخدام: bash Tools/ArchitectureCheck/check.sh   (من أي مكان — يحدّد جذر المشروع تلقائياً)
# الخروج: 0 لو صفر FAIL. غير صفري لو وُجد أي FAIL واحد على الأقل (WARN لا يوقف البناء).
set -uo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"
cd "$ROOT"

FAIL=0
WARN=0
PASS=0

fail() { echo "  ❌ FAIL: $1"; FAIL=$((FAIL+1)); }
warn() { echo "  ⚠️  WARN (دين تقني مسجَّل): $1"; WARN=$((WARN+1)); }
pass() { PASS=$((PASS+1)); }
section() { echo ""; echo "═══ $1 ═══"; }

CS_FILES() { find . -type f -name "*.cs" -not -path "./bin/*" -not -path "./obj/*" -not -path "*/bin/*" -not -path "*/obj/*" -not -path "./.git/*" -not -path "*/PrimeERP.Tests/*" -not -path "./.claude/*"; }
XAML_FILES() { find . -type f -name "*.xaml" -not -path "./bin/*" -not -path "./obj/*" -not -path "*/bin/*" -not -path "*/obj/*" -not -path "./.git/*" -not -path "./.claude/*"; }

echo "فحص حدود المعمارية — $(date '+%Y-%m-%d %H:%M')"

# ============================================================
section "1 — حدود الطبقات (اعتماد لأعلى / تخطي طبقتين / دائرية)"
# ============================================================

# Data(2) لا يعتمد على Application(4)/Design(5)/UI(6)/Composition(7)/Modules(8)
n=$(grep -rl "using PrimeERP\.\(Application\|Design\|UI\|Composition\|Modules\)\b" 2.Data --include="*.cs" 2>/dev/null | wc -l)
if [ "$n" -gt 0 ]; then
  fail "2.Data يعتمد على طبقة أعلى ($n ملف):"
  grep -rl "using PrimeERP\.\(Application\|Design\|UI\|Composition\|Modules\)\b" 2.Data --include="*.cs" 2>/dev/null | sed 's/^/       /'
else pass; fi

# Domain(3) يجب أن تكون نقية 100% — أي using PrimeERP.X خارج PrimeERP.Domain نفسها = خرق (الإشارة الذاتية بين
# مجلدات Domain الفرعية — Entities/Contracts/Results/Rules/Enums — مسموحة، هي طبقة واحدة لا طبقات متعددة)
offenders=""
for f in $(grep -rl "using PrimeERP\." 3.Domain --include="*.cs" 2>/dev/null); do
  bad=$(grep "using PrimeERP\." "$f" | grep -v "using PrimeERP\.Domain\.")
  if [ -n "$bad" ]; then offenders="$offenders$f\n"; fi
done
if [ -n "$offenders" ]; then
  fail "3.Domain ليست نقية — تعتمد على طبقة أخرى:"
  echo -e "$offenders" | sed 's/^/       /'
else pass; fi

# Application(4) لا يعتمد على Design(5)/UI(6)/Composition(7)/Modules(8)
n=$(grep -rl "using PrimeERP\.\(Design\|UI\|Composition\|Modules\)\b" 4.Application --include="*.cs" 2>/dev/null | wc -l)
if [ "$n" -gt 0 ]; then
  fail "4.Application يعتمد على طبقة أعلى ($n ملف):"
  grep -rl "using PrimeERP\.\(Design\|UI\|Composition\|Modules\)\b" 4.Application --include="*.cs" 2>/dev/null | sed 's/^/       /'
else pass; fi

# UI(6) لا يعتمد على Data(2) (تخطي طبقتين) ولا Composition(7)/Modules(8) (لأعلى)
n=$(grep -rl "using PrimeERP\.Data\b" 6.UI --include="*.cs" 2>/dev/null | wc -l)
if [ "$n" -gt 0 ]; then
  fail "6.UI يتخطى طبقتين مباشرة إلى 2.Data ($n ملف):"
  grep -rl "using PrimeERP\.Data\b" 6.UI --include="*.cs" 2>/dev/null | sed 's/^/       /'
else pass; fi
n=$(grep -rl "using PrimeERP\.\(Composition\|Modules\)\b" 6.UI --include="*.cs" 2>/dev/null | wc -l)
if [ "$n" -gt 0 ]; then
  fail "6.UI يعتمد على طبقة أعلى ($n ملف):"
  grep -rl "using PrimeERP\.\(Composition\|Modules\)\b" 6.UI --include="*.cs" 2>/dev/null | sed 's/^/       /'
else pass; fi

# Composition(7) لا يعتمد على Modules(8) (لأعلى) — سُجِّلت هنا لأول مرة R8، إذ 7.Composition/8.Modules كانتا
# فارغتين قبل ذلك (لا شيء يُفحص).
n=$(grep -rl "using PrimeERP\.Modules\b" 7.Composition --include="*.cs" 2>/dev/null | wc -l)
if [ "$n" -gt 0 ]; then
  fail "7.Composition يعتمد على طبقة أعلى (8.Modules) ($n ملف):"
  grep -rl "using PrimeERP\.Modules\b" 7.Composition --include="*.cs" 2>/dev/null | sed 's/^/       /'
else pass; fi

# Platform(1): لا يعتمد على شيء عدا 2.Data.Core/2.Data.Schema (أدوات SQL خام + تعريف جداول، لا Repositories
# التي تحمل منطق أعمال) و3.Domain — راجع ARCHITECTURE.md § قيد Platform
n=$(grep -rl "using PrimeERP\.\(Data\.Repositories\|Data\.Providers\|Application\|Design\|UI\|Composition\|Modules\)\b" 1.Platform --include="*.cs" 2>/dev/null | wc -l)
if [ "$n" -gt 0 ]; then
  fail "1.Platform يعتمد على ما هو أبعد من 2.Data.Core/2.Data.Schema/3.Domain ($n ملف):"
  grep -rl "using PrimeERP\.\(Data\.Repositories\|Data\.Providers\|Application\|Design\|UI\|Composition\|Modules\)\b" 1.Platform --include="*.cs" 2>/dev/null | sed 's/^/       /'
else pass; fi

# دائرية بين مجلدين: أي زوج طبقتين يستوردان من بعضهما بالاتجاهين
declare -A DEPS
for d in 1.Platform 2.Data 3.Domain 4.Application 5.Design 6.UI 7.Composition 8.Modules; do
  [ -d "$d" ] || continue
  targets=$(grep -rhoE "using PrimeERP\.[A-Za-z]+" "$d" --include="*.cs" 2>/dev/null | sed 's/using PrimeERP\.//' | sort -u)
  DEPS[$d]="$targets"
done
MAP_1="Platform"; MAP_2="Data"; MAP_3="Domain"; MAP_4="Application"; MAP_5="Design"; MAP_6="UI"; MAP_7="Composition"; MAP_8="Modules"
# استثناء موثَّق: 1.Platform↔2.Data ليس دائرياً فعلياً عند القياس الدقيق (Platform→Data.Core/Schema فقط،
# لا اعتماد عكسي منهما على Platform أبداً — يتحقّق منه فحص "1.Platform يعتمد على..." أعلاه بدقة أكبر).
# استثناء 1.Platform↔3.Domain لنفس السبب (Domain لا تعتمد على شيء، يتحقّق منه فحص نقاء Domain أعلاه).
for a in 1.Platform 2.Data 3.Domain 4.Application 5.Design 6.UI 7.Composition 8.Modules; do
  for b in 1.Platform 2.Data 3.Domain 4.Application 5.Design 6.UI 7.Composition 8.Modules; do
    [ "$a" = "$b" ] && continue
    if [ "$a" = "1.Platform" ] && { [ "$b" = "2.Data" ] || [ "$b" = "3.Domain" ]; }; then continue; fi
    if [ "$b" = "1.Platform" ] && { [ "$a" = "2.Data" ] || [ "$a" = "3.Domain" ]; }; then continue; fi
    bname="${b#*.}"
    aname="${a#*.}"
    if echo "${DEPS[$a]}" | grep -qw "$bname" && echo "${DEPS[$b]}" | grep -qw "$aname"; then
      fail "اعتماد دائري بين $a و $b"
    fi
  done
done

# L3 (5.Design/Components) يشير لـ L1 (P.*) مباشرة — يجب المرور عبر L2 Semantic دائماً (الاستثناء الموثَّق
# P.Font.*/P.Space.*/P.Radius.* من L1 مباشرة خاص بـ L4 فقط، راجع Style.Input.xaml/Style.Button.xaml — لا L3).
n=$(grep -rlE "DynamicResource P\." --include="*.xaml" 5.Design/Components 2>/dev/null)
if [ -n "$n" ]; then
  fail "L3 (5.Design/Components) يشير لـ L1 مباشرة (تخطي L2):"
  echo "$n" | sed 's/^/       /'
else pass; fi

# L4 (5.Design/Styles) يشير للون L1 (P.Color.*) مباشرة — يتجاوز L2 Semantic فيفقد تبدّل الوضع الفاتح/الداكن.
# الاستثناء الموثَّق (P.Font.*/P.Space.*/P.Radius.*... من L1 مباشرة لأسباب تقنية بحتة، راجع ⚠️ توقف 4) لا
# يشمل الألوان أبداً — لا سبب تقني يمنع تمرير اللون عبر L2، فهذا خرق حقيقي دائماً لا استثناء.
n=$(grep -rlE "DynamicResource P\.Color\." --include="*.xaml" 5.Design/Styles 2>/dev/null)
if [ -n "$n" ]; then
  fail "L4 (5.Design/Styles) يشير للون L1 مباشرة (يجب المرور عبر L2 Semantic):"
  echo "$n" | sed 's/^/       /'
else pass; fi

# ============================================================
section "2 — حدود المسؤولية"
# ============================================================

# استدعاء ساكن لمستودع (Repository) في أي مكان — كلها instance class منذ R5، تُحقن عبر المُنشئ.
n=$(grep -rlE "\b(Account|Customer|Journal|FiscalPeriod|NumberSequence|Backup)Repository\.[A-Za-z]" --include="*.cs" 1.Platform 2.Data 3.Domain 4.Application 5.Design 6.UI 7.Composition 8.Modules App PrimeERP.Tests 2>/dev/null | grep -v "2.Data/Repositories/")
if [ -n "$n" ]; then
  fail "استدعاء ساكن لمستودع (يجب حقن الواجهة عبر المُنشئ):"
  echo "$n" | sed 's/^/       /'
else pass; fi

# Repository يُستدعى مباشرة من ViewModel/Page/Control (6.UI/7.Composition/8.Modules)
n=$(grep -rl "using PrimeERP\.Data\.Repositories\b" 6.UI 7.Composition 8.Modules --include="*.cs" 2>/dev/null | wc -l)
if [ "$n" -gt 0 ]; then
  fail "Repository يُستدعى مباشرة من طبقة عرض ($n ملف):"
  grep -rl "using PrimeERP\.Data\.Repositories\b" 6.UI 7.Composition 8.Modules --include="*.cs" 2>/dev/null | sed 's/^/       /'
else pass; fi

# ISettingsService (Application، تحتاج ServiceBase: صلاحية+audit) مستدعاة من طبقة أدنى (Platform/Data) —
# الطبقات الدنيا تستخدم ISettingsProvider (Platform، عملية تقنية بحتة بلا صلاحية) فقط. راجع R6 §
# "مزوّد مقابل خدمة" في ARCHITECTURE.md — اكتُشف هذا القيد فعلياً عبر SettingsService قبل الفصل.
n=$(grep -rnE "ISettingsService" --include="*.cs" 1.Platform 2.Data 2>/dev/null | grep -vE ":\s*(///|//)")
if [ -n "$n" ]; then
  fail "ISettingsService مستدعاة من طبقة أدنى (يجب استخدام ISettingsProvider):"
  echo "$n" | sed 's/^/       /'
else pass; fi

# DbHelper يُستدعى من Service (4.Application) بلا مرور بـ Repository — استثناء BackupService (أوامر DB إدارية
# لا CRUD كيان: BACKUP/RESTORE VERIFYONLY، لا Repository مكافئ لها أصلاً).
n=$(grep -rl "Db\.\(Query\|Execute\|Scalar\|InsertAndGetId\|CreateCommand\)(" 4.Application --include="*.cs" 2>/dev/null | grep -v "Services/Backup/BackupService.cs")
if [ -n "$n" ]; then
  fail "خدمة تستدعي DbHelper مباشرة بدل المرور عبر Repository:"
  echo "$n" | sed 's/^/       /'
else pass; fi
if grep -q "Db\.Execute(" "4.Application/Services/Backup/BackupService.cs" 2>/dev/null; then
  warn "BackupService.cs يستدعي DbHelper مباشرة — استثناء مقبول (أوامر BACKUP/RESTORE إدارية، لا CRUD كيان له Repository)"
fi

# Repository يستدعي Repository آخر (خارج Seeders في 2.Data/Seeders، المُعفاة عمداً — بناء أولي متعدد الجداول)
n=""
for f in 2.Data/Repositories/*.cs; do
  [ -f "$f" ] || continue
  base=$(basename "$f" .cs)
  others=$(grep -oE "\b[A-Za-z]+Repository\." "$f" 2>/dev/null | sed 's/\.$//' | sort -u | grep -v "^${base}$")
  if [ -n "$others" ]; then n="$n$f: $others\n"; fi
done
if [ -n "$n" ]; then
  fail "Repository يستدعي Repository آخر مباشرة:"
  echo -e "$n" | sed 's/^/       /'
else pass; fi

# Service يستدعي Service آخر تنفيذاً لا عقداً (new XService() أو XService.Instance من ملف آخر) — عبر ServiceLocator/DI فقط مقبول
# (فحص إرشادي: يبحث عن ".Instance" لخدمة أخرى غير عبر ServiceLocator داخل ملفات Services)
n=$(grep -rlE "\b[A-Z][A-Za-z]+Service\.Instance\b" 4.Application/Services 6.UI/Services --include="*.cs" 2>/dev/null | xargs -I{} grep -L "class {}" {} 2>/dev/null)
# (فحص تقريبي مُعطَّل عمداً — إيجابيات كاذبة كثيرة مع نمط Instance المُستخدَم حالياً بانتظار R3 الذي يزيله جذرياً)

# Validator يكتب في قاعدة البيانات
n=$(grep -rlE "Db\.(Execute|InsertAndGetId)\(|Repository\.(Insert|Update|Delete)\(" 4.Application/Validation --include="*.cs" 2>/dev/null | wc -l)
if [ "$n" -gt 0 ]; then
  fail "Validator يكتب في قاعدة البيانات ($n ملف):"
  grep -rlE "Db\.(Execute|InsertAndGetId)\(|Repository\.(Insert|Update|Delete)\(" 4.Application/Validation --include="*.cs" 2>/dev/null | sed 's/^/       /'
else pass; fi

# Model (3.Domain/Entities) فيه دالة (غير مُنشئ/خاصية بسيطة)
n=$(grep -rlE "public [A-Za-z<>]+ [A-Za-z]+\([^)]*\)\s*(=>|\{)" 3.Domain/Entities --include="*.cs" 2>/dev/null | grep -v "Common/BaseModel.cs")
if [ -n "$n" ]; then
  fail "Entity يحتوي دالة (منطق/قرار أعمال محتمل):"
  echo "$n" | sed 's/^/       /'
else pass; fi

# UIServices (بوابة الوصول الوحيدة المسموحة للـ code-behind الذي لا يقبل حقن اعتمادية — راجع تعليق الملف نفسه
# وقرار المستخدم الصريح: "يُستهلك من code-behind فقط، صفر استهلاك من Service/VM") — أي استدعاء خارج
# 6.UI/**/*.xaml.cs أو App/**/*.xaml.cs = خرق. الدين التقني نفسه (حجم الاستهلاك) يتقلّص في R8، راجع الجدول.
n=$(grep -rl "UIServices\." --include="*.cs" 1.Platform 2.Data 3.Domain 4.Application 7.Composition 8.Modules 2>/dev/null)
if [ -n "$n" ]; then
  fail "UIServices مُستهلَكة خارج 6.UI/App كلياً (يجب ألا تُستخدم خارج طبقة العرض أصلاً):"
  echo "$n" | sed 's/^/       /'
else pass; fi
n=$(grep -rl "UIServices\." --include="*.cs" 6.UI App 2>/dev/null | grep -vE "\.xaml\.cs$")
if [ -n "$n" ]; then
  fail "UIServices مُستهلَكة من ملف .cs ليس code-behind لـ .xaml (يجب أن يكون الاستهلاك من code-behind فقط):"
  echo "$n" | sed 's/^/       /'
else pass; fi

# TextBox/PasswordBox بقياس حرفي (Height/MinHeight/Padding) داخل قطع Inputs — يجب أن يمر عبر C.Input.* دائماً
# (راجع AppPasswordBox/AppTextBox/AppNumericBox). مقصورة على 6.UI/Components/Inputs تحديداً لا كل 6.UI: محرِّرات
# الخلايا المضغوطة في شبكات المستندات (DocumentLinesGrid) قياس مختلف عمداً (سياق كثافة مختلف كلياً، ليست حقل
# نموذج مستقل) — فرض C.Input.* عليها كان سيغيّر شكلها الفعلي، ممنوع صراحة (راجع تعليمات الترحيل).
n=$(grep -rnE '<(TextBox|PasswordBox)[^>]*\s(Height|MinHeight|Padding)="[0-9]' --include="*.xaml" 6.UI/Components/Inputs 2>/dev/null)
if [ -n "$n" ]; then
  fail "قياس حرفي (Height/MinHeight/Padding) على TextBox/PasswordBox داخل 6.UI/Components/Inputs (يجب C.Input.*):"
  echo "$n" | sed 's/^/       /'
else pass; fi

# ViewModel فيه Brush/Color (منطق عرض بصري يجب أن يكون في القطعة لا الـ ViewModel)
n=$(grep -rlE "\bBrush\b|\bColor\b|SolidColorBrush" 6.UI/ViewModels --include="*.cs" 2>/dev/null)
if [ -n "$n" ]; then
  fail "ViewModel يحتوي إشارة Brush/Color مباشرة:"
  echo "$n" | sed 's/^/       /'
else pass; fi

# ============================================================
section "3 — حدود التصميم"
# ============================================================

# قيمة حرفية Hex خارج 5.Design (الاستثناءات الموثَّقة: ExportTheme.cs, PrintTheme.xaml — ثوابت ورق/تصدير مستقلة عمداً)
n=$(grep -rlE "#[0-9A-Fa-f]{6}" --include="*.xaml" --include="*.cs" . 2>/dev/null | grep -v "^\./5.Design" | grep -v "/bin/\|/obj/\|PrimeERP.Tests\|^\./.claude/")
if [ -n "$n" ]; then
  fail "قيمة Hex حرفية خارج 5.Design:"
  echo "$n" | sed 's/^/       /'
else pass; fi

# قيمة حرفية Hex داخل L2/L3/L4 (Semantic/Components/Styles) — L1 (Identity/Primitives) وحدها مصدر الحقيقة
# الحرفي المسموح به في السلسلة كاملة؛ أي طبقة أعلى تُدخل Hex جديداً تكسر تبدّل الهوية/الوضع لهذه القيمة تحديداً.
n=$(grep -rlE "#[0-9A-Fa-f]{6}" --include="*.xaml" 5.Design/Semantic 5.Design/Components 5.Design/Styles 2>/dev/null)
if [ -n "$n" ]; then
  fail "قيمة Hex حرفية داخل L2/L3/L4 (يجب أن تأتي من L1 عبر DynamicResource):"
  echo "$n" | sed 's/^/       /'
else pass; fi

# لونان بنفس القيمة الست عشرية باسمين مختلفين ضمن نفس ملف L1 (تكرار محتمل غير مقصود — إرشادي، WARN لا FAIL)
for f in 5.Design/Identity/*/Primitives.Color.xaml; do
  [ -f "$f" ] || continue
  dups=$(grep -oE '#[0-9A-Fa-f]{6}' "$f" | sort | uniq -d)
  if [ -n "$dups" ]; then
    warn "$f: قيم Hex مكررة تحت أسماء مختلفة: $(echo $dups | tr '\n' ' ')"
  fi
done

# StaticResource للون (يجب DynamicResource) — فحص لأسماء المفاتيح الدلالية اللونية فقط
n=$(grep -rlE "StaticResource (Brand|Surface|Text[A-Z]|Outline|Success|Danger|Warning|Info|Nav[A-Z]|TopBar|Table[A-Z]|State[A-Z]|FocusRing|Shadow)" --include="*.xaml" 6.UI 5.Design 2>/dev/null)
if [ -n "$n" ]; then
  fail "StaticResource لقيمة لون دلالية (يجب DynamicResource):"
  echo "$n" | sed 's/^/       /'
else pass; fi

# StaticResource لمفتاح P.* أو S.* — ممنوع دائماً بلا استثناء (كل قيم L1/L2 تتبدّل مع الهوية، راجع R4 § 5)
n=$(grep -rlE "StaticResource (P|S)\.[A-Za-z]" --include="*.xaml" 5.Design 6.UI 2>/dev/null)
if [ -n "$n" ]; then
  fail "StaticResource لمفتاح P./S. (يجب DynamicResource — يتبدّل مع حزمة الهوية):"
  echo "$n" | sed 's/^/       /'
else pass; fi

# StaticResource لمفتاح C.* — ممنوع إلا للثوابت البنيوية الموثَّقة صراحة (عرض عمود، لا قيمة تصميم)
C_STATIC_ALLOWLIST="C.Nav.Icon.ColumnWidth|C.Grid.RowHeader.Width"
n=$(grep -rnE "StaticResource C\.[A-Za-z]" --include="*.xaml" 5.Design 6.UI 2>/dev/null | grep -vE "$C_STATIC_ALLOWLIST")
if [ -n "$n" ]; then
  fail "StaticResource لمفتاح C.* خارج القائمة المسموحة (ثوابت بنيوية فقط):"
  echo "$n" | sed 's/^/       /'
else pass; fi

# Color="{DynamicResource X}" حيث X ليس Color حقيقياً — خطأ نوع (Brush لا Color) يُبنى بلا خطأ لكن يتعطّل وقت
# التشغيل فقط (⚠️ توقف 6، ARCHITECTURE.md). المفاتيح الصحيحة: معرَّفة <Color x:Key="X"> صراحة، أو تنتهي بـ
# ".Color" (تُشتق برمجياً في IdentityService.RefreshDerivedColors — لا XAML يعرّفها).
COLOR_DEFS=$(grep -rhoE '<Color x:Key="[^"]+"' --include="*.xaml" 5.Design 2>/dev/null | sed 's/<Color x:Key="//;s/"$//' | sort -u)
n=""
for f in $(grep -rlE 'Color="\{DynamicResource [^}]+\}"' --include="*.xaml" 5.Design 6.UI 2>/dev/null); do
  refs=$(grep -oE 'Color="\{DynamicResource [^}]+\}"' "$f" | sed -E 's/Color="\{DynamicResource ([^}]+)\}"/\1/' | sort -u)
  for ref in $refs; do
    case "$ref" in
      *.Color) continue ;;
    esac
    if ! echo "$COLOR_DEFS" | grep -qxF "$ref"; then
      n="$n$f: Color=\"{DynamicResource $ref}\" — $ref ليس Color حقيقياً\n"
    fi
  done
done
if [ -n "$n" ]; then
  fail "Color= يشير لمفتاح غير Color حقيقي (Brush لا يتحوّل تلقائياً — راجع توقف 6):"
  echo -e "$n" | sed 's/^/       /'
else pass; fi

# ============================================================
section "4 — التكرار"
# ============================================================

# كلاسان بنفس الاسم — فقط تعريفات على مستوى الطبقة الأولى داخل namespace (لا كلاسات متداخلة nested،
# ولا partial لنفس الكلاس عبر .xaml.cs). إرشادي — يحتاج مراجعة يدوية عند وجود نتيجة. يستبعد bin/obj/Tests صراحة
# عبر CS_FILES() (فلترة قبل استخراج النص لا بعده — obj/ يحمل نسخاً مولَّدة تُكرِّر كل تعريف لو فُحصت خطأً).
n=$(for f in $(CS_FILES); do grep -hoE "^    public (partial |static |sealed |abstract )*class [A-Za-z0-9_]+|^    public interface [A-Za-z0-9_]+" "$f" 2>/dev/null; done | grep -oE "[A-Za-z0-9_]+$" | sort | uniq -d)
if [ -n "$n" ]; then
  echo "  ⚠️  أسماء كلاسات/واجهات مكررة على مستوى الطبقة الأولى (تحقّق يدوي):"
  echo "$n" | sed 's/^/       /'
fi

# مفتاح مورد XAML مكرر داخل نفس الملف
for f in $(XAML_FILES); do
  dups=$(grep -oE 'x:Key="[^"]+"' "$f" 2>/dev/null | sort | uniq -d)
  if [ -n "$dups" ]; then
    fail "مفتاح مورد مكرر داخل $f: $dups"
  fi
done
pass

# مفتاح في Semantic.Light بلا مقابل في Semantic.Dark أو العكس
LIGHT_KEYS=$(grep -oE 'x:Key="[^"]+"' "5.Design/Semantic/Semantic.Light.xaml" 2>/dev/null | sort -u)
DARK_KEYS=$(grep -oE 'x:Key="[^"]+"' "5.Design/Semantic/Semantic.Dark.xaml" 2>/dev/null | sort -u)
MISSING_IN_DARK=$(comm -23 <(echo "$LIGHT_KEYS") <(echo "$DARK_KEYS"))
MISSING_IN_LIGHT=$(comm -13 <(echo "$LIGHT_KEYS") <(echo "$DARK_KEYS"))
if [ -n "$MISSING_IN_DARK" ]; then fail "مفاتيح في Semantic.Light بلا مقابل في Dark: $(echo $MISSING_IN_DARK | tr '\n' ' ')"; else pass; fi
if [ -n "$MISSING_IN_LIGHT" ]; then fail "مفاتيح في Semantic.Dark بلا مقابل في Light: $(echo $MISSING_IN_LIGHT | tr '\n' ' ')"; else pass; fi

# نص عربي حر خارج Strings (فحص إرشادي: سلسلة نصية عربية حرفية داخل Result.Fail/MessageBox خارج 5.Design/Strings)
n=$(grep -rlP "\"[\x{0600}-\x{06FF}]" --include="*.cs" 4.Application 6.UI 2>/dev/null | grep -v "Validation" | wc -l)
if [ "$n" -gt 0 ]; then
  warn "$n ملفاً يحتوي نصاً عربياً حرفياً محتملاً خارج Strings (فحص إرشادي، يحتاج مراجعة يدوية — رسائل تحقق داخلية كثيرة منها مقصودة قبل تعريب DTOs في R6)"
fi

# ============================================================
section "5 — المؤقت"
# ============================================================

# TEMPORARY بلا رقم بند
n=$(grep -rn "TEMPORARY" --include="*.cs" . 2>/dev/null | grep -v "/bin/\|/obj/\|PrimeERP.Tests\|^\./.claude/" | grep -v "TEMPORARY.*R[0-9]")
if [ -n "$n" ]; then
  fail "// TEMPORARY بلا رقم بند صريح:"
  echo "$n" | sed 's/^/       /'
else pass; fi

# TODO بلا رقم بند
n=$(grep -rn "// TODO" --include="*.cs" . 2>/dev/null | grep -v "/bin/\|/obj/\|PrimeERP.Tests\|^\./.claude/" | grep -v "TODO.*R[0-9]")
if [ -n "$n" ]; then
  warn "// TODO بلا رقم بند صريح ($( echo "$n" | wc -l) موضعاً) — يُفضَّل ربطها ببند أو حذفها"
fi

# سجل كل TEMPORARY الموجودة حالياً (للمرجعية، ليست فشلاً)
echo ""
echo "  التعليقات TEMPORARY المسجَّلة حالياً:"
grep -rn "// TEMPORARY" --include="*.cs" . 2>/dev/null | grep -v "/bin/\|/obj/\|PrimeERP.Tests\|^\./.claude/" | sed 's/^/       /'

# ============================================================
section "5.4 — الحذف بلا مصدر واحد"
# ============================================================

# الحذف الناعم جملةٌ واحدة لكل الجداول — مصدرها RepositoryBase.SoftDelete. كتابتها في مستودع تعني
# نسخةً سادسة عشرة من نفس السطر، وهو ما كان عليه الحال قبل التوحيد.
n=$(grep -rn "UPDATE .* SET IsDeleted = @" --include="*.cs" 2.Data/Repositories 2>/dev/null     | grep -v "Base/RepositoryBase.cs" | grep -v "DynamicRepository" | wc -l)
if [ "$n" -gt 0 ]; then
  fail "حذف ناعم مكتوب خارج RepositoryBase ($n موضع) — استعمل SoftDelete:"
  grep -rn "UPDATE .* SET IsDeleted = @" --include="*.cs" 2.Data/Repositories 2>/dev/null     | grep -v "Base/RepositoryBase.cs" | grep -v "DynamicRepository" | head -5 | sed 's/^/       /'
else pass; fi

# ============================================================
section "5.5 — منطق أعمال في طبقة التسجيل (8.Modules)"
# ============================================================

# 8.Modules تسجيلٌ إعلاني لا منطق: أي حساب أو تصنيف أو تجميع فيها يعني أن المنطق سكن الطبقة الخطأ.
# اكتُشف فعلياً في تقارير القوائم المالية — ٧٨ سطر حساب داخل دوال Generate، لأن التعريف كان يقبل دالة.
# الاستثناءات: البذور (بيانات تجريبية) والصلاحيات (تُولَّد من الوحدات لا تُحسب).
n=$(grep -rnE '\.(Sum|GroupBy|Average|Aggregate)\(' --include="*.cs" 8.Modules 2>/dev/null     | grep -vE 'DemoDataSeeder|PermissionModuleRegistrations' | wc -l)
if [ "$n" -gt 0 ]; then
  fail "منطق حساب/تجميع في 8.Modules ($n موضع) — مكانه خدمةٌ في 4.Application:"
  grep -rnE '\.(Sum|GroupBy|Average|Aggregate)\(' --include="*.cs" 8.Modules 2>/dev/null     | grep -vE 'DemoDataSeeder|PermissionModuleRegistrations' | head -8 | sed 's/^/       /'
else pass; fi

# تعريفٌ يقبل دالةً يمتلئ كوداً — لا Func في تعريفات التقارير والوحدات عدا ما هو عرضٌ صريح (RowKind).
n=$(grep -rnE 'Generate\s*=' --include="*.cs" 8.Modules 2>/dev/null | wc -l)
if [ "$n" -gt 0 ]; then
  fail "دالة Generate في تسجيل تقرير ($n موضع) — التقرير يُعلَن بخدمته ودالتها لا بكود:"
else pass; fi

# ============================================================
section "6 — قيم بصرية حرفية (6.UI/7.Composition)"
# ============================================================

# FontSize/Height/Width/Padding حرفي (رقم مباشر لا DynamicResource) على عناصر XAML في 6.UI/7.Composition —
# إرشادي (WARN): كثير من الاستخدامات الحالية سابق لهذا الفحص ويحتاج تنظيفاً تراكمياً لا دفعة واحدة، فرضه FAIL
# الآن يكسر كل شيء يعمل فعلاً. الهدف: صفر تراكمياً (راجع ARCHITECTURE.md § الدين التقني).
n=$(grep -rlE '\s(FontSize|Height|Width|Padding|Margin)="[0-9]' --include="*.xaml" 6.UI 7.Composition 2>/dev/null | wc -l)
if [ "$n" -gt 0 ]; then
  warn "$n ملف XAML في 6.UI/7.Composition فيه FontSize/Height/Width/Padding/Margin حرفي (يُفضَّل توكن C.*/P.* — دين تقني تراكمي، راجع 1.5)"
fi

# emoji داخل XAML — القاعدة: Geometry من Icons.xaml فقط، لا رموز تعبيرية كنص
n=$(grep -rlP "[\x{1F300}-\x{1FAFF}\x{2600}-\x{27BF}]" --include="*.xaml" 6.UI 7.Composition 5.Design 2>/dev/null)
if [ -n "$n" ]; then
  fail "emoji داخل XAML (يُمنع — أيقونات Geometry من Icons.xaml فقط):"
  echo "$n" | sed 's/^/       /'
else pass; fi

# ToolTip بقيمة نصية حرفية ثابتة (لا Binding/DynamicResource) — مرشّح قوي لتكرار نص ظاهر أصلاً بجوار العنصر
n=$(grep -rnE 'ToolTip="[^{][^"]*"' --include="*.xaml" 6.UI 7.Composition 2>/dev/null)
if [ -n "$n" ]; then
  warn "ToolTip بنص حرفي ثابت (تحقّق يدوياً أنه لا يكرر نصاً ظاهراً أصلاً):"
  echo "$n" | sed 's/^/       /'
fi

# ============================================================
section "الخلاصة"
# ============================================================
echo "  ناجح: $PASS  |  فشل: $FAIL  |  دين تقني (WARN): $WARN"
if [ "$FAIL" -gt 0 ]; then
  echo ""
  echo "  ❌ يوجد $FAIL خرقاً يجب إصلاحه قبل اعتبار البند مكتملاً."
  exit 1
else
  echo ""
  echo "  ✅ صفر خرق حاسم. $WARN دين تقني مسجَّل وموثَّق (راجع ARCHITECTURE.md § الدين التقني)."
  exit 0
fi
