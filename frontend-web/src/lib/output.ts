/** الطباعة والتصدير — المحتوى هنا مشترك، والتسليم (طباعة · حفظ) من `@/lib/platform` لكل عميل */
import { directionOf, text, useLanguage } from "@/lib/i18n";
import { formatGregorian, formatKind, formatMoney } from "@/lib/format";
import { printHtml, saveFile } from "@/lib/platform";
import { writeZip } from "@/lib/zip";
import { camel, plainValue } from "@/lib/values";
import type { AnalyticsResponse } from "@/features/workspace/data";
import type { ColumnSpec, DashboardSpec, ValueKind } from "@/features/workspace/spec";

export type OutputColumn = { label: string; numeric?: boolean };
/** خلية الطباعة والتصدير */
export type OutputCell = { text: string; number?: number };
export type OutputTable = { columns: OutputColumn[]; rows: OutputCell[][]; totals?: Array<[string, string]> };
export type Paper = {
  title: string;
  company?: string;
  subtitle?: string;
  fields?: Array<[string, string]>;
  table?: OutputTable;
  signatures?: string[];
};

const escape = (value: string) => value.replace(/[&<>"]/g, (c) => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;" })[c]!);

const tableHtml = ({ columns, rows, totals }: OutputTable) => `
<table>
  <thead><tr>${columns.map((c) => `<th class="${c.numeric ? "n" : ""}">${escape(c.label)}</th>`).join("")}</tr></thead>
  <tbody>${rows.map((r) => `<tr>${r.map((v, i) => `<td class="${columns[i]?.numeric ? "n" : ""}">${escape(v.text)}</td>`).join("")}</tr>`).join("")}</tbody>
</table>
${totals?.length ? `<div class="totals">${totals.map(([l, v]) => `<div><span>${escape(l)}</span><b>${escape(v)}</b></div>`).join("")}</div>` : ""}`;

export const paperHtml = (paper: Paper) => {
  const language = useLanguage.getState().language;
  return `<!doctype html><html lang="${language}" dir="${directionOf(language)}"><head><meta charset="utf-8"><title>${escape(paper.title)}</title>
<style>
  @page { size: A4; margin: 14mm; }
  * { box-sizing: border-box; }
  body { font-family: "IBM Plex Sans Arabic", "Segoe UI", Tahoma, sans-serif; color: #1b2233; font-size: 12px; margin: 0; }
  header { display: flex; justify-content: space-between; align-items: flex-end; border-bottom: 2px solid #1b2233; padding-bottom: 8px; margin-bottom: 12px; }
  h1 { font-size: 18px; margin: 0; }
  .muted { color: #6b7794; }
  .fields { display: grid; grid-template-columns: repeat(2, 1fr); gap: 4px 24px; margin-bottom: 12px; }
  .fields div { display: flex; gap: 8px; border-bottom: 1px dotted #cbd3e0; padding: 3px 0; }
  .fields span { color: #6b7794; min-width: 110px; }
  table { width: 100%; border-collapse: collapse; }
  th { background: #eff2f7; font-weight: 600; text-align: start; }
  th, td { border: 1px solid #cbd3e0; padding: 5px 7px; }
  tr:nth-child(even) td { background: #f7f9fc; }
  .n { text-align: center; font-variant-numeric: tabular-nums; white-space: nowrap; }
  .totals { margin-top: 10px; margin-inline-start: auto; width: 45%; }
  .totals div { display: flex; justify-content: space-between; border-bottom: 1px solid #e1e6ef; padding: 4px 0; }
  .signs { display: flex; justify-content: space-between; gap: 24px; margin-top: 48px; }
  .signs div { flex: 1; text-align: center; border-top: 1px solid #1b2233; padding-top: 6px; }
  thead { display: table-header-group; }
  tr { break-inside: avoid; }
</style></head><body>
<header><div><h1>${escape(paper.title)}</h1>${paper.subtitle ? `<div class="muted">${escape(paper.subtitle)}</div>` : ""}</div>
<div style="text-align:end">${paper.company ? `<b>${escape(paper.company)}</b><br>` : ""}<span class="muted">${formatGregorian(new Date())}</span></div></header>
${paper.fields?.length ? `<div class="fields">${paper.fields.map(([l, v]) => `<div><span>${escape(l)}</span><b>${escape(v)}</b></div>`).join("")}</div>` : ""}
${paper.table ? tableHtml(paper.table) : ""}
${paper.signatures?.length ? `<div class="signs">${paper.signatures.map((s) => `<div>${escape(s)}</div>`).join("")}</div>` : ""}
</body></html>`;
};

/** طباعة الورقة */
export const printPaper = (paper: Paper) => printHtml(paperHtml(paper), paper.title);

const safeName = (title: string) => title.replace(/[\\/:*?"<>|]+/g, " ").trim() || "PrimeERP";

export function exportCsv(title: string, table: OutputTable) {
  const lines = [table.columns.map((c) => c.label), ...table.rows.map((r) => r.map((v) => (v.number !== undefined ? String(v.number) : v.text)))]
    .map((line) => line.map((v) => `"${v.replaceAll('"', '""')}"`).join(","));
  void saveFile("﻿" + lines.join("\r\n"), `${safeName(title)}.csv`, "text/csv;charset=utf-8");
}

const cell = (value: OutputCell, ref: string) =>
  value.number !== undefined && Number.isFinite(value.number)
    ? `<c r="${ref}"><v>${value.number}</v></c>`
    : `<c r="${ref}" t="inlineStr"><is><t xml:space="preserve">${escape(value.text)}</t></is></c>`;

const columnName = (index: number) => {
  let name = "";
  for (let n = index + 1; n > 0; n = Math.floor((n - 1) / 26)) name = String.fromCharCode(65 + ((n - 1) % 26)) + name;
  return name;
};

/** تصدير Excel */
export function exportExcel(title: string, table: OutputTable) {
  const rtl = useLanguage.getState().language === "ar";
  const all: OutputCell[][] = [
    table.columns.map((c) => ({ text: c.label })),
    ...table.rows,
    ...(table.totals ?? []).map(([l, v]) => [{ text: l }, { text: v }]),
  ];
  const rows = all.map((line, r) => `<row r="${r + 1}">${line.map((v, c) => cell(v, `${columnName(c)}${r + 1}`)).join("")}</row>`).join("");
  const encoder = new TextEncoder();
  const xml = (s: string) => encoder.encode(`<?xml version="1.0" encoding="UTF-8" standalone="yes"?>${s}`);
  const bytes = writeZip([
    { name: "[Content_Types].xml", data: xml(`<Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types"><Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/><Default Extension="xml" ContentType="application/xml"/><Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/><Override PartName="/xl/worksheets/sheet1.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/></Types>`) },
    { name: "_rels/.rels", data: xml(`<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/></Relationships>`) },
    { name: "xl/workbook.xml", data: xml(`<workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships"><sheets><sheet name="${escape(safeName(title).slice(0, 31))}" sheetId="1" r:id="rId1"/></sheets></workbook>`) },
    { name: "xl/_rels/workbook.xml.rels", data: xml(`<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet1.xml"/></Relationships>`) },
    { name: "xl/worksheets/sheet1.xml", data: xml(`<worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main"><sheetViews><sheetView workbookViewId="0"${rtl ? ' rightToLeft="1"' : ""}/></sheetViews><sheetData>${rows}</sheetData></worksheet>`) },
  ]);
  void saveFile(bytes, `${safeName(title)}.xlsx`, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
}

/** خيارات التصدير */
export const exportChoices = (title: string, table: () => OutputTable, paper: () => Paper) => [
  { label: "Excel (.xlsx)", onSelect: () => exportExcel(title, table()) },
  { label: "CSV (.csv)", onSelect: () => exportCsv(title, table()) },
  { label: `PDF — ${text("Str.Print")}`, onSelect: () => printPaper(paper()) },
];

const numeric: ValueKind[] = ["money", "number", "count", "percent", "days"];

/** جدول الطباعة والتصدير */
export const outputTable = (columns: ColumnSpec[], rows: Array<Record<string, unknown>>, totals?: Array<[string, string]>): OutputTable => ({
  columns: columns.map((c) => ({ label: text(c.label), numeric: !!c.kind && numeric.includes(c.kind) })),
  rows: rows.map((row) =>
    columns.map((c) => {
      const value = row[c.key];
      const number = c.kind && numeric.includes(c.kind) && value !== null && value !== undefined && value !== "" ? Number(value) : undefined;
      return { text: plainValue(value, c.kind), number };
    }),
  ),
  totals,
});

/** مؤشّرات اللوحة ورسومها جدولاً */
export const analyticsTable = (spec: DashboardSpec, data: AnalyticsResponse | undefined): OutputTable => ({
  columns: [{ label: text("Str.Dashboard.Indicator") }, { label: text("Str.Dashboard.Item") }, { label: text("Str.Dashboard.Series") }, { label: text("Str.Dashboard.Value"), numeric: true }],
  rows: [
    ...spec.cards.map((card) => {
      const value = data?.cards[camel(card.key)]?.value ?? 0;
      return [{ text: text(`Str.Analytics.${spec.key}.${card.key}`) }, { text: "" }, { text: "" }, { text: formatKind(value, card.kind), number: value }];
    }),
    ...spec.charts.flatMap((chart) => {
      const series = data?.charts[camel(chart.key)];
      return (series?.series ?? []).flatMap((s) =>
        series!.labels.map((label, i) => [{ text: text(`Str.Analytics.${spec.key}.${chart.key}`) }, { text: label }, { text: s.name }, { text: formatMoney(s.values[i] ?? 0), number: s.values[i] ?? 0 }]),
      );
    }),
  ],
});

/** مجاميع التقرير بعناوين أعمدتها — مفاتيح الخادم بلا اعتبارٍ لحالة الأحرف */
export const reportTotals = (columns: ColumnSpec[], totals: Record<string, string> | undefined): Array<[string, string]> =>
  Object.entries(totals ?? {}).map(([key, value]) => [text(columns.find((c) => c.key.toLowerCase() === key.toLowerCase())?.label ?? key), value]);
