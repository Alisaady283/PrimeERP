import { useSearch } from "@tanstack/react-router";
import { createColumnHelper } from "@tanstack/react-table";
import { FilterX, LayoutGrid, List, Plus } from "lucide-react";
import { motion } from "motion/react";
import { useId, useMemo, useState, type ReactNode } from "react";
import { duration, tween } from "@/lib/motion";
import { Button } from "@/components/ui/Button";
import { Card } from "@/components/ui/Card";
import { DataGrid, GridTotals } from "@/components/ui/DataGrid";
import { Gallery } from "@/components/ui/Gallery";
import { PageHeader } from "@/components/ui/PageHeader";
import { SearchBox } from "@/components/ui/SearchBox";
import { useCan, useSession } from "@/features/auth/session";
import { formatCount, formatMoney } from "@/lib/format";
import { cn } from "@/lib/cn";
import { useText } from "@/lib/i18n";
import { imageUrl } from "@/lib/image";
import { useDebounced } from "@/lib/useDebounced";
import { allowedColumns, blankValues, columnTotal } from "@/lib/values";
import { usePageRows, type Query, type Row } from "@/features/workspace/data";
import { FieldInput, resolveField } from "@/features/workspace/FieldInput";
import { alignOf, Value } from "@/features/workspace/format";
import { outputTable } from "@/lib/output";
import { OutputActions } from "@/features/workspace/OutputActions";
import type { ColumnSpec, FieldSpec, GallerySpec } from "@/features/workspace/spec";

const column = createColumnHelper<Row>();

export const useColumns = (columns: ColumnSpec[]) => {
  const t = useText();
  const permissions = useSession((s) => s.user?.permissions);
  return useMemo(
    () =>
      allowedColumns(columns, permissions).map((c) =>
          column.accessor((row) => row[c.key], {
            id: c.key,
            header: t(c.label),
            meta: { width: c.width ?? (c.kind === "money" ? "9rem" : c.kind === "date" ? "8rem" : c.kind === "code" ? "8rem" : "minmax(9rem,1fr)"), align: alignOf(c.kind) },
            cell: (cell) => <Value value={cell.getValue()} kind={c.kind} variant={c.kind === "status" ? cell.row.original[c.key.replace(/Text$/, "Variant")] : undefined} />,
          }),
        ),
    [columns, t, permissions],
  );
};

/** الأعمدة المسموحة */
export const useVisibleColumns = (columns: ColumnSpec[]) => {
  const permissions = useSession((s) => s.user?.permissions);
  return useMemo(() => allowedColumns(columns, permissions), [columns, permissions]);
};

type ListFrameProps = {
  pageKey: string;
  title: string;
  permission: string;
  columns: ColumnSpec[];
  query?: Query;
  filters?: FieldSpec[];
  onAdd?: () => void;
  onOpen?: (row: Row) => void;
  toolbar?: ReactNode;
  actions?: ReactNode;
  addLabel?: string;
  gallery?: GallerySpec;
};

type View = "tiles" | "details";

/** العرض المحفوظ */
const useView = (pageKey: string, enabled: boolean) => {
  const key = `view.${pageKey}`;
  const [view, setView] = useState<View>(() => {
    try {
      return enabled && localStorage.getItem(key) === "tiles" ? "tiles" : "details";
    } catch {
      return "details";
    }
  });
  const choose = (next: View) => {
    setView(next);
    try {
      localStorage.setItem(key, next);
    } catch {
      /* تخزينٌ محجوب */
    }
  };
  return [enabled ? view : "details", choose] as const;
};

/** مبدّل العرض */
function ViewSwitch({ value, onChange }: { value: View; onChange: (view: View) => void }) {
  const t = useText();
  const id = useId();
  const items: Array<[View, typeof List, string]> = [["details", List, "Str.View.Details"], ["tiles", LayoutGrid, "Str.View.Tiles"]];
  return (
    <div className="flex rounded-lg border border-line bg-paper-soft p-0.5">
      {items.map(([key, Icon, label]) => (
        <button
          key={key}
          type="button"
          title={t(label)}
          aria-label={t(label)}
          aria-pressed={value === key}
          onClick={() => onChange(key)}
          className={cn("relative grid size-7 place-items-center rounded-md transition-colors", value === key ? "text-brand" : "text-ink-tertiary hover:text-ink")}
        >
          {value === key && <motion.span layoutId={`view-${id}`} transition={tween(duration.medium2)} className="absolute inset-0 rounded-md bg-card shadow-sm" />}
          <Icon className="relative size-4" />
        </button>
      ))}
    </div>
  );
}

export function ListFrame({ pageKey, title, permission, columns, query, filters = [], onAdd, onOpen, toolbar, actions, addLabel, gallery }: ListFrameProps) {
  const t = useText();
  const canCreate = useCan(`${permission}.Create`);
  const canView = useCan(`${permission}.View`);
  const { q, from, to } = useSearch({ strict: false }) as { q?: string; from?: string; to?: string };
  const [search, setSearch] = useState(q ?? "");
  const [view, setView] = useView(pageKey, !!gallery);
  const blank = () => blankValues(filters);
  /** نطاق التاريخ الذي فُتحت به الصفحة يملأ مرشّحَي من/إلى */
  const opened = (): Record<string, unknown> => {
    const range: Record<string, string | undefined> = { from, to };
    return { ...blank(), ...Object.fromEntries(filters.filter((f) => range[f.key.toLowerCase()]).map((f) => [f.key, range[f.key.toLowerCase()]])) };
  };
  const [filterValues, setFilterValues] = useState<Record<string, unknown>>(opened);
  const chosen = Object.fromEntries(Object.entries(filterValues).filter(([, v]) => v !== null && v !== undefined && v !== "")) as Query;
  const rowsQuery = usePageRows(pageKey, { ...query, ...chosen, search: useDebounced(search.trim()) });
  const rows = useMemo(() => rowsQuery.data?.pages.flatMap((p) => p.items) ?? [], [rowsQuery.data]);
  const total = rowsQuery.data?.pages[0]?.total ?? 0;
  const gridColumns = useColumns(columns);
  const totals = columns.filter((c) => c.total);
  const visible = useVisibleColumns(columns);
  const totalOf = (key: string) => formatMoney(columnTotal(rows, key));
  const table = () => outputTable(visible, rows, totals.map((c): [string, string] => [t(c.label), totalOf(c.key)]));

  return (
    <Card className="flex h-full min-h-0 flex-col overflow-hidden">
      <PageHeader
        className="items-center border-b border-line px-3 py-2.5"
        meta={t("Str.Results", formatCount(total))}
        actions={
          <>
            {actions}
            {canView && <OutputActions title={t(title)} table={table} />}
            {onAdd && canCreate && (
              <Button icon={<Plus className="size-4" />} onClick={onAdd}>
                {t(addLabel ?? "Str.Add")}
              </Button>
            )}
          </>
        }
      >
        <SearchBox value={search} onChange={setSearch} />
        {filters.map((f) => (
          <div key={f.key} className="w-48">
            <FieldInput compact field={{ ...resolveField(f, filterValues), required: false }} value={filterValues[f.key]} onChange={(v) => setFilterValues((s) => ({ ...s, [f.key]: v }))} />
          </div>
        ))}
        {filters.length > 0 && Object.keys(chosen).length > 0 && (
          <Button variant="ghost" size="sm" icon={<FilterX className="size-4" />} onClick={() => setFilterValues(blank())}>
            {t("Str.ClearFilter")}
          </Button>
        )}
        {toolbar}
        {gallery && <ViewSwitch value={view} onChange={setView} />}
      </PageHeader>
      <div className="min-h-0 flex-1">
        {gallery && view === "tiles" ? (
          <Gallery
            data={rows}
            tile={(row) => ({
              id: String(row.id),
              image: imageUrl(row[gallery.image]),
              title: String(row[gallery.title] ?? ""),
              hint: gallery.hint ? String(row[gallery.hint] ?? "") : undefined,
              value: gallery.value ? formatMoney(Number(row[gallery.value] ?? 0)) : undefined,
            })}
            onOpen={onOpen}
            loading={rowsQuery.isFetching}
            emptyMessage={t("Str.NoData")}
            hasMore={rowsQuery.hasNextPage}
            onReachEnd={() => void rowsQuery.fetchNextPage()}
          />
        ) : (
          <DataGrid
            columns={gridColumns}
            data={rows}
            getRowId={(row) => String(row.id ?? JSON.stringify(row))}
            onRowClick={onOpen}
            loading={rowsQuery.isFetching}
            emptyMessage={t("Str.NoData")}
            hasMore={rowsQuery.hasNextPage}
            onReachEnd={() => void rowsQuery.fetchNextPage()}
            storageKey={pageKey}
          />
        )}
      </div>
      {rows.length > 0 && <GridTotals items={totals.map((c): [string, string] => [t(c.label), totalOf(c.key)])} />}
    </Card>
  );
}
