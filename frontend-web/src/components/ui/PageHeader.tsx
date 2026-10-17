import type { ReactNode } from "react";
import { cn } from "@/lib/cn";

/** شريط الصفحة: صفٌّ واحد */
export function PageHeader({ children, meta, actions, className }: { children?: ReactNode; meta?: ReactNode; actions?: ReactNode; className?: string }) {
  return (
    <header className={cn("flex flex-wrap items-end gap-2", className)}>
      {children}
      <div className="ms-auto flex flex-wrap items-center gap-2">
        {meta !== undefined && <span className="px-1 text-xs tabular-nums text-ink-tertiary">{meta}</span>}
        {actions}
      </div>
    </header>
  );
}
