"""folders.txt مقابل الشجرة الفعلية: شبحٌ مذكورٌ لا وجود له، أو مجلدٌ قائمٌ بلا وصف"""
from pathlib import Path

ROOTS = ["1.Platform", "2.Data", "3.Domain", "4.Application", "5.Design", "6.UI", "7.Composition",
         "8.Modules", "App", "Tools", "PrimeERP.Tests", "PrimeERP.Setup"]
CODE = (".cs", ".xaml", ".sh")

listed = {line.split("|", 1)[0] for line in
          Path("Tools/Docs/folders.txt").read_text(encoding="utf-8").splitlines() if "|" in line}

actual = set()
for root in ROOTS:
    base = Path(root)
    if not base.is_dir(): continue
    actual.add(root)
    actual |= {str(d).replace("\\", "/") for d in base.rglob("*")
               if d.is_dir() and not {"bin", "obj"} & set(d.parts)}

for ghost in sorted(listed - actual):
    print(f"شبحٌ في folders.txt: {ghost}")
for orphan in sorted(actual - listed):
    if any(f.suffix in CODE for f in Path(orphan).glob("*")):
        print(f"مجلدٌ بلا وصف: {orphan}")
