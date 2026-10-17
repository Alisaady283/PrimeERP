import { formatCount, formatMoney } from "@/lib/format";
import { text } from "@/lib/i18n";
import type { ColumnSpec, FieldSpec, ValueKind } from "@/features/workspace/spec";

/** القيم نصّاً — مشتركةٌ مع الأندرويد */

export const dateOnly = (value: unknown) => (typeof value === "string" && value.length >= 10 ? value.slice(0, 10) : value == null ? "" : String(value));

/** القيمة نصّاً */
export const plainValue = (value: unknown, kind: ValueKind = "text"): string => {
  if (value === null || value === undefined || value === "") return "";
  switch (kind) {
    case "money":
      return formatMoney(Number(value));
    case "number":
    case "count":
    case "days":
      return formatCount(Number(value));
    case "percent":
      return `${Number(value).toFixed(1)}%`;
    case "date":
      return dateOnly(value);
    case "bool":
      return text(value ? "Str.Active" : "Str.Inactive");
    default:
      return String(value);
  }
};

/** قيمة الحقل الأولى */
export const emptyValue = (field: FieldSpec): unknown =>
  field.defaultValue !== undefined
    ? field.defaultValue === "today"
      ? new Date().toISOString().slice(0, 10)
      : field.defaultValue
    : field.kind === "switch"
      ? field.key === "isActive"
      : field.kind === "number"
        ? 0
        : field.kind === "select"
          ? null
          : "";

/** الأعمدة المسموحة بصلاحياتها */
export const allowedColumns = (columns: ColumnSpec[], permissions?: string[]) => columns.filter((c) => !c.permission || !!permissions?.includes(c.permission));

/** مفتاح الخادم بحرفٍ أول صغير (Cards.TotalSales ← totalSales) */
export const camel = (key: string) => key.charAt(0).toLowerCase() + key.slice(1);

/** مجموع عمودٍ في الصفوف */
export const columnTotal = (rows: Array<Record<string, unknown>>, key: string) => rows.reduce((sum, r) => sum + Number(r[key] ?? 0), 0);

/** قيمٌ فارغة لحقول التصفية أو المعاملات */
export const blankValues = (fields: FieldSpec[]) => Object.fromEntries(fields.map((f) => [f.key, emptyValue(f)]));

/** StatusVariant من الخادم (رقماً أو اسماً) ← لون الشارة — نظير VariantNameConverter في الدسكتوب */
const variants = ["neutral", "brand", "brand", "success", "warning", "danger"] as const;
export const toneOf = (variant: unknown): (typeof variants)[number] =>
  typeof variant === "number" ? variants[variant] ?? "neutral"
  : typeof variant === "string" ? ((variant.toLowerCase() === "info" ? "brand" : variant.toLowerCase()) as (typeof variants)[number])
  : "brand";
