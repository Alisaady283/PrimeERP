import { text, useLanguage } from "@/lib/i18n";

const money = { ar: new Intl.NumberFormat("ar-EG", { minimumFractionDigits: 2, maximumFractionDigits: 2 }), en: new Intl.NumberFormat("en-US", { minimumFractionDigits: 2, maximumFractionDigits: 2 }) };
const count = { ar: new Intl.NumberFormat("ar-EG"), en: new Intl.NumberFormat("en-US") };
const hijri = new Intl.DateTimeFormat("en-u-ca-islamic-civil-nu-latn", { day: "numeric", month: "numeric", year: "numeric" });

export const formatMoney = (value: number) => money[useLanguage.getState().language].format(value);
export const formatCount = (value: number) => count[useLanguage.getState().language].format(value);

/** التاريخ الميلادي dd/MM/yyyy */
export const formatGregorian = (date: Date) =>
  `${String(date.getDate()).padStart(2, "0")}/${String(date.getMonth() + 1).padStart(2, "0")}/${date.getFullYear()}`;

/** التاريخ الهجري بقالب القاموس */
export const formatHijri = (date: Date) => {
  const part = (type: string) => Number(hijri.formatToParts(date).find((p) => p.type === type)?.value ?? 0);
  return text("Str.TopBar.Hijri", part("day"), part("month"), part("year"));
};

/** الرقم مختصراً: 1.2K · 3.4M */
export const compact = (value: number) => new Intl.NumberFormat(useLanguage.getState().language, { notation: "compact", maximumFractionDigits: 1 }).format(value);

/** رقم المؤشّر بنوعه: نسبة · مبلغ · عدد */
export const formatKind = (value: number, kind = "money") => (kind === "percent" ? `${value.toFixed(1)}%` : kind === "money" ? formatMoney(value) : formatCount(value));

/** رقم البطاقة بقاعدةٍ واحدة: من الألف K ومن المليون M، وما دونه كاملاً — والنسبة قصيرةٌ أصلاً */
export const formatCard = (value: number, kind = "money") => (kind !== "percent" && Math.abs(value) >= 1000 ? compact(value) : formatKind(value, kind));
