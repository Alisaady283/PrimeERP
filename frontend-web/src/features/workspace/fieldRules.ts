import type { Row } from "@/features/workspace/data";
import type { FieldSpec } from "@/features/workspace/spec";
import { dateOnly, emptyValue } from "@/lib/values";

/** قواعد النموذج — مشتركةٌ بين الويب والأندرويد */

export const resolveField = (field: FieldSpec, values: Record<string, unknown>): FieldSpec => {
  const resolved = field.dependsOn ? { ...field, source: field.dependsOn.sources[String(values[field.dependsOn.field] ?? "")] ?? { enumOf: [] } } : field;
  const source = resolved.source;
  if (!source || !("page" in source) || !source.where) return resolved;
  const where = Object.fromEntries(
    Object.entries(source.where)
      .map(([key, value]) => [key, value.startsWith("@") ? values[value.slice(1)] : value])
      .filter(([, value]) => value !== null && value !== undefined && value !== ""),
  ) as Record<string, string>;
  return { ...resolved, source: { ...source, where } };
};

/** ظهور الحقل */
export const visibleIn = (field: FieldSpec, values: Record<string, unknown>) =>
  !field.visibleWhen || String(values[field.visibleWhen.field] ?? "") === String(field.visibleWhen.value);

export const validate = (fields: FieldSpec[], values: Record<string, unknown>, t: (key: string, ...args: Array<string | number>) => string) => {
  const errors: Record<string, string> = {};
  for (const field of fields) {
    const value = values[field.key];
    const label = t(field.label);
    if (field.required && (value === null || value === undefined || value === "")) errors[field.key] = t("Str.Rule.Required", label);
    else if (field.max && typeof value === "string" && value.length > field.max) errors[field.key] = t("Str.Rule.MaxLength", label, field.max);
    else if (field.min !== undefined && typeof value === "number" && value < field.min) errors[field.key] = t("Str.Rule.NonNegative", label);
  }
  return errors;
};

/** قيم النموذج الأولى: من السجلّ عند التعديل وإلا الافتراضية */
export const initialValues = (fields: FieldSpec[], id: number | null, source?: Row) =>
  Object.fromEntries(
    fields.map((f) => {
      const current = id && source ? source[f.key] : undefined;
      return [f.key, current === undefined ? emptyValue(f) : f.kind === "date" ? dateOnly(current) : current];
    }),
  );

/** جسم الحفظ: نسخة الصفّ ومعرّفه عند التعديل */
export const saveBody = (id: number | null, source: Row | undefined, values: Record<string, unknown>) => ({ ...(id && source ? { rowVersion: source.rowVersion, id } : {}), ...values });
