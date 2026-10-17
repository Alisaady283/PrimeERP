import { useEffect, useRef, useState, type PointerEvent as ReactPointerEvent } from "react";
import { flexRender, getCoreRowModel, useReactTable, type ColumnDef, type VisibilityState } from "@tanstack/react-table";
import { useVirtualizer } from "@tanstack/react-virtual";
import { Columns3, LoaderCircle } from "lucide-react";
import { EmptyState } from "@/components/ui/EmptyState";
import { cn } from "@/lib/cn";
import { useText } from "@/lib/i18n";

export type GridAlign = "start" | "center" | "end";

declare module "@tanstack/react-table" {
  interface ColumnMeta<TData, TValue> {
    align?: GridAlign;
    width?: string;
  }
}

type DataGridProps<T> = {
  columns: ColumnDef<T, any>[];
  data: T[];
  getRowId: (row: T) => string;
  onRowClick?: (row: T) => void;
  loading?: boolean;
  emptyMessage: string;
  hasMore?: boolean;
  onReachEnd?: () => void;
  /** حفظ الأعمدة */
  storageKey?: string;
};

type Layout = { sizes: Record<string, number>; hidden: VisibilityState };

const ROW_HEIGHT = 40;
const alignClass: Record<GridAlign, string> = { start: "text-start", center: "text-center", end: "text-end" };

const load = (key?: string): Layout => {
  try {
    return { sizes: {}, hidden: {}, ...(key ? JSON.parse(localStorage.getItem(`grid.${key}`) ?? "{}") : {}) };
  } catch {
    return { sizes: {}, hidden: {} };
  }
};

const save = (key: string | undefined, layout: Layout) => {
  try {
    if (key) localStorage.setItem(`grid.${key}`, JSON.stringify(layout));
  } catch {
    /* تخزينٌ محجوب */
  }
};

export function DataGrid<T>({ columns, data, getRowId, onRowClick, loading, emptyMessage, hasMore, onReachEnd, storageKey }: DataGridProps<T>) {
  const t = useText();
  const [layout, setLayout] = useState<Layout>(() => load(storageKey));
  const [picking, setPicking] = useState(false);
  const table = useReactTable({ data, columns, getRowId, getCoreRowModel: getCoreRowModel(), state: { columnVisibility: layout.hidden } });
  const rows = table.getRowModel().rows;
  const scrollRef = useRef<HTMLDivElement>(null);
  const headRef = useRef<HTMLDivElement>(null);
  const pickerRef = useRef<HTMLDivElement>(null);

  const update = (next: Layout) => {
    setLayout(next);
    save(storageKey, next);
  };

  const virtualizer = useVirtualizer({
    count: rows.length,
    getScrollElement: () => scrollRef.current,
    estimateSize: () => ROW_HEIGHT,
    overscan: 12,
  });

  const items = virtualizer.getVirtualItems();
  const last = items[items.length - 1];

  useEffect(() => {
    if (last && hasMore && !loading && last.index >= rows.length - 8) onReachEnd?.();
  }, [last, hasMore, loading, rows.length, onReachEnd]);

  useEffect(() => {
    if (!picking) return;
    const close = (event: MouseEvent) => !pickerRef.current?.contains(event.target as Node) && setPicking(false);
    document.addEventListener("mousedown", close);
    return () => document.removeEventListener("mousedown", close);
  }, [picking]);

  const visible = table.getVisibleLeafColumns();
  const template = visible.map((c) => (layout.sizes[c.id] ? `${layout.sizes[c.id]}px` : (c.columnDef.meta?.width ?? "minmax(8rem,1fr)"))).join(" ");

  /** سحب حدّ العمود */
  const resize = (id: string) => (event: ReactPointerEvent<HTMLSpanElement>) => {
    event.preventDefault();
    event.stopPropagation();
    const cell = event.currentTarget.parentElement!;
    const start = cell.getBoundingClientRect().width;
    const origin = event.clientX;
    const sign = getComputedStyle(cell).direction === "rtl" ? -1 : 1;
    let width = start;
    const move = (e: PointerEvent) => {
      width = Math.max(64, Math.round(start + (e.clientX - origin) * sign));
      setLayout((l) => ({ ...l, sizes: { ...l.sizes, [id]: width } }));
    };
    const up = () => {
      window.removeEventListener("pointermove", move);
      window.removeEventListener("pointerup", up);
      setLayout((l) => {
        save(storageKey, l);
        return l;
      });
    };
    window.addEventListener("pointermove", move);
    window.addEventListener("pointerup", up);
  };

  return (
    <div className="relative flex h-full flex-col">
      <div ref={headRef} className="overflow-hidden border-b border-line bg-paper-soft [scrollbar-gutter:stable]">
        <div role="row" className="grid min-w-full pe-9" style={{ gridTemplateColumns: template, width: "max-content" }}>
          {table.getFlatHeaders().map((header) => (
            <div
              key={header.id}
              role="columnheader"
              className={cn("group relative truncate px-3 py-2.5 text-[13px] font-semibold text-ink-soft", alignClass[header.column.columnDef.meta?.align ?? "start"])}
            >
              {flexRender(header.column.columnDef.header, header.getContext())}
              <span
                onPointerDown={resize(header.column.id)}
                onDoubleClick={() => {
                  const { [header.column.id]: _, ...sizes } = layout.sizes;
                  update({ ...layout, sizes });
                }}
                className="absolute inset-y-1.5 end-0 w-1.5 cursor-col-resize rounded-full transition-colors group-hover:bg-line-strong hover:!bg-brand"
              />
            </div>
          ))}
        </div>
      </div>

      <div ref={pickerRef} className="absolute end-1 top-1 z-20">
        <button
          type="button"
          title={t("Str.Grid.Columns")}
          aria-label={t("Str.Grid.Columns")}
          onClick={() => setPicking((p) => !p)}
          className={cn("grid size-7 place-items-center rounded-md text-ink-tertiary transition-colors hover:bg-card hover:text-ink", picking && "bg-card text-brand")}
        >
          <Columns3 className="size-4" />
        </button>
        {picking && (
          <div className="absolute end-0 top-full mt-1 max-h-80 w-56 animate-rise overflow-y-auto rounded-xl border border-line bg-card p-1 shadow-pop">
            {table.getAllLeafColumns().map((column) => (
              <label key={column.id} className="flex cursor-pointer items-center gap-2 rounded-lg px-2.5 py-1.5 text-sm text-ink-soft hover:bg-paper">
                <input
                  type="checkbox"
                  className="size-4 accent-brand"
                  checked={column.getIsVisible()}
                  disabled={column.getIsVisible() && visible.length === 1}
                  onChange={(e) => update({ ...layout, hidden: { ...layout.hidden, [column.id]: e.target.checked } })}
                />
                {String(column.columnDef.header ?? column.id)}
              </label>
            ))}
            {(Object.keys(layout.sizes).length > 0 || Object.values(layout.hidden).some((v) => !v)) && (
              <button type="button" onClick={() => update({ sizes: {}, hidden: {} })} className="mt-1 w-full rounded-lg border-t border-line px-2.5 py-1.5 text-start text-xs text-brand hover:bg-paper">
                {t("Str.Grid.Reset")}
              </button>
            )}
          </div>
        )}
      </div>

      <div
        ref={scrollRef}
        onScroll={(e) => headRef.current && (headRef.current.scrollLeft = e.currentTarget.scrollLeft)}
        className="scroll-thin relative min-h-0 flex-1 overflow-auto overscroll-contain [scrollbar-gutter:stable]"
      >
        <div className="min-w-full pe-9" style={{ width: "max-content" }}>
          {rows.length === 0 && !loading ? (
            <EmptyState message={emptyMessage} />
          ) : (
            <div className="relative" style={{ height: virtualizer.getTotalSize() }}>
              {items.map((item) => {
                const row = rows[item.index];
                if (!row) return null;
                return (
                  <div
                    key={row.id}
                    role="row"
                    onClick={onRowClick ? () => onRowClick(row.original) : undefined}
                    className={cn("absolute inset-x-0 grid items-center border-b border-line/70 text-sm text-ink transition-colors", onRowClick ? "cursor-pointer hover:bg-brand-soft" : "hover:bg-paper-soft")}
                    style={{ gridTemplateColumns: template, height: item.size, transform: `translateY(${item.start}px)` }}
                  >
                    {row.getVisibleCells().map((cell) => (
                      <div key={cell.id} role="cell" className={cn("truncate px-3", alignClass[cell.column.columnDef.meta?.align ?? "start"])}>
                        {flexRender(cell.column.columnDef.cell, cell.getContext())}
                      </div>
                    ))}
                  </div>
                );
              })}
            </div>
          )}

          {loading && (
            <div className="flex justify-center py-5 text-ink-faint">
              <LoaderCircle className="size-5 animate-spin" />
            </div>
          )}
        </div>
      </div>
    </div>
  );
}

/** صفّ المجاميع */
export function GridTotals({ items }: { items: Array<[string, string]> }) {
  if (items.length === 0) return null;
  return (
    <div className="flex flex-wrap items-center justify-end gap-2 border-t border-line bg-paper-soft px-3 py-2">
      {items.map(([label, value]) => (
        <span key={label} className="flex items-baseline gap-2 rounded-lg border border-line bg-card px-3 py-1">
          <span className="text-xs text-ink-tertiary">{label}</span>
          <b className="text-sm font-semibold tabular-nums text-ink">{value}</b>
        </span>
      ))}
    </div>
  );
}
