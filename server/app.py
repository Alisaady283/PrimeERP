"""خادم تراخيص PrimeERP: تفعيلٌ وتنزيلٌ وتحديث، وثلاثة جداول لا أكثر.

يعمل بنفس بيئة الموقع (gunicorn + systemd + nginx)، وقاعدته SQLite بجواره.
"""
import hashlib
import hmac
import json
import os
import sqlite3
from datetime import datetime, timezone
from http import HTTPStatus
from pathlib import Path

from flask import Flask, jsonify, request, send_from_directory

BASE = Path(__file__).resolve().parent
DB = Path(os.environ.get("PRIMEERP_DB", BASE / "licenses.db"))
PACKAGES = Path(os.environ.get("PRIMEERP_PACKAGES", BASE / "packages"))
ADMIN_TOKEN = os.environ.get("PRIMEERP_ADMIN_TOKEN", "")

app = Flask(__name__)


def db():
    connection = sqlite3.connect(DB)
    connection.row_factory = sqlite3.Row
    return connection


def setup():
    PACKAGES.mkdir(parents=True, exist_ok=True)
    with db() as connection:
        connection.executescript(
            """
            CREATE TABLE IF NOT EXISTS licenses (
                serial       TEXT PRIMARY KEY,
                customer     TEXT NOT NULL,
                location     TEXT,
                manifest     TEXT,
                simplified   INTEGER DEFAULT 0,
                package      TEXT,
                machine      TEXT,
                activated_at TEXT,
                created_at   TEXT NOT NULL,
                is_active    INTEGER DEFAULT 1
            );
            CREATE TABLE IF NOT EXISTS releases (
                version    TEXT PRIMARY KEY,
                package    TEXT NOT NULL,
                notes      TEXT,
                created_at TEXT NOT NULL
            );
            """
        )


def authorized() -> bool:
    return bool(ADMIN_TOKEN) and hmac.compare_digest(
        request.headers.get("X-Admin-Token", ""), ADMIN_TOKEN
    )


def now() -> str:
    return datetime.now(timezone.utc).isoformat(timespec="seconds")


def latest_release():
    with db() as connection:
        return connection.execute(
            "SELECT version, package FROM releases ORDER BY created_at DESC LIMIT 1"
        ).fetchone()


@app.get("/health")
def health():
    return jsonify(status="ok", time=now())


@app.post("/licenses")
def upsert_license():
    """يرفعها برنامج البناء عند توليد السريال — بالتوكن وحده."""
    if not authorized():
        return jsonify(error="غير مصرّح"), HTTPStatus.UNAUTHORIZED

    data = request.get_json(silent=True) or {}
    serial = (data.get("serial") or "").strip()
    if not serial:
        return jsonify(error="السريال مطلوب"), HTTPStatus.BAD_REQUEST

    with db() as connection:
        connection.execute(
            """
            INSERT INTO licenses (serial, customer, location, manifest, simplified, package, created_at)
            VALUES (?, ?, ?, ?, ?, ?, ?)
            ON CONFLICT(serial) DO UPDATE SET
                customer   = excluded.customer,
                location   = excluded.location,
                manifest   = excluded.manifest,
                simplified = excluded.simplified,
                package    = COALESCE(excluded.package, licenses.package)
            """,
            (
                serial,
                data.get("customer", ""),
                data.get("location", ""),
                data.get("manifest", ""),
                1 if data.get("simplified") else 0,
                data.get("package"),
                now(),
            ),
        )

    return jsonify(ok=True)


@app.post("/activate")
def activate():
    """أول تفعيل يربط السريال بجهازه؛ وما بعده يقبل الجهاز نفسه ويرفض غيره."""
    data = request.get_json(silent=True) or {}
    serial = (data.get("serial") or "").strip().upper()
    machine = (data.get("machine") or "").strip()

    if not serial or not machine:
        return jsonify(error="السريال وبصمة الجهاز مطلوبان"), HTTPStatus.BAD_REQUEST

    with db() as connection:
        row = connection.execute(
            "SELECT * FROM licenses WHERE serial = ? AND is_active = 1", (serial,)
        ).fetchone()

        if row is None:
            return jsonify(error="سريال غير معروف"), HTTPStatus.NOT_FOUND

        if row["machine"] and not hmac.compare_digest(row["machine"], machine):
            return jsonify(error="السريال مُفعَّل على جهاز آخر"), HTTPStatus.CONFLICT

        if not row["machine"]:
            connection.execute(
                "UPDATE licenses SET machine = ?, activated_at = ? WHERE serial = ?",
                (machine, now(), serial),
            )

    release = latest_release()
    package = row["package"] or (release["package"] if release else None)

    if not package:
        return jsonify(error="لا حزمة منشورة بعد"), HTTPStatus.SERVICE_UNAVAILABLE

    return jsonify(
        customer=row["customer"],
        manifest=row["manifest"] or "",
        simplified=bool(row["simplified"]),
        version=release["version"] if release else "",
        package=f"/package/{package}",
    )


@app.post("/update")
def update():
    """المرفوع أحدث من المُشغَّل؟ — وإلا فلا تحديث."""
    data = request.get_json(silent=True) or {}
    serial = (data.get("serial") or "").strip().upper()
    current = (data.get("version") or "").strip()
    machine = (data.get("machine") or "").strip()

    with db() as connection:
        row = connection.execute(
            "SELECT machine FROM licenses WHERE serial = ? AND is_active = 1", (serial,)
        ).fetchone()

    if row is None:
        return jsonify(error="سريال غير معروف"), HTTPStatus.NOT_FOUND

    # السريال مقيَّدٌ بجهازه: نسخةٌ على جهازٍ آخر لا تُحدَّث
    if row["machine"] and machine and not hmac.compare_digest(row["machine"], machine):
        return jsonify(error="هذا السريال مُفعَّل على جهاز آخر"), HTTPStatus.FORBIDDEN

    release = latest_release()
    if release is None or release["version"] == current:
        return jsonify(available=False, version=current)

    return jsonify(available=True, version=release["version"], package=f"/package/{release['package']}")


@app.post("/releases")
def publish_release():
    if not authorized():
        return jsonify(error="غير مصرّح"), HTTPStatus.UNAUTHORIZED

    data = request.get_json(silent=True) or {}
    version = (data.get("version") or "").strip()
    package = (data.get("package") or "").strip()

    if not version or not package:
        return jsonify(error="الإصدار والحزمة مطلوبان"), HTTPStatus.BAD_REQUEST

    with db() as connection:
        connection.execute(
            """
            INSERT INTO releases (version, package, notes, created_at) VALUES (?, ?, ?, ?)
            ON CONFLICT(version) DO UPDATE SET package = excluded.package, notes = excluded.notes
            """,
            (version, package, data.get("notes", ""), now()),
        )

    return jsonify(ok=True)


@app.get("/package/<path:name>")
def package(name):
    return send_from_directory(PACKAGES, name, as_attachment=True)


setup()

if __name__ == "__main__":
    app.run(host="127.0.0.1", port=8100)
