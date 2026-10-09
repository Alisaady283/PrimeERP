import argparse
import hashlib
import json
import os
import shutil
import subprocess
import sys
import tarfile
import urllib.error
import urllib.request
from pathlib import Path

#رفع التحديثات ونشرها على الخادم
ROOT = Path(__file__).resolve().parents[2]
HOST = "ubuntu@primelogic-eg.com"
SERVER_URL = "https://primelogic-eg.com/erp"
REMOTE_REPO = "/home/ubuntu/PrimeERP"
REMOTE_PACKAGES = "/home/ubuntu/primeerp-data/packages"
REMOTE_CURRENT = "/home/ubuntu/primeerp-data/current"
OUT = ROOT / "bin" / "Release-package"
LOCAL = Path(__file__).with_name("deploy.local.json")
LISTING = "files.json"
SKIP = {"appsettings.json", "license.json", LISTING}


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


def update_server(branch):
    run(*remote("ssh", "-t", HOST), f"cd {REMOTE_REPO} && git fetch origin +refs/heads/{branch}:refs/remotes/origin/{branch} && git checkout -B {branch} origin/{branch} "
                     f"&& server/.venv/bin/pip install -q -r server/requirements.txt && sudo systemctl restart primeerp-api")
    with urllib.request.urlopen(f"{SERVER_URL}/health", timeout=20) as response:
        print("الخادم:", response.read().decode())


def fingerprint(path):
    digest = hashlib.sha256()
    with open(path, "rb") as stream:
        for block in iter(lambda: stream.read(1 << 20), b""):
            digest.update(block)
    return digest.hexdigest()


def build():
    publish = OUT / "files"
    if publish.exists():
        shutil.rmtree(publish)
    run("dotnet", "publish", "PrimeERP.csproj", "-c", "Release", "-r", "win-x64", "--self-contained", "true", "-o", str(publish))
    return publish, {
        file.relative_to(publish).as_posix(): {"sha256": fingerprint(file), "size": file.stat().st_size}
        for file in sorted(publish.rglob("*")) if file.is_file() and file.name not in SKIP
    }


def server_files():
    try:
        with urllib.request.urlopen(f"{SERVER_URL}/files/{LISTING}", timeout=30) as response:
            return json.loads(response.read().decode()).get("files", {})
    except urllib.error.HTTPError as error:
        if error.code == 404:
            return {}
        raise


def sync_files():
    publish, local = build()
    online = server_files()
    changed = [name for name, entry in local.items() if online.get(name, {}).get("sha256") != entry["sha256"]]
    removed = [name for name in online if name not in local]

    if not changed and not removed:
        print("لا تغيير في ملفات البرنامج — لا شيء يُرفع")
        return

    listing = OUT / LISTING
    listing.write_text(json.dumps({"files": local}, ensure_ascii=False), encoding="utf-8")
    gone = OUT / "removed.txt"
    gone.write_text("\n".join(removed), encoding="utf-8")
    batch = OUT / "files.tar.gz"
    with tarfile.open(batch, "w:gz") as archive:
        for name in changed:
            archive.add(publish / name, arcname=name)
        archive.add(listing, arcname=LISTING)
        archive.add(gone, arcname="removed.txt")

    size = sum(local[name]["size"] for name in changed)
    print(f"يُرفع {len(changed)} ملفاً ({size / (1024 * 1024):.1f} م.ب) ويُحذف {len(removed)}")
    run(*remote("scp"), str(batch), f"{HOST}:/tmp/primeerp-files.tar.gz")
    run(*remote("ssh", HOST), f"mkdir -p {REMOTE_CURRENT} && tar -xzf /tmp/primeerp-files.tar.gz -C {REMOTE_CURRENT} "
                              f"&& cd {REMOTE_CURRENT} && xargs -a removed.txt -d '\\n' -r rm -f -- "
                              f"&& rm -f removed.txt /tmp/primeerp-files.tar.gz")


def publish_setup():
    setup = OUT / "setup"
    run("dotnet", "publish", "PrimeERP.Setup/PrimeERP.Setup.csproj", "-c", "Release", "-r", "win-x64",
        "--self-contained", "true", "-p:PublishSingleFile=true", "-o", str(setup))
    run(*remote("scp"), str(setup / "PrimeERP.Setup.exe"), f"{HOST}:{REMOTE_PACKAGES}/PrimeERP.Setup.exe")


def main():
    parser = argparse.ArgumentParser(description="رفع التحديثات على الخادم: كوده من GitHub، وملفات البرنامج المتغيّرة وحدها")
    parser.add_argument("--branch", help="فرع كود الخادم (افتراضيّه الحالي)")
    parser.add_argument("--server-only", action="store_true", help="كود الخادم وحده")
    parser.add_argument("--files-only", action="store_true", help="ملفات البرنامج وحدها")
    parser.add_argument("--setup", action="store_true", help="رفع المنصِّب PrimeERP.Setup.exe أيضاً")
    args = parser.parse_args()

    if not args.files_only:
        update_server(args.branch or current_branch())
    if not args.server_only:
        sync_files()
    if args.setup:
        publish_setup()
    print("تمّ")


if __name__ == "__main__":
    sys.stdout.reconfigure(encoding="utf-8")
    main()
