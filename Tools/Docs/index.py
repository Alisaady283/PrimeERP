"""فهرس الملفات: نسخةٌ بثلاثة مستويات (طبقة/مجلد/ملف) وأخرى بمستويين"""
import re
from pathlib import Path

LAYERS = ["1.Platform", "2.Data", "3.Domain", "4.Application", "5.Design", "6.UI", "7.Composition",
          "8.Modules", "App", "Tools", "PrimeERP.Tests", "PrimeERP.Setup"]
CODE = (".cs", ".xaml", ".sh")

notes = dict(line.split("|", 1) for line in
             Path("Tools/Docs/folders.txt").read_text(encoding="utf-8").splitlines() if "|" in line)

XAML = [("Strings.", "النصوص الظاهرة"), ("Style.", "نمط عنصر WPF"), ("Icons.xaml", "الأيقونات"),
        ("Theme.xaml", "دمج طبقات التصميم"), ("PrintTheme.", "ألوان الطباعة"),
        ("Colors.xaml", "ألوان النظام"), ("Sizes.xaml", "مقاسات النظام")]

SUFFIX = [("Service", "خدمة"), ("Repository", "مستودع"), ("ViewModel", "نموذج عرض"), ("Renderer", "مُصيِّر"),
          ("Registrations", "تسجيل وحدات"), ("Definition", "تعريف"), ("Validator", "تحقق"), ("Dto", "بيانات"),
          ("Tests", "اختبار"), ("Converter", "محوّل"), ("Factory", "مصنع"), ("Rules", "قواعد"),
          ("Seeder", "بذر"), ("Builder", "بنّاء")]

SUMMARY = re.compile(r"///\s*<summary>(.*?)</summary>", re.S)
TYPE = re.compile(r"\b(?:class|interface|record|struct|enum)\s+([A-Za-z_]\w*)")

def trim(text):
    for cut in (". ", " — ", "؛ "):
        text = text.split(cut)[0]
    text = re.sub(r"\s+", " ", text.replace("|", " ")).strip().rstrip(".")
    return text[:80] + "…" if len(text) > 80 else text

def describe(path: Path):
    name = path.stem
    if path.suffix == ".xaml":
        for key, text in XAML:
            if path.name.startswith(key) or path.name == key: return text
        return "واجهة"

    body = path.read_text(encoding="utf-8", errors="ignore")
    best = first = None
    pending = None
    for token in re.finditer(r"///\s*<summary>(.*?)</summary>|"
                             r"\b(?:class|interface|record|struct|enum)\s+([A-Za-z_]\w*)", body, re.S):
        if token.group(1) is not None:
            pending = re.sub(r"<[^>]*>", " ", token.group(1))
        elif pending is not None:
            if token.group(2) == name: best = best or pending
            elif first is None: first = pending
            pending = None

    text = trim(best or first or "")
    if text: return text

    if name.startswith("I") and len(name) > 1 and name[1].isupper():
        stem = name[1:]
        if stem.endswith("Service"): return f"عقد خدمة {stem[:-7]}"
        if stem.endswith("Repository"): return f"عقد مستودع {stem[:-10]}"
        return f"عقد {stem}"
    for suffix, word in SUFFIX:
        if name.endswith(suffix): return f"{word} {name[:-len(suffix)]}"
    return ""

three = ["# FILES.md — فهرس الملفات", "",
         "مولَّد بـ`bash Tools/Docs/generate.sh` — لا يُعدَّل يدوياً. الوظيفة سطرٌ واحد، "
         "وما لا يُقال في سطر مكانه `ARCHITECTURE.md`."]
two = ["# FOLDERS.md — فهرس المجلدات", "",
       "مولَّد بـ`bash Tools/Docs/generate.sh` — لا يُعدَّل يدوياً. نسخةُ `FILES.md` بمستويين: "
       "الطبقة ومجلداتها. تفصيل الملفات هناك."]

rows = folders = 0
for index, layer in enumerate((l for l in LAYERS if Path(l).is_dir()), start=1):
    found = sorted((f for f in Path(layer).rglob("*")
                    if f.suffix in CODE and not {"bin", "obj"} & set(f.parts)),
                   key=lambda f: (str(f.parent).replace("\\", "/"), f.name))
    if not found: continue

    for out, head in ((three, "| # | الملف | الوظيفة |\n|---|---|---|"),
                      (two, "| المجلد | الوظيفة |\n|---|---|")):
        out += ["", f"## {index}. {layer}", "", *head.split("\n")]

    row, seen = 0, None
    for f in found:
        holder = str(f.parent).replace("\\", "/")
        if holder != seen:
            seen = holder
            if holder != layer:                      # جذر الطبقة عنوانها، فلا صفّ له
                note = notes.get(holder) or notes.get(str(Path(holder).parent).replace("\\", "/"), "")
                three.append(f"| | **{holder[len(layer) + 1:]}/** | {note} |")
                two.append(f"| **{holder[len(layer) + 1:]}/** | {note} |")
                folders += 1
        row += 1; rows += 1
        three.append(f"| {index}.{row:02d} | {f.name} | {describe(f)} |")

Path("docs/FILES.md").write_text("\n".join(three) + "\n", encoding="utf-8")
Path("docs/FOLDERS.md").write_text("\n".join(two) + "\n", encoding="utf-8")
print(f"docs/FILES.md: {rows} ملفاً | docs/FOLDERS.md: {folders} مجلداً")
