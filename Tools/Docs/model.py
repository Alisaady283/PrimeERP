"""يقرأ الحقيقة من الكود: جداول SchemaBuilder وكيانات 3.Domain — أساسُ توليد نموذج EF"""
import re
from pathlib import Path

ROOT = Path(".")
CALL = re.compile(r'\.(Id|Text|Int|Decimal|Bool|DateCol|Audit|SoftDelete|Concurrency|Index|ForeignKey)\(([^)]*)\)')


def columns_of(chain):
    """[(اسم, نوع, مطلوب, طول)] بالترتيب، مع فهارس الجدول"""
    cols, indexes = [], []
    for c in CALL.finditer(chain):
        kind, arg = c.group(1), c.group(2).strip()
        named = re.match(r'"(\w+)"', arg)
        name = named.group(1) if named else None
        if kind == "Id":
            cols.append(("Id", "int", True, None))
        elif kind == "Audit":
            cols += [("CreatedAt", "date", False, None), ("CreatedBy", "text", False, 100),
                     ("UpdatedAt", "date", False, None), ("UpdatedBy", "text", False, 100)]
        elif kind == "SoftDelete":
            cols += [("IsDeleted", "bool", True, None), ("DeletedAt", "date", False, None),
                     ("DeletedBy", "text", False, 100)]
        elif kind == "Concurrency":
            cols.append((name or "RowVersion", "long", True, None))
        elif kind == "Index":
            if name: indexes.append((name, "unique: true" in arg))
        elif kind == "ForeignKey":
            pass
        elif kind == "Text":
            length = re.search(r'"\w+",\s*(\d+)', arg)
            cols.append((name, "text", "required: true" in arg, int(length.group(1)) if length else None))
            if "unique: true" in arg: indexes.append((name, True))
        elif kind == "Int":
            cols.append((name, "int", "nullable: false" in arg, None))
        elif kind == "Decimal":
            cols.append((name, "decimal", True, None))
        elif kind == "Bool":
            cols.append((name, "bool", True, None))
        elif kind == "DateCol":
            cols.append((name, "date", "nullable: false" in arg, None))
    return [c for c in cols if c[0]], indexes


def table_names():
    """اسم كل جدول كما يعلنه الصنف: TableName / HeaderTable / LinesTable / base("h","l")"""
    names = {}
    for f in ROOT.glob("2.Data/Repositories/**/*.cs"):
        text = f.read_text(encoding="utf-8")
        for m in re.finditer(r'class\s+(\w+)', text):
            tail = text[m.end():m.end() + 1200]
            slot = names.setdefault(m.group(1), {})
            for attr, value in re.findall(r'string\s+(\w+)\s*=>\s*"(\w+)"', tail):
                slot[attr] = value
            pair = re.search(r'base\("(\w+)",\s*"(\w+)"\)', tail)
            if pair:
                slot["_headerTable"], slot["_lineTable"] = pair.group(1), pair.group(2)
    return names


def tables():
    """الجدول ← (أعمدة، فهارس). المُعامَل يُحلّ باسمه من الوارث"""
    literal, template = {}, []
    for f in list(ROOT.glob("2.Data/**/*.cs")) + list(ROOT.glob("1.Platform/**/*.cs")):
        text = f.read_text(encoding="utf-8")
        for m in re.finditer(r'SchemaBuilder\.Table\((?:"(\w+)"|(\w+))\)(.*?)\.Create\(\)', text, re.S):
            chain = columns_of(m.group(3))
            if m.group(1):
                literal[m.group(1)] = chain
            else:
                template.append((m.group(2), chain))
        for m in re.finditer(r'SchemaBuilder\s+\w+\(string (\w+)\)\s*=>\s*\n?\s*SchemaBuilder\.Table\(\1\)(.*?);', text, re.S):
            template.append((m.group(1), columns_of(m.group(2))))

    names = table_names()
    for placeholder, chain in template:
        for attrs in names.values():
            if placeholder in attrs:
                literal.setdefault(attrs[placeholder], chain)
    return literal


def entities():
    """كل كيان مع خصائصه الموروثة — بعدّ الأقواس، فالصنف الفارغ لا يبتلع تاليه"""
    own, parent = {}, {}
    for f in list(ROOT.glob("3.Domain/**/*.cs")) + list(ROOT.glob("2.Data/Repositories/*.cs")):
        text = f.read_text(encoding="utf-8")
        for m in re.finditer(r'public (?:abstract |sealed )?class (\w+)(?:\s*:\s*([\w<>]+))?', text):
            start = text.find("{", m.end())
            if start < 0:
                continue
            depth, i = 0, start
            while i < len(text):
                if text[i] == "{":
                    depth += 1
                elif text[i] == "}":
                    depth -= 1
                    if depth == 0:
                        break
                i += 1
            own[m.group(1)] = dict((p[1], p[0]) for p in re.findall(
                r'public\s+([\w<>?\[\].,\s]*?[\w>?\]])\s+(\w+)\s*\{\s*get', text[start:i]))
            if m.group(2):
                parent[m.group(1)] = m.group(2)

    def flat(name, seen=()):
        merged = dict(own.get(name, {}))
        base = parent.get(name)
        if base and base not in seen:
            merged.update(flat(base, seen + (name,)))
        return merged
    return {name: flat(name) for name in own}


def links(tabs, ents):
    """الجدول ← الكيان: من المستودع أولاً، ثم بمطابقة الاسم"""
    found = {}
    for f in ROOT.glob("2.Data/Repositories/**/*.cs"):
        text = f.read_text(encoding="utf-8")
        for m in re.finditer(r'class\s+(\w+)\s*:\s*\w+<(\w+)(?:,\s*(\w+))?>', text):
            tail = text[m.end():m.end() + 1200]
            for attr, idx in (("TableName", 2), ("HeaderTable", 2), ("LinesTable", 3)):
                hit = re.search(rf'string\s+{attr}\s*=>\s*"(\w+)"', tail)
                if hit and m.group(idx):
                    found[hit.group(1)] = m.group(idx)

    def guess(tb):
        for cand in (tb[:-1], tb[:-3] + "y" if tb.endswith("ies") else None,
                     tb[:-2] if tb.endswith("es") else None, tb):
            if cand and cand in ents:
                return cand
    for tb in tabs:
        if tb not in found and guess(tb):
            found[tb] = guess(tb)
    return found


if __name__ == "__main__":
    tabs, ents = tables(), entities()
    bond = links(tabs, ents)
    print(f"جداول {len(tabs)} | كيانات {len(ents)} | مربوط {len(bond)}")
    print("بلا كيان:", sorted(set(tabs) - set(bond) - {"__Migrations"}))
