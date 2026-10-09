import re
import sys
from collections import defaultdict
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
LAYERS = ["1.Platform", "2.Data", "3.Domain", "4.Application", "5.Design",
          "6.UI", "7.Composition", "8.Modules", "App", "PrimeERP.Setup"]

TYPE = re.compile(r"^\s*(?:public|internal)\s+(?:static\s+|sealed\s+|abstract\s+|partial\s+)*"
                  r"(class|interface|record|struct|enum)\s+([A-Za-z_]\w*)", re.M)
NS = re.compile(r"^\s*namespace\s+([\w.]+)", re.M)
PROP = re.compile(r"^\s*public\s+[\w<>?\[\],\s]+\s+\w+\s*\{\s*get;")
NOISE = re.compile(r"^[{}\[\]();,]*$|^\}\s*;?$|^\{$|^\)\s*;?$|^else$|^try$")

BY_CONCERN = ("1.Platform", "3.Domain", "7.Composition", "PrimeERP.Setup")
EXEMPT_SUFFIX = {
    "Service": ("Reporting", "PageServices"),
    "Renderer": ("Print",),   
    "ViewModel": ("Components",), 
    "Seeder": ("8.Modules",), 
}
LOCAL_ENUM_LAYERS = ("5.Design", "6.UI", "7.Composition", "PrimeERP.Setup", "1.Platform")
SUFFIX_FOLDER = {"Service": "Services", "Repository": "Repositories", "ViewModel": "ViewModels",
                 "Validator": "Validation", "Renderer": "Renderers", "Definition": "Definitions",
                 "Converter": "Converters", "Seeder": "Seeders"}


def files():
    for layer in LAYERS:
        for f in (ROOT / layer).rglob("*.cs"):
            if not any(p in ("obj", "bin") for p in f.parts):
                yield f


def rel(path):
    return str(path.relative_to(ROOT)).replace("\\", "/")


def read(f):
    text = f.read_text(encoding="utf-8", errors="ignore")
    ns = NS.search(text)
    return TYPE.findall(text), (ns.group(1) if ns else ""), text


def inventory():
    rows = []
    for f in files():
        found, ns, _ = read(f)
        for kind, name in found:
            rows.append(f"{ns}.{name}")
    for row in sorted(rows):
        print(row)


def violations():
    for f in files():
        found, ns, _ = read(f)
        parts = f.relative_to(ROOT).parts
        layer, folders = parts[0], parts[1:-1]

        for kind, name in found:
            base = name[1:] if name.startswith("I") and len(name) > 1 and name[1].isupper() else name

            if kind == "enum" and "Enums" not in folders and layer not in LOCAL_ENUM_LAYERS \
                    and "Results" not in folders and "Contracts" not in folders and "Core" not in folders:
                print(f"enum-outside-Enums|{rel(f)}|{name}")

            for suffix, folder in SUFFIX_FOLDER.items():
                if layer in BY_CONCERN or not base.endswith(suffix) or base.endswith("Base") or folder in folders:
                    continue
                if any(x in folders or x == layer for x in EXEMPT_SUFFIX.get(suffix, ())):
                    continue
                print(f"{suffix.lower()}-outside-{folder}|{rel(f)}|{name}")

        if found and ns:
            expect = list(folders)
            got = ns.split(".")
            if expect and got[-len(expect):] != expect:
                print(f"namespace-vs-folder|{rel(f)}|{ns}")

        if f.stem.endswith("Base") and "Base" not in folders and "Repositories" in folders:
            print(f"base-outside-Base|{rel(f)}|{f.stem}")

        if len(found) == 1 and found[0][1] != f.stem \
                and not f.stem.startswith(found[0][1]) and not found[0][1].startswith(f.stem):
            print(f"name-vs-file|{rel(f)}|{found[0][1]}")


def dupes():
    blocks = defaultdict(set)
    for f in files():
        kept = []
        for raw in f.read_text(encoding="utf-8", errors="ignore").splitlines():
            line = raw.strip()
            if not line or line.startswith(("//", "using ", "namespace", "[")) \
                    or PROP.match(line) or NOISE.match(line):
                continue
            kept.append(line)
        for i in range(len(kept) - 6):
            key = "\n".join(kept[i:i + 6])
            if len(key) > 220:
                blocks[key].add(rel(f))

    pairs = defaultdict(int)
    for key, where in blocks.items():
        if len(where) > 1:
            pairs[tuple(sorted(where))] += 1

    for places, count in sorted(pairs.items(), key=lambda kv: -kv[1]):
        print(f"{count}|{' '.join(places)}")


{"inventory": inventory, "violations": violations, "dupes": dupes}[sys.argv[1]]()
