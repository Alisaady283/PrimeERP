/** أنواع الرسوم: بياناتٌ يقرؤها كل عميل */
export type ChartKind = "Column" | "Bar" | "Line" | "Area" | "Donut" | "Gauge";
export type ChartTone = "Auto" | "Severity" | "Improving";
export type ChartSeries = { name: string; values: number[]; comparison?: boolean };
/** keys وdimension: معرّف كل تسمية وما تُصفّى به اللوحة عند النقر عليها */
export type ChartData = { labels: string[]; series: ChartSeries[]; keys?: (string | null)[] | null; dimension?: string | null };

export type ValueKind = "text" | "code" | "money" | "number" | "count" | "percent" | "days" | "date" | "bool" | "status";

export type ColumnSpec = { key: string; label: string; kind?: ValueKind; width?: string; total?: boolean; group?: string; permission?: string };

export type Source =
  | { page: string; by?: "id" | "code"; where?: Record<string, string> }
  | { categories: string }
  | { accounts: true; by?: "id" | "code" }
  | { enumOf: Array<[number | string, string]> }
  | { pages: true }
  | { actions: true }
  | { alertEvents: true };

export type FieldKind = "text" | "number" | "date" | "time" | "textarea" | "switch" | "select" | "password" | "image";

export type FieldSpec = {
  key: string;
  label: string;
  kind: FieldKind;
  source?: Source;
  required?: boolean;
  max?: number;
  min?: number;
  span?: 2;
  createOnly?: boolean;
  lockOnEdit?: boolean;
  visibleWhen?: { field: string; value: string | number | boolean };
  defaultValue?: unknown;
  width?: string;
  readOnly?: boolean;
  dependsOn?: { field: string; sources: Record<string, Source> };
  /** حقل سطرٍ يُجمع في ملخّص المستند */
  total?: boolean;
};

/** balance: حقلان يجب أن يتساويا مجموعاً (المدين والدائن) */
export type LinesSpec = { key: string; fields: FieldSpec[]; trade?: boolean; balance?: [string, string] };

type Base = { key: string; title: string; permission: string; fullCycleOnly?: boolean };

/** زرّ صفحة مُعلَن */
export type PageAction = { key: string; label: string; permission?: string; run: "depreciation" | "openingStock" };

/** زرٌّ على سجلٍّ مفتوح في حواره: الفعل يُضاف لبوّابة الصفحة (Edit ← ApiKeys.Edit) — نظير RowAction في الدسكتوب */
export type RecordAction = { key: string; label: string; action: "View" | "Create" | "Edit" | "Delete"; permission?: string; run: "issueApiKey" | "paymentLink" | "submitEInvoice" };

/** مصدر سحب */
/** scope: نطاقٌ يضيّق المرشّحين في الخادم (IPullScope) — مثل مرتجع نقطة البيع */
export type PullSpec = { source: string; permission: string; match?: { field: string; header: string }; fullCycleOnly?: boolean; scope?: string };

/** عرض المربّعات */
export type GallerySpec = { image: string; title: string; hint?: string; value?: string };

type Listing = { filters?: FieldSpec[]; pageActions?: PageAction[]; gallery?: GallerySpec; recordActions?: RecordAction[] };

export type ListSpec = Base & Listing & { type: "list"; columns: ColumnSpec[]; form?: FieldSpec[]; formTitles?: [string, string]; readOnly?: boolean };

export type DocumentSpec = Base & Listing & {
  type: "document";
  columns: ColumnSpec[];
  header: FieldSpec[];
  lines?: LinesSpec;
  noUpdate?: boolean;
  open?: { path: string; params: FieldSpec[] };
  pull?: PullSpec[];
  /** حفظٌ إلى مسارٍ غير /pages — مثل مرتجع نقطة البيع إلى /pos/return */
  saveTo?: string;
  /** صفّ بمحرّر نوعه */
  kindEditors?: Array<{ property: string; value: string; page: string }>;
  signatures?: string[];
};

export type ReportSpec = Base & { type: "report"; params: FieldSpec[]; columns: ColumnSpec[] };

export type CardSpec = { key: string; kind?: "money" | "count" | "percent" | "days"; lowerIsBetter?: boolean; drill?: string; fullCycleOnly?: boolean };
export type ChartSpec = { key: string; kind: ChartKind; tone?: ChartTone; span?: 1 | 2 | 3; smooth?: boolean; drill?: string };
export type DashboardSpec = Base & { type: "dashboard"; cards: CardSpec[]; charts: ChartSpec[] };

/** شاشةٌ خاصة: كل عميلٍ يربط اسمها بمكوّنه */
export type CustomSpec = Base & { type: "custom"; screen: string };

export type PageSpec = ListSpec | DocumentSpec | ReportSpec | DashboardSpec | CustomSpec;
