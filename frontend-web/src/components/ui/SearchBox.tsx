import { Search } from "lucide-react";
import { motion } from "motion/react";
import { useId } from "react";
import { duration, tween } from "@/lib/motion";
import { cn } from "@/lib/cn";
import { useText } from "@/lib/i18n";

export function SearchBox({ value, onChange, className }: { value: string; onChange: (value: string) => void; className?: string }) {
  const t = useText();
  return (
    <div className={cn("relative min-w-48 max-w-sm flex-1", className)}>
      <Search className="pointer-events-none absolute start-3.5 top-1/2 size-4 -translate-y-1/2 text-ink-faint" />
      <input
        value={value}
        onChange={(e) => onChange(e.target.value)}
        placeholder={t("Str.SearchPlaceholder")}
        className="h-9 w-full rounded-lg border border-line bg-paper-soft ps-10 pe-3 text-sm outline-none transition focus:border-brand focus:bg-card focus:ring-4 focus:ring-brand/10"
      />
    </div>
  );
}

export function Segmented<V extends string | number | boolean | null>({ items, value, onChange }: { items: Array<{ value: V; label: string }>; value: V; onChange: (value: V) => void }) {
  const id = useId();
  return (
    <div className="flex rounded-xl bg-paper p-1">
      {items.map((item) => (
        <button
          key={String(item.value)}
          type="button"
          onClick={() => onChange(item.value)}
          className={cn("relative rounded-lg px-3.5 py-1.5 text-sm transition-colors", value === item.value ? "font-medium text-ink" : "text-ink-tertiary hover:text-ink")}
        >
          {value === item.value && <motion.span layoutId={`seg-${id}`} transition={tween(duration.medium2)} className="absolute inset-0 rounded-lg bg-card shadow-sm" />}
          <span className="relative">{item.label}</span>
        </button>
      ))}
    </div>
  );
}
