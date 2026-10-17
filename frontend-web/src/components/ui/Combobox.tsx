import { Check, ChevronDown, Search, X } from "lucide-react";
import { useEffect, useMemo, useRef, useState } from "react";
import { cn } from "@/lib/cn";
import { useText } from "@/lib/i18n";

export type Option = { value: string | number; label: string; hint?: string };

type ComboboxProps = {
  label?: string;
  value: string | number | null;
  options: Option[];
  onChange: (value: string | number | null) => void;
  placeholder?: string;
  error?: string;
  required?: boolean;
  disabled?: boolean;
  clearable?: boolean;
  dense?: boolean;
  className?: string;
};

const MAX_VISIBLE = 80;

export function Combobox({ label, value, options, onChange, placeholder, error, required, disabled, clearable = true, dense, className }: ComboboxProps) {
  const t = useText();
  const [open, setOpen] = useState(false);
  const [term, setTerm] = useState("");
  const [cursor, setCursor] = useState(0);
  const root = useRef<HTMLDivElement>(null);
  const input = useRef<HTMLInputElement>(null);

  const selected = options.find((o) => o.value === value) ?? null;
  const filtered = useMemo(() => {
    const q = term.trim().toLowerCase();
    const list = q ? options.filter((o) => o.label.toLowerCase().includes(q) || o.hint?.toLowerCase().includes(q)) : options;
    return list.slice(0, MAX_VISIBLE);
  }, [options, term]);

  useEffect(() => {
    if (!open) return;
    const close = (event: MouseEvent) => {
      if (!root.current?.contains(event.target as Node)) setOpen(false);
    };
    document.addEventListener("mousedown", close);
    input.current?.focus();
    return () => document.removeEventListener("mousedown", close);
  }, [open]);

  useEffect(() => {
    setCursor(0);
  }, [term]);

  const pick = (option: Option) => {
    onChange(option.value);
    setOpen(false);
    setTerm("");
  };

  return (
    <div ref={root} className={cn("relative flex flex-col gap-1.5", className)}>
      {label && (
        <span className="text-xs font-medium text-ink-soft">
          {label}
          {required && <span className="ms-0.5 text-danger">*</span>}
        </span>
      )}
      <button
        type="button"
        disabled={disabled}
        onClick={() => setOpen((o) => !o)}
        className={cn(
          "flex w-full items-center gap-2 border bg-card px-3.5 text-start text-sm transition-colors",
          dense ? "h-9 rounded-lg" : "h-11 rounded-xl",
          "hover:border-line-strong focus:border-brand focus:outline-none focus:ring-4 focus:ring-brand/10 disabled:bg-paper-soft disabled:text-ink-tertiary",
          error ? "border-danger" : open ? "border-brand ring-4 ring-brand/10" : "border-line",
        )}
      >
        <span className={cn("flex-1 truncate", !selected && "text-ink-faint")}>{selected?.label ?? placeholder ?? "—"}</span>
        {clearable && selected && !disabled && (
          <span
            role="button"
            tabIndex={-1}
            onClick={(event) => {
              event.stopPropagation();
              onChange(null);
            }}
            className="rounded-md p-0.5 text-ink-faint hover:bg-paper hover:text-ink"
          >
            <X className="size-3.5" />
          </span>
        )}
        <ChevronDown className={cn("size-4 text-ink-faint transition-transform", open && "rotate-180")} />
      </button>

      {open && (
        <div className="absolute inset-x-0 top-full z-50 mt-1.5 animate-rise overflow-hidden rounded-xl border border-line bg-card shadow-pop">
          <div className="flex items-center gap-2 border-b border-line px-3">
            <Search className="size-4 text-ink-faint" />
            <input
              ref={input}
              value={term}
              onChange={(e) => setTerm(e.target.value)}
              onKeyDown={(e) => {
                if (e.key === "ArrowDown") { e.preventDefault(); setCursor((c) => Math.min(c + 1, filtered.length - 1)); }
                else if (e.key === "ArrowUp") { e.preventDefault(); setCursor((c) => Math.max(c - 1, 0)); }
                else if (e.key === "Enter") { e.preventDefault(); const o = filtered[cursor]; if (o) pick(o); }
                else if (e.key === "Escape") setOpen(false);
              }}
              placeholder={t("Str.SearchPlaceholder")}
              className="h-10 flex-1 bg-transparent text-sm outline-none"
            />
          </div>
          <ul role="listbox" className="max-h-64 overflow-y-auto p-1">
            {filtered.length === 0 && <li className="px-3 py-6 text-center text-sm text-ink-faint">{t("Str.NoData")}</li>}
            {filtered.map((o, i) => (
              <li
                key={o.value}
                role="option"
                aria-selected={o.value === value}
                onMouseEnter={() => setCursor(i)}
                onMouseDown={(e) => { e.preventDefault(); pick(o); }}
                className={cn("flex cursor-pointer items-center gap-2 rounded-lg px-3 py-2 text-sm", i === cursor ? "bg-brand-soft" : "", o.value === value && "font-medium text-brand")}
              >
                <span className="flex-1 truncate">{o.label}</span>
                {o.hint && <span className="font-mono text-xs text-ink-faint">{o.hint}</span>}
                {o.value === value && <Check className="size-4" />}
              </li>
            ))}
          </ul>
        </div>
      )}
      {error && <span className="text-xs text-danger">{error}</span>}
    </div>
  );
}
