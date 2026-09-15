# خادم تراخيص PrimeERP

خدمةٌ صغيرة بجوار موقع PrimeLogic على نفس الخادم — نفس بيئته: gunicorn وsystemd وnginx.

## التركيب (مرّة واحدة)

```bash
git clone https://github.com/Alisaady283/PrimeERP.git /home/ubuntu/PrimeERP
cd /home/ubuntu/PrimeERP/server
python3 -m venv .venv && .venv/bin/pip install -r requirements.txt

mkdir -p /home/ubuntu/primeerp-data/packages
printf 'PRIMEERP_ADMIN_TOKEN=%s\n' "$(openssl rand -hex 24)" > /home/ubuntu/primeerp-data/api.env
chmod 600 /home/ubuntu/primeerp-data/api.env

sudo cp primeerp-api.service /etc/systemd/system/
sudo systemctl daemon-reload && sudo systemctl enable --now primeerp-api
```

ثم يُدرَج محتوى `nginx-location.conf` داخل كتلة `server` في
`/etc/nginx/sites-enabled/primelogicsite` ويُعاد تحميل nginx.

## النقاط

| النقطة | الدخل | الخرج |
|---|---|---|
| `GET  /erp/health` | — | حالة الخدمة |
| `POST /erp/licenses` | ترخيص + توكن المطوّر | تسجيل السريال |
| `POST /erp/activate` | سريال + بصمة جهاز | البيان والإصدار ورابط الحزمة |
| `POST /erp/update` | سريال + الإصدار الحالي | هل يوجد أحدث |
| `POST /erp/releases` | إصدار + اسم حزمة + توكن | نشر إصدار |
| `GET  /erp/package/<اسم>` | — | ملف الحزمة |

## التحديث

```bash
cd /home/ubuntu/PrimeERP && git pull && sudo systemctl restart primeerp-api
```
