import { api } from "@/lib/api";
import { formatMoney } from "@/lib/format";
import { text } from "@/lib/i18n";
import type { OutputCell, Paper } from "@/lib/output";
import type { Row } from "@/features/workspace/data";
import { plainValue } from "@/lib/values";
import type { DocumentSpec, FieldSpec } from "@/features/workspace/spec";

/** القيمة الظاهرة لحقل منتقى */
const shown = (record: Record<string, unknown>, field: FieldSpec) => {
  const base = field.key.replace(/(Id|Code)$/, "");
  const name = record[`${base}Name`];
  if (field.kind === "select" && name) return String(name);
  if (field.kind === "switch") return text(record[field.key] ? "Str.Yes" : "Str.No");
  return plainValue(record[field.key], field.kind === "date" ? "date" : field.kind === "number" ? "money" : "text");
};

const totalKeys: Array<[string, string]> = [
  ["subTotal", "Str.Total"],
  ["discountAmount", "Str.Line.Discount"],
  ["vatAmount", "Str.Print.Vat"],
  ["withholdingAmount", "Str.Print.Withholding"],
  ["netTotal", "Str.Print.NetDue"],
];

/** ورقة المستند */
export async function documentPaper(spec: DocumentSpec, row: Row, company?: string): Promise<Paper> {
  const record = await api.get<Record<string, unknown>>(`/pages/${spec.key}/${row.id}`);
  const fields = spec.lines?.fields ?? [];
  const lines = (record[spec.lines?.key ?? "lines"] as Array<Record<string, unknown>> | undefined) ?? [];
  const numeric = (f: FieldSpec) => f.kind === "number";

  const columns = fields.map((f) => ({ label: text(f.label), numeric: numeric(f) }));
  const rows: OutputCell[][] = lines.map((line) =>
    fields.map((f) => {
      const value = f.key.startsWith("product") ? (line.productName ?? line[f.key]) : shown(line, f);
      return { text: String(value ?? ""), number: numeric(f) && line[f.key] != null ? Number(line[f.key]) : undefined };
    }),
  );
  if (spec.lines?.trade) {
    columns.push({ label: text("Str.Print.NetAmount"), numeric: true });
    rows.forEach((cells, i) => {
      const net = Number(lines[i]?.netAmount ?? 0);
      cells.push({ text: formatMoney(net), number: net });
    });
  }

  const totals = totalKeys.filter(([key]) => record[key] !== undefined && Number(record[key]) !== 0).map(([key, label]): [string, string] => [text(label), formatMoney(Number(record[key]))]);

  return {
    title: text(spec.title),
    company,
    fields: spec.header.filter((f) => f.kind !== "textarea").map((f): [string, string] => [text(f.label), shown(record, f)]).filter(([, v]) => v !== ""),
    table: fields.length ? { columns, rows, totals } : undefined,
    signatures: spec.signatures?.map((s) => text(s)),
  };
}
