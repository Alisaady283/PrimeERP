#!/usr/bin/env bash
# يولّد 5.Design/DESIGN_TOKENS.md من ملفات XAML الفعلية — لا يُعدَّل الملف الناتج يدوياً أبداً، يُعاد توليده
# فقط. الاستخدام: bash Tools/DesignTokens/generate.sh (من أي مكان — يحدّد جذر المشروع تلقائياً).
#
# ملاحظة أداء: أول تنفيذ استخدم grep -rl واحداً لكل رمز (مئات الرموز × مسح شجرة كاملة لكل واحد) — بطيء جداً
# على Windows/MSYS (عبء استدعاء عملية جديدة لكل grep). الحل: مسح واحد فقط لكل الإشارات (Dynamic|Static)Resource
# في الشجرة كاملة، ثم بناء جدول بحث في الذاكرة (bash associative array) — لا عملية فرعية جديدة لكل رمز.
set -uo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"
cd "$ROOT"

OUT="5.Design/DESIGN_TOKENS.md"
TMP="$(mktemp)"
SEARCH_DIRS="5.Design 6.UI App"

echo "يمسح إشارات الرموز عبر $SEARCH_DIRS ..." >&2

declare -A USED_COUNT
declare -A USED_HAS_SELF_ONLY

REFS_FILE="$(mktemp)"
grep -rnoE '(Dynamic|Static)Resource[[:space:]]+[A-Za-z0-9_.]+' $SEARCH_DIRS --include="*.xaml" --include="*.cs" 2>/dev/null \
    | sed -E 's/^([^:]+):[0-9]+:(Dynamic|Static)Resource[[:space:]]+/\1\t/' \
    | sort -u > "$REFS_FILE"

declare -A KEY_FILES
while IFS=$'\t' read -r file key; do
    [ -z "$key" ] && continue
    KEY_FILES["$key"]+="$file"$'\n'
    USED_COUNT["$key"]=$(( ${USED_COUNT["$key"]:-0} + 1 ))
done < "$REFS_FILE"
rm -f "$REFS_FILE"

echo "تم بناء جدول البحث (${#USED_COUNT[@]} رمزاً مُشاراً إليه على الأقل مرة). يستخرج الجداول الآن..." >&2

used_in_excluding_self() {
    local key="$1" self="$2"
    local total="${USED_COUNT[$key]:-0}"
    if [ "$total" -eq 0 ]; then echo 0; return; fi
    if [[ "${KEY_FILES[$key]:-}" == *"$self"$'\n'* ]]; then
        echo $(( total - 1 ))
    else
        echo "$total"
    fi
}

extract_table() {
    local file="$1"

    grep -n 'x:Key="' "$file" | while IFS=: read -r lineno content; do
        [[ "$content" =~ x:Key=\"([^\"]+)\" ]] || continue
        key="${BASH_REMATCH[1]}"

        value=$(echo "$content" \
            | sed -E 's/^\s*<[A-Za-z0-9_:]+\s+//' \
            | sed -E "s/x:Key=\"[^\"]+\"\s*//" \
            | sed -E 's/^\s*>\s*//' \
            | sed -E "s/<\/[A-Za-z0-9_:]+>\s*\$//" \
            | sed -E 's/\s*\/>\s*$//' \
            | sed -E 's/^\s+|\s+$//g' \
            | sed -E 's/\|/\\|/g')
        [ -z "$value" ] && value="(بلا محتوى مباشر)"

        pointsTo="—"
        if [[ "$content" =~ \{(Dynamic|Static)Resource[[:space:]]+([A-Za-z0-9_.]+)\} ]]; then
            pointsTo="${BASH_REMATCH[2]}"
        fi

        usedIn=$(used_in_excluding_self "$key" "$file")

        echo "$key|$value|$pointsTo|$usedIn"
    done
}

{
    echo "# رموز التصميم (DESIGN_TOKENS.md)"
    echo ""
    echo "**مولَّد آلياً بواسطة \`Tools/DesignTokens/generate.sh\` — لا تُعدِّله يدوياً، أعد توليده بعد أي تغيير في 5.Design.**"
    echo ""
    echo "تاريخ التوليد: $(date '+%Y-%m-%d %H:%M')"
    echo ""

    echo "## L1 — Identity/Default"
    echo ""
    echo "| المفتاح | القيمة | يشير إلى | يُستخدم في (عدد الملفات) |"
    echo "|---|---|---|---|"
    for f in 5.Design/Identity/Default/*.xaml; do
        extract_table "$f" | while IFS='|' read -r key value pointsTo usedIn; do
            echo "| $key | \`$value\` | $pointsTo | $usedIn |"
        done
    done
    echo ""

    echo "## L1 — Identity/Corporate"
    echo ""
    echo "| المفتاح | القيمة | يشير إلى | يُستخدم في (عدد الملفات) |"
    echo "|---|---|---|---|"
    for f in 5.Design/Identity/Corporate/*.xaml; do
        extract_table "$f" | while IFS='|' read -r key value pointsTo usedIn; do
            echo "| $key | \`$value\` | $pointsTo | $usedIn |"
        done
    done
    echo ""

    echo "## L2 — Semantic"
    echo ""
    for f in 5.Design/Semantic/*.xaml; do
        echo "### $(basename "$f")"
        echo ""
        echo "| المفتاح | القيمة | يشير إلى | يُستخدم في (عدد الملفات) |"
        echo "|---|---|---|---|"
        extract_table "$f" | while IFS='|' read -r key value pointsTo usedIn; do
            echo "| $key | \`$value\` | $pointsTo | $usedIn |"
        done
        echo ""
    done

    echo "## L3 — Components"
    echo ""
    for f in 5.Design/Components/*.xaml; do
        echo "### $(basename "$f")"
        echo ""
        echo "| المفتاح | القيمة | يشير إلى | يُستخدم في (عدد الملفات) |"
        echo "|---|---|---|---|"
        extract_table "$f" | while IFS='|' read -r key value pointsTo usedIn; do
            echo "| $key | \`$value\` | $pointsTo | $usedIn |"
        done
        echo ""
    done

    echo "## L4 — Styles (فهرس فقط — Style بأكمله لا قيمة سطرية واحدة)"
    echo ""
    echo "| المفتاح | TargetType | الملف |"
    echo "|---|---|---|"
    for f in 5.Design/Styles/*.xaml; do
        grep -nE '<Style[[:space:]]' "$f" | while IFS=: read -r lineno content; do
            key="(بلا x:Key — ضمني)"
            if [[ "$content" =~ x:Key=\"([^\"]+)\" ]]; then key="${BASH_REMATCH[1]}"; fi
            target="—"
            if [[ "$content" =~ x:Type[[:space:]]+([A-Za-z0-9_:.]+)\} ]]; then
                target="${BASH_REMATCH[1]}"
            elif [[ "$content" =~ TargetType=\"([A-Za-z0-9_:.]+)\" ]]; then
                target="${BASH_REMATCH[1]}"
            fi
            echo "| $key | $target | $(basename "$f") |"
        done
    done
    echo ""

    echo "## الرموز غير المستخدمة (صفر مستهلك عبر DynamicResource/StaticResource خارج ملف تعريفها)"
    echo ""
    echo "| المفتاح | الملف |"
    echo "|---|---|"
    for dir in 5.Design/Identity/Default 5.Design/Identity/Corporate 5.Design/Semantic 5.Design/Components; do
        for f in "$dir"/*.xaml; do
            [ -f "$f" ] || continue
            extract_table "$f" | while IFS='|' read -r key value pointsTo usedIn; do
                if [ "$usedIn" -eq 0 ]; then
                    echo "| $key | $f |"
                fi
            done
        done
    done
    echo ""

    echo "## مفاتيح Semantic.Light بلا مقابل في Semantic.Dark (أو العكس)"
    echo ""
    LIGHT_KEYS=$(grep -oE 'x:Key="[^"]+"' "5.Design/Semantic/Semantic.Light.xaml" 2>/dev/null | sed 's/x:Key="//;s/"$//' | sort -u)
    DARK_KEYS=$(grep -oE 'x:Key="[^"]+"' "5.Design/Semantic/Semantic.Dark.xaml" 2>/dev/null | sed 's/x:Key="//;s/"$//' | sort -u)
    MISSING_IN_DARK=$(comm -23 <(echo "$LIGHT_KEYS") <(echo "$DARK_KEYS"))
    MISSING_IN_LIGHT=$(comm -13 <(echo "$LIGHT_KEYS") <(echo "$DARK_KEYS"))
    if [ -z "$MISSING_IN_DARK" ] && [ -z "$MISSING_IN_LIGHT" ]; then
        echo "لا نقص — كل مفتاح في Light له مقابل في Dark والعكس."
    else
        [ -n "$MISSING_IN_DARK" ] && { echo ""; echo "**بلا مقابل في Dark:**"; echo "$MISSING_IN_DARK" | sed 's/^/- /'; }
        [ -n "$MISSING_IN_LIGHT" ] && { echo ""; echo "**بلا مقابل في Light:**"; echo "$MISSING_IN_LIGHT" | sed 's/^/- /'; }
    fi
    echo ""

} > "$TMP"

mv "$TMP" "$OUT"
echo "تم توليد $OUT" >&2
