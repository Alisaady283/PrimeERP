import { ChevronDown } from "lucide-react";
import { useEffect, useRef, useState, type ReactNode } from "react";
import { cn } from "@/lib/cn";

export type MenuItem = { label: string; icon?: ReactNode; danger?: boolean; onSelect: () => void };

/** زرٌّ تنسدل منه أفعاله */
/** tone="field": على خلفية السايدبار */
export function Menu({ label, items, icon, size = "md", disabled, className, tone = "card" }: { label: ReactNode; items: MenuItem[]; icon?: ReactNode; size?: "sm" | "md"; disabled?: boolean; className?: string; tone?: "card" | "field" }) {
  const [open, setOpen] = useState(false);
  const root = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!open) return;
    const close = (event: MouseEvent) => {
      if (!root.current?.contains(event.target as Node)) setOpen(false);
    };
    document.addEventListener("mousedown", close);
    return () => document.removeEventListener("mousedown", close);
  }, [open]);

  return (
    <div ref={root} className={cn("relative", className)}>
      <button
        type="button"
        onClick={() => setOpen(!open)}
        disabled={disabled}
        className={cn(
          "flex items-center gap-2 rounded-lg text-sm font-medium transition-colors disabled:pointer-events-none disabled:opacity-50",
          tone === "field" ? "text-on-field hover:bg-field-hover" : "border border-line bg-card text-ink hover:border-line-strong hover:bg-paper-soft",
          size === "md" ? "h-9 px-3.5" : "h-8 px-2.5",
        )}
      >
        {icon}
        <span className="max-w-40 truncate">{label}</span>
        <ChevronDown className={cn("size-4 transition-transform", tone === "field" ? "text-on-field-faint" : "text-ink-tertiary", open && "rotate-180")} />
      </button>
      {open && (
        <div className="absolute end-0 top-full z-50 mt-1.5 min-w-48 animate-rise overflow-hidden rounded-xl border border-line bg-card py-1 shadow-pop">
          {items.map((item) => (
            <button
              key={item.label}
              type="button"
              onClick={() => {
                setOpen(false);
                item.onSelect();
              }}
              className={cn(
                "flex w-full items-center gap-2 px-3 py-2 text-start text-sm transition-colors",
                item.danger ? "text-danger hover:bg-danger-soft" : "text-ink-soft hover:bg-paper hover:text-ink",
              )}
            >
              {item.icon}
              {item.label}
            </button>
          ))}
        </div>
      )}
    </div>
  );
}
