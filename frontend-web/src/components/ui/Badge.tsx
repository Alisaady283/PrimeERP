import type { ReactNode } from "react";
import { cn } from "@/lib/cn";

const tones = {
  brand: "bg-brand-soft text-brand",
  success: "bg-success-soft text-success",
  danger: "bg-danger-soft text-danger",
  warning: "bg-warning-soft text-warning",
  neutral: "bg-paper text-ink-tertiary",
} as const;

export function Badge({ tone = "neutral", children, className }: { tone?: keyof typeof tones; children: ReactNode; className?: string }) {
  return (
    <span className={cn("inline-flex items-center gap-1.5 rounded-full px-2.5 py-0.5 text-xs font-medium", tones[tone], className)}>
      <span className="size-1.5 rounded-full bg-current" />
      {children}
    </span>
  );
}
