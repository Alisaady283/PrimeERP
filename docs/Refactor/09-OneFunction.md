# دالةٌ واحدة بالمعاملات — التحقق وDTO وخدمة الصفحة

## ما وجدته

| الموضع | التكرار |
|---|---|
| `Validation/Groups.cs` + `RuleSet` | دالةٌ لكل نوع فحص: `Required` · `MaxLength` · `MinLength` · `Phone` · `Email` · `Positive` · `Unique` · `Range` · `Custom`، ثم `CodedNamed` · `Named` · `Contact` · `Money` · `Picked` · `Dated` · `Must` فوقها. ستّ عشرة دالة لما هو فحصٌ واحد: حقلٌ وشروطه |
| ملفات المتحقّقين | 17 ملفاً، ملفٌّ لكل كيان، أغلبها يكرّر «كود مطلوب، اسم مطلوب بطول، هاتف، بريد، مبلغ» |
| شروط الحقل مكتوبة مرتين | `IsRequired = true` في 115 حقلاً و`MaxLength` في 60 حقلاً في `8.Modules`، وهي نفسها في المتحقّقين |
| DTO لكل صفحة | 42 `Create…Dto` و13 `Update…Dto` و25 `…Filter`. في 13 كياناً ثلاثة أصناف (`XDto` · `CreateXDto` · `UpdateXDto`) تتقاسم الحقول نفسها، و`Update` = `Create` + `Id` |
| خدمة الصفحة | `New` و`Apply` و`ToDto` تنسخ الحقول نفسها بين الكيان وDTO يدوياً في كل صفحة |

## التصميم

### التحقق: دالةٌ واحدة

```csharp
// Validation/Check.cs — الدالة الوحيدة
public static ValidationResult Fields<T>(T item, params Field<T>[] fields)

// Validation/Field.cs — شرطٌ واحد بكل معاملاته، كلها اختيارية
public sealed record Field<T>(Expression<Func<T, object>> Of, string Label,
    bool Required = false, int Max = 0, int Min = 0, decimal? From = null, decimal? To = null,
    Format Format = Format.None, Func<T, bool> Must = null, string MustKey = null);
```

والصفحة في `PageServices` تمرّر شروطها معاملات، فلا ملف متحقّق لكل كيان:

```csharp
// PageServices/Parties/CustomerService — شروط الصفحة معاملات
protected override Field<Customer>[] Fields => new Field<Customer>[]
{
    new(x => x.Code,  "Str.Field.CustomerCode", Required: true),
    new(x => x.Name,  "Str.Field.CustomerName", Required: true, Max: 150),
    new(x => x.Phone, "Str.Field.Phone", Format: Format.Phone),
    new(x => x.Email, "Str.Email", Format: Format.Email),
    new(x => x.CreditLimit, "Str.CreditLimit", From: 0),
};
```

- `EntityService` و`DocumentService` يستدعيان `Check.Fields(entity, Fields)` وحده.
- القاعدة التي تحتاج بيانات (فريد، أب الفئة، توازن القيد) تُمرَّر `Must` بدالةٍ من الصفحة.
- يُحذف: `RuleSet` · `Groups` · `AccountingRules` · `NameValidator` · ملفات المتحقّقين السبعة عشر.

### DTO: صنفٌ واحد للكيان

> **نُفِّذ بصيغةٍ أبعد**: لا `XDto` ولا نسخ — الكيان نفسه هو الصفّ والمُدخل في أحد عشر كياناً، والـDTO باقٍ للمستندات والمُدخل المختلف شكلاً وإخفاء السرّ. المرجع: `ARCHITECTURE.md § الكيان والـDTO`.

| الآن | بعد |
|---|---|
| `XDto` + `CreateXDto` + `UpdateXDto` | `XDto` واحد للقراءة والكتابة، و`Id = 0` يعني إنشاءً |
| 25 `…Filter` | `ListFilter` واحد: البحث والفرز والمعرّفات الاختيارية |
| `…DetailDto` + `…LineDto` للمستندات | تبقى، لأن سطورها مكتوبة الأنواع |

### خدمة الصفحة: بلا نسخ حقول

- نسخ الحقول بين الكيان وDTO بالاسم مرةً واحدة في `Rows`، فتسقط `New` و`Apply` و`ToDto` حين تتطابق الحقول.
- الصفحة تُعلن ما يخصّها فقط: شروطها، حساباتها، حرّاسها، وما يُحسب لعرضها.

## ترتيب التنفيذ

| # | الخطوة | يُحذف |
|---|---|---|
| ١ | `Check.Fields` و`Field<T>`، وتحويل كل المتحقّقين إلى معاملات في صفحاتهم | `RuleSet` · `Groups` · `AccountingRules` · `NameValidator` · 17 ملف متحقّق |
| ٢ | دمج `Create/Update/XDto` في `XDto`، و`ListFilter` واحد | 55 صنف DTO و24 فلتراً |
| ٣ | نسخ الحقول بالاسم في `EntityService`، وحذف `New`/`Apply`/`ToDto` المطابقة من الصفحات | النسخ اليدوي في الصفحات |
| ٤ | الوثائق والتقرير، ثم بناء و`check.sh` | — |

## ما يُحسم منك

1. **شروط الحقل في `8.Modules`** (`IsRequired` و`MaxLength`) تبقى الآن كما هي، وتُقرأ من `Field<T>` نفسها مع مرحلة الإعلان، فيصير لها مصدرٌ واحد.
2. **DTO المستندات** (`DetailDto` و`LineDto`) تبقى كما هي.
