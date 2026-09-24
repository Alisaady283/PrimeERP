"""خريطة النظام: طبقةٌ في كل بيضة، ودوائر مجلداتها حول جذرها"""
import math
import re
from collections import defaultdict
from pathlib import Path

NL = chr(10)
ROOT = Path(".").resolve()

RING = ["8.Modules", "7.Composition", "6.UI", "5.Design", "4.Application", "3.Domain", "2.Data", "1.Platform"]
CORE = ["App", "PrimeERP.Tests", "PrimeERP.Setup"]          # الإقلاع والاختبارات والمنصِّب
ALL = RING + CORE

FILL = {"1.Platform": "#aecbe6", "2.Data": "#b8dfb9", "3.Domain": "#e7d7ad", "4.Application": "#eab8b8",
        "5.Design": "#f0c9a0", "6.UI": "#d3bde6", "7.Composition": "#a9e0d9", "8.Modules": "#e8d2ad",
        "App": "#d5d9de", "PrimeERP.Tests": "#cfd8c8", "PrimeERP.Setup": "#dcd0c0"}

NS = re.compile(r"^\s*namespace\s+([\w.]+)", re.M)
USING = re.compile(r"^using\s+(PrimeERP\.[\w.]+)\s*;", re.M)
KEY = re.compile(r'x:Key="([^"]+)"')
USE = re.compile(r'\{(?:Dynamic|Static)Resource\s+([A-Za-z0-9._]+)')
SOURCE = re.compile(r'<ResourceDictionary\s+Source="([^"]+)"')
RESOURCE = re.compile(r'(?:SetResourceReference|TryFindResource|FindResource|Resources\[)')
PACK = re.compile(r'component/([\w./]+\.xaml)')
CLR = re.compile(r'clr-namespace:(PrimeERP\.[\w.]+)')

notes = dict(line.split("|", 1) for line in
             Path("Tools/Docs/folders.txt").read_text(encoding="utf-8").splitlines() if "|" in line)

def top(path):
    """المجلد الأول تحت الطبقة — ما دونه تفصيلٌ لا دائرة له"""
    parts = path.split("/")
    return "/".join(parts[:2]) if len(parts) > 1 else path

# ── قراءة الكود: C# بـ using، وXAML بمفاتيح الموارد ──
files, owner, keys = [], {}, {}
for layer in ALL:
    for f in (ROOT / layer).rglob("*"):
        if f.suffix not in (".cs", ".xaml") or {"obj", "bin"} & set(f.parts): continue
        folder = str(f.parent.relative_to(ROOT)).replace("\\", "/")
        text = f.read_text(encoding="utf-8", errors="ignore")
        files.append((folder, f, text))
        if f.suffix == ".cs":
            m = NS.search(text)
            if m: owner.setdefault(m.group(1), folder)
        else:
            for k in KEY.findall(text): keys.setdefault(k, folder)

edges = defaultdict(int)
def link(src, dst):
    if dst and top(dst) != top(src): edges[(top(src), top(dst))] += 1

for folder, f, text in files:
    if f.suffix == ".cs":
        for used in USING.findall(text):
            target, probe = None, used
            while target is None and "." in probe:
                target = owner.get(probe); probe = probe.rsplit(".", 1)[0]
            link(folder, target)
        for line in text.splitlines():                  # مفتاح موردٍ يُنادى بالاسم
            if RESOURCE.search(line):
                for word in re.findall(r'"([A-Za-z][A-Za-z0-9._]*)"', line):
                    link(folder, keys.get(word))
        for path in PACK.findall(text):                 # قاموسٌ يُحمَّل بمساره
            hit = ROOT / path
            if hit.exists(): link(folder, str(hit.parent.relative_to(ROOT)).replace("\\", "/"))
    else:
        for used in USE.findall(text):
            link(folder, keys.get(used))
        for source in SOURCE.findall(text):             # قاموسٌ يدمج قاموساً
            merged = (f.parent / source).resolve()
            if merged.exists():
                link(folder, str(merged.parent.relative_to(ROOT)).replace("\\", "/"))
        for space in CLR.findall(text):                 # قطعةٌ تُستدعى بنطاقها
            link(folder, owner.get(space))

def breach(a, b):
    a, b = a.split("/")[0], b.split("/")[0]
    if a == b or b in ("3.Domain", "5.Design") or a in CORE or b in CORE: return ""   # المصبّان، ومن في القلب
    if a in ("3.Domain", "5.Design"): return "red"
    if a == "1.Platform" and b == "2.Data": return "deviation"
    return "red" if int(b[0]) > int(a[0]) else ""

# ── الهندسة: شبكةٌ من خلايا متساوية، وفي كل خلية بيضةٌ تملؤها ──
NEAR = 0.16           # فجوةٌ مضمونة بين دائرتين داخل البيضة
PAD = 0.26            # فراغ بين آخر دائرة وحافة البيضة
LIFT = 0.46           # مكان اسم الطبقة فوق بيضتها

def diameter(label, scale):
    return (max(0.74, 0.094 * len(label) + 0.26)) * scale

def folders_of(layer):
    paths = sorted({top(p) for p in notes
                    if (p == layer or p.startswith(layer + "/")) and (ROOT / p).is_dir()})
    paths = [p for p in paths if p != layer                      # جذرٌ بلا ملفات لا دائرة له
             or any(f.suffix in (".cs", ".xaml") for f in (ROOT / p).glob("*"))]
    label = {p: (p[len(layer) + 1:] if p != layer else "الجذر") for p in paths}
    return sorted(((p, label[p]) for p in paths), key=lambda r: (r[1] != "الجذر", r[1]))

def pack(items):
    """الجذر في المركز، والباقي حلقاتٍ حوله بفجوةٍ مضمونة — يعيد المواضع ونصف القطر"""
    spots = {items[0][0]: (0.0, 0.0)}
    reach = items[0][2] / 2
    rest, i = items[1:], 0
    while i < len(rest):
        widest = max(d for *_, d in rest[i:])
        radius = reach + NEAR + widest / 2
        room = max(1, int(math.pi / math.asin(min(0.999, (widest / 2 + NEAR / 2) / radius))))
        band = rest[i:i + room]
        for j, (key, _, _) in enumerate(band):
            angle = math.pi / 2 - j * 2 * math.pi / len(band)
            spots[key] = (radius * math.cos(angle), radius * math.sin(angle))
        reach = radius + widest / 2
        i += room
    return spots, reach

WIDE = 1.76

def shape(n):
    """ثلاثة صفوف: الأعلى والأسفل متساويان، والأوسط أقلّ — ٨ ⇦ ٣،٢،٣"""
    side = max(1, round(n * 0.375))
    return [side, n - 2 * side, side]

def arrange(groups, middle, cell_rx, cell_ry):
    """صفوفٌ من أعلى لأسفل، وداخل الصفّ من اليمين لليسار — ترتيب القراءة لا أقصر خط"""
    rows, place, taken = shape(len(groups)), {}, list(groups)

    index = 0
    for r, count in enumerate(rows):
        y = -(r - (len(rows) - 1) / 2) * (2 * cell_ry + LIFT)
        for c in range(count):
            if index >= len(taken): break
            x = ((count - 1) / 2 - c) * 2 * cell_rx          # يمينٌ أولاً
            place[taken[index]] = (x, y)
            index += 1

    return place

def layout(groups, scale, middle):
    packed = {}
    for layer in groups:
        items = [(p, l, diameter(l, scale)) for p, l in folders_of(layer)]
        spots, reach = pack(items)
        packed[layer] = (items, spots, reach)

    cell_ry = max(reach for _, _, reach in packed.values()) + PAD
    rows = shape(len(groups))                                   # عرض الخلية يضبط نسبة اللوحة
    cell_rx = WIDE * len(rows) * (2 * cell_ry + LIFT) / (2 * max(rows))
    center = arrange(groups, list(middle), cell_rx, cell_ry)

    inner, size = {}, {}
    for layer in groups:
        size[layer] = (cell_rx - PAD, cell_ry - PAD)
        items, spots, _ = packed[layer]
        wide = (cell_rx - PAD) / (cell_ry - PAD)                # البيضة أوسع، فتنفرد الدوائر أفقياً
        reach_y = max(abs(spots[k][1]) + d / 2 for k, _, d in items)
        reach_x = max(abs(spots[k][0]) * wide + d / 2 for k, _, d in items)
        grow = min(2.6, (cell_ry - PAD) / reach_y, (cell_rx - PAD) / reach_x)
        inner[layer] = [(key, label, d * grow,
                         spots[key][0] * grow * wide, spots[key][1] * grow)
                        for key, label, d in items]
    return inner, size, center

# ── الكتابة ──
COMPASS = ["e", "ne", "n", "nw", "w", "sw", "s", "se"]

def port(here, there):
    """أقرب جهةٍ من الدائرة نحو هدفها، فيخرج السهم من أقصر طريق"""
    angle = math.atan2(there[1] - here[1], there[0] - here[0])
    return COMPASS[round(angle / (math.pi / 4)) % 8]

def paint(kind, n):
    if kind == "red": return 'color="#d62828" penwidth=3.5'
    if kind == "deviation": return 'color="#e08c1a" style=dashed penwidth=2.5'
    tone = "#46525e" if n >= 40 else "#6f7d8b" if n >= 15 else "#9aa5b1"
    return f'color="{tone}" penwidth={3.5 if n >= 40 else 2 if n >= 15 else 1.1}'

def draw(target, groups, title, joint, scale=1.0, middle=()):
    """joint: 'layer' يصل البيضات، 'folder' يصل الدوائر الداخلية"""
    inner, size, center = layout(groups, scale, middle)
    spot = {}
    out = ["// مولَّد بـ Tools/Docs/modules.py — لا يُعدَّل يدوياً",
           f"// يُرسم: python Tools/Docs/render.py  أو  dot -Kneato -Tsvg {target}",
           'digraph PrimeERP {',
           '  layout=neato; overlap=true; splines=curved; outputorder=edgesfirst;',
           f'  graph [fontname="Segoe UI" fontsize={30 * scale:.0f} labelloc=t label="{title}"];',
           '  node [fontname="Segoe UI" fixedsize=true style=filled];',
           f'  edge [fontname="Segoe UI" fontsize={11 * scale:.0f}];']

    for layer in groups:                                    # الإطار أولاً فيبقى تحت الخطوط
        x, y = center[layer]
        rx, ry = size[layer]
        spot[f"@{layer}"] = (x, y)
        out.append(f'  "@{layer}" [pos="{x:.2f},{y:.2f}!" shape=ellipse width={rx * 2:.2f} '
                   f'height={ry * 2:.2f} style="" color="{FILL[layer]}" penwidth=3 label=""];')

    for layer in groups:                                    # الاسم فوق البيضة
        x, y = center[layer]
        out.append(f'  "#{layer}" [pos="{x:.2f},{y + size[layer][1] + LIFT / 2:.2f}!" '
                   f'shape=plaintext style="" fixedsize=false fontsize={17 * scale:.0f} '
                   f'label="{layer} — {notes.get(layer, "")}"];')

    for layer in groups:                                    # ثم دوائر المجلدات
        cx, cy = center[layer]
        for path, label, d, dx, dy in inner[layer]:
            spot[path] = (cx + dx, cy + dy)
            out.append(f'  "{path}" [pos="{cx + dx:.2f},{cy + dy:.2f}!" shape=circle width={d:.2f} '
                       f'height={d:.2f} fillcolor="{FILL[layer]}" color="#6f7b88" '
                       f'fontsize={13 * scale:.0f} label="{label}"];')

    live = {p for layer in groups for p, *_ in inner[layer]}
    drawn = defaultdict(int)
    for (a, b), n in edges.items():
        if a not in live or b not in live: continue
        drawn[(a.split("/")[0], b.split("/")[0]) if joint == "layer" else (a, b)] += n

    for (a, b), n in sorted(drawn.items(), key=lambda kv: -kv[1]):
        if a == b: continue
        head, tail = (f"@{a}", f"@{b}") if joint == "layer" else (a, b)
        gate = f' tailport={port(spot[head], spot[tail])} headport={port(spot[tail], spot[head])}'
        tip = f' label="{n}"' if joint == "layer" else ""
        out.append(f'  "{head}" -> "{tail}" [{paint(breach(a, b), n)}{gate}{tip} arrowsize=0.8];')

    out.append("}")
    Path(target).write_text(NL.join(out) + NL, encoding="utf-8")
    reds = sum(1 for e in drawn if breach(*e) == "red")
    print(f"{target}: {len(groups)} بيضة | {len(live)} مجلداً | {len(drawn)} خطاً | {reds} خرقاً")

draw("docs/System.dot", ALL, "PrimeERP — الطبقات ومجلداتها", "layer", middle=["8.Modules"])
draw("docs/System.Folders.dot", ALL, "PrimeERP — الربط بين المجلدات", "folder", middle=["8.Modules"])
draw("docs/System.App.dot", RING, "PrimeERP — البرنامج وحده", "folder", scale=1.35, middle=["8.Modules"])
