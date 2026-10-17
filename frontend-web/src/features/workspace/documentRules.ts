import { openingStock } from "@/catalog/pages";
import type { Row } from "@/features/workspace/data";
import type { Pulled } from "@/features/workspace/pull";
import type { DocumentSpec, FieldSpec, PullSpec } from "@/features/workspace/spec";
import { api } from "@/lib/api";
import { lineParts } from "@/lib/lineCalc";
import { dateOnly, emptyValue } from "@/lib/values";

/** قواعد المستند — مشتركةٌ بين الويب والأندرويد */

export type Line = Record<string, unknown>;

/** قيم الحقول من السجلّ وإلا الافتراضية */
export const pick = (source: Row | undefined, fields: FieldSpec[]) =>
  Object.fromEntries(fields.map((f) => [f.key, source && source[f.key] !== undefined ? (f.kind === "date" ? dateOnly(source[f.key]) : source[f.key]) : emptyValue(f)]));

/** صافي سطرٍ تجاري */
export const tradeParts = (line: Line) =>
  lineParts(Number(line.qty ?? 0), Number(line.unitPrice ?? 0), Number(line.discountPercent ?? 0), Number(line.vatPercent ?? 0), Number(line.withholdingPercent ?? 0));

export const tradeNet = (line: Line) => tradeParts(line).net;

/** سطرٌ فارغ بحقول المستند */
export const blankLine = (spec: DocumentSpec): Line => Object.fromEntries((spec.lines?.fields ?? []).map((f) => [f.key, emptyValue(f)]));

/** جسم الحفظ: الرأس والسطور مرقّمة */
export const documentBody = (spec: DocumentSpec, id: number | null, header: Record<string, unknown>, lines: Line[]) => ({
  ...header,
  id: id ?? 0,
  [spec.lines?.key ?? "lines"]: lines.map((line, i) => ({ ...line, lineNo: i + 1 })),
});

/** إجمالي المستند التجاري */
export const documentTotal = (spec: DocumentSpec, lines: Line[]) => (spec.lines?.trade ? lines.reduce((sum, l) => sum + tradeNet(l), 0) : null);

/** مصادر السحب المتاحة: بصلاحيتها، وما يخصّ الدورة الكاملة يُخفى في المختصرة */
export const pullSources = (spec: DocumentSpec, simplifiedFlow: boolean, permissions: string[] | undefined): PullSpec[] =>
  (spec.pull ?? []).filter((p) => !(p.fullCycleOnly && simplifiedFlow) && !!permissions?.includes(p.permission));

/** السطور المسحوبة تُضاف إلى السطور ذات الصنف، وقيم الرأس تملأ الفارغ منه وحده */
export function mergePulled(spec: DocumentSpec, lines: Line[], header: Record<string, unknown>, { lines: picked, header: values }: Pulled) {
  const fields = spec.lines?.fields ?? [];
  const productKey = fields.find((f) => f.key.startsWith("product"))?.key ?? "productCode";
  const priceKey = fields.find((f) => f.key === "unitPrice" || f.key === "unitCost")?.key;
  return {
    lines: [
      ...lines.filter((l) => l[productKey]),
      ...picked.map(({ line, qty, candidate }) => ({
        ...blankLine(spec),
        [productKey]: line.productCode,
        qty,
        ...(priceKey ? { [priceKey]: line.unitValue } : {}),
        sourceType: candidate.sourceType,
        sourceId: candidate.sourceId,
        sourceNo: candidate.sourceNo,
        sourceLineId: line.sourceLineId,
      })),
    ],
    header: { ...header, ...Object.fromEntries(Object.entries(values).filter(([k]) => k in header && (header[k] === null || header[k] === undefined || header[k] === ""))) },
  };
}

/** فتح ورقةٍ بمعاملاتها (spec.open): الرأس والسطور من الخادم */
export async function openSheet(spec: DocumentSpec, params: Record<string, unknown>) {
  const sheet = await api.get<Row>(spec.open!.path, params as Record<string, string>);
  return { header: { ...sheet, ...pick(sheet, spec.header) }, lines: ((sheet[spec.lines?.key ?? "lines"] as Line[] | undefined) ?? []).map((l) => ({ ...l })) };
}

/** السطور من السجلّ */
export const recordLines = (spec: DocumentSpec, source: Row | undefined) => ((source?.[spec.lines?.key ?? "lines"] as Line[] | undefined) ?? []).map((l) => ({ ...l }));

/** محرّر الصفّ: صفّ رصيد أول المدة يُفتح بمحرّره */
export const editorFor = (spec: DocumentSpec, row: Row): DocumentSpec => (spec.kindEditors?.some((k) => String(row[k.property] ?? "") === k.value) ? openingStock : spec);

/** ملخّص السطور التجارية: مجموع كل جزء */
type TradeParts = ReturnType<typeof tradeParts>;

export const tradeSummary = (lines: Line[]) =>
  lines.reduce<TradeParts>(
    (sum, l) => {
      const p = tradeParts(l);
      return { gross: sum.gross + p.gross, discount: sum.discount + p.discount, vat: sum.vat + p.vat, withholding: sum.withholding + p.withholding, net: sum.net + p.net };
    },
    { gross: 0, discount: 0, vat: 0, withholding: 0, net: 0 },
  );

/** مجاميع الحقول المُعلَنة total */
export const lineTotals = (spec: DocumentSpec, lines: Line[]) =>
  (spec.lines?.fields ?? []).filter((f) => f.total).map((field) => ({ field, sum: lines.reduce((s, l) => s + Number(l[field.key] ?? 0), 0) }));

/** فرق التوازن (المدين − الدائن)، وnull لما لا توازن له */
export const imbalance = (spec: DocumentSpec, lines: Line[]) => {
  const pair = spec.lines?.balance;
  if (!pair) return null;
  const sum = (key: string) => lines.reduce((s, l) => s + Number(l[key] ?? 0), 0);
  return Math.round((sum(pair[0]) - sum(pair[1])) * 100) / 100;
};

/** حقل عنوان السطر: أول اختيار، وإلا أول نصّ */
export const titleField = (spec: DocumentSpec) => {
  const fields = spec.lines?.fields ?? [];
  return fields.find((f) => f.kind === "select") ?? fields.find((f) => f.kind === "text" && f.key !== "notes");
};
