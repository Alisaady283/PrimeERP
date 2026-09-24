import re
import sys
from pathlib import Path

TYPE = re.compile(r"\b(class|interface|record|struct|enum)\s+[A-Za-z_]")
KEEP = ("TEMPORARY", "⚠️")
LIMIT = 60


def label(block):
    text = " ".join(re.sub(r"^\s*///\s?", "", l) for l in block)
    text = re.sub(r"<[^>]*>", " ", text)
    text = re.sub(r"\s+", " ", text).strip()
    for sep in (". ", " — ", "؛ ", ": ", "، "):
        head = text.split(sep)[0]
        if 4 < len(head) < len(text):
            text = head
    return text[:LIMIT].rstrip(" .،—") if len(text) > LIMIT else text.rstrip(" .")


def cs(path):
    lines = path.read_text(encoding="utf-8").splitlines(True)
    out, i, cut = [], 0, 0

    while i < len(lines):
        stripped = lines[i].lstrip()

        if stripped.startswith("///"):
            start = i
            while i < len(lines) and lines[i].lstrip().startswith("///"):
                i += 1
            look = i
            while look < len(lines) and (not lines[look].strip() or lines[look].lstrip().startswith(("[", "//"))):
                look += 1
            indent = lines[start][: len(lines[start]) - len(lines[start].lstrip())]
            if look < len(lines) and TYPE.search(lines[look]):
                text = label(lines[start:i])
                if text:
                    out.append(f"{indent}/// <summary>{text}</summary>\n")
                cut += i - start - 1
            else:
                cut += i - start
            continue

        if stripped.startswith("//"):
            start = i
            while i < len(lines) and lines[i].lstrip().startswith("//") and not lines[i].lstrip().startswith("///"):
                i += 1
            block = "".join(lines[start:i])
            if any(k in block for k in KEEP):
                out.append(lines[start])
                cut += i - start - 1
            else:
                cut += i - start
            continue

        out.append(lines[i])
        i += 1

    if cut:
        path.write_text("".join(out), encoding="utf-8")
    return cut


def xaml(path):
    text = path.read_text(encoding="utf-8")
    cut = len(re.findall(r"<!--", text))

    def keep(m):
        body = re.sub(r"\s+", " ", m.group(1)).strip(" =")
        if any(k in body for k in KEEP):
            return f"<!-- {body[:LIMIT]} -->"
        if "=====" in m.group(0):
            return f"<!-- {body[:40]} -->"
        return ""

    text = re.sub(r"<!--(.*?)-->", keep, text, flags=re.S)
    text = re.sub(r"\n[ \t]*\n[ \t]*\n+", "\n\n", text)
    path.write_text(text, encoding="utf-8")
    return cut


total = 0
for target in sys.argv[1:]:
    root = Path(target)
    files = [root] if root.is_file() else list(root.rglob("*.cs")) + list(root.rglob("*.xaml"))
    for f in files:
        if any(p in ("obj", "bin") for p in f.parts):
            continue
        total += cs(f) if f.suffix == ".cs" else xaml(f)

print(f"حُذف {total} تعليقاً")
