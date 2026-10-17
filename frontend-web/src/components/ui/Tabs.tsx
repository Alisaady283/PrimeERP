import { motion } from "motion/react";
import { useId, type ReactNode } from "react";
import { duration, tween } from "@/lib/motion";
import { cn } from "@/lib/cn";

export type TabItem<K extends string> = { key: K; label: string; icon?: ReactNode };

export function Tabs<K extends string>({ items, value, onChange, className }: { items: TabItem<K>[]; value: K; onChange: (key: K) => void; className?: string }) {
  const id = useId();
  return (
    <div role="tablist" className={cn("flex gap-1 overflow-x-auto overflow-y-hidden border-b border-line [scrollbar-width:none]", className)}>
      {items.map((item) => (
        <button
          key={item.key}
          role="tab"
          type="button"
          aria-selected={item.key === value}
          onClick={() => onChange(item.key)}
          className={cn("relative flex shrink-0 items-center gap-2 px-4 py-3 text-sm transition-colors", item.key === value ? "font-semibold text-brand" : "text-ink-tertiary hover:text-ink")}
        >
          {item.icon}
          {item.label}
          {item.key === value && <motion.span layoutId={`tab-${id}`} transition={tween(duration.medium2)} className="absolute inset-x-2 bottom-0 h-0.5 rounded-full bg-brand" />}
        </button>
      ))}
    </div>
  );
}
