# RUN.md — متطلبات تشغيل الدسكتوب والويب والأندرويد

الثلاثة واجهاتٌ لنظامٍ واحد وقاعدة بياناتٍ واحدة: `%LOCALAPPDATA%\PrimeERP\PrimeERP.db` (SQLite). الدسكتوب يقرؤها مباشرةً، والويب والأندرويد عبر الـAPI.

المجلد `%LOCALAPPDATA%\PrimeERP` وصلةٌ (junction) إلى `D:\Ali\مشاريع دجانجو\ERP_C\PrimeERP_data\PrimeERP` — القاعدة ونسخها الاحتياطية على D، والمسار في الكود كما هو.

```
الدسكتوب (WPF) ──────────────► PrimeERP.db
الويب  :5173 ──/api──► API :5080 ──► PrimeERP.db
الأندرويد (Expo) ─────► API :5080
```

## المتطلبات على الحاسوب

| الأداة | الإصدار | لماذا |
|---|---|---|
| Windows | 10 أو 11 | الدسكتوب WPF |
| .NET SDK | 10 (`net10.0`) — المثبَّت 10.0.401 | الدسكتوب والـAPI |
| Node.js | 22 LTS (المثبَّت 22.15) | الويب والأندرويد |
| Android Studio | Rabbit 1 فأحدث — مثبَّت في `D:\Android\Sdk\Android Studio`، والـSDK في `D:\Android\Sdk` (`ANDROID_HOME`، والمحاكيات في `ANDROID_USER_HOME` = `D:\Android\.android`؛ و`%LOCALAPPDATA%\Android` وصلةٌ إلى `D:\Android`) | المحاكي (اختياري مع هاتفٍ حقيقي) |
| Expo Go | على الهاتف أو المحاكي، بإصدار SDK 57 | تشغيل الأندرويد في التطوير |

أول مرة في كل مجلد واجهة: `npm install`.

## 1. الدسكتوب

```powershell
dotnet run --project frontend-desktop/PrimeERP.csproj
```
- يُغلَق البرنامج قبل أي `dotnet build` أو `dotnet test`، لأنه يقفل ملفه التنفيذي.
- لا يحتاج الـAPI.

## 2. الـAPI (للويب والأندرويد)

```powershell
dotnet run --project api/PrimeERP.Api.csproj
```
- يستمع على `http://*:5080` (`api/Properties/launchSettings.json`) — أي على كل واجهات الشبكة، ليصله الهاتف.
- **جدار الحماية** (مرة واحدة، PowerShell كمسؤول) ليصل الهاتف إليه:
  ```powershell
  New-NetFirewallRule -DisplayName "PrimeERP API" -Direction Inbound -Protocol TCP -LocalPort 5080 -Action Allow -Profile Private
  ```

## 3. الويب

```powershell
cd frontend-web
npm run dev
```
- يفتح على `http://localhost:5173`، ويمرّر `/api` إلى `http://localhost:5080` — فيُشغَّل الـAPI أولاً.
- من هاتفٍ على الشبكة نفسها: `http://<عنوان الحاسوب>:5173` (`npm run dev` يستمع على الشبكة بـ`--host`).
- نسخة الإنتاج: `npm run build` ← المجلد `frontend-web/dist`.

## 4. الأندرويد

```powershell
cd frontend-android
npx expo start --lan
```
- يطبع عنواناً `exp://<عنوان الحاسوب>:8081` — يُفتح في **Expo Go** (أو يُمسح رمز QR).
- **جدار الحماية** للمنفذ 8081 كذلك (`-LocalPort 8081` في الأمر أعلاه).
- **عنوان الخادم** يُكتب في شاشة الدخول:

| الجهاز | العنوان |
|---|---|
| هاتفٌ على الشبكة نفسها | `http://<عنوان الحاسوب>:5080` |
| محاكي Android Studio | `http://10.0.2.2:5080` |

- **المحاكي**: Android Studio ← Device Manager ← Pixel 8 · Android 15 (API 35) · x86_64 ← تشغيل، ثم Expo Go فيه.
- إن تعطّل التحميل بعد تغيير المكتبات: `npx expo start --lan --clear`.

## الترتيب المختصر

| ما تريد | شغّل |
|---|---|
| الدسكتوب وحده | 1 |
| الويب | 2 ثم 3 |
| الأندرويد | 2 ثم 4 |
| الثلاثة معاً | 2 · 3 · 4 · 1 — كلٌّ في نافذة |

## مشكلات شائعة

| العرَض | السبب والحل |
|---|---|
| الويب يعرض خطأ اتصال | الـAPI غير مشغَّل (الخطوة 2) |
| Expo Go: «تعذّر الاتصال» أو يعيد المحاولة | جدار الحماية على 8081، أو الهاتف على شبكةٍ أخرى |
| الأندرويد: «الخادم غير متاح» عند الدخول | عنوان الخادم خطأ (الجدول أعلاه)، أو جدار الحماية على 5080 |
| `dotnet build` يفشل بقفل ملف | الدسكتوب مفتوح — يُغلَق |
| `localhost` من الهاتف لا يعمل | `localhost` على الهاتف هو الهاتف نفسه — يُكتب عنوان الحاسوب |
