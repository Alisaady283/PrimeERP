import { Plus, Trash2 } from "lucide-react";
import type { ReactNode } from "react";
import { Combobox, type Option } from "@/components/ui/Combobox";
import { cn } from "@/lib/cn";
import { useText } from "@/lib/i18n";

export type LineColumn<L> = {
  key: string;
  label: string;
  width: string;
  editor?: "text" | "number" | "date" | "select";
  options?: Option[];
  readOnly?: boolean;
  render?: (line: L) => ReactNode;
  onEdit?: (line: L, value: unknown) => L;
};

type LinesGridProps<L> = {
  lines: L[];
  columns: LineColumn<L>[];
  onChange: (lines: L[]) => void;
  newLine: () => L;
  readOnly?: boolean;
};

const cell = "h-10 w-full rounded-lg border border-transparent bg-transparent px-2 text-sm outline-none transition hover:border-line focus:border-brand focus:bg-card focus:ring-2 focus:ring-brand/10";

export function LinesGrid<L extends Record<string, unknown>>({ lines, columns, onChange, newLine, readOnly }: LinesGridProps<L>) {
  const t = useText();
  const template = `2.5rem ${columns.map((c) => c.width).join(" ")}${readOnly ? "" : " 2.75rem"}`;

  const edit = (index: number, column: LineColumn<L>, value: unknown) => {
    const current = lines[index]!;
    const updated = column.onEdit ? column.onEdit(current, value) : ({ ...current, [column.key]: value } as L);
    onChange(lines.map((line, i) => (i === index ? updated : line)));
  };

  return (
    <div className="overflow-x-auto rounded-2xl border border-line">
      <div style={{ minWidth: "max-content" }}>
        <div className="grid bg-paper text-xs font-semibold text-ink-soft" style={{ gridTemplateColumns: template }}>
          <span className="px-2 py-3 text-center">#</span>
          {columns.map((c) => <span key={c.key} className="px-3 py-3">{c.label}</span>)}
          {!readOnly && <span />}
        </div>

        {lines.map((line, index) => (
          <div key={index} className="grid items-center border-t border-line/70 odd:bg-card even:bg-paper-soft" style={{ gridTemplateColumns: template }}>
            <span className="text-center text-xs text-ink-faint">{index + 1}</span>
            {columns.map((c) => {
              const value = line[c.key];
              if (c.render || readOnly || c.readOnly || !c.editor)
                return <div key={c.key} className="truncate px-3 text-sm">{c.render ? c.render(line) : String(value ?? "")}</div>;
              if (c.editor === "select")
                return (
                  <div key={c.key} className="px-1 py-1">
                    <Combobox value={(value as string | number | null) ?? null} options={c.options ?? []} onChange={(v) => edit(index, c, v)} clearable={false} />
                  </div>
                );
              return (
                <div key={c.key} className="px-1">
                  <input
                    type={c.editor}
                    step={c.editor === "number" ? "any" : undefined}
                    value={value == null ? "" : String(value)}
                    onChange={(e) => edit(index, c, c.editor === "number" ? (e.target.value === "" ? 0 : Number(e.target.value)) : e.target.value)}
                    className={cn(cell, c.editor === "number" && "text-center tabular-nums")}
                  />
                </div>
              );
            })}
            {!readOnly && (
              <button type="button" onClick={() => onChange(lines.filter((_, i) => i !== index))} className="mx-auto grid size-8 place-items-center rounded-lg text-ink-faint transition hover:bg-danger-soft hover:text-danger">
                <Trash2 className="size-4" />
              </button>
            )}
          </div>
        ))}
      </div>

      {!readOnly && (
        <button type="button" onClick={() => onChange([...lines, newLine()])} className="flex w-full items-center justify-center gap-2 border-t border-dashed border-line py-3 text-sm font-medium text-brand transition hover:bg-brand-soft">
          <Plus className="size-4" />
          {t("Str.Add")}
        </button>
      )}
    </div>
  );
}
