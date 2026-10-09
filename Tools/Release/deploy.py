import argparse
import json
import os
import re
import shutil
import sqlite3
import subprocess
import sys
import urllib.request
import zipfile
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
HOST = "ubuntu@primelogic-eg.com"
SERVER_URL = "https://primelogic-eg.com/erp"
REMOTE_REPO = "/home/ubuntu/PrimeERP"
REMOTE_PACKAGES = "/home/ubuntu/primeerp-data/packages"
OUT = ROOT / "bin" / "Release-package"
LOCAL = Path(__file__).with_name("deploy.local.json")


def ssh_key():
    key = os.environ.get("PRIMEERP_SSH_KEY")
    if not key and LOCAL.exists():
        key = json.loads(LOCAL.read_text(encoding="utf-8")).get("key")
    if not key:
        key = input("مسار مفتاح SSH للخادم (يُحفظ مرّةً): ").strip().strip('"')
        LOCAL.write_text(json.dumps({"key": key}, ensure_ascii=False), encoding="utf-8")
    if not Path(key).exists():
        LOCAL.unlink(missing_ok=True)
        sys.exit(f"المفتاح غير موجود: {key}")
    return key


def remote(tool, *args):
    return [tool, "-i", ssh_key(), "-o", "StrictHostKeyChecking=accept-new", *args]


def run(*args, cwd=ROOT):
    print("> " + " ".join(args))
    if subprocess.run(list(args), cwd=cwd, check=False).returncode != 0:
        sys.exit(f"فشل: {args[0]}")


def current_branch():
    return subprocess.run(["git", "branch", "--show-current"], cwd=ROOT, text=True,
                          capture_output=True, check=True).stdout.strip()


def project_version():
    text = (ROOT / "PrimeERP.csproj").read_text(encoding="utf-8")
    match = re.search(r"<Version>([^<]+)</Version>", text)
    return match.group(1) if match else "1.0.0"


def local_token():
    path = Path(os.path.expandvars(r"%LOCALAPPDATA%\PrimeERP\PrimeERP.db"))
    if not path.exists():
        return ""
    with sqlite3.connect(f"file:{path}?mode=ro", uri=True) as db:
        row = db.execute("SELECT Value FROM AppSettings WHERE Key = 'Developer.AdminToken'").fetchone()
    return row[0] if row else ""


def update_server(branch):
    run(*remote("ssh", "-t", HOST), f"cd {REMOTE_REPO} && git fetch origin +refs/heads/{branch}:refs/remotes/origin/{branch} && git checkout -B {branch} origin/{branch} "
                     f"&& server/.venv/bin/pip install -q -r server/requirements.txt && sudo systemctl restart primeerp-api")
    with urllib.request.urlopen(f"{SERVER_URL}/health", timeout=20) as response:
        print("الخادم:", response.read().decode())


def build_package(version, with_settings):
    if OUT.exists():
        shutil.rmtree(OUT)
    publish = OUT / "files"
    run("dotnet", "publish", "PrimeERP.csproj", "-c", "Release", "-r", "win-x64", "--self-contained", "true",
        f"-p:Version={version}", "-o", str(publish))

    archive = OUT / f"PrimeERP-{version}.zip"
    with zipfile.ZipFile(archive, "w", zipfile.ZIP_DEFLATED) as package:
        for file in publish.rglob("*"):
            if file.is_dir() or (file.name == "appsettings.json" and not with_settings):
                continue
            package.write(file, file.relative_to(publish))
    print(f"الحزمة: {archive} ({archive.stat().st_size // (1024 * 1024)} م.ب)")
    return archive


def publish_release(archive, version, token, notes):
    run(*remote("scp"), str(archive), f"{HOST}:{REMOTE_PACKAGES}/{archive.name}")

    request = urllib.request.Request(
        f"{SERVER_URL}/releases",
        data=json.dumps({"version": version, "package": archive.name, "notes": notes}).encode(),
        headers={"Content-Type": "application/json", "X-Admin-Token": token}, method="POST")
    with urllib.request.urlopen(request, timeout=60) as response:
        print("الإصدار:", response.read().decode())


def publish_setup():
    setup = OUT / "setup"
    run("dotnet", "publish", "PrimeERP.Setup/PrimeERP.Setup.csproj", "-c", "Release", "-r", "win-x64",
        "--self-contained", "true", "-p:PublishSingleFile=true", "-o", str(setup))
    run(*remote("scp"), str(setup / "PrimeERP.Setup.exe"), f"{HOST}:{REMOTE_PACKAGES}/PrimeERP.Setup.exe")


def main():
    parser = argparse.ArgumentParser(description="رفع التحديثات على الخادم")
    parser.add_argument("--version", help="رقم الإصدار — يُبنى إصدارٌ (≈80 م.ب) ويُنشر حين يُعطى وحده")
    parser.add_argument("--branch", help="فرع كود الخادم (افتراضيّه الحالي)")
    parser.add_argument("--notes", default="", help="ملاحظات الإصدار")
    parser.add_argument("--token", help="توكن المطوّر (افتراضيّه من إعدادات البرنامج)")
    parser.add_argument("--release-only", action="store_true", help="نشر إصدار البرنامج وحده")
    parser.add_argument("--setup", action="store_true", help="رفع المنصِّب PrimeERP.Setup.exe أيضاً")
    parser.add_argument("--with-settings", action="store_true", help="تضمين appsettings.json في الحزمة")
    args = parser.parse_args()

    if not args.release_only:
        update_server(args.branch or current_branch())

    if not args.version and not args.release_only:
        print("تمّ تحديث الخادم وحده — لنشر إصدار البرنامج أعطِ --version")
        return

    token = args.token or os.environ.get("PRIMEERP_ADMIN_TOKEN") or local_token()
    if not token:
        sys.exit("لا توكن مطوّر: مرّره بـ --token أو اضبطه في إعدادات البرنامج")

    version = args.version or project_version()
    publish_release(build_package(version, args.with_settings), version, token, args.notes)
    if args.setup:
        publish_setup()
    print(f"تمّ: الإصدار {version} منشور، والعملاء المُسنَد إليهم يجدونه عند فحص التحديث")


if __name__ == "__main__":
    sys.stdout.reconfigure(encoding="utf-8")
    main()
