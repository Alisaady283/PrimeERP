"""يحوّل كل .dot في docs إلى PNG عالي الدقة و SVG متجّه"""
import subprocess
import sys
from pathlib import Path

DPI = sys.argv[1] if len(sys.argv) > 1 else "160"
OUT = Path("docs/Images")
OUT.mkdir(parents=True, exist_ok=True)

for dot in sorted(Path("docs").glob("*.dot")):
    for kind in ("png", "svg"):
        target = OUT / f"{dot.stem}.{kind}"
        done = subprocess.run(["dot", "-Kneato", f"-T{kind}", f"-Gdpi={DPI}", str(dot), "-o", str(target)],
                              capture_output=True, text=True, encoding="utf-8", errors="ignore")
        if done.returncode:
            print(f"تعذّر {target}: {done.stderr.strip()[:120]}")
        else:
            print(f"{target}: {target.stat().st_size // 1024} كيلوبايت")
