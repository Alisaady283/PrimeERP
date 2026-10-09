import argparse
import subprocess
import sys
from datetime import datetime
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
SKIP = {"err.txt"}


def git(*args, capture=False):
    result = subprocess.run(["git", *args], cwd=ROOT, text=True, encoding="utf-8",
                            capture_output=capture, check=False)
    if result.returncode != 0:
        if capture:
            sys.stderr.write(result.stderr)
        sys.exit(f"فشل: git {' '.join(args)}")
    return result.stdout if capture else ""


def auto_message(changes):
    folders = sorted({line[3:].strip('"').split("/")[0] for line in changes})
    stamp = datetime.now().strftime("%Y-%m-%d %H:%M")
    return f"تحديث {stamp}: {len(changes)} ملفاً في {'، '.join(folders[:6])}{'…' if len(folders) > 6 else ''}"


def main():
    parser = argparse.ArgumentParser(description="رفع التعديلات على GitHub")
    parser.add_argument("message", nargs="?", help="رسالة الحفظ")
    parser.add_argument("--branch", help="الفرع (افتراضيّه الحالي)")
    args = parser.parse_args()

    branch = args.branch or git("branch", "--show-current", capture=True).strip()
    changes = [line for line in git("status", "--porcelain", capture=True).splitlines()
               if line[3:].strip('"') not in SKIP]

    if changes:
        print(f"{len(changes)} ملفاً متغيّراً على {branch}:")
        for line in changes[:30]:
            print("  " + line)
        message = args.message or auto_message(changes)
        print(f"الرسالة: {message}")

        git("add", "-A")
        for name in SKIP:
            subprocess.run(["git", "reset", "-q", name], cwd=ROOT, check=False)
        git("commit", "-m", message)
    else:
        print("لا تعديلات جديدة — يُرفع ما سبق حفظه")

    git("push", "-u", "origin", branch)
    print(f"رُفع الفرع {branch}: {git('log', '--oneline', '-1', capture=True).strip()}")


if __name__ == "__main__":
    sys.stdout.reconfigure(encoding="utf-8")
    main()
