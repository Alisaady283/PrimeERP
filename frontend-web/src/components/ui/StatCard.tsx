import { ArrowDownRight, ArrowUpRight } from "lucide-react";
import type { ReactNode } from "react";
import { cn } from "@/lib/cn";
import { formatCard } from "@/lib/format";
import { AnimatedNumber } from "./AnimatedNumber";
import { FitText } from "./FitText";

const tones = {
  brand: "bg-brand-soft text-brand",
  success: "bg-success-soft text-success",
  danger: "bg-danger-soft text-danger",
  warning: "bg-warning-soft text-warning",
  info: "bg-info-soft text-info",
} as const;

type StatCardProps = {
  label: string;
  value: number;
  /** نوع الرقم: مبلغ · عدد · نسبة */
  kind?: string;
  /** تأخير حركة العدّ */
  delay?: number;
  icon?: ReactNode;
  tone?: keyof typeof tones;
  change?: number | null;
  lowerIsBetter?: boolean;
  /** النقر المزدوج يفتح صفحة المؤشّر */
  onOpen?: () => void;
  /** لون البطاقة */
  accent?: string;
};

export function StatCard({ label, value, kind, delay, icon, tone = "brand", change, lowerIsBetter, onOpen, accent }: StatCardProps) {
  const good = change == null ? null : lowerIsBetter ? change <= 0 : change >= 0;
  return (
    <button
      type="button"
      onDoubleClick={onOpen}
      disabled={!onOpen}
      className="group relative flex h-full w-full items-start gap-3 overflow-hidden rounded-2xl border border-line/70 bg-card p-3 text-start sm:p-4 shadow-card transition-all duration-200 enabled:hover:-translate-y-0.5 enabled:hover:shadow-pop enabled:active:translate-y-0"
      style={accent ? { backgroundImage: `linear-gradient(135deg, color-mix(in oklab, ${accent} 10%, transparent), transparent 62%)` } : undefined}
    >
      {accent && <span className="absolute inset-y-0 start-0 w-1 transition-all duration-200 group-hover:w-1.5" style={{ background: accent }} />}
      {accent && <span className="pointer-events-none absolute -end-8 -top-8 size-24 rounded-full opacity-0 blur-2xl transition-opacity duration-300 group-hover:opacity-60" style={{ background: accent }} />}
      {icon && (
        <span
          className={cn("relative grid size-10 shrink-0 place-items-center rounded-xl transition-transform duration-200 group-hover:scale-110", !accent && tones[tone])}
          style={accent ? { background: `color-mix(in oklab, ${accent} 15%, transparent)`, color: accent } : undefined}
        >
          {icon}
        </span>
      )}
      <span className="@container relative min-w-0 flex-1">
        <span className="line-clamp-2 text-[clamp(0.6875rem,8cqi,0.8125rem)] font-medium leading-tight text-ink-tertiary" title={label}>{label}</span>
        <FitText full={formatCard(value, kind)} className="mt-1 text-[clamp(1rem,15cqi,1.75rem)] font-bold leading-tight tabular-nums text-ink">
          <AnimatedNumber value={value} format={(v) => formatCard(v, kind)} delay={delay} />
        </FitText>
        {change != null && (
          <span className={cn("mt-1 inline-flex items-center gap-0.5 rounded-full px-1.5 text-xs font-medium", good ? "bg-success-soft text-success" : "bg-danger-soft text-danger")}>
            {change >= 0 ? <ArrowUpRight className="size-3.5" /> : <ArrowDownRight className="size-3.5" />}
            {Math.abs(change).toFixed(1)}%
          </span>
        )}
      </span>
    </button>
  );
}
