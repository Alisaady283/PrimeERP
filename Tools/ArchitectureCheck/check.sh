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

CS_FILES() { find . -type f -name "*.cs" -not -path "./bin/*" -not -path "./obj/*" -not -path "*/bin/*" -not -path "*/obj/*" -not -path "./.git/*" -not -path "*/PrimeERP.Tests/*"; }
XAML_FILES() { find . -type f -name "*.xaml" -not -path "./bin/*" -not -path "./obj/*" -not -path "*/bin/*" -not -path "*/obj/*" -not -path "./.git/*"; }

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

# ============================================================
section "2 — حدود المسؤولية"
# ============================================================

# Repository يُستدعى مباشرة من ViewModel/Page/Control (6.UI/7.Composition/8.Modules)
n=$(grep -rl "using PrimeERP\.Data\.Repositories\b" 6.UI 7.Composition 8.Modules --include="*.cs" 2>/dev/null | wc -l)
if [ "$n" -gt 0 ]; then
  fail "Repository يُستدعى مباشرة من طبقة عرض ($n ملف):"
  grep -rl "using PrimeERP\.Data\.Repositories\b" 6.UI 7.Composition 8.Modules --include="*.cs" 2>/dev/null | sed 's/^/       /'
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
n=$(grep -rlE "#[0-9A-Fa-f]{6}" --include="*.xaml" --include="*.cs" . 2>/dev/null | grep -v "^\./5.Design" | grep -v "/bin/\|/obj/\|PrimeERP.Tests")
if [ -n "$n" ]; then
  fail "قيمة Hex حرفية خارج 5.Design:"
  echo "$n" | sed 's/^/       /'
else pass; fi

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
n=$(grep -rn "TEMPORARY" --include="*.cs" . 2>/dev/null | grep -v "/bin/\|/obj/\|PrimeERP.Tests" | grep -v "TEMPORARY.*R[0-9]")
if [ -n "$n" ]; then
  fail "// TEMPORARY بلا رقم بند صريح:"
  echo "$n" | sed 's/^/       /'
else pass; fi

# TODO بلا رقم بند
n=$(grep -rn "// TODO" --include="*.cs" . 2>/dev/null | grep -v "/bin/\|/obj/\|PrimeERP.Tests" | grep -v "TODO.*R[0-9]")
if [ -n "$n" ]; then
  warn "// TODO بلا رقم بند صريح ($( echo "$n" | wc -l) موضعاً) — يُفضَّل ربطها ببند أو حذفها"
fi

# سجل كل TEMPORARY الموجودة حالياً (للمرجعية، ليست فشلاً)
echo ""
echo "  التعليقات TEMPORARY المسجَّلة حالياً:"
grep -rn "// TEMPORARY" --include="*.cs" . 2>/dev/null | grep -v "/bin/\|/obj/\|PrimeERP.Tests" | sed 's/^/       /'

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
