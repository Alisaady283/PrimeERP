import { ChevronLeft, ChevronRight, ChevronsDownUp, ChevronsUpDown, RefreshCw } from "lucide-react";
import type { ReactNode } from "react";
import { Card } from "@/components/ui/Card";
import { cn } from "@/lib/cn";
import { useLanguage, useText } from "@/lib/i18n";
import type { TreeCommand } from "@/components/ui/Tree";

/** شجرةٌ ضيّقة وتفاصيل عريضة */
export function TreeSplit({ toolbar, filters, tree, children }: { toolbar: ReactNode; filters?: ReactNode; tree: ReactNode; children: ReactNode }) {
  return (
    <Card className="grid h-full min-h-0 overflow-hidden max-lg:grid-rows-[minmax(0,42%)_minmax(0,1fr)] lg:grid-cols-[19rem_minmax(0,1fr)]">
      <aside className="flex min-h-0 flex-col border-line bg-paper-soft/60 max-lg:border-b lg:border-e">
        <div className="flex items-center gap-1 border-b border-line ps-3 pe-1.5 py-2.5">{toolbar}</div>
        {filters}
        <div className="scroll-thin min-h-0 flex-1 overflow-y-auto p-2">{tree}</div>
      </aside>
      <section className="min-h-0 overflow-y-auto">{children}</section>
    </Card>
  );
}

/** تحديث · توسيع الكل · طيّ الكل */
export function TreeTools({ onRefresh, refreshing, onCommand }: { onRefresh: () => void; refreshing?: boolean; onCommand: (command: TreeCommand) => void }) {
  const t = useText();
  const tools = [
    { key: "Str.Refresh", Icon: RefreshCw, run: onRefresh, spin: refreshing },
    { key: "Str.ExpandAll", Icon: ChevronsUpDown, run: () => onCommand({ expand: true, at: Date.now() }) },
    { key: "Str.CollapseAll", Icon: ChevronsDownUp, run: () => onCommand({ expand: false, at: Date.now() }) },
  ];
  return (
    <div className="flex shrink-0 items-center">
      {tools.map(({ key, Icon, run, spin }) => (
        <button key={key} type="button" title={t(key)} aria-label={t(key)} onClick={run} className="grid size-8 place-items-center rounded-lg text-ink-tertiary transition-colors hover:bg-paper hover:text-ink">
          <Icon className={cn("size-4", spin && "animate-spin")} />
        </button>
      ))}
    </div>
  );
}

/** شرائح التصفية */
export function TreeChips<V extends string | number | null>({ items, value, onChange }: { items: Array<{ value: V; label: string }>; value: V; onChange: (value: V) => void }) {
  return (
    <div className="scroll-thin flex gap-1.5 overflow-x-auto border-b border-line px-3 py-2">
      {items.map((item) => (
        <button
          key={String(item.value)}
          type="button"
          onClick={() => onChange(item.value)}
          className={cn("h-7 shrink-0 rounded-full px-3 text-xs transition-colors", value === item.value ? "bg-brand font-medium text-on-field" : "bg-paper text-ink-soft hover:bg-brand-soft hover:text-ink")}
        >
          {item.label}
        </button>
      ))}
    </div>
  );
}

/** طريق العنصر */
export function TreePath({ items, onPick }: { items: Array<{ id: string; label: string }>; onPick: (id: string | null) => void }) {
  const Sep = useLanguage((s) => s.language) === "ar" ? ChevronLeft : ChevronRight;
  return (
    <nav className="flex flex-wrap items-center gap-1 text-xs text-ink-tertiary">
      {items.map((item, i) => (
        <span key={item.id} className="flex items-center gap-1">
          {i > 0 && <Sep className="size-3 text-ink-faint" />}
          <button type="button" onClick={() => onPick(i === 0 ? null : item.id)} className={cn("rounded px-1 py-0.5 hover:bg-paper hover:text-ink", i === items.length - 1 && "font-medium text-ink")}>
            {item.label}
          </button>
        </span>
      ))}
    </nav>
  );
}

/** خانة ملخّص */
export function DetailTile({ label, value, tone }: { label: string; value: ReactNode; tone?: "brand" }) {
  return (
    <div className={cn("rounded-xl border border-line px-4 py-3", tone === "brand" ? "bg-brand-soft" : "bg-paper-soft")}>
      <p className="text-xs text-ink-tertiary">{label}</p>
      <p className={cn("mt-1 text-lg font-semibold tabular-nums", tone === "brand" ? "text-brand" : "text-ink")}>{value}</p>
    </div>
  );
}

/** قائمة الفروع */
export function ChildRows<T>({ title, rows, columns, onOpen }: { title: string; rows: T[]; columns: Array<{ label: string; cell: (row: T) => ReactNode; end?: boolean }>; onOpen: (row: T) => void }) {
  return (
    <div className="overflow-hidden rounded-xl border border-line">
      <p className="border-b border-line bg-paper-soft px-4 py-2 text-xs font-medium text-ink-soft">{title} · {rows.length}</p>
      <table className="w-full text-sm">
        <thead>
          <tr className="text-xs text-ink-tertiary">
            {columns.map((c) => <th key={c.label} className={cn("px-4 py-2 font-medium", c.end ? "text-end" : "text-start")}>{c.label}</th>)}
          </tr>
        </thead>
        <tbody>
          {rows.map((row, i) => (
            <tr key={i} onClick={() => onOpen(row)} className="cursor-pointer border-t border-line/70 transition-colors hover:bg-paper">
              {columns.map((c) => <td key={c.label} className={cn("px-4 py-2", c.end && "text-end tabular-nums")}>{c.cell(row)}</td>)}
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
