#!/usr/bin/env bash
set -u
export LC_ALL=C.UTF-8
cd "$(dirname "$0")/../.." || exit 1

mkdir -p "${OUT_DIR:-docs}"

PYTHONIOENCODING=utf-8 python Tools/Docs/index.py     # FILES.md و FOLDERS.md
PYTHONIOENCODING=utf-8 python Tools/Docs/modules.py   # System.dot
PYTHONIOENCODING=utf-8 python Tools/Docs/drift.py     # folders.txt مقابل الشجرة
