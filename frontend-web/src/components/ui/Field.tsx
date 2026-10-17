import type { InputHTMLAttributes, ReactNode, Ref, SelectHTMLAttributes, TextareaHTMLAttributes } from "react";
import { cn } from "@/lib/cn";

const control =
  "w-full rounded-xl border border-line bg-card px-3.5 text-sm text-ink placeholder:text-ink-faint transition-colors " +
  "hover:border-line-strong focus:border-brand focus:outline-none focus:ring-4 focus:ring-brand/10 disabled:bg-paper-soft disabled:text-ink-tertiary";

type FrameProps = { label?: string; error?: string; required?: boolean; className?: string; children: ReactNode };

function Frame({ label, error, required, className, children }: FrameProps) {
  return (
    <label className={cn("flex flex-col gap-1.5", className)}>
      {label && (
        <span className="text-xs font-medium text-ink-soft">
          {label}
          {required && <span className="ms-0.5 text-danger">*</span>}
        </span>
      )}
      {children}
      {error && <span className="text-xs text-danger">{error}</span>}
    </label>
  );
}

type Shared = { label?: string; error?: string; frameClassName?: string; icon?: ReactNode; end?: ReactNode };

export function TextField({ label, error, required, frameClassName, className, icon, end, ref, ...props }: Shared & InputHTMLAttributes<HTMLInputElement> & { ref?: Ref<HTMLInputElement> }) {
  const input = <input ref={ref} aria-invalid={!!error} className={cn(control, "h-11", icon && "ps-10", end && "pe-10", error && "border-danger", className)} {...props} />;
  return (
    <Frame label={label} error={error} required={required} className={frameClassName}>
      {icon || end ? (
        <span className="relative block">
          {icon && <span className="pointer-events-none absolute start-3.5 top-1/2 -translate-y-1/2 text-ink-faint">{icon}</span>}
          {input}
          {end && <span className="absolute end-2 top-1/2 -translate-y-1/2">{end}</span>}
        </span>
      ) : (
        input
      )}
    </Frame>
  );
}

export function TextArea({ label, error, required, frameClassName, className, ref, ...props }: Shared & TextareaHTMLAttributes<HTMLTextAreaElement> & { ref?: Ref<HTMLTextAreaElement> }) {
  return (
    <Frame label={label} error={error} required={required} className={frameClassName}>
      <textarea ref={ref} rows={3} aria-invalid={!!error} className={cn(control, "resize-y py-2.5", error && "border-danger", className)} {...props} />
    </Frame>
  );
}

export function SelectField({ label, error, required, frameClassName, className, ref, children, ...props }: Shared & SelectHTMLAttributes<HTMLSelectElement> & { ref?: Ref<HTMLSelectElement> }) {
  return (
    <Frame label={label} error={error} required={required} className={frameClassName}>
      <select ref={ref} aria-invalid={!!error} className={cn(control, "h-11 appearance-none", error && "border-danger", className)} {...props}>
        {children}
      </select>
    </Frame>
  );
}

export function Switch({ label, checked, onChange }: { label: string; checked: boolean; onChange: (checked: boolean) => void }) {
  return (
    <button type="button" role="switch" aria-checked={checked} onClick={() => onChange(!checked)} className="flex items-center gap-3 text-sm text-ink">
      <span className={cn("relative h-6 w-11 rounded-full transition-colors", checked ? "bg-success" : "bg-line-strong")}>
        <span className={cn("absolute top-0.5 size-5 rounded-full bg-white shadow transition-all", checked ? "start-[1.375rem]" : "start-0.5")} />
      </span>
      {label}
    </button>
  );
}
