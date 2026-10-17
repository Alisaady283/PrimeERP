import type { ColumnSpec, FieldKind, FieldSpec, Source, ValueKind } from "@/features/workspace/spec";

export const col = (key: string, label: string, kind?: ValueKind, extra: Partial<ColumnSpec> = {}): ColumnSpec => ({ key, label, kind, ...extra });
export const money = (key: string, label: string, extra: Partial<ColumnSpec> = {}) => col(key, label, "money", extra);

export const field = (key: string, label: string, kind: FieldKind = "text", extra: Partial<FieldSpec> = {}): FieldSpec => ({ key, label, kind, ...extra });
export const pick = (key: string, label: string, source: Source, extra: Partial<FieldSpec> = {}) => field(key, label, "select", { source, ...extra });
export const date = (key: string, label: string, extra: Partial<FieldSpec> = {}) => field(key, label, "date", { defaultValue: "today", ...extra });
export const num = (key: string, label: string, extra: Partial<FieldSpec> = {}) => field(key, label, "number", extra);

export const active = field("isActive", "Str.Active", "switch");
export const notes = field("notes", "Str.Notes", "textarea", { max: 2000 });
export const notesLine = field("notes", "Str.Notes");

export const product = (key = "productCode", label = "Str.Product") => pick(key, label, { page: "Products", by: "code" }, { width: "minmax(16rem,2fr)" });
export const warehouse = (key = "warehouseId", label = "Str.Warehouse") => pick(key, label, { page: "Warehouses" }, { required: true });

export const tradeLines = {
  key: "lines",
  trade: true,
  fields: [
    product(),
    num("qty", "Str.Qty", { defaultValue: 1 }),
    num("unitPrice", "Str.UnitPrice"),
    num("discountPercent", "Str.Line.DiscountPercent"),
    num("vatPercent", "Str.Trade.VatPercent"),
    num("withholdingPercent", "Str.Trade.WithholdingPercent"),
    notesLine,
  ],
};

export const cycleLines = { key: "lines", fields: [product(), num("qty", "Str.Qty", { defaultValue: 1 }), num("unitPrice", "Str.UnitPrice"), notesLine] };

export const stockLines = { key: "lines", fields: [product(), num("qty", "Str.Qty", { defaultValue: 1 }), num("unitCost", "Str.UnitCost"), field("batchNo", "Str.Batch"), notesLine] };

const monthStart = () => {
  const now = new Date();
  return `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, "0")}-01`;
};

export const range = [date("from", "Str.DateFrom", { defaultValue: monthStart() }), date("to", "Str.DateTo")];
export const asOf = [date("asOf", "Str.AsOfDate")];

export const enumOf = (prefix: string, names: Array<[number, string]>): Source => ({ enumOf: names.map(([value, name]) => [value, `${prefix}.${name}`]) });

export const flowState = pick("state", "Str.Status", { enumOf: [[-1, "Str.Flow.State.All"], [0, "Str.Flow.State.Open"], [1, "Str.Flow.State.Partial"], [2, "Str.Flow.State.Awaiting"], [3, "Str.Flow.State.Complete"]] }, { defaultValue: -1 });
