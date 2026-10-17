from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]

HOME = {"1.Platform": "backend", "2.Data": "backend", "3.Domain": "backend", "4.Application": "backend",
        "5.Design": "frontend-desktop", "6.UI": "frontend-desktop", "7.Composition": "frontend-desktop",
        "8.Modules": "frontend-desktop", "App": "frontend-desktop"}


def place(name):
    head = name.replace("\\", "/").split("/")[0]
    return ROOT / HOME[head] / name if head in HOME else ROOT / name


def named(path):
    parts = Path(path).resolve().relative_to(ROOT).parts
    if len(parts) > 1 and parts[1] in HOME and HOME[parts[1]] == parts[0]:
        parts = parts[1:]
    return "/".join(parts)
